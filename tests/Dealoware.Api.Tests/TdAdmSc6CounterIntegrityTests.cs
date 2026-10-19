using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>SC-6: durable atomic counters; lock survives a new instance.</summary>
public sealed class TdAdmSc6CounterIntegrityTests : IDisposable
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = "io@aiknowhow.com";

    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        "dealoware-sc6-" + Guid.NewGuid().ToString("N") + ".db");
    private readonly string _hasherKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _password = "Pw-" + Guid.NewGuid().ToString("N") + "-xx";

    [Fact]
    public async Task TdAdmSc6_ParallelFailures_DoNotPassTheLimit()
    {
        using var factory = CreateFactory();
        SeedOwner(factory);
        var client = CreateClient(factory);

        var tasks = Enumerable.Range(0, 12)
            .Select(_ => SendWrongPasswordAsync(client))
            .ToArray();
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.Equal(5, await CountFailuresAsync(factory, AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        Assert.NotNull(await ActiveLockAsync(factory, AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public async Task TdAdmSc6_LockSurvivesNewServiceInstance()
    {
        using (var first = CreateFactory())
        {
            SeedOwner(first);
            var client = CreateClient(first);
            for (var i = 0; i < 5; i++)
            {
                using var fail = await SendWrongPasswordAsync(client);
                Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
            }

            Assert.NotNull(await ActiveLockAsync(first, AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        }

        using var second = CreateFactory();
        SeedOwner(second);
        var clientB = CreateClient(second);
        using var correct = await SendAuthAsync(
            clientB,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        Assert.NotNull(await ActiveLockAsync(second, AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        Assert.Equal(5, await CountFailuresAsync(second, AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
            File.Delete(_dbPath + "-wal");
            File.Delete(_dbPath + "-shm");
        }
        catch (IOException)
        {
        }
    }

    private SharedFileSqliteFactory CreateFactory() => new(_dbPath, _hasherKey);

    private void SeedOwner(SharedFileSqliteFactory factory)
    {
        var directory = factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        directory.SetPasswordAsync(OwnerEmail, _password, CancellationToken.None).GetAwaiter().GetResult();
        directory.SeedTotpCode("246813");
    }

    private static HttpClient CreateClient(SharedFileSqliteFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task<int> CountFailuresAsync(
        SharedFileSqliteFactory factory,
        string scope,
        string key)
    {
        using var scopeSvc = factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .CountFailuresAsync(scope, key, CancellationToken.None);
    }

    private static async Task<AdminAuthLockout?> ActiveLockAsync(
        SharedFileSqliteFactory factory,
        string scope,
        string key)
    {
        using var scopeSvc = factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .GetActiveLockoutAsync(scope, key, CancellationToken.None);
    }

    private static Task<HttpResponseMessage> SendWrongPasswordAsync(HttpClient client) =>
        SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });

    private static async Task<HttpResponseMessage> SendAuthAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        if (HttpMethod.Post.Equals(method))
        {
            var afRequest = new HttpRequestMessage(HttpMethod.Get, "/admin/sign-in");
            afRequest.Headers.Host = AdminHost;
            afRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
            using var afResponse = await client.SendAsync(afRequest);
            afResponse.EnsureSuccessStatusCode();
            var token = afResponse.Headers.TryGetValues(AdminAntiForgery.HeaderName, out var values)
                ? values.First()
                : string.Empty;
            var cookie = ReadCookie(afResponse, AdminAntiForgery.CookieName) ?? string.Empty;
            request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, token);
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminAntiForgery.CookieName}={cookie}");
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static string? ReadCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                var value = cookie[(name.Length + 1)..];
                var end = value.IndexOf(';');
                return end < 0 ? value : value[..end];
            }
        }

        return null;
    }
}

internal sealed class SharedFileSqliteFactory : WebApplicationFactory<Program>
{
    private readonly string _path;
    private readonly string _hasherKey;

    public SharedFileSqliteFactory(string path, string hasherKey)
    {
        _path = path;
        _hasherKey = hasherKey;
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=8000;";
        cmd.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(IpHasher.KeyEnvironmentVariable, _hasherKey);
        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(DbContextOptions<DealowareDbContext>)
                                     || d.ServiceType == typeof(DbContextOptions)
                                     || d.ServiceType == typeof(IDbContextOptionsConfiguration<DealowareDbContext>)
                                     || d.ServiceType == typeof(DealowareDbContext))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DealowareDbContext>(options => options.UseSqlite($"Data Source={_path}"));

            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(ITurnstileVerifier)
                                     || d.ServiceType == typeof(IAdminClock)
                                     || d.ServiceType == typeof(FakeTurnstileVerifier)
                                     || d.ServiceType == typeof(TestAdminClock))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<FakeTurnstileVerifier>();
            services.AddSingleton<ITurnstileVerifier>(sp => sp.GetRequiredService<FakeTurnstileVerifier>());
            services.AddSingleton<TestAdminClock>();
            services.AddSingleton<IAdminClock>(sp => sp.GetRequiredService<TestAdminClock>());
        });
    }
}

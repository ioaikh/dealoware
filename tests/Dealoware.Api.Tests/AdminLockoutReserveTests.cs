using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// R1 parallel reserve/consume tests. Names match the postgres-sec filter
/// <c>FullyQualifiedName~SecR|FullyQualifiedName~SecSc</c>.
/// </summary>
[Collection("WebAppTests")]
public class AdminLockoutReserveTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;
    private readonly string _password;

    public AdminLockoutReserveTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
        _password = "Pw-" + Guid.NewGuid().ToString("N") + "-xx";
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        directory.SetPasswordAsync(OwnerEmail, _password, CancellationToken.None).GetAwaiter().GetResult();
        directory.SeedTotpCode("246813");
    }

    [Fact]
    public async Task SecR1_ParallelPasswordAttempts_CannotExceedFive()
    {
        await ClearAsync();
        var client = CreateClient();
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => SendWrongAsync(client)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.Equal(5, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
    }

    [Fact]
    public async Task SecR1_CorrectPasswordAfterFifthReserve_ReturnsLocked()
    {
        await ClearAsync();
        var client = CreateClient();
        for (var i = 0; i < 5; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var correct = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await correct.Content.ReadAsStringAsync());
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
    }

    [Fact]
    public async Task SecR1_NormalizedAccountKey_IsSharedAcrossCasing()
    {
        await ClearAsync();
        var client = CreateClient();
        using var upper = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail.ToUpperInvariant(), password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, upper.StatusCode);
        Assert.Equal(1, await CountAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public async Task SecR1_ParallelPendingConsume_IsSingleUse()
    {
        await ClearAsync();
        var client = CreateClient();
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        Assert.False(string.IsNullOrEmpty(pending));

        var now = DateTimeOffset.UtcNow;
        var hash = AdminAuthService.HashToken(pending!);
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => ConsumeOnNewScopeAsync(hash, now)));
        Assert.Equal(1, results.Count(ok => ok));
    }

    [Fact]
    public async Task SecR1_ParallelLinkConsume_IsSingleUse()
    {
        await ClearAsync();
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
        var raw = AdminAuthService.CreateOpaqueToken();
        var now = DateTimeOffset.UtcNow;
        await tokens.AddAsync(
            AdminAuthToken.Create(
                AdminAuthToken.KindReset,
                AdminAuthService.HashToken(raw),
                OwnerEmail.ToLowerInvariant(),
                now,
                TimeSpan.FromMinutes(30)),
            CancellationToken.None);
        await tokens.SaveChangesAsync(CancellationToken.None);

        var hash = AdminAuthService.HashToken(raw);
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => ConsumeOnNewScopeAsync(hash, now)));
        Assert.Equal(1, results.Count(ok => ok));
    }

    [Fact]
    public async Task SecSc6_ParallelFailures_DoNotPassTheLimit()
    {
        await SecR1_ParallelPasswordAttempts_CannotExceedFive();
    }

    [Fact]
    public async Task SecSc9_ResetMail_LeavesRequestBeforeSend()
    {
        await ClearAsync();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services
                             .Where(d => d.ImplementationType == typeof(AdminMailQueueHostedService))
                             .ToList())
                {
                    services.Remove(descriptor);
                }
            });
        });

        var mailer = factory.Services.GetRequiredService<RecordingMailSender>();
        var before = mailer.SendCount;
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var owner = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(before, mailer.SendCount);

        using var unknown = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = "nobody@example.com", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal(before, mailer.SendCount);
        Assert.Equal(await owner.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());

        var queue = factory.Services.GetRequiredService<AdminMailQueue>();
        Assert.True(queue.Reader.TryRead(out var ownerMail));
        Assert.NotNull(ownerMail);
        Assert.True(queue.Reader.TryRead(out var unknownMail));
        Assert.Null(unknownMail);
    }

    private async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        await db.AdminAuthFailureEvents.ExecuteDeleteAsync();
        await db.AdminAuthLockouts.ExecuteDeleteAsync();
        await db.AdminAuthTokens.ExecuteDeleteAsync();
        await db.AdminSessions.ExecuteDeleteAsync();
        await db.AdminPendingAuths.ExecuteDeleteAsync();
        await db.AdminRecoveryCodes.ExecuteDeleteAsync();
        await db.AdminCoreOwnerAccounts.ExecuteDeleteAsync();
        _factory.Services.GetRequiredService<TestAdminClock>().UtcNow = DateTimeOffset.UtcNow;
    }

    private async Task<bool> ConsumeOnNewScopeAsync(string hash, DateTimeOffset now)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>()
            .TryConsumeAsync(hash, now, CancellationToken.None);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private Task<HttpResponseMessage> SendWrongAsync(HttpClient client) =>
        SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });

    private static async Task<HttpResponseMessage> SendAuthAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body)
    {
        var af = new HttpRequestMessage(HttpMethod.Get, "/admin/sign-in");
        af.Headers.Host = AdminHost;
        using var issued = await client.SendAsync(af);
        issued.EnsureSuccessStatusCode();
        var token = issued.Headers.TryGetValues(AdminAntiForgery.HeaderName, out var values)
            ? values.First()
            : string.Empty;
        var cookie = issued.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.First(c => c.StartsWith(AdminAntiForgery.CookieName + "=", StringComparison.Ordinal))
            : string.Empty;
        var cookieValue = cookie[(AdminAntiForgery.CookieName.Length + 1)..];
        var end = cookieValue.IndexOf(';');
        if (end >= 0)
        {
            cookieValue = cookieValue[..end];
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, token);
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminAntiForgery.CookieName}={cookieValue}");
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<int> CountAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .CountFailuresAsync(scope, key, CancellationToken.None);
    }

    private async Task<AdminAuthLockout?> ActiveAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .GetActiveLockoutAsync(scope, key, CancellationToken.None);
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
                var cut = value.IndexOf(';');
                return cut < 0 ? value : value[..cut];
            }
        }

        return null;
    }
}

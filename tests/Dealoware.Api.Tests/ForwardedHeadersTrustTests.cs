using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

public class ForwardedHeadersTrustTests
{
    [Fact]
    public void F3_Parse_OneEntry()
    {
        Assert.True(ForwardedHeadersTrust.TryParse("10.0.0.0/16", out var networks, out var error));
        Assert.Null(error);
        Assert.Single(networks);
        Assert.Equal("10.0.0.0/16", networks[0].ToString());
    }

    [Fact]
    public void F3_Parse_SeveralEntries()
    {
        Assert.True(ForwardedHeadersTrust.TryParse("10.0.1.0/24,10.0.2.0/24", out var networks, out var error));
        Assert.Null(error);
        Assert.Equal(2, networks.Count);
        Assert.Equal("10.0.1.0/24", networks[0].ToString());
        Assert.Equal("10.0.2.0/24", networks[1].ToString());
    }

    [Fact]
    public void F3_Parse_WhitespaceAroundEntries()
    {
        Assert.True(ForwardedHeadersTrust.TryParse(" 10.0.1.0/24 , 10.0.2.0/24 ", out var networks, out var error));
        Assert.Null(error);
        Assert.Equal(2, networks.Count);
    }

    [Fact]
    public void F3_Parse_InvalidValue()
    {
        Assert.False(ForwardedHeadersTrust.TryParse("not-a-cidr", out var networks, out var error));
        Assert.Empty(networks);
        Assert.Contains("invalid", error, StringComparison.OrdinalIgnoreCase);
        Assert.False(ForwardedHeadersTrust.TryParse("10.0.0.0/16,", out _, out var emptyEntry));
        Assert.Contains("empty", emptyEntry, StringComparison.OrdinalIgnoreCase);
        Assert.False(ForwardedHeadersTrust.TryParse("", out _, out _));
        Assert.False(ForwardedHeadersTrust.TryParse(null, out _, out _));
    }

    [Fact]
    public void F3_EmptyDevelopmentConfig_ClearsLists_ForwardLimitIs1()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownProxies.Add(IPAddress.Loopback);
        var networks = ForwardedHeadersTrust.Resolve("", requireConfigured: false);
        ForwardedHeadersTrust.Apply(options, networks);

        Assert.Empty(options.KnownIPNetworks);
        Assert.Empty(options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
    }

    [Fact]
    public void F3_Apply_LeavesKnownProxiesEmpty()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownProxies.Add(IPAddress.Loopback);
        Assert.True(ForwardedHeadersTrust.TryParse("10.8.0.0/16", out var networks, out _));
        ForwardedHeadersTrust.Apply(options, networks);
        Assert.Single(options.KnownIPNetworks);
        Assert.Empty(options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
    }

    [Fact]
    public void F3_Production_MissingKnownNetworks_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => ForwardedHeadersTrust.Resolve(null, requireConfigured: true));
        Assert.Contains(ForwardedHeadersTrust.ConfigurationKey, ex.Message);
        Assert.DoesNotContain("10.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void F3_Production_InvalidCidr_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => ForwardedHeadersTrust.Resolve("not-a-cidr", requireConfigured: true));
        Assert.Contains(ForwardedHeadersTrust.ConfigurationKey, ex.Message);
        Assert.DoesNotContain("not-a-cidr", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void F3_ProductionHost_MissingKnownNetworks_FailsAtStartup()
    {
        AssertHostFailsWithKeyName(new ProductionKnownNetworksFactory(string.Empty));
    }

    [Fact]
    public void F3_ProductionHost_InvalidCidr_FailsAtStartup()
    {
        AssertHostFailsWithKeyName(new ProductionKnownNetworksFactory("not-a-cidr"));
    }

    private static void AssertHostFailsWithKeyName(IsolatedWebApplicationFactory factory)
    {
        using (factory)
        {
            var ex = Record.Exception(() => factory.CreateClient());
            Assert.NotNull(ex);
            var messages = new List<string>();
            for (var current = ex; current is not null; current = current.InnerException)
            {
                messages.Add(current.Message);
            }

            Assert.Contains(messages, m => m.Contains(ForwardedHeadersTrust.ConfigurationKey, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task F3_SpoofedXff_FromPeerOutsideKnownNetworks_DoesNotChangeThrottleKey()
    {
        var peer = IPAddress.Parse("203.0.113.10");
        var spoofed = "198.51.100.20";
        using var factory = new ForwardedHeadersPeerFactory("10.8.0.0/16", peer);
        SeedOwner(factory);
        var client = CreateClient(factory);

        using var fail = await SendWrongPasswordAsync(client, spoofed);
        Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        Assert.Equal(1, await CountIpFailuresAsync(factory, peer.ToString()));
        Assert.Equal(0, await CountIpFailuresAsync(factory, spoofed));
    }

    [Fact]
    public async Task F3_Xff_FromPeerInsideKnownNetworks_UsesRightMostHop()
    {
        var peer = IPAddress.Parse("10.8.1.4");
        using var factory = new ForwardedHeadersPeerFactory("10.8.0.0/16", peer);
        SeedOwner(factory);
        var client = CreateClient(factory);

        using var fail = await SendWrongPasswordAsync(client, "198.51.100.1, 198.51.100.2");
        Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        Assert.Equal(1, await CountIpFailuresAsync(factory, "198.51.100.2"));
        Assert.Equal(0, await CountIpFailuresAsync(factory, "198.51.100.1"));
        Assert.Equal(0, await CountIpFailuresAsync(factory, peer.ToString()));
    }

    private static void SeedOwner(ForwardedHeadersPeerFactory factory)
    {
        var directory = factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        directory.SetPasswordAsync("io@aiknowhow.com", "Pw-f3-known-networks-xx", CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static HttpClient CreateClient(ForwardedHeadersPeerFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task<int> CountIpFailuresAsync(ForwardedHeadersPeerFactory factory, string ip)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AdminAuthService>()
            .CountFailuresAsync(AdminAuthScopes.LoginIp, ip, CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> SendWrongPasswordAsync(HttpClient client, string forwardedFor)
    {
        const string adminHost = "admin.core.dealoware.com";
        var afRequest = new HttpRequestMessage(HttpMethod.Get, "/admin/sign-in");
        afRequest.Headers.Host = adminHost;
        afRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        using var afResponse = await client.SendAsync(afRequest);
        afResponse.EnsureSuccessStatusCode();
        var token = afResponse.Headers.TryGetValues(AdminAntiForgery.HeaderName, out var values)
            ? values.First()
            : string.Empty;
        var cookie = ReadCookie(afResponse, AdminAntiForgery.CookieName) ?? string.Empty;

        var request = new HttpRequestMessage(HttpMethod.Post, AdminAuthEndpoints.SignInPath);
        request.Headers.Host = adminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, token);
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminAntiForgery.CookieName}={cookie}");
        request.Content = JsonContent.Create(new
        {
            email = "io@aiknowhow.com",
            password = "wrong",
            turnstileToken = FakeTurnstileVerifier.ValidToken
        });
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

internal sealed class ProductionKnownNetworksFactory : IsolatedWebApplicationFactory
{
    private readonly string _knownNetworks;

    public ProductionKnownNetworksFactory(string knownNetworks)
    {
        _knownNetworks = knownNetworks;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(Environments.Production);
        builder.UseSetting(
            JwtSigningKeyValidator.EnvironmentVariableName,
            EnvironmentWebApplicationFactory.TestSigningKey64);
        builder.UseSetting("Jwt:SigningKey", string.Empty);
        builder.UseSetting(IpHasher.KeyEnvironmentVariable, EnvironmentWebApplicationFactory.TestIpHmacKey);
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=5432;Database=dealoware_test;Username=test;Password=test");
        builder.UseSetting(ForwardedHeadersTrust.ConfigurationKey, _knownNetworks);
    }
}

internal sealed class ForwardedHeadersPeerFactory : IsolatedWebApplicationFactory
{
    private readonly string _knownNetworks;
    private readonly IPAddress _peer;

    public ForwardedHeadersPeerFactory(string knownNetworks, IPAddress peer)
    {
        _knownNetworks = knownNetworks;
        _peer = peer;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting(ForwardedHeadersTrust.ConfigurationKey, _knownNetworks);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new TestPeerIpStartupFilter(_peer));
        });
    }
}

internal sealed class TestPeerIpStartupFilter : IStartupFilter
{
    private readonly IPAddress _peer;

    public TestPeerIpStartupFilter(IPAddress peer) => _peer = peer;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, nextMw) =>
            {
                context.Connection.RemoteIpAddress = _peer;
                return nextMw();
            });
            next(app);
        };
    }
}

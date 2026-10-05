using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Auth.Dtos;
using Microsoft.AspNetCore.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Rate-limit tests use a dedicated host with tight PermitLimit overrides so the shared
/// WebAppTests suite (Development + high limits) is unaffected.
/// </summary>
public sealed class AuthRateLimitTests : IDisposable
{
    private readonly RateLimitedWebApplicationFactory _factory = new(
        registerPermit: 3,
        tokenPermit: 3,
        windowSeconds: 60);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_ExceedingPermitLimit_Returns429()
    {
        var client = _factory.CreateClient();

        HttpStatusCode? lastStatus = null;
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
            {
                DisplayName = $"rate-limit-{i}"
            });
            statuses.Add(response.StatusCode);
            lastStatus = response.StatusCode;
        }

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task Token_ExceedingPermitLimit_Returns429()
    {
        // Use a separate factory so register traffic does not share the token partition budget.
        // Register and token use different policies but the same IP partition key; limits are per-policy.
        using var factory = new RateLimitedWebApplicationFactory(
            registerPermit: 100,
            tokenPermit: 3,
            windowSeconds: 60);
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/token", new TokenRequest
            {
                ApiKey = "dlw_invalid_key123456"
            });
            statuses.Add(response.StatusCode);
        }

        // Invalid key still counts toward the limiter; first 3 are 401, then 429.
        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task Health_Unaffected_WhenAuthRegisterLimited()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 4; i++)
        {
            await client.PostAsJsonAsync("/auth/register", new { });
        }

        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}

/// <summary>
/// Isolated host with explicit RateLimiting:* settings (overrides Development defaults).
/// </summary>
public sealed class RateLimitedWebApplicationFactory : IsolatedWebApplicationFactory
{
    private readonly int _registerPermit;
    private readonly int _tokenPermit;
    private readonly int _windowSeconds;

    public RateLimitedWebApplicationFactory(
        int registerPermit,
        int tokenPermit,
        int windowSeconds)
    {
        _registerPermit = registerPermit;
        _tokenPermit = tokenPermit;
        _windowSeconds = windowSeconds;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:AuthRegister:PermitLimit", _registerPermit.ToString());
        builder.UseSetting("RateLimiting:AuthRegister:WindowSeconds", _windowSeconds.ToString());
        builder.UseSetting("RateLimiting:AuthToken:PermitLimit", _tokenPermit.ToString());
        builder.UseSetting("RateLimiting:AuthToken:WindowSeconds", _windowSeconds.ToString());
    }
}

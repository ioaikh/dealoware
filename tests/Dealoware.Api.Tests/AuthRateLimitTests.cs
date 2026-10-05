using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.RateLimiting;
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

    [Fact]
    public async Task Register_429_IncludesRetryAfterHeaderAndJsonBody()
    {
        var client = _factory.CreateClient();

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 4; i++)
        {
            rejected = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
            {
                DisplayName = $"retry-after-{i}"
            });
        }

        await AssertRateLimitedWithRetryAfter(rejected!, windowSeconds: 60);
    }

    [Fact]
    public async Task Token_429_IncludesRetryAfterHeader()
    {
        using var factory = new RateLimitedWebApplicationFactory(
            registerPermit: 100,
            tokenPermit: 2,
            windowSeconds: 30);
        var client = factory.CreateClient();

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 3; i++)
        {
            rejected = await client.PostAsJsonAsync("/auth/token", new TokenRequest
            {
                ApiKey = "dlw_invalid_key123456"
            });
        }

        await AssertRateLimitedWithRetryAfter(rejected!, windowSeconds: 30);
    }

    [Fact]
    public async Task Register_DistinctForwardedForClients_GetSeparateBuckets()
    {
        var client = _factory.CreateClient();

        // Client A (documentation-range address) exhausts its 3-request budget.
        var clientAStatuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            clientAStatuses.Add(await RegisterFrom(client, "203.0.113.10", $"client-a-{i}"));
        }

        Assert.Equal(3, clientAStatuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(HttpStatusCode.TooManyRequests, clientAStatuses[^1]);

        // Client B arrives through the same proxy hop but a different X-Forwarded-For: its own bucket.
        Assert.Equal(HttpStatusCode.Created, await RegisterFrom(client, "203.0.113.20", "client-b-0"));

        // Requests without the header (the proxy hop itself) are also a separate partition.
        var direct = await client.PostAsJsonAsync("/auth/register", new RegisterRequest { DisplayName = "direct" });
        Assert.Equal(HttpStatusCode.Created, direct.StatusCode);

        // Client A is still limited.
        Assert.Equal(HttpStatusCode.TooManyRequests, await RegisterFrom(client, "203.0.113.10", "client-a-again"));
    }

    [Fact]
    public async Task Register_SpoofedLeftmostForwardedFor_DoesNotEscapeBucket()
    {
        var client = _factory.CreateClient();

        // The load balancer appends the real client IP as the right-most entry; anything to its left
        // is client-supplied. Changing the left-most value every request must not reset the budget.
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            statuses.Add(await RegisterFrom(client, $"198.51.100.{i + 1}, 203.0.113.30", $"spoof-{i}"));
        }

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public void PartitionKey_UsesConnectionRemoteIpAddress()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        Assert.Equal("unknown", AuthRateLimiting.GetPartitionKey(httpContext));

        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.40");
        Assert.Equal("203.0.113.40", AuthRateLimiting.GetPartitionKey(httpContext));
    }

    private static async Task<HttpStatusCode> RegisterFrom(HttpClient client, string forwardedFor, string displayName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest { DisplayName = displayName })
        };
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task AssertRateLimitedWithRetryAfter(HttpResponseMessage response, int windowSeconds)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var retryAfter = response.Headers.RetryAfter;
        Assert.NotNull(retryAfter);
        Assert.NotNull(retryAfter!.Delta);
        var headerSeconds = (int)retryAfter.Delta!.Value.TotalSeconds;
        Assert.InRange(headerSeconds, 1, windowSeconds);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(AuthRateLimiting.RateLimitedErrorCode, body.RootElement.GetProperty("error").GetString());
        Assert.Equal(headerSeconds, body.RootElement.GetProperty("retryAfterSeconds").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("message").GetString()));
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

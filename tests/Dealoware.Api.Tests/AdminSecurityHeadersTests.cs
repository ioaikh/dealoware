using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Tests;

/// <summary>
/// A11 items 13 and 17: HSTS, CSP, and browser hardening headers on every
/// admin-host response (page, API, 404), including signed-out /admin/sign-in
/// and /admin/auth/ paths. Headers must not appear on api.core.
/// </summary>
[Collection("WebAppTests")]
public class AdminSecurityHeadersTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminSecurityHeadersTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Kestrel_AddServerHeader_IsDisabled()
    {
        var options = _factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        Assert.False(options.AddServerHeader);
    }

    [Fact]
    public async Task AdminPagePath_SignIn_HasHardeningHeaders()
    {
        using var response = await SendAdminAsync("/admin/sign-in", https: true);
        AssertAdminHardeningHeaders(response, expectHsts: true);
    }

    [Fact]
    public async Task AdminApiPath_Me_HasHardeningHeaders()
    {
        using var response = await SendAdminAsync("/admin/api/me", https: true);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertAdminHardeningHeaders(response, expectHsts: true);
    }

    [Fact]
    public async Task Admin404_NonAdminPathOnAdminHost_HasHardeningHeaders()
    {
        using var response = await SendAdminAsync("/artifacts", https: true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertAdminHardeningHeaders(response, expectHsts: true);
    }

    [Fact]
    public async Task AdminAuthStaticFile_HasHardeningHeaders()
    {
        using var response = await SendAdminAsync("/admin/auth/signed-out.css", https: true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Signed-out admin auth asset", body);
        Assert.DoesNotContain("DEALOWARE_", body, StringComparison.Ordinal);
        AssertAdminHardeningHeaders(response, expectHsts: true);
    }

    [Fact]
    public async Task AdminHost_Http_OmitsHsts_KeepsOtherHeaders()
    {
        using var response = await SendAdminAsync("/admin/api/me", https: false);
        AssertAdminHardeningHeaders(response, expectHsts: false);
    }

    [Fact]
    public async Task AdminAuthenticated404_HasHardeningHeaders()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create("io@aiknowhow.com", ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/does-not-exist");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session.Id:D}");
        using var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertAdminHardeningHeaders(response, expectHsts: true);
    }

    [Theory]
    [InlineData("/admin/api/me", "localhost")]
    [InlineData("/admin/api/me", "core.dealoware.com")]
    [InlineData("/admin/sign-in", "localhost")]
    [InlineData("/admin/sign-in", "core.dealoware.com")]
    [InlineData("/admin/auth/turnstile.js", "localhost")]
    [InlineData("/admin/", "api.core.dealoware.com")]
    [InlineData("/admin/api/me", "admin.platform.dealoware.com")]
    [InlineData("/admin/sign-in", "admin.platform.dealoware.com")]
    public async Task C73_HostGateDeny_ReturnsEmpty404_No401OrSetCookieOrAdminHtml(string path, string host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={Guid.NewGuid():D}");
        using var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(string.Empty, body);
        Assert.True(response.Content.Headers.ContentLength is null or 0);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.False(response.Headers.Contains("Server"));
        Assert.DoesNotContain("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sign-in", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CoreOwner", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unauthorized", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dw_admin", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonAdminHost_Health_DoesNotHaveAdminHardeningHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "localhost";
        using var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
        Assert.False(response.Headers.Contains("Referrer-Policy"));
        Assert.False(response.Headers.Contains("X-Robots-Tag"));
        Assert.False(response.Headers.Contains("X-Frame-Options"));
    }

    private async Task<HttpResponseMessage> SendAdminAsync(string path, bool https)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (https)
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        return await _factory.CreateClient().SendAsync(request);
    }

    internal static void AssertAdminHardeningHeaders(HttpResponseMessage response, bool expectHsts)
    {
        if (expectHsts)
        {
            Assert.Equal(
                AdminSecurityHeadersMiddleware.StrictTransportSecurity,
                GetHeader(response, "Strict-Transport-Security"));
            Assert.DoesNotContain("preload", GetHeader(response, "Strict-Transport-Security"), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.False(response.Headers.Contains("Strict-Transport-Security"));
        }

        var csp = GetHeader(response, "Content-Security-Policy");
        Assert.Equal(AdminSecurityHeadersMiddleware.ContentSecurityPolicy, csp);
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("base-uri 'none'", csp);
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("script-src 'self' https://challenges.cloudflare.com", csp);
        Assert.Contains("frame-src https://challenges.cloudflare.com", csp);

        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, GetHeader(response, "X-Content-Type-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.ReferrerPolicy, GetHeader(response, "Referrer-Policy"));
        Assert.Equal(AdminSecurityHeadersMiddleware.RobotsTag, GetHeader(response, "X-Robots-Tag"));
        Assert.Equal(AdminSecurityHeadersMiddleware.CacheControl, GetHeader(response, "Cache-Control"));
        Assert.Equal(AdminSecurityHeadersMiddleware.FrameOptions, GetHeader(response, "X-Frame-Options"));
    }

    internal static string GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values)
            || response.Content.Headers.TryGetValues(name, out values))
        {
            return string.Join(",", values);
        }

        return string.Empty;
    }
}

public class AdminUnhandledExceptionHeaderTests
{
    [Fact]
    public async Task AdminHost_UnhandledException_ReturnsGeneric500WithHardeningHeaders()
    {
        using var factory = new AdminTestExceptionWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create("io@aiknowhow.com", ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, AdminTestExceptionEndpoint.Path);
        request.Headers.Host = AdminHostOptions.ProductionAdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session.Id:D}");
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(body) || body == """{"error":"Internal Server Error"}""");
        Assert.DoesNotContain(AdminTestExceptionEndpoint.ProbeExceptionMessage, body);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("io@aiknowhow.com", body);
        Assert.DoesNotContain("at Dealoware", body, StringComparison.Ordinal);
        AdminSecurityHeadersTests.AssertAdminHardeningHeaders(response, expectHsts: true);
        Assert.False(response.Headers.Contains("Server"));
    }
}

file sealed class AdminTestExceptionWebApplicationFactory : IsolatedWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting(AdminTestExceptionEndpoint.ConfigKey, "true");
    }
}

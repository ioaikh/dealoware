using System.Net;
using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// r2 §2 exemptions, denies, and no-data HTML. Token rows use the Development gate.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuthPageTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuthPageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new() { AllowAutoRedirect = false });

    private static HttpRequestMessage Req(HttpMethod method, string path, string host, string? accept = "text/html")
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = host;
        if (accept is not null)
            request.Headers.TryAddWithoutValidation("Accept", accept);
        return request;
    }

    private static void AddCookie(HttpRequestMessage request, string name, string value) =>
        request.Headers.TryAddWithoutValidation("Cookie", $"{name}={value}");

    [Theory]
    [InlineData("GET", "/admin/sign-in", "Sign in · Dealoware admin", "S-A1")]
    [InlineData("POST", "/admin/sign-in", "Sign in · Dealoware admin", "S-A1")]
    [InlineData("GET", "/admin/reset", "Reset password · Dealoware admin", "S-A8")]
    [InlineData("POST", "/admin/reset", "Reset password · Dealoware admin", "S-A8")]
    [InlineData("GET", "/admin/reset/sent", "Check your email · Dealoware admin", "S-A9")]
    [InlineData("GET", "/admin/link-expired", "Link no longer works · Dealoware admin", "S-A5")]
    public async Task ExemptRows_WithoutToken_ServeHtml(string method, string path, string title, string screen)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(new HttpMethod(method), path, AdminHost));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(CoreOwnerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerHTML", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/assets/", html, StringComparison.Ordinal);
        Assert.Contains("/admin/auth/", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GET", "/admin/sign-in/code", "dw_admin_pending", "Enter code · Dealoware admin", "S-A2")]
    [InlineData("POST", "/admin/sign-in/code", "dw_admin_pending", "Enter code · Dealoware admin", "S-A2")]
    [InlineData("GET", "/admin/sign-in/recovery", "dw_admin_pending", "Recovery code · Dealoware admin", "S-A3")]
    [InlineData("POST", "/admin/sign-in/recovery", "dw_admin_pending", "Recovery code · Dealoware admin", "S-A3")]
    [InlineData("GET", "/admin/setup/authenticator", "dw_admin_enrol", "Authenticator setup · Dealoware admin", "S-A6")]
    [InlineData("POST", "/admin/setup/authenticator", "dw_admin_enrol", "Authenticator setup · Dealoware admin", "S-A6")]
    [InlineData("GET", "/admin/setup/recovery-codes", "dw_admin_enrol", "Recovery codes · Dealoware admin", "S-A7")]
    public async Task TokenRows_WithCookie_ServeHtml(string method, string path, string cookie, string title, string screen)
    {
        var client = CreateClient();
        var request = Req(new HttpMethod(method), path, AdminHost);
        AddCookie(request, cookie, "valid");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/bootstrap")]
    [InlineData("/admin/reset/confirm")]
    public async Task TokenRows_WithQueryToken_ServeHtml(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, path + "?token=opaque", AdminHost));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin/sign-in/code")]
    [InlineData("/admin/sign-in/recovery")]
    [InlineData("/admin/bootstrap")]
    [InlineData("/admin/reset/confirm")]
    [InlineData("/admin/setup/authenticator")]
    [InlineData("/admin/setup/recovery-codes")]
    public async Task TokenRows_MissingToken_Generic401(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, path, AdminHost));
        await AssertGeneric401(response);
    }

    [Theory]
    [InlineData("/admin/sign-in/extra")]
    [InlineData("/admin/sign-in/code/extra")]
    [InlineData("/admin/reset/sent/extra")]
    [InlineData("/admin/auth")]
    [InlineData("/admin/settings/security")]
    public async Task ExtraSegment_Generic401(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, path, AdminHost));
        await AssertGeneric401(response);
    }

    [Theory]
    [InlineData("DELETE", "/admin/sign-in")]
    [InlineData("PUT", "/admin/reset")]
    [InlineData("GET", "/admin/sign-out")]
    [InlineData("GET", "/admin/api/auth/sign-in")]
    [InlineData("POST", "/admin/api/me")]
    public async Task WrongMethodOrUnlistedApi_Generic401(string method, string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(new HttpMethod(method), path, AdminHost, "application/json"));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task QueryString_DoesNotCreateExemption()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(
            Req(HttpMethod.Get, "/admin/settings/security?path=/admin/sign-in", AdminHost));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task AdminBare_RedirectsToCanonicalStats()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin", AdminHost));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AdminAuthPaths.Stats, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CanonicalStats_WithoutSession_Generic401()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/", AdminHost));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task PostSignOut_WithoutSession_ClearsCookies()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Post, "/admin/sign-out", AdminHost, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var setCookie = string.Join(" ", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains(AdminSessionCookie.Name, setCookie, StringComparison.Ordinal);
        Assert.Contains("path=/admin", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSignOut_WithoutSession_DoesNotClear()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/sign-out", AdminHost, "application/json"));
        await AssertGeneric401(response);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task AuthAssets_AnonymousOnAdminHost()
    {
        var client = CreateClient();
        using var ok = await client.SendAsync(Req(HttpMethod.Get, "/admin/auth/js/admin-api.js", AdminHost, null));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var js = await ok.Content.ReadAsStringAsync();
        Assert.Contains("/admin/api/auth/sign-in", js);
        Assert.DoesNotContain("innerHTML", js);
        using var offHost = await client.SendAsync(Req(HttpMethod.Get, "/admin/auth/js/admin-api.js", "localhost", null));
        Assert.Equal(HttpStatusCode.NotFound, offHost.StatusCode);
    }

    [Fact]
    public async Task SignedInShellAsset_BeforeSession_Denied()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/app.js", AdminHost, null));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task TopLevelSignIn_NotAnAdminPage()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/sign-in", AdminHost));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminHost_DoesNotServeParticipantWwwroot()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/", AdminHost));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminApiMe_WithoutSession_StillGeneric401()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/api/me", AdminHost, "application/json"));
        await AssertGeneric401(response);
    }

    [Fact]
    public void CookieOptions_MatchSection24()
    {
        var options = AdminSessionCookie.CreateOptions();
        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/admin", options.Path);
        Assert.Null(options.Domain);
        var pending = AdminAuthCookies.CreateOptions();
        Assert.Equal("/admin", pending.Path);
        Assert.Null(pending.Domain);
    }

    [Fact]
    public void SafeReturnPath_FollowsSection5()
    {
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath(null));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin"));
        Assert.Equal("/admin/participants", AdminAuthPaths.SafeReturnPath("/admin/participants"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/sign-in"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/setup/authenticator"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("https://evil.example/admin/"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/participants?q=1"));
    }

    [Fact]
    public void ExemptionMatch_IsExactOrdinalIgnoreCase()
    {
        Assert.True(AdminAuthPaths.TryMatchExemption("/admin/sign-in", "GET", out _));
        Assert.True(AdminAuthPaths.TryMatchExemption("/admin/SIGN-IN", "post", out _));
        Assert.False(AdminAuthPaths.TryMatchExemption("/admin/sign-in/extra", "GET", out _));
        Assert.False(AdminAuthPaths.TryMatchExemption("/admin/sign-in", "DELETE", out _));
        Assert.False(AdminAuthPaths.TryMatchExemption("/admin/sign-out", "GET", out _));
        Assert.True(AdminAuthPaths.TryMatchExemption("/admin/sign-out", "POST", out _));
    }

    [Fact]
    public void UiSource_HasNoInnerHtmlOrLockWording()
    {
        var root = RepoRoot();
        foreach (var dir in new[]
                 {
                     Path.Combine(root, "src", "Dealoware.Api", "AdminUi", "pages"),
                     Path.Combine(root, "src", "Dealoware.Api", "wwwroot", "admin", "auth")
                 })
        {
            foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("innerHTML", text, StringComparison.Ordinal);
                Assert.DoesNotContain("locked", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("countdown", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(CoreOwnerEmail, text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("/admin/assets/", text, StringComparison.Ordinal);
            }
        }
    }

    private static async Task AssertGeneric401(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }
}

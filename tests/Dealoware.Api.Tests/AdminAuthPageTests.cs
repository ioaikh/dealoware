using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// r5 sha c5e9d232 §10 C1–C8 exemptions, denies, AF, cookies, and no-data HTML.
/// Token rows use the Development gate.
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

    private HttpClient CreateClient() =>
        _factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });

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

    private static string? ReadSetCookieValue(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return null;
        foreach (var raw in values)
        {
            if (!raw.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                continue;
            var rest = raw[(name.Length + 1)..];
            var end = rest.IndexOf(';');
            return end >= 0 ? rest[..end] : rest;
        }

        return null;
    }

    private async Task<(string Cookie, string Token)> IssueAntiForgeryAsync(HttpClient client)
    {
        using var get = await client.SendAsync(Req(HttpMethod.Get, AdminAuthPaths.SignIn, AdminHost));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.True(get.Headers.Contains(AdminAuthCookies.AntiForgeryHeader));
        var token = get.Headers.GetValues(AdminAuthCookies.AntiForgeryHeader).Single();
        var cookie = ReadSetCookieValue(get, AdminAuthCookies.AntiForgery);
        Assert.False(string.IsNullOrWhiteSpace(cookie));
        var setCookie = string.Join(" ", get.Headers.GetValues("Set-Cookie"));
        Assert.DoesNotContain("__Host-", setCookie, StringComparison.Ordinal);
        Assert.Contains("path=/admin", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        return (cookie!, token);
    }

    private async Task<HttpResponseMessage> SendExemptPostAsync(
        HttpClient client,
        string path,
        string accept,
        params (string Name, string Value)[] extraCookies)
    {
        var (_, token) = await IssueAntiForgeryAsync(client);
        var request = Req(HttpMethod.Post, path, AdminHost, accept);
        request.Headers.TryAddWithoutValidation(AdminAuthCookies.AntiForgeryHeader, token);
        var cookies = extraCookies.Select(c => $"{c.Name}={c.Value}").ToList();
        cookies.Add($"{AdminAuthCookies.AntiForgery}={token}");
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        return await client.SendAsync(request);
    }

    [Theory]
    [InlineData("GET", "/admin/sign-in", "Sign in · Dealoware admin", "S-A1")]
    [InlineData("GET", "/admin/reset", "Reset password · Dealoware admin", "S-A8")]
    [InlineData("GET", "/admin/reset/sent", "Check your email · Dealoware admin", "S-A9")]
    [InlineData("GET", "/admin/link-expired", "Link no longer works · Dealoware admin", "S-A5")]
    public async Task ExemptGetRows_WithoutToken_ServeHtml(string method, string path, string title, string screen)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(new HttpMethod(method), path, AdminHost));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(CoreOwnerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerHTML", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/assets/", html, StringComparison.Ordinal);
        Assert.Contains("/admin/auth/", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/sign-in", "Sign in · Dealoware admin", "S-A1")]
    [InlineData("/admin/reset", "Reset password · Dealoware admin", "S-A8")]
    public async Task ExemptPostRows_WithAntiForgery_ServeHtml(string path, string title, string screen)
    {
        var client = CreateClient();
        using var response = await SendExemptPostAsync(client, path, "text/html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/sign-in")]
    [InlineData("/admin/reset")]
    [InlineData("/admin/sign-out")]
    public async Task ExemptPost_WithoutAntiForgery_Generic401(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Post, path, AdminHost, "application/json"));
        await AssertGeneric401(response);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("GET", "/admin/sign-in/code", "valid", "Enter code · Dealoware admin", "S-A2")]
    [InlineData("GET", "/admin/sign-in/recovery", "valid", "Recovery code · Dealoware admin", "S-A3")]
    [InlineData("GET", "/admin/setup/authenticator", AdminAuthCookies.EnrolPendingValue, "Authenticator setup · Dealoware admin", "S-A6")]
    [InlineData("GET", "/admin/setup/recovery-codes", AdminAuthCookies.EnrolPendingValue, "Recovery codes · Dealoware admin", "S-A7")]
    public async Task TokenRows_WithPendingCookie_ServeHtml(
        string method,
        string path,
        string pendingValue,
        string title,
        string screen)
    {
        var client = CreateClient();
        var request = Req(new HttpMethod(method), path, AdminHost);
        AddCookie(request, AdminAuthCookies.PendingSignIn, pendingValue);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/sign-in/code", "valid", "Enter code · Dealoware admin", "S-A2")]
    [InlineData("/admin/sign-in/recovery", "valid", "Recovery code · Dealoware admin", "S-A3")]
    [InlineData("/admin/setup/authenticator", AdminAuthCookies.EnrolPendingValue, "Authenticator setup · Dealoware admin", "S-A6")]
    public async Task TokenRows_PostWithAntiForgery_ServeHtml(
        string path,
        string pendingValue,
        string title,
        string screen)
    {
        var client = CreateClient();
        using var response = await SendExemptPostAsync(
            client,
            path,
            "text/html",
            (AdminAuthCookies.PendingSignIn, pendingValue));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/bootstrap", "Set password · Dealoware admin", "S-A4")]
    [InlineData("/admin/reset/confirm", "Set a new password · Dealoware admin", "S-A10")]
    public async Task LinkPages_GetWithoutToken_ServeHtml_NoStore_NoReferrer(string path, string title, string screen)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, path, AdminHost));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains($"data-screen=\"{screen}\"", html, StringComparison.Ordinal);
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var setCookie = string.Join(" ", cookies);
            Assert.DoesNotContain("token=", setCookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(AdminAuthCookies.PendingSignIn + "=", setCookie, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("/admin/bootstrap")]
    [InlineData("/admin/reset/confirm")]
    public async Task LinkPages_QueryToken_IsNotRequiredAndNotStored(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, path + "?token=opaque", AdminHost));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var setCookie = string.Join(" ", cookies);
            Assert.DoesNotContain("opaque", setCookie, StringComparison.Ordinal);
            Assert.DoesNotContain("token=", setCookie, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("/admin/bootstrap")]
    [InlineData("/admin/reset/confirm")]
    public async Task LinkPages_TokenHeader_IsIgnoredAndNotStored(string path)
    {
        var client = CreateClient();
        using var request = Req(HttpMethod.Get, path, AdminHost);
        request.Headers.TryAddWithoutValidation("Token", "opaque");
        request.Headers.TryAddWithoutValidation("X-Link-Token", "opaque");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var setCookie = string.Join(" ", cookies);
            Assert.DoesNotContain("opaque", setCookie, StringComparison.Ordinal);
            Assert.DoesNotContain("token=", setCookie, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("/admin/sign-in/code")]
    [InlineData("/admin/sign-in/recovery")]
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
    [InlineData("/admin/bootstrap/opaque")]
    [InlineData("/admin/reset/confirm/opaque")]
    [InlineData("/admin/auth")]
    [InlineData("/admin/sign-in/")]
    [InlineData("/admin/sign-in;x")]
    public async Task ExtraSegmentOrNonCanonical_Generic401(string path)
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
    public async Task SettingsSecurity_WithoutSession_Generic401()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, AdminAuthPaths.SettingsSecurity, AdminHost));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task SettingsSecurity_WithSession_ServesHtml_NoStore()
    {
        var client = CreateClient();
        var sessionId = await SeedSessionAsync();
        var request = Req(HttpMethod.Get, AdminAuthPaths.SettingsSecurity, AdminHost);
        AddCookie(request, AdminSessionCookie.Name, sessionId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Security settings · Dealoware admin", html, StringComparison.Ordinal);
        Assert.Contains("data-screen=\"S-A12\"", html, StringComparison.Ordinal);
        Assert.Contains("change-password-form", html, StringComparison.Ordinal);
        Assert.DoesNotContain("turnstile", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(CoreOwnerEmail, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanonicalStats_WithoutSession_Generic401()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/", AdminHost));
        await AssertGeneric401(response);
    }

    [Fact]
    public async Task PostSignOut_WithAntiForgery_ClearsCookies()
    {
        var client = CreateClient();
        using var response = await SendExemptPostAsync(client, "/admin/sign-out", "application/json");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var setCookie = string.Join(" ", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains(AdminSessionCookie.Name, setCookie, StringComparison.Ordinal);
        Assert.Contains(AdminAuthCookies.PendingSignIn, setCookie, StringComparison.Ordinal);
        Assert.Contains(AdminAuthCookies.AntiForgery, setCookie, StringComparison.Ordinal);
        Assert.Contains("path=/admin", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("__Host-", setCookie, StringComparison.Ordinal);
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
    public async Task OffHost_AdminPage_Empty404_NoCookies()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(Req(HttpMethod.Get, "/admin/sign-in", "localhost"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Empty(await response.Content.ReadAsStringAsync());
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
        Assert.Empty(await offHost.Content.ReadAsStringAsync());
        Assert.False(offHost.Headers.Contains("Set-Cookie"));
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
    public void CookieNames_MatchC5()
    {
        Assert.Equal("dw_admin_session", AdminSessionCookie.Name);
        Assert.Equal("dw_admin_pending", AdminAuthCookies.PendingSignIn);
        Assert.Equal("dw_admin_af", AdminAuthCookies.AntiForgery);
        Assert.DoesNotContain("__Host-", AdminSessionCookie.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("__Host-", AdminAuthCookies.PendingSignIn, StringComparison.Ordinal);
        Assert.DoesNotContain("__Host-", AdminAuthCookies.AntiForgery, StringComparison.Ordinal);
        var options = AdminSessionCookie.CreateOptions();
        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/admin", options.Path);
        Assert.Null(options.Domain);
        var pending = AdminAuthCookies.CreateOptions();
        Assert.Equal("/admin", pending.Path);
        Assert.Null(pending.Domain);
        Assert.True(pending.Secure);
        Assert.True(pending.HttpOnly);
    }

    [Fact]
    public void SafeReturnPath_FollowsC6()
    {
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath(null));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin"));
        Assert.Equal("/admin/participants", AdminAuthPaths.SafeReturnPath("/admin/participants"));
        Assert.Equal("/admin/audit", AdminAuthPaths.SafeReturnPath("/admin/audit"));
        var id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        Assert.Equal($"/admin/offers/{id}", AdminAuthPaths.SafeReturnPath($"/admin/offers/{id}"));
        Assert.Equal($"/admin/audit/{id}", AdminAuthPaths.SafeReturnPath($"/admin/audit/{id}"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/sign-in"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/setup/authenticator"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/settings/security"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("https://evil.example/admin/"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/participants?q=1"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("/admin/participants/%2e%2e"));
        Assert.Equal("/admin/", AdminAuthPaths.SafeReturnPath("//admin/participants"));
    }

    [Fact]
    public void Canonicalize_FollowsC1()
    {
        Assert.True(AdminAuthPaths.TryCanonicalize("/admin/sign-in", out var signIn));
        Assert.Equal("/admin/sign-in", signIn);
        Assert.False(AdminAuthPaths.TryCanonicalize("/admin/sign-in/", out _));
        Assert.False(AdminAuthPaths.TryCanonicalize("/admin/sign-in/../reset", out _));
        Assert.False(AdminAuthPaths.TryCanonicalize("/admin//sign-in", out _));
        Assert.False(AdminAuthPaths.TryCanonicalize("/admin/sign-in;x", out _));
        Assert.True(AdminAuthPaths.TryCanonicalize("/admin/", out var stats));
        Assert.Equal("/admin/", stats);
        Assert.True(AdminAuthPaths.TryCanonicalize("/admin/auth/js/admin-api.js", out var asset));
        Assert.True(AdminAuthPaths.IsAuthAssetPath(asset));
    }

    [Fact]
    public void ExemptionMatch_IsExactOrdinalIgnoreCase()
    {
        Assert.True(AdminAuthPaths.TryMatchExemption("/admin/sign-in", "GET", out _));
        Assert.True(AdminAuthPaths.TryMatchExemption("/admin/SIGN-IN", "post", out _));
        Assert.False(AdminAuthPaths.TryMatchExemption("/admin/sign-in/extra", "GET", out _));
        Assert.False(AdminAuthPaths.TryMatchExemption("/admin/sign-in/", "GET", out _));
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

    private async Task<Guid> SeedSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
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

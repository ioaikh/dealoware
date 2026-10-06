using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

[Collection("BootstrapTests")]
public class AdminBootstrapTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string TrustedClient = "203.0.113.10";

    private readonly BootstrapWebApplicationFactory _factory;

    public AdminBootstrapTests(BootstrapWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task ResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminCredentials.RemoveRange(db.AdminCredentials);
        db.AdminBootstrapTokens.RemoveRange(db.AdminBootstrapTokens);
        db.AdminBootstrapIpThrottles.RemoveRange(db.AdminBootstrapIpThrottles);
        db.AdminAuditLog.RemoveRange(db.AdminAuditLog);
        await db.SaveChangesAsync();
        ClearSentMail();
        _factory.Turnstile.Unavailable = false;
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });

    private static string NewPassword()
        => "radio station " + Guid.NewGuid().ToString("N")[..8];

    private async Task<string> IssueTokenAsync()
    {
        ClearSentMail();
        using var scope = _factory.Services.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
        var issued = await bootstrap.IssueLinkAsync("testhmac");
        Assert.True(issued.Sent);
        return TokenFromLatestMail();
    }

    private void ClearSentMail()
    {
        while (_factory.Mail.Sent.TryTake(out _))
        {
        }
    }

    private string LastMailBody()
        => Assert.Single(_factory.Mail.Sent).TextBody;

    private string TokenFromLatestMail()
        => TokenFromLink(LastMailBody());

    private static string TokenFromLink(string body)
    {
        const string origin = "https://admin.core.dealoware.com";
        var start = body.IndexOf(origin, StringComparison.Ordinal);
        Assert.True(start >= 0, body);
        var end = body.IndexOfAny(['\r', '\n'], start);
        var link = end < 0 ? body[start..] : body[start..end];
        var uri = new Uri(link);
        Assert.Equal("https://admin.core.dealoware.com/admin/bootstrap", uri.GetLeftPart(UriPartial.Path));
        Assert.True(string.IsNullOrEmpty(uri.Query));
        Assert.DoesNotContain("?token=", link, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("#token=", uri.Fragment, StringComparison.Ordinal);
        var token = Uri.UnescapeDataString(uri.Fragment["#token=".Length..]);
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private async Task<(string Token, string AntiForgery)> IssueReadyAsync(HttpClient client)
    {
        var token = await IssueTokenAsync();
        var af = await IssueAntiForgeryAsync(client);
        return (token, af);
    }

    private static async Task<string> IssueAntiForgeryAsync(HttpClient client)
    {
        using var get = await client.SendAsync(AdminRequest(HttpMethod.Get, AdminSignedOutAccess.BootstrapPage));
        var inspectBody = await get.Content.ReadAsStringAsync();
        Assert.True(get.StatusCode == HttpStatusCode.OK, inspectBody);
        var json = JsonSerializer.Deserialize<JsonElement>(inspectBody)!;
        var af = json.GetProperty("antiForgeryToken").GetString();
        Assert.False(string.IsNullOrEmpty(af));
        Assert.False(json.TryGetProperty("token", out _));
        return af!;
    }

    private static HttpRequestMessage AdminRequest(
        HttpMethod method,
        string path,
        object? body = null,
        string host = AdminHost,
        string? antiForgery = null,
        string? forwardedFor = TrustedClient)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = host;
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        }

        if (antiForgery is not null)
        {
            request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, antiForgery);
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminAntiForgery.CookieName}={antiForgery}");
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static async Task<string> AssertGenericUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("expired", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("used", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        return body;
    }

    private static async Task<string> AssertGoesToLinkExpired(HttpResponseMessage response)
    {
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.Found,
            $"expected redirect to link-expired, got {(int)response.StatusCode}");
        var location = response.Headers.Location?.ToString();
        Assert.Contains(AdminSignedOutAccess.LinkExpired, location);
        Assert.DoesNotContain("token=", location, StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("used", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        return body;
    }

    [Fact]
    public async Task TD_ADM_010_BootstrapLink_SingleUseAndTimeLimited()
    {
        await ResetAsync();
        var client = CreateClient();
        var (token, af) = await IssueReadyAsync(client);
        var password = NewPassword();

        using var first = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password, turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var ok = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(ok.GetProperty("passwordSet").GetBoolean());
        AssertNoSessionCookie(first);

        using var reuse = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(reuse);

        var expiredToken = await IssueFreshTokenAfterResetAsync();
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddHours(24).AddMinutes(1);
        using var expired = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = expiredToken, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(expired);

        using var random = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = "truncated-or-random", password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(random);

        Assert.DoesNotContain("password", LastMailBody(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("?token=", LastMailBody(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#token=", LastMailBody(), StringComparison.Ordinal);
    }

    private async Task<string> IssueFreshTokenAfterResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminCredentials.RemoveRange(db.AdminCredentials);
        await db.SaveChangesAsync();
        ClearSentMail();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
        var issued = await bootstrap.IssueLinkAsync("testhmac");
        Assert.True(issued.Sent);
        return TokenFromLatestMail();
    }

    [Fact]
    public async Task F2_GetBootstrap_TakesNoToken_TokenCheckedOnPostOnly()
    {
        await ResetAsync();
        var client = CreateClient();
        var token = await IssueTokenAsync();

        using var get = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(token)}"));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var json = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("token", out _));
        Assert.False(string.IsNullOrEmpty(json.GetProperty("antiForgeryToken").GetString()));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.All(db.AdminBootstrapTokens.ToList(), t => Assert.Null(t.ConsumedAt));
            Assert.Empty(db.AdminBootstrapIpThrottles.ToList());
        }

        var af = json.GetProperty("antiForgeryToken").GetString();
        using var queryOnlyPost = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            $"{AdminSignedOutAccess.BootstrapApi}?token={Uri.EscapeDataString(token)}",
            new { password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(queryOnlyPost);

        using var bodyPost = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, bodyPost.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_011_FirstPassword_OnlyViaBootstrapLink()
    {
        await ResetAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
            Assert.False(bootstrap.HasPasswordSet());
        }

        var client = CreateClient();
        using var before = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/me"));
        await AssertGenericUnauthorized(before);

        var (token, af) = await IssueReadyAsync(client);
        var password = NewPassword();
        using var set = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password, turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
            Assert.True(bootstrap.HasPasswordSet());
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = Assert.Single(db.AdminCredentials.ToList());
            Assert.Equal(CoreOwnerEmail, row.Email);
            Assert.False(string.IsNullOrEmpty(row.PasswordHash));
            Assert.DoesNotContain(password, row.PasswordHash);
            Assert.True(AdminPasswordHasher.Verify(password, row.PasswordHash!));
        }

        var ruleRejectToken = await IssueFreshTokenAfterResetAsync();
        var ruleAf = await IssueAntiForgeryAsync(client);
        using var tooShort = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = ruleRejectToken, password = "short", turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = ruleAf },
            antiForgery: ruleAf));
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        var shortBody = await tooShort.Content.ReadAsStringAsync();
        Assert.Contains(AdminAuthMessages.PasswordTooShort, shortBody);

        using var stillValid = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = ruleRejectToken, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = ruleAf },
            antiForgery: ruleAf));
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_012_And_004_NoSessionUntilTotpEnrolled()
    {
        await ResetAsync();
        var client = CreateClient();
        var (token, af) = await IssueReadyAsync(client);
        using var set = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        AssertNoSessionCookie(set);

        using var me = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/me"));
        await AssertGenericUnauthorized(me);
    }

    [Fact]
    public async Task TD_ADM_040_TurnstileRequiredOnBootstrap_BeforePasswordCheck()
    {
        await ResetAsync();
        var client = CreateClient();
        var (token, af) = await IssueReadyAsync(client);
        var password = NewPassword();

        async Task<HttpResponseMessage> Post(string? turnstile)
            => await client.SendAsync(AdminRequest(
                HttpMethod.Post,
                AdminSignedOutAccess.BootstrapApi,
                new { token, password, turnstileToken = turnstile, antiForgeryToken = af },
                antiForgery: af));

        using var missing = await Post(null);
        using var invalid = await Post("forged");
        _factory.Turnstile.Unavailable = true;
        using var unavailable = await Post(FakeTurnstileVerifier.ValidToken);
        _factory.Turnstile.Unavailable = false;

        foreach (var response in new[] { missing, invalid, unavailable })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains(AdminAuthMessages.TurnstileFailed, body);
            Assert.DoesNotContain(CoreOwnerEmail, body);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.False(db.AdminCredentials.Any(c => c.PasswordHash != null));
            Assert.Empty(db.AdminBootstrapIpThrottles.ToList());
            var captcha = db.AdminAuditLog.Where(e => e.ReasonClass == AdminAuthMessages.CaptchaFailedReason).ToList();
            Assert.True(captcha.Count >= 3);
            Assert.All(captcha, row =>
            {
                Assert.False(string.IsNullOrEmpty(row.IpHmac));
                Assert.DoesNotContain("forged", row.IpHmac);
                Assert.DoesNotContain("ok", row.AfterSnapshot ?? string.Empty);
            });
        }

        using var inspect = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.BootstrapPage));
        Assert.Equal(HttpStatusCode.OK, inspect.StatusCode);

        using var valid = await Post(FakeTurnstileVerifier.ValidToken);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_010_RouteExemption_ExtraSegmentWrongMethodMissingToken()
    {
        await ResetAsync();
        var client = CreateClient();
        var token = await IssueTokenAsync();
        var af = await IssueAntiForgeryAsync(client);

        using var extra = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap/extra?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(extra);

        using var wrongMethod = await client.SendAsync(AdminRequest(
            HttpMethod.Put,
            $"{AdminSignedOutAccess.BootstrapApi}?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(wrongMethod);

        using var getNoToken = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.BootstrapPage));
        Assert.Equal(HttpStatusCode.OK, getNoToken.StatusCode);

        using var apiMissing = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(apiMissing);

        using var queryTokenPost = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            $"{AdminSignedOutAccess.BootstrapApi}?token={Uri.EscapeDataString(token)}",
            new { password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        await AssertGoesToLinkExpired(queryTokenPost);

        using var expiredPage = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.LinkExpired));
        Assert.Equal(HttpStatusCode.OK, expiredPage.StatusCode);
        var expiredBody = await expiredPage.Content.ReadAsStringAsync();
        Assert.Contains(AdminAuthMessages.InvalidOrExpiredLink, expiredBody);

        using var trailing = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap/?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(trailing);

        using var encoded = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap%2fextra?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(encoded);

        using var dots = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap/..?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(dots);

        using var matrix = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap;x?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(matrix);

        using var missingAf = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken }));
        await AssertGoesToLinkExpired(missingAf);

        using var wrongHost = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.BootstrapPage,
            host: "core.dealoware.com"));
        Assert.Equal(HttpStatusCode.NotFound, wrongHost.StatusCode);
        var wrongHostBody = await wrongHost.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(wrongHostBody));
        Assert.DoesNotContain("Unauthorized", wrongHostBody);

        using var authStatic = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            "/admin/auth/placeholder.css"));
        Assert.Equal(HttpStatusCode.OK, authStatic.StatusCode);

        using var getTwice = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(token)}"));
        Assert.Equal(HttpStatusCode.OK, getTwice.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.All(db.AdminBootstrapTokens.ToList(), t => Assert.Null(t.ConsumedAt));
        }

        using var stillWorks = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);

        var cookie = AdminSessionCookie.CreateOptions();
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Strict, cookie.SameSite);
        Assert.Equal("/admin", cookie.Path);
        Assert.Null(cookie.Domain);
        Assert.Equal("dw_admin_session", AdminSessionCookie.Name);
        Assert.Equal("dw_admin_pending", AdminSessionCookie.PendingName);
        Assert.Equal("dw_admin_af", AdminAntiForgery.CookieName);
        Assert.DoesNotContain("__Host-", AdminSessionCookie.Name);
        Assert.DoesNotContain("__Host-", AdminAntiForgery.CookieName);
    }

    [Fact]
    public async Task SC6_BootstrapIpThrottle_IsDatabaseAtomic_AndIgnoresSpoofedLeftMostXff()
    {
        await ResetAsync();
        var client = CreateClient();
        var af = await IssueAntiForgeryAsync(client);
        var password = NewPassword();

        async Task<HttpResponseMessage> Guess(string xff, string guess)
            => await client.SendAsync(AdminRequest(
                HttpMethod.Post,
                AdminSignedOutAccess.BootstrapApi,
                new { token = guess, password, turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
                antiForgery: af,
                forwardedFor: xff));

        for (var i = 0; i < AdminBootstrapIpThrottle.AttemptLimit - 1; i++)
        {
            var spoof = i % 2 == 0 ? "198.51.100.1, " + TrustedClient : "198.51.100.99, " + TrustedClient;
            using var fail = await Guess(spoof, $"guess-{i}");
            await AssertGoesToLinkExpired(fail);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
            var expectedKey = hasher.Hash(TrustedClient);
            var row = Assert.Single(db.AdminBootstrapIpThrottles.ToList());
            Assert.Equal(expectedKey, row.IpKey);
            Assert.Equal(AdminBootstrapIpThrottle.AttemptLimit - 1, row.AttemptCount);
            Assert.Null(row.LockedUntil);
            Assert.DoesNotContain("198.51.100", row.IpKey);
            Assert.DoesNotContain(TrustedClient, row.IpKey);
        }

        var liveToken = await IssueTokenAsync();
        using var twentieth = await Guess("203.0.113.1, " + TrustedClient, "guess-last");
        Assert.Equal(HttpStatusCode.TooManyRequests, twentieth.StatusCode);
        Assert.Null(twentieth.Headers.RetryAfter);
        var throttledBody = await twentieth.Content.ReadAsStringAsync();
        Assert.Contains(AdminAuthMessages.InvalidOrExpiredLink, throttledBody);
        Assert.DoesNotContain("locked", throttledBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(CoreOwnerEmail, throttledBody);

        using var whileThrottled = await Guess(TrustedClient, liveToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, whileThrottled.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = Assert.Single(db.AdminBootstrapTokens.ToList());
            Assert.Null(row.ConsumedAt);
            var throttle = Assert.Single(db.AdminBootstrapIpThrottles.ToList());
            Assert.Equal(AdminBootstrapIpThrottle.AttemptLimit, throttle.AttemptCount);
            Assert.NotNull(throttle.LockedUntil);
        }

        using var otherIp = await Guess("198.51.100.50", liveToken);
        Assert.Equal(HttpStatusCode.OK, otherIp.StatusCode);
    }

    [Fact]
    public async Task SC6_TurnstileAndPasswordRule_DoNotIncrementBootstrapIpThrottle()
    {
        await ResetAsync();
        var client = CreateClient();
        var (token, af) = await IssueReadyAsync(client);

        for (var i = 0; i < 5; i++)
        {
            using var captcha = await client.SendAsync(AdminRequest(
                HttpMethod.Post,
                AdminSignedOutAccess.BootstrapApi,
                new { token, password = NewPassword(), turnstileToken = "forged", antiForgeryToken = af },
                antiForgery: af));
            Assert.Equal(HttpStatusCode.BadRequest, captcha.StatusCode);
        }

        using var tooShort = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = "short", turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Empty(db.AdminBootstrapIpThrottles.ToList());
        }

        using var ok = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public void SC6_BootstrapThrottle_IsNotProcessMemory()
    {
        var root = RepoRoot();
        var service = File.ReadAllText(Path.Combine(root, "src", "Dealoware.Infrastructure", "Admin", "AdminBootstrapService.cs"));
        Assert.DoesNotContain("ConcurrentDictionary", service, StringComparison.Ordinal);
        Assert.DoesNotContain("MemoryCache", service, StringComparison.Ordinal);
        Assert.DoesNotContain("static int", service, StringComparison.Ordinal);
        Assert.Contains("ExecuteUpdateAsync", service, StringComparison.Ordinal);
        Assert.Contains("AdminBootstrapIpThrottles", service, StringComparison.Ordinal);
    }

    private static void AssertNoSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return;
        }

        Assert.DoesNotContain(cookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
    }

    [Fact]
    public void BootstrapFactory_UsesRecordingMailSender_Only()
    {
        var sender = _factory.Services.GetRequiredService<IAdminMailSender>();
        Assert.IsType<RecordingMailSender>(sender);
        Assert.Same(sender, _factory.Mail);
        Assert.Empty(typeof(BootstrapWebApplicationFactory).Assembly
            .GetTypes()
            .Where(t => t.Name.Contains("CapturingAdminMail", StringComparison.Ordinal)
                        || t.Name.Contains("NoOpAdminMail", StringComparison.Ordinal)));

        var root = RepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "src", "Dealoware.Api", "Admin", "AdminBootstrapEndpoints.cs"));
        Assert.DoesNotContain("AddSingleton<IAdminMailSender>", endpoints);
        Assert.DoesNotContain("NoOpAdminMailSender", endpoints);
        var support = File.ReadAllText(Path.Combine(root, "tests", "Dealoware.Api.Tests", "BootstrapTestSupport.cs"));
        Assert.DoesNotContain("CapturingAdminMailSender", support);
        Assert.DoesNotContain("AddSingleton<IAdminMailSender>", support);
    }

    [Fact]
    public void TD_ADM_150_NoSecretsInBootstrapSource()
    {
        var root = RepoRoot();
        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}"))
            .ToList();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AKIA", text);
            Assert.DoesNotContain("aws_secret_access_key", text, StringComparison.OrdinalIgnoreCase);
        }

        var options = File.ReadAllText(Path.Combine(root, "src", "Dealoware.Infrastructure", "Admin", "TurnstileOptions.cs"));
        Assert.Contains("DEALOWARE_TURNSTILE_SECRET", options);
        Assert.DoesNotContain("1x0000000000000000000000000000000AA", options);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }
}

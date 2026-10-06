using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

[Collection("BootstrapTests")]
public class AdminBootstrapTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

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
        db.AdminAuditLog.RemoveRange(db.AdminAuditLog);
        await db.SaveChangesAsync();
        _factory.Mail.Sent.Clear();
        _factory.Turnstile.Unavailable = false;
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

    private static string NewPassword()
        => "radio station " + Guid.NewGuid().ToString("N")[..8];

    private async Task<string> IssueTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
        var issued = await bootstrap.IssueLinkAsync("testhmac");
        Assert.True(issued.Sent);
        return TokenFromLink(_factory.Mail.LastTextBody!);
    }

    private static string TokenFromLink(string link)
    {
        var uri = new Uri(link);
        Assert.StartsWith("https://admin.core.dealoware.com/admin/bootstrap", uri.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(uri.Query);
        var token = query["token"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private async Task<(string Token, string AntiForgery)> IssueReadyAsync(HttpClient client)
    {
        var token = await IssueTokenAsync();
        using var get = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(token)}"));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var json = await get.Content.ReadFromJsonAsync<JsonElement>();
        var af = json.GetProperty("antiForgeryToken").GetString();
        Assert.False(string.IsNullOrEmpty(af));
        return (token, af!);
    }

    private static HttpRequestMessage AdminRequest(
        HttpMethod method,
        string path,
        object? body = null,
        string host = AdminHost,
        string? antiForgery = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = host;
        if (antiForgery is not null)
        {
            request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, antiForgery);
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
        Assert.False(first.Headers.TryGetValues("Set-Cookie", out _));

        using var reuse = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        var reuseBody = await AssertGenericUnauthorized(reuse);

        var expiredToken = await IssueFreshTokenAfterResetAsync();
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddHours(24).AddMinutes(1);
        using var expired = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = expiredToken, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        var expiredBody = await AssertGenericUnauthorized(expired);

        using var random = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = "truncated-or-random", password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken, antiForgeryToken = af },
            antiForgery: af));
        var randomBody = await AssertGenericUnauthorized(random);

        Assert.Equal(reuseBody, expiredBody);
        Assert.Equal(expiredBody, randomBody);
        Assert.DoesNotContain("password", _factory.Mail.LastTextBody!, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> IssueFreshTokenAfterResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminCredentials.RemoveRange(db.AdminCredentials);
        await db.SaveChangesAsync();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
        var issued = await bootstrap.IssueLinkAsync("testhmac");
        Assert.True(issued.Sent);
        return TokenFromLink(_factory.Mail.LastTextBody!);
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

        var token = await IssueTokenAsync();
        var password = NewPassword();
        using var set = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password, turnstileToken = FakeTurnstileVerifier.ValidToken }));
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
        using var tooShort = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token = ruleRejectToken, password = "short", turnstileToken = FakeTurnstileVerifier.ValidToken }));
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        var shortBody = await tooShort.Content.ReadAsStringAsync();
        Assert.Contains(AdminAuthMessages.PasswordTooShort, shortBody);

        using var stillValid = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(ruleRejectToken)}"));
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_012_And_004_NoSessionUntilTotpEnrolled()
    {
        await ResetAsync();
        var client = CreateClient();
        var token = await IssueTokenAsync();
        using var set = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { token, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken }));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.False(set.Headers.Contains("Set-Cookie"));

        using var me = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/me"));
        await AssertGenericUnauthorized(me);
    }

    [Fact]
    public async Task TD_ADM_040_TurnstileRequiredOnBootstrap_BeforePasswordCheck()
    {
        await ResetAsync();
        var client = CreateClient();
        var token = await IssueTokenAsync();
        var password = NewPassword();

        async Task<HttpResponseMessage> Post(string? turnstile)
            => await client.SendAsync(AdminRequest(
                HttpMethod.Post,
                AdminSignedOutAccess.BootstrapApi,
                new { token, password, turnstileToken = turnstile }));

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

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.False(db.AdminCredentials.Any(c => c.PasswordHash != null));
        var captcha = db.AdminAuditLog.Where(e => e.ReasonClass == AdminAuthMessages.CaptchaFailedReason).ToList();
        Assert.True(captcha.Count >= 3);
        Assert.All(captcha, row =>
        {
            Assert.False(string.IsNullOrEmpty(row.IpHmac));
            Assert.DoesNotContain("forged", row.IpHmac);
            Assert.DoesNotContain("ok", row.AfterSnapshot ?? string.Empty);
        });

        using var inspect = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(token)}"));
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

        using var extra = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"/admin/bootstrap/extra?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(extra);

        using var wrongMethod = await client.SendAsync(AdminRequest(
            HttpMethod.Put,
            $"{AdminSignedOutAccess.BootstrapApi}?token={Uri.EscapeDataString(token)}"));
        await AssertGenericUnauthorized(wrongMethod);

        using var missing = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.BootstrapPage));
        await AssertGenericUnauthorized(missing);

        using var apiMissing = await client.SendAsync(AdminRequest(
            HttpMethod.Post,
            AdminSignedOutAccess.BootstrapApi,
            new { password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken }));
        await AssertGenericUnauthorized(apiMissing);

        using var pageOk = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            $"{AdminSignedOutAccess.BootstrapPage}?token={Uri.EscapeDataString(token)}"));
        Assert.Equal(HttpStatusCode.OK, pageOk.StatusCode);

        using var expiredPage = await client.SendAsync(AdminRequest(
            HttpMethod.Get,
            AdminSignedOutAccess.LinkExpired));
        Assert.Equal(HttpStatusCode.OK, expiredPage.StatusCode);
        var expiredBody = await expiredPage.Content.ReadAsStringAsync();
        Assert.Contains(AdminAuthMessages.InvalidOrExpiredLink, expiredBody);

        var cookie = AdminSessionCookie.CreateOptions();
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Strict, cookie.SameSite);
        Assert.Equal("/admin", cookie.Path);
        Assert.Null(cookie.Domain);
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

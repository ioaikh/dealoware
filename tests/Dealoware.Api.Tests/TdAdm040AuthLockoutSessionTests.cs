using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Http;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-040..054 plus r2 §2.2 session-gate rows this step owns.
/// </summary>
[Collection("AdminAuthTests")]
public class TdAdm040AuthLockoutSessionTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = "io@aiknowhow.com";
    private const string SignInBody = AdminAuthCopy.SignInFailure;
    private const string CaptchaBody = AdminAuthCopy.VerificationFailed;

    private readonly IsolatedWebApplicationFactory _factory;
    private readonly string _password;
    private readonly string _totp = "246813";

    public TdAdm040AuthLockoutSessionTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
        _password = "Pw-" + Guid.NewGuid().ToString("N") + "-xx";
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        directory.SetPasswordAsync(OwnerEmail, _password, CancellationToken.None).GetAwaiter().GetResult();
        directory.SeedTotpCode(_totp);
        ResetClock();
    }

    [Fact]
    public async Task TdAdm040_MissingTurnstile_RejectsBeforeCredential_NoCounter_AuditsCaptchaFailed()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(CaptchaJson(), await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("Retry-After"));
        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
        Assert.Contains(await AuditAsync(), e => e.ReasonClass == AdminAuthReason.CaptchaFailed);
        Assert.DoesNotContain(await AuditAsync(), e => e.IpHmac == "127.0.0.1");
    }

    [Fact]
    public async Task TdAdm040_InvalidAndUnavailableTurnstile_FailClosed()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var fake = _factory.Services.GetRequiredService<FakeTurnstileVerifier>();

        using var invalid = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = "expired" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(CaptchaJson(), await invalid.Content.ReadAsStringAsync());

        fake.Unavailable = true;
        try
        {
            using var down = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, down.StatusCode);
            Assert.Equal(CaptchaJson(), await down.Content.ReadAsStringAsync());
        }
        finally
        {
            fake.Unavailable = false;
        }

        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
    }

    [Fact]
    public void TdAdm041_NoNonTurnstileCaptchaProviders()
    {
        var root = RepoRoot();
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("recaptcha", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hcaptcha", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WafCaptcha", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TdAdm050_FiveFailuresLockAccount_Generic401_InLockNotCounted()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var failureBody = string.Empty;
        for (var i = 0; i < 5; i++)
        {
            using var baseline = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong-" + i, turnstileToken = FakeTurnstileVerifier.ValidToken });
            failureBody = await baseline.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, baseline.StatusCode);
            Assert.Equal(SignInJson(), failureBody);
            Assert.False(baseline.Headers.Contains("Retry-After"));
            Assert.DoesNotContain("locked", failureBody, StringComparison.OrdinalIgnoreCase);
        }

        using var correctWhileLocked = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, correctWhileLocked.StatusCode);
        Assert.Equal(failureBody, await correctWhileLocked.Content.ReadAsStringAsync());

        var counted = await CountFailuresAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant());
        Assert.Equal(5, counted);
        Assert.NotNull(await ActiveLockAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        Assert.Contains(await AuditAsync(), e => e.Action == AdminAuthAction.LockStart);
        Assert.Contains(await AuditAsync(), e => e.ReasonClass == AdminAuthReason.Locked);
    }

    [Fact]
    public async Task TdAdmSc5_AccountLock_ConsumesPending_EndsSessions()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var sessionId = await SignInFullAsync(client);
        using var step1 = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        Assert.False(string.IsNullOrEmpty(pending));

        for (var i = 0; i < 5; i++)
        {
            using var fail = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong-" + i, turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        Assert.NotNull(await ActiveLockAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        using var oldSession = await AdminGetAsync(client, "/admin/api/me", sessionId);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        using var code = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInCodePath,
            new { totpCode = _totp, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminSessionExemptions.PendingCookieName}={pending}");
        Assert.Equal(HttpStatusCode.Unauthorized, code.StatusCode);
    }

    [Fact]
    public async Task TdAdm050_SlidingWindow_EventExactly15MinOldIsOutside()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var clock = _factory.Services.GetRequiredService<TestAdminClock>();
        for (var i = 0; i < 4; i++)
        {
            using var fail = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        clock.Advance(TimeSpan.FromMinutes(15));
        using var fifth = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Null(await ActiveLockAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));

        for (var i = 0; i < 4; i++)
        {
            using var more = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, more.StatusCode);
        }

        Assert.NotNull(await ActiveLockAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public async Task TdAdm051_TwentyLoginFailures_Throttle429_NoRetryAfter()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        for (var i = 0; i < 20; i++)
        {
            using var fail = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = "unknown" + i + "@example.com", password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var throttled = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal(SignInJson(), await throttled.Content.ReadAsStringAsync());
        Assert.False(throttled.Headers.Contains("Retry-After"));
        Assert.Contains(await AuditAsync(), e => e.ReasonClass == AdminAuthReason.RateLimited);
    }

    [Fact]
    public async Task TdAdm052_SessionIdleAbsoluteAndRotation()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var clock = _factory.Services.GetRequiredService<TestAdminClock>();
        var firstId = await SignInFullAsync(client);
        var options = AdminSessionCookie.CreateOptions();
        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/admin", options.Path);
        Assert.Null(options.Domain);

        clock.Advance(TimeSpan.FromMinutes(31));
        using var idle = await AdminGetAsync(client, "/admin/api/me", firstId);
        Assert.Equal(HttpStatusCode.Unauthorized, idle.StatusCode);

        ResetClock();
        var rotated = await SignInFullAsync(client);
        Assert.NotEqual(firstId, rotated);
        using var oldDenied = await AdminGetAsync(client, "/admin/api/me", firstId);
        Assert.Equal(HttpStatusCode.Unauthorized, oldDenied.StatusCode);
        using var newOk = await AdminGetAsync(client, "/admin/api/me", rotated);
        Assert.Equal(HttpStatusCode.OK, newOk.StatusCode);

        clock.Advance(TimeSpan.FromHours(8));
        using var absolute = await AdminGetAsync(client, "/admin/api/me", rotated);
        Assert.Equal(HttpStatusCode.Unauthorized, absolute.StatusCode);
    }

    [Fact]
    public async Task TdAdm053_AuditIpIsKeyedHmac_RawIpOnlyInCounters()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var fail = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });

        var audit = await AuditAsync();
        Assert.All(audit, e =>
        {
            Assert.NotEqual("127.0.0.1", e.IpHmac);
            Assert.DoesNotContain("127.0.0.1", e.IpHmac);
        });
        Assert.True(await CountFailuresAsync(AdminAuthScopes.LoginIp, "127.0.0.1") >= 1);
    }

    [Fact]
    public async Task TdAdmSc6_SpoofedLeftMostXff_DoesNotChangeIpKey()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var spoofed = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken },
            forwardedFor: "203.0.113.88, 127.0.0.1");
        Assert.Equal(HttpStatusCode.Unauthorized, spoofed.StatusCode);
        Assert.Equal(1, await CountFailuresAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.LoginIp, "203.0.113.88"));
        Assert.Equal(1, await CountFailuresAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public void TdAdm054_NoProcessGlobalAuthFloodLimiter()
    {
        var names = typeof(Program).Assembly.GetTypes().Select(t => t.Name);
        Assert.DoesNotContain(names, n => n.Contains("ProcessGlobal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TdAdmSessionGate_SignInPost_IsExemptWithoutSession()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("second_factor", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmSessionGate_WrongMethod_IsDenied()
    {
        var client = CreateClient();
        using var response = await SendAuthAsync(client, HttpMethod.Get, AdminAuthEndpoints.SignInPath, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Unauthorized", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmSessionGate_ExtraSegment_IsDenied()
    {
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath + "/extra",
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Unauthorized", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmSessionGate_MissingPendingToken_IsDenied()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInCodePath,
            new { totpCode = _totp, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Unauthorized", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmC1_TrailingSlash_IsNotExempt()
    {
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath + "/",
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Unauthorized", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmC3_GetSignIn_HasNoLockoutSideEffect()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var get = await SendAuthAsync(client, HttpMethod.Get, "/admin/sign-in", null);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
        Assert.False(string.IsNullOrEmpty(ReadCookie(get, AdminAntiForgery.CookieName)));
        Assert.DoesNotContain("__Host-", ReadCookie(get, AdminAntiForgery.CookieName) ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task TdAdmC3_PostWithoutAntiForgery_IsDenied()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, AdminAuthEndpoints.SignInPath);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        request.Content = JsonContent.Create(new
        {
            email = OwnerEmail,
            password = _password,
            turnstileToken = FakeTurnstileVerifier.ValidToken
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public void TdAdmC5_CookieNames_HaveNoHostPrefix()
    {
        Assert.Equal("dw_admin_session", AdminSessionCookie.Name);
        Assert.Equal("dw_admin_pending", AdminSessionExemptions.PendingCookieName);
        Assert.Equal("dw_admin_af", AdminAntiForgery.CookieName);
        Assert.DoesNotContain("__Host-", AdminSessionCookie.Name, StringComparison.Ordinal);
        Assert.Null(AdminSessionCookie.CreateOptions().Domain);
    }

    [Fact]
    public void TdAdmC6_ReturnPathAllowlist()
    {
        Assert.True(AdminReturnPath.TryValidate("/admin/", out _));
        Assert.True(AdminReturnPath.TryValidate("/admin/participants", out _));
        Assert.Equal("/admin/", AdminReturnPath.Resolve("/admin/sign-in"));
        Assert.Equal("/admin/", AdminReturnPath.Resolve("/admin/setup/authenticator"));
        Assert.Equal("/admin/", AdminReturnPath.Resolve("https://evil.example/"));
        Assert.StartsWith("https://admin.core.dealoware.com/admin/", AdminReturnPath.ToFixedOriginLocation(AdminReturnPath.Resolve("/nope")));
    }

    [Fact]
    public async Task TdAdmSessionGate_QueryStringDoesNotCreateExemption()
    {
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client, HttpMethod.Post, "/admin/api/me?next=/admin/sign-in", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TdAdmSessionGate_ResetConfirmLinkExpiredAndBootstrap_GetIsNotUnauthorized()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        foreach (var path in new[]
                 {
                     AdminSignedOutExemptions.LinkExpired,
                     AdminSignedOutExemptions.ResetConfirm,
                     AdminSignedOutExemptions.Bootstrap
                 })
        {
            using var get = await SendAuthAsync(client, HttpMethod.Get, path, null);
            Assert.NotEqual(HttpStatusCode.Unauthorized, get.StatusCode);
        }
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private void ResetClock()
    {
        _factory.Services.GetRequiredService<TestAdminClock>().UtcNow = DateTimeOffset.UtcNow;
    }

    private async Task ClearAuthStateAsync()
    {
        ResetClock();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
        db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
        db.AdminAuthTokens.RemoveRange(db.AdminAuthTokens);
        db.AdminAuditLog.RemoveRange(db.AdminAuditLog);
        db.AdminSessions.RemoveRange(db.AdminSessions);
        await db.SaveChangesAsync();
    }

    private async Task<int> CountFailuresAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        var auth = scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>();
        return await auth.CountFailuresAsync(scope, key, CancellationToken.None);
    }

    private async Task<AdminAuthLockout?> ActiveLockAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        var auth = scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>();
        return await auth.GetActiveLockoutAsync(scope, key, CancellationToken.None);
    }

    private async Task<List<AdminAuditEntry>> AuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return db.AdminAuditLog.AsEnumerable().OrderBy(e => e.Timestamp).ToList();
    }

    private async Task<Guid> SignInFullAsync(HttpClient client)
    {
        using var step1 = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        using var step2 = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInCodePath,
            new { totpCode = _totp, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminSessionExemptions.PendingCookieName}={pending}");
        step2.EnsureSuccessStatusCode();
        return Guid.Parse(ReadCookie(step2, AdminSessionCookie.Name)!);
    }

    private static async Task<(string Cookie, string Token)> IssueAntiForgeryAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/sign-in");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var token = response.Headers.TryGetValues(AdminAntiForgery.HeaderName, out var values)
            ? values.First()
            : string.Empty;
        var cookie = ReadCookie(response, AdminAntiForgery.CookieName) ?? string.Empty;
        return (cookie, token);
    }

    private async Task<HttpResponseMessage> SendAuthAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body,
        string? extraCookie = null,
        string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor ?? "127.0.0.1");
        if (HttpMethod.Post.Equals(method))
        {
            var (afCookie, afToken) = await IssueAntiForgeryAsync(client);
            request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, afToken);
            var cookies = $"{AdminAntiForgery.CookieName}={afCookie}";
            if (!string.IsNullOrEmpty(extraCookie))
            {
                cookies += $"; {extraCookie}";
            }

            request.Headers.TryAddWithoutValidation("Cookie", cookies);
        }
        else if (!string.IsNullOrEmpty(extraCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", extraCookie);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> AdminGetAsync(HttpClient client, string path, Guid sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
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

    private static string SignInJson() => $$"""{"error":"{{SignInBody}}"}""";

    private static string CaptchaJson() => $$"""{"error":"{{CaptchaBody}}"}""";

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

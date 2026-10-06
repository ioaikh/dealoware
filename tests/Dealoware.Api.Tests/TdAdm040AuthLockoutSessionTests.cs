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
    private const string SignInBody =
        "We couldn't sign you in. Check your details and try again later. You can also reset your password.";
    private const string CaptchaBody = "Verification failed, please try again.";

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
                client, HttpMethod.Post, AdminAuthEndpoints.ResetPath,
                new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, down.StatusCode);
            Assert.Equal(CaptchaJson(), await down.Content.ReadAsStringAsync());
        }
        finally
        {
            fake.Unavailable = false;
        }

        Assert.Equal(0, await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1"));
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
    public async Task TdAdm050_ResetDoesNotLiftLock()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        for (var i = 0; i < 5; i++)
        {
            using var fail = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        }

        using var reset = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.NotNull(await ActiveLockAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));

        using var stillLocked = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, stillLocked.StatusCode);
        Assert.Equal(SignInJson(), await stillLocked.Content.ReadAsStringAsync());
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
    public async Task TdAdm051_ResetThrottle_SameBody_NoMail()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var mailer = _factory.Services.GetRequiredService<RecordingAdminMailSender>();
        var before = mailer.SendCount;
        for (var i = 0; i < 20; i++)
        {
            using var req = await SendAuthAsync(
                client, HttpMethod.Post, AdminAuthEndpoints.ResetPath,
                new { email = "other" + i + "@example.com", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.OK, req.StatusCode);
        }

        using var throttled = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal(ResetJson(), await throttled.Content.ReadAsStringAsync());
        Assert.False(throttled.Headers.Contains("Retry-After"));
        Assert.Equal(before, mailer.SendCount);
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
        Assert.Equal(HttpStatusCode.NoContent, get.StatusCode);
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
        Assert.StartsWith("https://admin.core.dealoware.com/admin/", AdminReturnPath.ToLocation("/nope"));
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
    public async Task TdAdmSessionGate_ResetAndBootstrapPaths_MatchR2()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        using var reset = await SendAuthAsync(
            client, HttpMethod.Post, "/admin/api/auth/reset",
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        using var bootstrapMissing = await SendAuthAsync(
            client, HttpMethod.Post, "/admin/api/auth/bootstrap",
            new { password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, bootstrapMissing.StatusCode);
        Assert.Equal(InvalidLinkJson(), await bootstrapMissing.Content.ReadAsStringAsync());

        using var confirmMissing = await SendAuthAsync(
            client, HttpMethod.Post, "/admin/api/auth/reset/confirm",
            new { password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, confirmMissing.StatusCode);
        Assert.Equal(InvalidLinkJson(), await confirmMissing.Content.ReadAsStringAsync());
        Assert.True(await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1") >= 1);
        Assert.Contains(await AuditAsync(), e => e.ReasonClass == AdminAuthReason.InvalidLink);
    }

    [Fact]
    public async Task TdAdmC4_GetConfirmAndBootstrap_TakeNoToken_NoStoreNoReferrer()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var raw = await RequestResetTokenAsync(client);
        var ipCountAfterIssue = await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1");

        foreach (var path in new[] { "/admin/reset/confirm", "/admin/bootstrap", "/admin/reset/confirm?token=" + raw })
        {
            using var get = await SendAuthAsync(client, HttpMethod.Get, path, null);
            Assert.Equal(HttpStatusCode.NoContent, get.StatusCode);
            Assert.True(
                get.Headers.TryGetValues("Cache-Control", out var cache)
                && cache.Any(v => v.Contains("no-store", StringComparison.OrdinalIgnoreCase)));
            Assert.True(get.Headers.TryGetValues("Referrer-Policy", out var referrer));
            Assert.Contains(referrer, v => string.Equals(v, "no-referrer", StringComparison.OrdinalIgnoreCase));
            Assert.True(string.IsNullOrEmpty(ReadCookie(get, AdminSessionCookie.Name))
                        || ReadCookie(get, AdminSessionCookie.Name) == string.Empty);
            Assert.True(string.IsNullOrEmpty(ReadCookie(get, AdminSessionExemptions.PendingCookieName)));
        }

        Assert.Equal(ipCountAfterIssue, await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1"));
        using var confirm = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
    }

    [Fact]
    public async Task TdAdmC4_QueryStringToken_IsIgnoredOnPost()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var raw = await RequestResetTokenAsync(client);
        using var queryOnly = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath + "?token=" + raw,
            new { password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, queryOnly.StatusCode);
        Assert.Equal(InvalidLinkJson(), await queryOnly.Content.ReadAsStringAsync());

        using var bodyOk = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, bodyOk.StatusCode);
        Assert.True(string.IsNullOrEmpty(ReadCookie(bodyOk, AdminSessionCookie.Name)));
    }

    [Fact]
    public async Task TdAdmC4_UnknownUsedExpiredWrongAccount_SameGeneric_CountsIp_NoTokenInAudit()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var raw = await RequestResetTokenAsync(client);
        var unknown = AdminAuthService.CreateOpaqueToken();

        using var unknownResponse = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = unknown, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await unknownResponse.Content.ReadAsStringAsync());

        using var wrongAccount = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, email = "other@example.com", password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await wrongAccount.Content.ReadAsStringAsync());

        using var wrongKind = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.BootstrapPath,
            new { token = raw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await wrongKind.Content.ReadAsStringAsync());

        using var ok = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        using var used = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await used.Content.ReadAsStringAsync());
        Assert.True(await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1") >= 4);

        var expiredRaw = await RequestResetTokenAsync(client);
        _factory.Services.GetRequiredService<TestAdminClock>().Advance(TimeSpan.FromMinutes(30));
        using var expired = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = expiredRaw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await expired.Content.ReadAsStringAsync());
        Assert.True(await CountFailuresAsync(AdminAuthScopes.ResetIp, "127.0.0.1") >= 1);
        foreach (var entry in await AuditAsync())
        {
            Assert.DoesNotContain(raw, entry.Action, StringComparison.Ordinal);
            Assert.DoesNotContain(raw, entry.ActorEmail ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(raw, entry.IpHmac ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(raw, entry.ReasonClass ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(raw, entry.BeforeSnapshot ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(raw, entry.AfterSnapshot ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain(unknown, entry.BeforeSnapshot ?? "", StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task TdAdmC4_NewResetToken_CancelsEarlier_AndMailUsesFragmentOnly()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var first = await RequestResetTokenAsync(client);
        var second = await RequestResetTokenAsync(client);
        var mailer = _factory.Services.GetRequiredService<RecordingAdminMailSender>();
        Assert.StartsWith(AdminAuthLinks.ResetConfirm(second), mailer.Last!.TextBody);
        Assert.DoesNotContain("?token=", mailer.Last.TextBody, StringComparison.Ordinal);
        Assert.True(second.Length >= 32);

        using var old = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = first, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await old.Content.ReadAsStringAsync());

        using var next = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = second, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
    }

    [Fact]
    public async Task TdAdmC4_ResetSetsPasswordOnly_EndsSessions_NoSessionIssued()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var sessionId = await SignInFullAsync(client);
        var raw = await RequestResetTokenAsync(client);
        var newPassword = "Pw-" + Guid.NewGuid().ToString("N") + "-yy";

        using var confirm = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, password = newPassword, totpCode = _totp, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.True(string.IsNullOrEmpty(ReadCookie(confirm, AdminSessionCookie.Name)));
        using var oldSession = await AdminGetAsync(client, "/admin/api/me", sessionId);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        using var step1 = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = newPassword, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        Assert.Contains("second_factor", await step1.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdmC4_ResetExpiresAt30Minutes_BootstrapAt24Hours()
    {
        await ClearAuthStateAsync();
        var client = CreateClient();
        var clock = _factory.Services.GetRequiredService<TestAdminClock>();
        var resetRaw = await RequestResetTokenAsync(client);
        clock.Advance(TimeSpan.FromMinutes(29));
        using var stillValid = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = resetRaw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);

        var expiredRaw = await RequestResetTokenAsync(client);
        clock.Advance(TimeSpan.FromMinutes(30));
        using var expired = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetConfirmPath,
            new { token = expiredRaw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await expired.Content.ReadAsStringAsync());

        var bootstrapRaw = AdminAuthService.CreateOpaqueToken();
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AdminAuthService>()
                .IssueLinkAsync(AdminAuthToken.KindBootstrap, OwnerEmail, bootstrapRaw, CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromHours(24));
        using var bootstrapExpired = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.BootstrapPath,
            new { token = bootstrapRaw, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(InvalidLinkJson(), await bootstrapExpired.Content.ReadAsStringAsync());
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
        string? extraCookie = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
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

    private async Task<string> RequestResetTokenAsync(HttpClient client)
    {
        using var reset = await SendAuthAsync(
            client, HttpMethod.Post, AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        reset.EnsureSuccessStatusCode();
        var mailer = _factory.Services.GetRequiredService<RecordingAdminMailSender>();
        var token = AdminAuthLinks.TryReadFragmentToken(mailer.Last?.TextBody);
        Assert.False(string.IsNullOrEmpty(token));
        Assert.True(token!.Length >= 32);
        return token;
    }

    private static string SignInJson() => JsonSerializer.Serialize(new { error = SignInBody });

    private static string CaptchaJson() => JsonSerializer.Serialize(new { error = CaptchaBody });

    private static string ResetJson() => JsonSerializer.Serialize(new { error = AdminAuthCopy.ResetExists });

    private static string InvalidLinkJson() => JsonSerializer.Serialize(new { error = AdminAuthCopy.InvalidLink });

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

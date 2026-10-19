using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Application.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

[Collection("WebAppTests")]
public class AdminPasswordResetTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string CurrentPassword = "radio station prior!";
    private const string NextPassword = "radio station later!";

    private readonly IsolatedWebApplicationFactory _factory;
    private byte[]? _totpSecret;
    private IReadOnlyList<string>? _recoveryCodes;

    public AdminPasswordResetTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TD_ADM_030_Request_SameGenericReply_KnownUnknownLocked_NoEnumeration()
    {
        await SeedCredentialAsync();
        Mail().Clear();

        var known = await PostRequestAsync(CoreOwnerEmail);
        var unknown = await PostRequestAsync("nobody@example.com");
        var locked = await PostRequestAsync(CoreOwnerEmail);

        var knownBody = await known.Content.ReadAsStringAsync();
        var unknownBody = await unknown.Content.ReadAsStringAsync();
        var lockedBody = await locked.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(known.StatusCode, locked.StatusCode);
        Assert.Equal(knownBody, unknownBody);
        Assert.Equal(knownBody, lockedBody);
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, knownBody);
        Assert.DoesNotContain("locked", knownBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", knownBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(CoreOwnerEmail, knownBody);

        await Mail().WaitForSentAsync(2);
        var sent = Mail().Sent;
        Assert.Equal(2, sent.Count);
        Assert.All(sent, item =>
        {
            Assert.Equal(CoreOwnerEmail, item.To);
            Assert.Equal(AdminMailDispatcher.PasswordResetSubject, item.Subject);
            Assert.Contains("https://admin.core.dealoware.com/admin/reset/confirm#token=", item.TextBody);
            Assert.DoesNotContain("?token=", item.TextBody);
            Assert.DoesNotContain(CurrentPassword, item.TextBody);
            Assert.DoesNotContain(NextPassword, item.TextBody);
            Assert.DoesNotContain("password=", item.TextBody, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task TD_ADM_030_Complete_RequiresTokenAnd2Fa_Skip2FaAbsent()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        var skip = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword
        });
        AssertLinkExpired(skip);

        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));

        var totp = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, totp.StatusCode);
        var totpBody = await totp.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.CompleteSucceeded, totpBody);
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
        Assert.False(await CredentialStillMatchesAsync(CurrentPassword));
    }

    [Fact]
    public async Task TD_ADM_030_Complete_RecoveryCode_WorksAndIsSingleUse()
    {
        await SeedCredentialAsync();
        var code = FirstRecoveryCode();
        var token = await RequestRawTokenAsync();

        var first = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            recoveryCode = code
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var token2 = await RequestRawTokenAsync();
        var replayCode = await PostConfirmAsync(new
        {
            token = token2,
            newPassword = "radio station third!",
            recoveryCode = code
        });
        await AssertWrongFactorUnauthorizedAsync(replayCode, "radio station third!");
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
    }

    [Fact]
    public async Task TD_ADM_030_AfterReset_SessionsRevoked_TotpStillRequired()
    {
        await SeedCredentialAsync();
        var sessionId = await SeedSessionAsync();
        var token = await RequestRawTokenAsync();

        var complete = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        Assert.DoesNotContain(
            ReadSetCookie(complete),
            c => c.StartsWith(AdminCookieNames.Session + "=", StringComparison.Ordinal));
        var client = CreateClient();
        using var me = await client.SendAsync(AdminGet("/admin/api/me", sessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Empty(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail).ToList());
        Assert.Contains(db.AdminAuditLog.ToList(), e => e.Action == "reset_complete");
        Assert.DoesNotContain(db.AdminAuditLog.ToList(), e =>
            e.BeforeSnapshot != null && e.BeforeSnapshot.Contains("pbkdf2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TD_ADM_030_PasswordRuleReject_DoesNotConsumeToken()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        var tooShort = await PostConfirmAsync(new
        {
            token,
            newPassword = "short-value",
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        var shortBody = await tooShort.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordRules.TooShortMessage, shortBody);
        Assert.Contains("too_short", shortBody);

        var same = await PostConfirmAsync(new
        {
            token,
            newPassword = CurrentPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Contains(AdminPasswordRules.SameAsCurrentMessage, await same.Content.ReadAsStringAsync());

        var ok = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_031_ReplayExpiredRandom_SameInvalidLinkReply()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var complete = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var replay = await PostConfirmAsync(new
        {
            token,
            newPassword = "radio station other!",
            totp = CurrentTotp()
        });

        var expiredToken = await RequestRawTokenAsync();
        Clock().Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        var expired = await PostConfirmAsync(new
        {
            token = expiredToken,
            newPassword = "radio station other!",
            totp = CurrentTotp()
        });

        var random = await PostConfirmAsync(new
        {
            token = "totally-random-token-value",
            newPassword = "radio station other!",
            totp = CurrentTotp()
        });

        AssertLinkExpired(replay);
        AssertLinkExpired(expired);
        AssertLinkExpired(random);
        Assert.Equal(
            replay.Headers.Location?.ToString(),
            expired.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TD_ADM_031_ConfirmGet_IgnoresQueryToken_SameForm_NoStore()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        using var withQuery = await SendAsync(
            HttpMethod.Get,
            $"{AdminSignedOutExemptions.ResetConfirmPage}?token={Uri.EscapeDataString(token)}");
        using var bare = await SendAsync(HttpMethod.Get, AdminSignedOutExemptions.ResetConfirmPage);

        Assert.Equal(HttpStatusCode.OK, withQuery.StatusCode);
        Assert.Equal(withQuery.StatusCode, bare.StatusCode);
        AssertNoStoreNoReferrer(withQuery);
        AssertNoStoreNoReferrer(bare);
        var queryBody = await withQuery.Content.ReadAsStringAsync();
        var bareBody = await bare.Content.ReadAsStringAsync();
        Assert.DoesNotContain(token, queryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("history.replaceState", queryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("window.__dwCsrf", queryBody, StringComparison.Ordinal);
        Assert.Contains($"<title>{AdminPasswordResetCopy.ConfirmPageTitle}</title>", queryBody);
        Assert.Contains(AdminPasswordResetCopy.ConfirmNoscript, queryBody);
        Assert.Contains("id=\"reset-status\" role=\"status\" hidden", queryBody, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", queryBody, StringComparison.Ordinal);
        Assert.Contains(">Change password</button>", queryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Update password", queryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-timeout-ms", queryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("__dwResetTimeoutMs", queryBody, StringComparison.Ordinal);
        Assert.Contains($"src=\"{AdminPasswordResetCopy.ConfirmScriptPath}\"", queryBody);
        Assert.Contains("autocomplete=\"one-time-code\"", queryBody, StringComparison.Ordinal);
        Assert.Contains("name=\"dw-csrf\"", queryBody, StringComparison.Ordinal);
        AssertConfirmPageNonceCsp(withQuery, queryBody);
        Assert.Equal("text/html", withQuery.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(
            ReadSetCookie(withQuery),
            c => c.Contains(token, StringComparison.Ordinal));
        Assert.DoesNotContain(
            ReadSetCookie(withQuery),
            c => c.StartsWith(AdminCookieNames.Pending + "=", StringComparison.Ordinal));
        Assert.DoesNotContain(
            ReadSetCookie(withQuery),
            c => c.StartsWith(AdminCookieNames.Session + "=", StringComparison.Ordinal));
        Assert.Contains($"src=\"{AdminPasswordResetCopy.ConfirmScriptPath}\"", bareBody);
        AssertConfirmPageNonceCsp(bare, bareBody);

        using var script = await SendAsync(HttpMethod.Get, AdminPasswordResetCopy.ConfirmScriptPath);
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        var scriptBody = await script.Content.ReadAsStringAsync();
        Assert.Contains("history.replaceState", scriptBody, StringComparison.Ordinal);
        Assert.Contains("location.hash", scriptBody, StringComparison.Ordinal);
        Assert.Contains("r.redirected", scriptBody, StringComparison.Ordinal);
        Assert.Contains("meta[name=\"dw-csrf\"]", scriptBody, StringComparison.Ordinal);
        Assert.DoesNotContain("status===303", scriptBody.Replace(" ", ""), StringComparison.Ordinal);
        Assert.DoesNotContain("r.status === 303", scriptBody, StringComparison.Ordinal);
        AssertConfirmClientSource(scriptBody);
    }

    [Fact]
    public void TD_ADM_031_ConfirmClient_MatchesChiefUiN1ToN4()
    {
        var html = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminPasswordResetEndpoints.cs"));
        Assert.Contains(
            "<p id=\\\"reset-status\\\" role=\\\"status\\\" hidden>",
            html,
            StringComparison.Ordinal);
        Assert.Contains("<button type=\\\"submit\\\">Change password</button>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Update password", html, StringComparison.Ordinal);

        var script = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "wwwroot", "admin", "auth", "reset-confirm.js"));
        AssertConfirmClientSource(script);
    }

    private static void AssertConfirmClientSource(string script)
    {
        var showRetry = Regex.Match(script, @"function showRetry\([^)]*\) \{[\s\S]*?\n  \}");
        Assert.True(showRetry.Success, "showRetry() must be present");
        Assert.Contains("retryAction = action || check", showRetry.Value, StringComparison.Ordinal);
        Assert.Contains("retryBtn.focus()", showRetry.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("alertEl.focus()", showRetry.Value, StringComparison.Ordinal);
        Assert.Contains("retryBtn.hidden = false", showRetry.Value, StringComparison.Ordinal);

        Assert.DoesNotContain("__dwResetTimeoutMs", script, StringComparison.Ordinal);
        Assert.Contains("var TIMEOUT_MS = 15000;", script, StringComparison.Ordinal);
        Assert.Contains("function submitConfirm()", script, StringComparison.Ordinal);
        Assert.Contains("retryAction();", script, StringComparison.Ordinal);
        Assert.Contains("totp-error", script, StringComparison.Ordinal);
        Assert.Contains("Enter your authenticator code or a recovery code.", script, StringComparison.Ordinal);
        Assert.Contains("totpVal ? \"\" : recVal", script, StringComparison.Ordinal);
        Assert.Contains("showRetry(undefined, submitConfirm)", script, StringComparison.Ordinal);
        Assert.Contains("if (!token)", script, StringComparison.Ordinal);
        Assert.Contains("location.replace(EXPIRED)", script, StringComparison.Ordinal);
        Assert.Contains("newPassword-error", script, StringComparison.Ordinal);
        Assert.Contains("aria-describedby", script, StringComparison.Ordinal);
        Assert.Contains("aria-invalid", script, StringComparison.Ordinal);
        Assert.Contains("r.status === 400", script, StringComparison.Ordinal);
        Assert.Contains("r.status === 401", script, StringComparison.Ordinal);
        Assert.Contains("r.status === 429", script, StringComparison.Ordinal);
        Assert.Contains(AdminPasswordResetCopy.SignInGeneric, script, StringComparison.Ordinal);
        Assert.Contains("totpEl.value = \"\"", script, StringComparison.Ordinal);
        Assert.Contains("recoveryEl.value = \"\"", script, StringComparison.Ordinal);
        Assert.Contains("body.message", script, StringComparison.Ordinal);
        var beforeCheck = script[..script.LastIndexOf("\n  check();", StringComparison.Ordinal)];
        Assert.Contains("if (!token)", beforeCheck, StringComparison.Ordinal);
    }

    [Fact]
    public void TD_ADM_031_ResetCopy_MatchesApprovedSentences()
    {
        Assert.Equal("If that account exists, we've sent instructions.", AdminPasswordResetCopy.RequestAccepted);
        Assert.Equal("This link is no longer valid. Request a new one.", AdminPasswordResetCopy.InvalidLink);
        Assert.Equal(
            "We couldn't sign you in. Check your details and try again later. You can also reset your password.",
            AdminPasswordResetCopy.SignInGeneric);

        var endpoints = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminPasswordResetEndpoints.cs"));
        Assert.Contains("RequestThrottledResult()", endpoints, StringComparison.Ordinal);
        Assert.Contains("ConfirmThrottledResult()", endpoints, StringComparison.Ordinal);
        Assert.Contains(
            "new { message = AdminPasswordResetCopy.RequestAccepted }",
            endpoints,
            StringComparison.Ordinal);
        Assert.Contains(
            "new { message = AdminPasswordResetCopy.SignInGeneric }",
            endpoints,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ConfirmThrottledResult()\n        => Results.Json(\n            new { message = AdminPasswordResetCopy.RequestAccepted }",
            endpoints.Replace("\r\n", "\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TD_ADM_030_OwnedRows_Exempt_ExactPathAndMethod()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        using var resetGet = await SendAsync(HttpMethod.Get, "/admin/reset");
        using var resetSent = await SendAsync(HttpMethod.Get, "/admin/reset/sent");
        using var linkExpired = await SendAsync(HttpMethod.Get, "/admin/link-expired");
        using var confirmGet = await SendAsync(
            HttpMethod.Get,
            $"/admin/reset/confirm?token={Uri.EscapeDataString(token)}");
        using var authStatic = await SendAsync(HttpMethod.Get, "/admin/auth/reset.css");

        Assert.NotEqual(HttpStatusCode.Unauthorized, resetGet.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, resetSent.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, linkExpired.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, confirmGet.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, authStatic.StatusCode);
        Assert.Contains(
            $"<title>{AdminPasswordResetCopy.LinkExpiredPageTitle}</title>",
            await linkExpired.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TD_ADM_030_OwnedRows_Deny_ExtraSegment_WrongMethod_MissingToken()
    {
        await SeedCredentialAsync();

        using var extraApi = await SendAsync(HttpMethod.Post, "/admin/api/auth/reset/extra");
        using var extraPage = await SendAsync(HttpMethod.Get, "/admin/reset/extra");
        using var extraConfirm = await SendAsync(HttpMethod.Post, "/admin/api/auth/reset/confirm/extra");
        using var wrongMethodApi = await SendAsync(HttpMethod.Get, "/admin/api/auth/reset");
        using var wrongMethodPage = await SendAsync(HttpMethod.Delete, "/admin/reset");
        using var missingAfConfirm = await SendAsync(HttpMethod.Post, "/admin/api/auth/reset/confirm");
        using var missingAfPage = await SendAsync(HttpMethod.Post, "/admin/reset");

        var extraGetBody = await extraPage.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, extraApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraPage.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, extraConfirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongMethodApi.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongMethodPage.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, missingAfConfirm.StatusCode);
        Assert.Equal(extraGetBody, await wrongMethodApi.Content.ReadAsStringAsync());
        Assert.Contains("\"error\":\"Unauthorized\"", extraGetBody);
        await AssertGenericForbiddenAsync(extraApi);
        await AssertGenericForbiddenAsync(missingAfConfirm);
        await AssertHtmlForbiddenAsync(wrongMethodPage);
        await AssertHtmlForbiddenAsync(missingAfPage);
    }

    [Fact]
    public async Task TD_ADM_030_Request_WrongHost_Is404_NoSetCookie_C7()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        request.Headers.Host = "core.dealoware.com";
        request.Content = JsonContent.Create(new { email = CoreOwnerEmail });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(body));
        Assert.DoesNotContain("Unauthorized", body);
        Assert.Empty(ReadSetCookie(response));
        Assert.False(response.Headers.Contains(AdminCookieNames.AntiForgeryHeader));
    }

    [Fact]
    public async Task TD_ADM_030_C1_TrailingSlash_Encoded_SlashSlash_Matrix_Denied()
    {
        using var trailing = await SendAsync(HttpMethod.Get, "/admin/reset/");
        using var extraEncoded = await SendAsync(HttpMethod.Get, "/admin/reset%2fextra");
        using var extraSegment = await SendAsync(HttpMethod.Get, "/admin/reset/extra");
        using var slashSlash = await SendAsync(HttpMethod.Get, "/admin//reset");
        using var matrix = await SendAsync(HttpMethod.Get, "/admin/reset;jsessionid=1");

        Assert.Equal(HttpStatusCode.Unauthorized, trailing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraEncoded.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraSegment.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, slashSlash.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, matrix.StatusCode);
        var body = await trailing.Content.ReadAsStringAsync();
        Assert.Equal(body, await extraEncoded.Content.ReadAsStringAsync());
        Assert.Equal(body, await extraSegment.Content.ReadAsStringAsync());
        Assert.Contains("\"error\":\"Unauthorized\"", body);
    }

    [Fact]
    public async Task TD_ADM_030_C3_ExemptPost_MissingOrInvalidAntiForgery_IsGenericDeny()
    {
        await SeedCredentialAsync();
        Mail().Clear();

        using var missing = await SendAsync(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        var issued = await IssueAntiForgeryAsync();
        var client = _factory.CreateClient();
        using var bad = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        bad.Headers.Host = AdminHost;
        bad.Headers.TryAddWithoutValidation("Cookie", issued.CookieHeader);
        bad.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, "not-the-cookie");
        bad.Content = JsonContent.Create(new { email = CoreOwnerEmail });
        using var invalid = await client.SendAsync(bad);

        await AssertGenericForbiddenAsync(missing);
        await AssertGenericForbiddenAsync(invalid);
        Assert.Equal(
            await missing.Content.ReadAsStringAsync(),
            await invalid.Content.ReadAsStringAsync());
        Assert.Empty(Mail().Sent);
    }

    [Fact]
    public async Task TD_ADM_030_C3_GetConfirm_DoesNotConsumeToken()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        using var get = await SendAsync(
            HttpMethod.Get,
            $"{AdminSignedOutExemptions.ResetConfirmPage}?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.DoesNotContain(token, await get.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var complete = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
    }

    [Fact]
    public async Task TD_ADM_030_B6194918_QueryTokenOnPost_IsIgnored_GoesToLinkExpired()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        var before = await ResetCounterCountAsync();
        var af = await IssueAntiForgeryAsync();
        var client = CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{AdminPasswordResetEndpoints.ConfirmApiPath}?token={Uri.EscapeDataString(token)}");
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(new
        {
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        using var response = await client.SendAsync(request);
        AssertLinkExpired(response);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
        Assert.True(await ResetCounterCountAsync() > before);
    }

    [Fact]
    public async Task TD_ADM_030_B6194918_NewTokenCancelsOld_AndHas128Bits()
    {
        await SeedCredentialAsync();
        var first = await RequestRawTokenAsync();
        var second = await RequestRawTokenAsync();
        Assert.NotEqual(first, second);

        var raw = Convert.FromBase64String(PadBase64Url(second));
        Assert.True(raw.Length * 8 >= 128);

        var stale = await PostConfirmAsync(new
        {
            token = first,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        AssertLinkExpired(stale);

        var ok = await PostConfirmAsync(new
        {
            token = second,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_030_C5_CookieNames_NoHostPrefix()
    {
        Assert.Equal("dw_admin_session", AdminCookieNames.Session);
        Assert.Equal("dw_admin_pending", AdminCookieNames.Pending);
        Assert.Equal("dw_admin_af", AdminCookieNames.AntiForgery);
        Assert.Equal(AdminCookieNames.Session, AdminSessionCookie.Name);
        Assert.DoesNotContain("__Host-", AdminCookieNames.Session, StringComparison.Ordinal);
        Assert.DoesNotContain("__Host-", AdminCookieNames.Pending, StringComparison.Ordinal);
        Assert.DoesNotContain("__Host-", AdminCookieNames.AntiForgery, StringComparison.Ordinal);

        var issued = await IssueAntiForgeryAsync();
        Assert.Contains($"{AdminCookieNames.AntiForgery}=", issued.SetCookie, StringComparison.Ordinal);
        Assert.DoesNotContain("__Host-", issued.SetCookie, StringComparison.Ordinal);
        Assert.Contains("path=/admin", issued.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", issued.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", issued.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", issued.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", issued.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Admin-AF", issued.SetCookie, StringComparison.Ordinal);
        Assert.NotEqual(issued.CookieNonce, issued.Token);
    }

    [Fact]
    public async Task TD_ADM_030_R12_Request_OwnerAndUnknown_EqualDbWorkAndResponse()
    {
        await SeedCredentialAsync();
        Mail().Clear();

        _factory.CommandCounter.Reset();
        using var known = await PostRequestAsync(CoreOwnerEmail);
        var knownCommands = _factory.CommandCounter.Count;
        var knownBody = await known.Content.ReadAsStringAsync();

        _factory.CommandCounter.Reset();
        using var unknown = await PostRequestAsync("nobody@example.com");
        var unknownCommands = _factory.CommandCounter.Count;
        var unknownBody = await unknown.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(knownBody, unknownBody);
        Assert.Equal(knownCommands, unknownCommands);
        Assert.True(knownCommands > 0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(2, db.AdminPasswordResetTokens.Count());
        Assert.Contains(db.AdminPasswordResetTokens, t => t.Email == CoreOwnerEmail);
        Assert.Contains(db.AdminPasswordResetTokens, t => t.Email == "nobody@example.com");
    }

    [Fact]
    public async Task TD_ADM_030_PasswordRuleReject_DoesNotConsumeTotpOrRecovery()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var totp = CurrentTotp();

        var tooShort = await PostConfirmAsync(new
        {
            token,
            newPassword = "short-value",
            totp
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            Assert.Null(account.LastUsedTotpTimestep);
            Assert.Equal(0, account.FailedFactorAttempts);
            var reset = db.AdminPasswordResetTokens.Single(t => t.ConsumedAt == null);
            Assert.Equal(0, reset.FailedCodeAttempts);
            Assert.All(db.AdminRecoveryCodes, c => Assert.Null(c.UsedAt));
        }

        var ok = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_031_R13_CrossOrigin_Sibling_Null_AndMissing_AreGeneric403()
    {
        await SeedCredentialAsync();
        var issued = await IssueAntiForgeryAsync();

        using var foreign = await SendResetWithOriginAsync(
            issued,
            origin: "https://evil.example",
            referer: null);
        await AssertGenericForbiddenAsync(foreign);

        using var sibling = await SendResetWithOriginAsync(
            issued,
            origin: "https://www.dealoware.com",
            referer: null);
        await AssertGenericForbiddenAsync(sibling);

        using var originNull = await SendResetWithOriginAsync(
            issued,
            origin: "null",
            referer: null);
        await AssertGenericForbiddenAsync(originNull);

        using var bothMissing = await SendResetWithOriginAsync(
            issued,
            origin: null,
            referer: null);
        await AssertGenericForbiddenAsync(bothMissing);

        using var refererOk = await SendResetWithOriginAsync(
            issued,
            origin: null,
            referer: $"{AdminHostOptions.ProductionAdminOrigin}/admin/reset");
        Assert.Equal(HttpStatusCode.OK, refererOk.StatusCode);

        using var exactOrigin = await SendResetWithOriginAsync(
            issued,
            origin: AdminHostOptions.ProductionAdminOrigin,
            referer: null);
        Assert.Equal(HttpStatusCode.OK, exactOrigin.StatusCode);
        Assert.Empty(Mail().Sent.Where(m => m.To != CoreOwnerEmail));
    }

    [Fact]
    public async Task TD_ADM_031_R15_SignOut_NoSessionNoToken_IsGeneric403()
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminSignedOutExemptions.ApiSignOut);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Origin", AdminHostOptions.ProductionAdminOrigin);
        request.Content = JsonContent.Create(new { });
        using var response = await client.SendAsync(request);
        await AssertGenericForbiddenAsync(response);
    }

    [Fact]
    public async Task TD_ADM_031_R15_NewlyMappedDummyPost_IsProtectedByDefault()
    {
        var client = CreateClient();
        using var missing = new HttpRequestMessage(HttpMethod.Post, AdminCsrfProbeEndpoint.Path);
        missing.Headers.Host = AdminHost;
        missing.Content = JsonContent.Create(new { });
        using var missingResponse = await client.SendAsync(missing);
        await AssertGenericForbiddenAsync(missingResponse);

        var sessionId = await SeedSessionAsync();
        var issued = await IssueAntiForgeryAsync();
        using var ok = new HttpRequestMessage(HttpMethod.Post, AdminCsrfProbeEndpoint.Path);
        ok.Headers.Host = AdminHost;
        ok.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{issued.CookieHeader}; {AdminSessionCookie.Name}={sessionId:D}");
        ok.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, issued.Token);
        ok.Content = JsonContent.Create(new { });
        using var okResponse = await client.SendAsync(ok);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_031_R16_TokenWithoutPurposeLabel_IsRejected()
    {
        await SeedCredentialAsync();
        var issued = await IssueAntiForgeryAsync();
        var derived = AdminAntiForgeryService.DeriveKey(Convert.FromBase64String(_factory.IpHmacKey));
        var wrongPurpose = AdminAntiForgeryService.ComputeRequestToken(
            derived,
            issued.CookieNonce,
            purpose: "other-purpose");
        var noPurpose = Convert.ToHexString(
            HMACSHA256.HashData(derived, Encoding.UTF8.GetBytes(issued.CookieNonce)));

        foreach (var forged in new[] { wrongPurpose, noPurpose })
        {
            var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
            request.Headers.Host = AdminHost;
            request.Headers.TryAddWithoutValidation("Cookie", issued.CookieHeader);
            request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, forged);
            request.Content = JsonContent.Create(new { email = CoreOwnerEmail });
            using var response = await client.SendAsync(request);
            await AssertGenericForbiddenAsync(response);
        }

        Assert.Empty(Mail().Sent);
    }

    [Fact]
    public async Task TD_ADM_031_R10_ValidTotp_ConsumesToken_RevokesSessions_SignInWorks()
    {
        await SeedCredentialAsync();
        var sessionId = await SeedSessionAsync();
        var token = await RequestRawTokenAsync();
        var totp = CurrentTotp();

        var complete = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            Assert.NotNull(account.LastUsedTotpTimestep);
            Assert.Empty(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail).ToList());
            Assert.DoesNotContain(db.AdminPasswordResetTokens, t => t.ConsumedAt == null);
        }

        using var me = await CreateClient().SendAsync(AdminGet("/admin/api/me", sessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var client = CreateClient();
        var signInAf = await IssueSignInAntiForgeryAsync(client);
        using var step1 = await client.SendAsync(SignInJson(
            AdminSignedOutExemptions.ApiSignIn,
            new { email = CoreOwnerEmail, password = NextPassword },
            signInAf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var lastUsed = scope2.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail).LastUsedTotpTimestep;
        var nowStep = Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow);
        var signInCode = lastUsed == nowStep
            ? Rfc6238Totp.ComputeCode(_totpSecret!, nowStep + 1)
            : Rfc6238Totp.ComputeCode(_totpSecret!, nowStep);
        var codeAf = await IssueSignInAntiForgeryAsync(client, AdminSignedOutExemptions.SignInCode);
        using var step2 = await client.SendAsync(SignInJson(
            AdminSignedOutExemptions.ApiSignInCode,
            new { code = signInCode },
            codeAf));
        Assert.Equal(HttpStatusCode.OK, step2.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_031_R10_TotpReplay_SameTimestep_IsRejected()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var totp = CurrentTotp();
        var first = await PostConfirmAsync(new { token, newPassword = NextPassword, totp });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var token2 = await RequestRawTokenAsync();
        var replay = await PostConfirmAsync(new
        {
            token = token2,
            newPassword = "radio station third!",
            totp
        });
        await AssertWrongFactorUnauthorizedAsync(replay, "radio station third!");
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
    }

    [Fact]
    public async Task TD_ADM_031_R10_WrongCode_DoesNotConsumeToken_CountsTowardLock()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        var wrong = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = "000000"
        });
        await AssertWrongFactorUnauthorizedAsync(wrong, NextPassword);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            Assert.Equal(1, account.FailedFactorAttempts);
            Assert.Null(account.LastUsedTotpTimestep);
            Assert.Contains(db.AdminPasswordResetTokens, t => t.ConsumedAt == null && t.FailedCodeAttempts == 1);
        }

        var ok = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_031_R10_BothFactorsOrUnenrolled_AreRejected()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var both = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp(),
            recoveryCode = FirstRecoveryCode()
        });
        AssertLinkExpired(both);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));

        await SeedCredentialAsync(enrolled: false);
        var unenrolledToken = await RequestRawTokenAsync();
        var unenrolled = await PostConfirmAsync(new
        {
            token = unenrolledToken,
            newPassword = NextPassword,
            totp = "123456"
        });
        AssertLinkExpired(unenrolled);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
    }

    [Fact]
    public async Task TD_ADM_031_R11_FiveFailedCodes_LockAccount_AndBurnToken()
    {
        await SeedCredentialAsync();
        var sessionId = await SeedSessionAsync();
        var token = await RequestRawTokenAsync();

        HttpResponseMessage? last = null;
        for (var i = 0; i < AdminPasswordResetToken.MaxFailedCodeAttempts; i++)
        {
            last?.Dispose();
            last = await PostConfirmAsync(new
            {
                token,
                newPassword = NextPassword,
                totp = "000000"
            });
            await AssertWrongFactorUnauthorizedAsync(last, NextPassword);
        }

        last!.Dispose();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
            Assert.NotNull(account.FactorLockedUntil);
            Assert.True(account.IsFactorLocked(Clock().UtcNow));
            var burned = Assert.Single(db.AdminPasswordResetTokens.Where(t => t.ConsumedAt != null));
            Assert.Equal(AdminPasswordResetToken.MaxFailedCodeAttempts, burned.FailedCodeAttempts);
            Assert.Empty(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail).ToList());
        }

        using var me = await CreateClient().SendAsync(AdminGet("/admin/api/me", sessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var afterBurn = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        AssertLinkExpired(afterBurn);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
    }

    [Fact]
    public async Task TD_ADM_031_SC6_R10_PostgresParallelConfirm_ExactlyOne200()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var totp = CurrentTotp();
        var body = new { token, newPassword = NextPassword, totp };

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => PostConfirmAsync(body)));
        try
        {
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.SeeOther));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(1, db.AdminPasswordResetTokens.Count(t => t.ConsumedAt != null));
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
    }

    [Fact]
    public async Task TD_ADM_031_C6_TwoTokens_TogetherTripAccountLock_BurnsBothAndSessions_NeitherAtTokenCap()
    {
        await SeedCredentialAsync();
        var sessionId = await SeedSessionAsync();
        var extraSessionId = await SeedSessionAsync();
        var tokenA = await InsertResetTokenAsync();
        var tokenB = await InsertResetTokenAsync();

        for (var i = 0; i < 3; i++)
        {
            using var wrongA = await PostConfirmAsync(new
            {
                token = tokenA,
                newPassword = NextPassword,
                totp = "000000"
            });
            await AssertWrongFactorUnauthorizedAsync(wrongA, NextPassword);
        }

        using var firstB = await PostConfirmAsync(new
        {
            token = tokenB,
            newPassword = NextPassword,
            totp = "000000"
        });
        await AssertWrongFactorUnauthorizedAsync(firstB, NextPassword);

        using var lockTrip = await PostConfirmAsync(new
        {
            token = tokenB,
            newPassword = NextPassword,
            totp = "000000"
        });
        await AssertWrongFactorUnauthorizedAsync(lockTrip, NextPassword);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
            Assert.True(account.IsFactorLocked(Clock().UtcNow));
            var rows = db.AdminPasswordResetTokens.Where(t => t.Email == CoreOwnerEmail).ToList();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, t => Assert.NotNull(t.ConsumedAt));
            Assert.All(
                rows,
                t => Assert.True(t.FailedCodeAttempts < AdminPasswordResetToken.MaxFailedCodeAttempts));
            Assert.Contains(rows, t => t.FailedCodeAttempts == 3);
            Assert.Contains(rows, t => t.FailedCodeAttempts == 2);
            Assert.Empty(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail).ToList());
        }

        using var meA = await CreateClient().SendAsync(AdminGet("/admin/api/me", sessionId));
        using var meB = await CreateClient().SendAsync(AdminGet("/admin/api/me", extraSessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, meA.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meB.StatusCode);

        using var afterA = await PostConfirmAsync(new
        {
            token = tokenA,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        using var afterB = await PostConfirmAsync(new
        {
            token = tokenB,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        AssertLinkExpired(afterA);
        AssertLinkExpired(afterB);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
    }

    [Fact]
    public async Task TD_ADM_031_C2_PostgresParallelWrongCodes_NeitherCounterExceedsCap()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        await ClearResetCountersAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ =>
            PostConfirmAsync(new
            {
                token,
                newPassword = NextPassword,
                totp = "000000"
            })));

        try
        {
            Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.OK);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            var row = db.AdminPasswordResetTokens.Single(t => t.Email == CoreOwnerEmail);
            Assert.True(account.FailedFactorAttempts <= AdminCoreOwnerAccount.MaxFailedFactorAttempts);
            Assert.True(row.FailedCodeAttempts <= AdminPasswordResetToken.MaxFailedCodeAttempts);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task TD_ADM_031_C3_WrongRecovery_401_DoesNotRedeemCode()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var code = FirstRecoveryCode();

        using var wrong = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            recoveryCode = "not-a-real-recovery-code"
        });
        await AssertWrongFactorUnauthorizedAsync(wrong, NextPassword);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.All(db.AdminRecoveryCodes, c => Assert.Null(c.UsedAt));
            Assert.Contains(db.AdminPasswordResetTokens, t => t.ConsumedAt == null);
        }

        using var ok = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            recoveryCode = code
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(await CredentialStillMatchesAsync(NextPassword));
    }

    [Fact]
    public async Task TD_ADM_031_C4_WrongTotpAndWrongRecovery_Same401()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();

        var totpSw = System.Diagnostics.Stopwatch.StartNew();
        using var totp = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = "000000"
        });
        totpSw.Stop();
        var recoverySw = System.Diagnostics.Stopwatch.StartNew();
        using var recovery = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            recoveryCode = "not-a-real-recovery-code"
        });
        recoverySw.Stop();

        await AssertWrongFactorUnauthorizedAsync(totp, NextPassword);
        await AssertWrongFactorUnauthorizedAsync(recovery, NextPassword);
        var totpBody = await totp.Content.ReadAsStringAsync();
        var recoveryBody = await recovery.Content.ReadAsStringAsync();
        Assert.Equal(totp.StatusCode, recovery.StatusCode);
        Assert.Equal(totpBody, recoveryBody);
        Assert.Equal(totp.Content.Headers.ContentType?.ToString(), recovery.Content.Headers.ContentType?.ToString());
        Assert.Equal(totp.Headers.Location?.ToString(), recovery.Headers.Location?.ToString());
        Assert.True(totpSw.Elapsed < TimeSpan.FromSeconds(2));
        Assert.True(recoverySw.Elapsed < TimeSpan.FromSeconds(2));
        Assert.True((totpSw.Elapsed - recoverySw.Elapsed).Duration() < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TD_ADM_031_C5_WrongCodes_DoNotExtendExpiry()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        var hash = AdminPasswordResetToken.HashRaw(token);
        DateTimeOffset expiresAt;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            expiresAt = db.AdminPasswordResetTokens.Single(t => t.TokenHash == hash).ExpiresAt;
        }

        for (var i = 0; i < 3; i++)
        {
            using var wrong = await PostConfirmAsync(new
            {
                token,
                newPassword = NextPassword,
                totp = "000000"
            });
            await AssertWrongFactorUnauthorizedAsync(wrong, NextPassword);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = db.AdminPasswordResetTokens.Single(t => t.TokenHash == hash);
            Assert.Equal(expiresAt, row.ExpiresAt);
            Assert.Equal(3, row.FailedCodeAttempts);
            Assert.Null(row.ConsumedAt);
        }
    }

    [Fact]
    public async Task TD_ADM_031_K1_K4_Check_BodyTokenOnly_ValidBool_NoStore()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        await ClearResetCountersAsync();
        _factory.LogCollector.Clear();

        using var queryOnly = await PostCheckAsync(
            body: new { },
            queryToken: token);
        using var good = await PostCheckAsync(new { token });
        using var unknown = await PostCheckAsync(new { token = "totally-random-token-value" });

        Assert.Equal(HttpStatusCode.OK, queryOnly.StatusCode);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        AssertNoStoreNoReferrer(queryOnly);
        AssertNoStoreNoReferrer(good);
        Assert.Equal("""{"valid":false}""", await queryOnly.Content.ReadAsStringAsync());
        Assert.Equal("""{"valid":true}""", await good.Content.ReadAsStringAsync());
        Assert.Equal("""{"valid":false}""", await unknown.Content.ReadAsStringAsync());
        Assert.Equal(3, await ResetCounterCountAsync());

        var logs = string.Join("\n", _factory.LogCollector.Lines);
        Assert.Contains("Admin reset check: valid", logs, StringComparison.Ordinal);
        Assert.Contains("Admin reset check: invalid", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(CoreOwnerEmail, logs, StringComparison.OrdinalIgnoreCase);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Contains(db.AdminPasswordResetTokens, t => t.ConsumedAt == null && t.FailedCodeAttempts == 0);
        }

        using var complete = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_030_C8_AuthFolder_StaticFileOnly_NoHandlers()
    {
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>();
        Assert.DoesNotContain(
            endpoints.Endpoints.OfType<RouteEndpoint>(),
            e => e.RoutePattern.RawText is { } raw
                 && raw.StartsWith("/admin/auth", StringComparison.OrdinalIgnoreCase));

        using var css = await SendAsync(HttpMethod.Get, "/admin/auth/reset.css");
        using var script = await SendAsync(HttpMethod.Get, AdminPasswordResetCopy.ConfirmScriptPath);
        using var directory = await SendAsync(HttpMethod.Get, "/admin/auth/");
        using var handlerLike = await SendAsync(HttpMethod.Get, "/admin/auth/handler");
        using var postFile = await SendAsync(HttpMethod.Post, "/admin/auth/reset.css");

        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, directory.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, handlerLike.StatusCode);
        await AssertHtmlForbiddenAsync(postFile);
    }

    [Fact]
    public async Task TD_ADM_030_Audit_ResetRequestAndComplete_NoSecrets()
    {
        await SeedCredentialAsync();
        Mail().Clear();
        var token = await RequestRawTokenAsync();
        await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var rows = db.AdminAuditLog.ToList();
        Assert.Contains(rows, e => e.Action == "reset_request");
        Assert.Contains(rows, e => e.Action == "reset_complete");
        foreach (var row in rows)
        {
            Assert.DoesNotContain("pbkdf2", row.IpHmac, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token, row.ActorEmail);
            Assert.DoesNotContain(token, row.ReasonClass ?? string.Empty);
            Assert.DoesNotContain(token, row.BeforeSnapshot ?? string.Empty);
            Assert.DoesNotContain(token, row.AfterSnapshot ?? string.Empty);
            Assert.DoesNotContain(NextPassword, row.ActorEmail);
            Assert.False(string.IsNullOrWhiteSpace(row.IpHmac));
        }
    }

    [Fact]
    public async Task TD_ADM_030_SC9_Request_SameStatusBodyAndTiming_MailOffPath()
    {
        await SeedCredentialAsync();
        var mail = Mail();
        mail.Clear();
        mail.SendDelay = TimeSpan.FromMilliseconds(350);

        var knownSw = System.Diagnostics.Stopwatch.StartNew();
        using var known = await PostRequestAsync(CoreOwnerEmail);
        knownSw.Stop();
        var unknownSw = System.Diagnostics.Stopwatch.StartNew();
        using var unknown = await PostRequestAsync("nobody@example.com");
        unknownSw.Stop();
        var lockedSw = System.Diagnostics.Stopwatch.StartNew();
        using var locked = await PostRequestAsync(CoreOwnerEmail);
        lockedSw.Stop();

        var knownBody = await known.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(known.StatusCode, locked.StatusCode);
        Assert.Equal(knownBody, await unknown.Content.ReadAsStringAsync());
        Assert.Equal(knownBody, await locked.Content.ReadAsStringAsync());
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, knownBody);
        Assert.True(knownSw.Elapsed < mail.SendDelay);
        Assert.True(unknownSw.Elapsed < mail.SendDelay);
        Assert.True(lockedSw.Elapsed < mail.SendDelay);
        Assert.Empty(mail.Sent);

        await mail.WaitForSentAsync(2);
        Assert.Equal(2, mail.Sent.Count);
        mail.SendDelay = TimeSpan.Zero;
    }

    [Fact]
    public async Task TD_ADM_030_N4_MailDispatchFailure_SameReply_LogsTypeAndTraceOnly()
    {
        await SeedCredentialAsync();
        var mail = Mail();
        mail.Clear();
        mail.FailWithDisabledSender = true;
        _factory.LogCollector.Clear();

        using var known = await PostRequestAsync(CoreOwnerEmail);
        using var unknown = await PostRequestAsync("nobody@example.com");
        var knownBody = await known.Content.ReadAsStringAsync();
        var unknownBody = await unknown.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(knownBody, unknownBody);
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, knownBody);
        Assert.Empty(mail.Sent);

        var dispatch = await WaitForDispatchFailureLogAsync();
        Assert.Contains("ExceptionType=System.InvalidOperationException", dispatch, StringComparison.Ordinal);
        Assert.Contains("TraceId=", dispatch, StringComparison.Ordinal);
        var all = string.Join("\n", _factory.LogCollector.Lines);
        Assert.DoesNotContain(CoreOwnerEmail, all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nobody@example.com", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#token=", all, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/reset/confirm", all, StringComparison.Ordinal);
        mail.FailWithDisabledSender = false;
    }

    [Fact]
    public async Task TD_ADM_030_SC6_Request_TwentyEventsThen429_PersistedAtomically()
    {
        await SeedCredentialAsync();
        HttpResponseMessage? lastOk = null;
        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            lastOk?.Dispose();
            lastOk = await PostRequestAsync(i % 2 == 0 ? CoreOwnerEmail : "nobody@example.com");
            Assert.Equal(HttpStatusCode.OK, lastOk.StatusCode);
            Assert.DoesNotContain(
                "Retry-After",
                lastOk.Headers.Select(h => h.Key),
                StringComparer.OrdinalIgnoreCase);
        }

        Assert.NotNull(lastOk);
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, await lastOk.Content.ReadAsStringAsync());
        lastOk.Dispose();

        var counter = await ResetCounterAsync();
        Assert.NotNull(counter);
        Assert.Equal(AdminResetIpCounter.Threshold, counter.FailureCount);
        Assert.True(counter.IsLocked(Clock().UtcNow));

        Mail().Clear();
        using var knownLocked = await PostRequestAsync(CoreOwnerEmail);
        using var unknownLocked = await PostRequestAsync("nobody@example.com");
        var knownLockedBody = await AssertRequestThrottledAsync(knownLocked);
        var unknownLockedBody = await AssertRequestThrottledAsync(unknownLocked);
        Assert.Equal(knownLockedBody, unknownLockedBody);
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
        Assert.Empty(Mail().Sent);
    }

    [Fact]
    public async Task TD_ADM_030_SC6_R3_ParallelBurst_AtMostTwentyPassOrIssueTokens()
    {
        const string ipHmac = "burst-ip-hmac";
        await using var store = await OpenThrottleStoreAsync("ResetIpBurst");
        await ClearThrottleIpAsync(store.Options, ipHmac);

        var now = DateTimeOffset.UtcNow;
        var results = await ParallelReserveAsync(store.Options, ipHmac, now, 40);

        Assert.Equal(AdminResetIpCounter.Threshold, results.Count(reserved => reserved));
        Assert.Equal(40 - AdminResetIpCounter.Threshold, results.Count(reserved => !reserved));

        await using var check = new DealowareDbContext(store.Options);
        var row = check.AdminResetIpCounters.Single(c => c.IpHmac == ipHmac);
        Assert.Equal(AdminResetIpCounter.Threshold, row.FailureCount);
        Assert.True(row.IsLocked(now));
    }

    [Fact]
    public async Task TD_ADM_030_SC6_R5_ExpiredRowParallelBurst_AtMostTwenty()
    {
        const string ipHmac = "r5-expired-burst";
        await using var store = await OpenThrottleStoreAsync("ResetIpR5Burst");
        await ClearThrottleIpAsync(store.Options, ipHmac);

        var now = DateTimeOffset.UtcNow;
        await SeedExpiredLockedRowAsync(
            store.Options,
            ipHmac,
            windowStartedAt: now - TimeSpan.FromMinutes(20),
            lockedUntil: now - TimeSpan.FromSeconds(1),
            failureCount: AdminResetIpCounter.Threshold);

        var results = await ParallelReserveAsync(store.Options, ipHmac, now, 40);

        Assert.Equal(AdminResetIpCounter.Threshold, results.Count(reserved => reserved));
        Assert.Equal(40 - AdminResetIpCounter.Threshold, results.Count(reserved => !reserved));

        await using var check = new DealowareDbContext(store.Options);
        var row = check.AdminResetIpCounters.Single(c => c.IpHmac == ipHmac);
        Assert.Equal(AdminResetIpCounter.Threshold, row.FailureCount);
        Assert.True(row.IsLocked(now));
    }

    [Fact]
    public async Task TD_ADM_030_SC6_R5_DelayedReset_LeavesNewerLock()
    {
        const string ipHmac = "r5-delayed-reset";
        await using var store = await OpenThrottleStoreAsync("ResetIpR5Delay");
        await ClearThrottleIpAsync(store.Options, ipHmac);

        var t0 = new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        var lockUntil = t0.Add(AdminResetIpCounter.LockDuration);
        var staleNow = lockUntil.AddSeconds(1);
        await SeedExpiredLockedRowAsync(
            store.Options,
            ipHmac,
            windowStartedAt: t0,
            lockedUntil: lockUntil,
            failureCount: AdminResetIpCounter.Threshold);

        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            await using var dbB = new DealowareDbContext(store.Options);
            Assert.True(await new AdminResetIpThrottle(dbB).TryReserveAsync(
                ipHmac, staleNow, CancellationToken.None));
        }

        await using var stale = new DealowareDbContext(store.Options);
        var overwritten = await stale.AdminResetIpCounters
            .Where(c =>
                c.IpHmac == ipHmac
                && c.FailureCount == AdminResetIpCounter.Threshold
                && c.WindowStartedAt == t0
                && c.LockedUntil == lockUntil)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.FailureCount, 1)
                .SetProperty(c => c.WindowStartedAt, staleNow)
                .SetProperty(c => c.LockedUntil, (DateTimeOffset?)null));
        Assert.Equal(0, overwritten);

        await using var check = new DealowareDbContext(store.Options);
        var row = check.AdminResetIpCounters.Single(c => c.IpHmac == ipHmac);
        Assert.Equal(AdminResetIpCounter.Threshold, row.FailureCount);
        Assert.True(row.IsLocked(staleNow));
        Assert.NotEqual(t0, row.WindowStartedAt);

        var source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Infrastructure", "Admin", "AdminResetIpThrottle.cs"));
        Assert.Contains("c.WindowStartedAt == row.WindowStartedAt", source, StringComparison.Ordinal);
        Assert.Contains("c.LockedUntil == row.LockedUntil", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TD_ADM_031_RealVerifier_AcceptsEnrolledTotp()
    {
        const string email = "reset-2fa-verifier@aiknowhow.com";
        using var scope = _factory.Services.CreateScope();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var recoveryHasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IAdminClock>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAdminCoreOwnerAccountRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var secret = Rfc6238Totp.GenerateSecret();
        var account = AdminCoreOwnerAccount.Create(email);
        account.SetPasswordHash("unused-hash");
        account.CompleteEnrollment(protector.Encrypt(secret), now: clock.UtcNow);
        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();

        var verifier = new AdminSecondFactorVerifier(accounts, protector, recoveryHasher, clock);
        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(clock.UtcNow));
        var ok = await verifier.VerifyAsync(email, code, null);
        Assert.True(ok.Succeeded);
        Assert.False(ok.UsedRecoveryCode);

        var bad = await verifier.VerifyAsync(email, "000000", null);
        Assert.False(bad.Succeeded);
    }

    [Fact]
    public void TD_ADM_031_AddAdminTotp_RegistersRealSecondFactorVerifier()
    {
        var totp = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminTotpServiceExtensions.cs"));
        Assert.Contains(
            "AddScoped<IAdminSecondFactorVerifier, AdminSecondFactorVerifier>",
            totp,
            StringComparison.Ordinal);
        Assert.DoesNotContain("PendingStep3SecondFactorVerifier", totp, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "Dealoware.Infrastructure", "Admin", "PendingStep3SecondFactorVerifier.cs")));
    }

    [Fact]
    public async Task TD_ADM_031_SC6_F2_BadToken_TwentyThen429_PasswordRuleCounts()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        await ClearResetCountersAsync();

        var tooShort = await PostConfirmAsync(new
        {
            token,
            newPassword = "short-value",
            totp = CurrentTotp()
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(1, await ResetCounterCountAsync());
        await ClearResetCountersAsync();

        HttpResponseMessage? lastExpired = null;
        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            lastExpired?.Dispose();
            lastExpired = await PostConfirmAsync(new
            {
                token = $"bad-token-{i}",
                newPassword = NextPassword,
                totp = CurrentTotp()
            });
            AssertLinkExpired(lastExpired);
        }

        lastExpired!.Dispose();
        var counter = await ResetCounterAsync();
        Assert.NotNull(counter);
        Assert.Equal(AdminResetIpCounter.Threshold, counter.FailureCount);

        using var throttled = await PostConfirmAsync(new
        {
            token = "bad-token-over",
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        await AssertConfirmThrottledAsync(throttled);

        using var goodWhileLocked = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = CurrentTotp()
        });
        await AssertConfirmThrottledAsync(goodWhileLocked);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
    }

    [Fact]
    public async Task TD_ADM_030_SC6_B1_LockedIp_AllowedAfterWindowOrLock_FakeClock()
    {
        Clock().UtcNow = new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        await SeedCredentialAsync();

        HttpResponseMessage? lastOk = null;
        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            lastOk?.Dispose();
            lastOk = await PostRequestAsync(i % 2 == 0 ? CoreOwnerEmail : "nobody@example.com");
            Assert.Equal(HttpStatusCode.OK, lastOk.StatusCode);
        }

        lastOk!.Dispose();
        var locked = await ResetCounterAsync();
        Assert.NotNull(locked);
        Assert.Equal(AdminResetIpCounter.Threshold, locked.FailureCount);
        Assert.True(locked.IsLocked(Clock().UtcNow));

        Clock().Advance(AdminResetIpCounter.Window);
        using var stillLocked = await PostRequestAsync(CoreOwnerEmail);
        await AssertRequestThrottledAsync(stillLocked);
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
        Assert.True((await ResetCounterAsync())!.IsLocked(Clock().UtcNow));

        Clock().Advance(AdminResetIpCounter.LockDuration - AdminResetIpCounter.Window + TimeSpan.FromSeconds(1));
        using var unlocked = await PostRequestAsync(CoreOwnerEmail);
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, await unlocked.Content.ReadAsStringAsync());

        var after = await ResetCounterAsync();
        Assert.NotNull(after);
        Assert.Equal(1, after.FailureCount);
        Assert.False(after.IsLocked(Clock().UtcNow));
        Assert.Null(after.LockedUntil);
    }

    [Fact]
    public async Task TD_ADM_030_SC6_OQ3_EventExactlyFifteenMinutesOld_IsOutside()
    {
        Clock().UtcNow = new DateTimeOffset(2026, 10, 12, 13, 0, 0, TimeSpan.Zero);
        await SeedCredentialAsync();

        using var first = await PostRequestAsync(CoreOwnerEmail);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(1, (await ResetCounterAsync())!.FailureCount);

        Clock().Advance(AdminResetIpCounter.Window);
        using var agedOut = await PostRequestAsync("nobody@example.com");
        Assert.Equal(HttpStatusCode.OK, agedOut.StatusCode);

        var after = await ResetCounterAsync();
        Assert.NotNull(after);
        Assert.Equal(1, after.FailureCount);
        Assert.False(after.IsLocked(Clock().UtcNow));
        Assert.True(after.WindowOpen(Clock().UtcNow));
    }

    [Fact]
    public async Task TD_ADM_031_SC6_B2_ParallelBadTokenBurst_AtMostTwenty()
    {
        await SeedCredentialAsync();
        await ClearResetCountersAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            PostConfirmAsync(new
            {
                token = $"bad-token-burst-{i}",
                newPassword = NextPassword,
                totp = CurrentTotp()
            })));

        try
        {
            Assert.Equal(
                AdminResetIpCounter.Threshold,
                responses.Count(r => r.StatusCode == HttpStatusCode.SeeOther));
            Assert.Equal(
                40 - AdminResetIpCounter.Threshold,
                responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
            Assert.All(
                responses.Where(r => r.StatusCode == HttpStatusCode.SeeOther),
                AssertLinkExpired);
            foreach (var throttled in responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests))
            {
                await AssertConfirmThrottledAsync(throttled);
            }

            var counter = await ResetCounterAsync();
            Assert.NotNull(counter);
            Assert.Equal(AdminResetIpCounter.Threshold, counter.FailureCount);
            Assert.True(counter.IsLocked(Clock().UtcNow));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public void TD_ADM_031_R14_DerivedKey_IsStableAcrossInstances_AndFailsClosed()
    {
        var source = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var first = AdminAntiForgeryService.Create(_ => source, isDevelopment: false);
        var second = AdminAntiForgeryService.Create(_ => source, isDevelopment: false);
        Assert.Equal(first.ComputeRequestToken("browser-nonce"), second.ComputeRequestToken("browser-nonce"));

        var missing = Assert.Throws<InvalidOperationException>(
            () => AdminAntiForgeryService.Create(_ => null, isDevelopment: false));
        Assert.Contains(IpHasher.KeyEnvironmentVariable, missing.Message);
        Assert.DoesNotContain(source, missing.Message);

        var afSource = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminAntiForgeryCookie.cs"));
        Assert.Contains("HKDF.DeriveKey", afSource, StringComparison.Ordinal);
        Assert.Contains("dw-admin-antiforgery-v1", afSource, StringComparison.Ordinal);
        Assert.Contains("dw-admin-af-token-v1", afSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_key = RandomNumberGenerator.GetBytes", afSource, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminAntiForgery.cs")));
    }

    [Fact]
    public void TD_ADM_052_CookieOptions_MatchRouteNote24()
    {
        var options = AdminSessionCookie.CreateOptions();
        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/admin", options.Path);
        Assert.Null(options.Domain);

        var af = AdminAntiForgeryService.CreateOptions();
        Assert.True(af.HttpOnly);
        Assert.True(af.Secure);
        Assert.Equal(SameSiteMode.Strict, af.SameSite);
        Assert.Equal("/admin", af.Path);
        Assert.Null(af.Domain);
    }

    private RecordingMailSender Mail()
        => _factory.Services.GetRequiredService<RecordingMailSender>();

    private async Task<string> WaitForDispatchFailureLogAsync()
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var hit = _factory.LogCollector.Lines.FirstOrDefault(l =>
                l.Contains("Admin mail dispatch failed", StringComparison.Ordinal));
            if (hit is not null)
            {
                return hit;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Dispatch failure was not logged.");
    }

    private FakeAdminClock Clock()
        => _factory.Services.GetRequiredService<FakeAdminClock>();

    private string CurrentTotp()
    {
        Assert.NotNull(_totpSecret);
        return Rfc6238Totp.ComputeCode(_totpSecret, Rfc6238Totp.TimestepAt(Clock().UtcNow));
    }

    private string FirstRecoveryCode()
    {
        Assert.NotNull(_recoveryCodes);
        return Assert.Single(_recoveryCodes.Take(1));
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private HttpClient CreateClientWithoutDefaultOrigin()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Remove("Origin");
        return client;
    }

    private async Task<HttpResponseMessage> SendResetWithOriginAsync(
        IssuedAntiForgery issued,
        string? origin,
        string? referer)
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, issued);
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin);
        }

        if (referer is not null)
        {
            request.Headers.TryAddWithoutValidation("Referer", referer);
        }

        request.Content = JsonContent.Create(new { email = CoreOwnerEmail });
        return await client.SendAsync(request);
    }

    private async Task<string> IssueSignInAntiForgeryAsync(
        HttpClient client,
        string path = AdminSignedOutExemptions.SignIn)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = payload.GetProperty("csrfToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private static HttpRequestMessage SignInJson(string path, object body, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, csrf);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private Task SeedCredentialAsync() => SeedCredentialAsync(enrolled: true);

    private async Task SeedCredentialAsync(bool enrolled)
    {
        Clock().UtcNow = DateTimeOffset.UtcNow;
        Mail().Clear();
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var recoveryHasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminResetIpCounters.RemoveRange(db.AdminResetIpCounters);
        db.AdminPasswordResetTokens.RemoveRange(db.AdminPasswordResetTokens);
        var existingCredential = db.AdminCredentials.SingleOrDefault(c => c.Email == CoreOwnerEmail);
        if (existingCredential is null)
        {
            db.AdminCredentials.Add(AdminCredential.Create(
                CoreOwnerEmail,
                hasher.Hash(CurrentPassword),
                DateTimeOffset.UtcNow));
        }
        else
        {
            existingCredential.ReplacePassword(hasher.Hash(CurrentPassword), DateTimeOffset.UtcNow);
        }

        var existingAccount = db.AdminCoreOwnerAccounts.SingleOrDefault(a => a.Email == CoreOwnerEmail);
        if (existingAccount is not null)
        {
            db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes.Where(c => c.AccountId == existingAccount.Id));
            db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths.Where(p => p.Email == CoreOwnerEmail));
            db.AdminSessions.RemoveRange(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail));
            db.AdminCoreOwnerAccounts.Remove(existingAccount);
            await db.SaveChangesAsync();
        }

        _totpSecret = null;
        _recoveryCodes = null;
        var account = AdminCoreOwnerAccount.Create(CoreOwnerEmail);
        account.SetPasswordHash(hasher.Hash(CurrentPassword));
        if (enrolled)
        {
            _totpSecret = Rfc6238Totp.GenerateSecret();
            _recoveryCodes = AdminRecoveryCodes.Generate();
            foreach (var code in _recoveryCodes)
            {
                db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, recoveryHasher.Hash(code)));
            }

            account.MarkRecoveryCodesIssued();
            account.CompleteEnrollment(protector.Encrypt(_totpSecret));
        }

        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
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

    private async Task<string> InsertResetTokenAsync()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(AdminPasswordResetEndpoints.TokenEntropyBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminPasswordResetTokens.Add(
            AdminPasswordResetToken.Create(
                CoreOwnerEmail,
                AdminPasswordResetToken.HashRaw(raw),
                Clock().UtcNow));
        await db.SaveChangesAsync();
        return raw;
    }

    private async Task<string> RequestRawTokenAsync()
    {
        Mail().Clear();
        var response = await PostRequestAsync(CoreOwnerEmail);
        response.EnsureSuccessStatusCode();
        await Mail().WaitForSentAsync(1);
        var link = Assert.Single(Mail().Sent).TextBody;
        var hashAt = link.IndexOf("#token=", StringComparison.Ordinal);
        Assert.True(hashAt >= 0);
        var token = link[(hashAt + 7)..];
        var end = token.IndexOfAny(['\r', '\n', ' ', '&']);
        if (end >= 0)
        {
            token = token[..end];
        }

        return Uri.UnescapeDataString(token);
    }

    private async Task<HttpResponseMessage> PostRequestAsync(string email)
    {
        var af = await IssueAntiForgeryAsync();
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(new { email });
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostConfirmAsync(object body)
    {
        var af = await IssueAntiForgeryAsync();
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.ConfirmApiPath);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostCheckAsync(object body, string? queryToken = null)
    {
        var af = await IssueAntiForgeryAsync();
        var client = CreateClient();
        var path = AdminPasswordResetEndpoints.ConfirmCheckApiPath;
        if (queryToken is not null)
        {
            path += "?token=" + Uri.EscapeDataString(queryToken);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<IssuedAntiForgery> IssueAntiForgeryAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, AdminSignedOutExemptions.ResetConfirmPage);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains(AdminAntiForgeryCookie.HeaderName));
        var html = await response.Content.ReadAsStringAsync();
        var setCookies = ReadSetCookie(response);
        var setCookie = Assert.Single(
            setCookies,
            c => c.StartsWith(AdminCookieNames.AntiForgery + "=", StringComparison.Ordinal));
        var cookieNonce = setCookie.Split(';', 2)[0].Split('=', 2)[1];
        var match = Regex.Match(html, "<meta name=\"dw-csrf\" content=\"([0-9A-Fa-f]+)\"");
        Assert.True(match.Success, "confirm page must embed the HMAC CSRF token in meta dw-csrf");
        var hmac = match.Groups[1].Value;
        Assert.NotEqual(cookieNonce, hmac);
        Assert.DoesNotContain(cookieNonce, html, StringComparison.Ordinal);
        Assert.DoesNotContain("window.__dwCsrf", html, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Admin-AF", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"af\"", html, StringComparison.Ordinal);
        return new IssuedAntiForgery(hmac, $"{AdminCookieNames.AntiForgery}={cookieNonce}", setCookie, cookieNonce);
    }

    private static void AttachAntiForgery(HttpRequestMessage request, IssuedAntiForgery issued)
    {
        request.Headers.TryAddWithoutValidation("Cookie", issued.CookieHeader);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, issued.Token);
    }

    private static IReadOnlyList<string> ReadSetCookie(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var headerValues))
        {
            return headerValues.ToList();
        }

        if (response.Content.Headers.TryGetValues("Set-Cookie", out var contentValues))
        {
            return contentValues.ToList();
        }

        return [];
    }

    private sealed record IssuedAntiForgery(
        string Token,
        string CookieHeader,
        string SetCookie,
        string CookieNonce);

    private static async Task AssertGenericForbiddenAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("""{"error":"Forbidden"}""", body);
        Assert.DoesNotContain("origin", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("csrf", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("anti-forgery", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Referer", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AdminSignInDeny.GenericCopy, body);
    }

    private static async Task AssertHtmlForbiddenAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>Forbidden</title>", body, StringComparison.Ordinal);
        Assert.Contains("<p>Forbidden</p>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"error\":\"Forbidden\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("origin", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("csrf", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("anti-forgery", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AdminSignInDeny.GenericCopy, body);
    }

    private static async Task AssertWrongFactorUnauthorizedAsync(
        HttpResponseMessage response,
        string submittedPassword)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.SignInGeneric, body);
        Assert.DoesNotContain(submittedPassword, body, StringComparison.Ordinal);
        Assert.DoesNotContain(CurrentPassword, body, StringComparison.Ordinal);
        Assert.DoesNotContain("invalid", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recovery", body, StringComparison.OrdinalIgnoreCase);
        AssertNoStoreNoReferrer(response);
        Assert.False(response.Headers.Location is not null);
    }

    private static void AssertConfirmPageNonceCsp(HttpResponseMessage response, string html)
    {
        var csp = GetHeader(response, "Content-Security-Policy");
        var nonceMatch = Regex.Match(html, "nonce=\"([^\"]+)\"");
        Assert.True(nonceMatch.Success);
        var nonce = nonceMatch.Groups[1].Value;
        Assert.Equal(
            AdminPasswordResetEndpoints.ConfirmPageContentSecurityPolicy(nonce),
            csp);
        Assert.Contains($"script-src 'nonce-{nonce}'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("*", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("sha256-", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("object-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("base-uri 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("form-action 'self'", csp, StringComparison.Ordinal);
        Assert.NotEqual(AdminSecurityHeadersMiddleware.ContentSecurityPolicy, csp);
        Assert.Contains($"nonce=\"{nonce}\"", html, StringComparison.Ordinal);
        Assert.Contains($"src=\"{AdminPasswordResetCopy.ConfirmScriptPath}\"", html, StringComparison.Ordinal);
    }

    private static string GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values)
            || response.Content.Headers.TryGetValues(name, out values))
        {
            return string.Join(",", values);
        }

        return string.Empty;
    }

    private async Task<AdminResetIpCounter?> ResetCounterAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return db.AdminResetIpCounters.SingleOrDefault();
    }

    private async Task<int> ResetCounterCountAsync()
        => (await ResetCounterAsync())?.FailureCount ?? 0;

    private async Task ClearResetCountersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminResetIpCounters.RemoveRange(db.AdminResetIpCounters);
        await db.SaveChangesAsync();
    }

    private static async Task<string> AssertRequestThrottledAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.False(response.Headers.Contains("Retry-After"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.RequestAccepted, body);
        Assert.DoesNotContain("Too many attempts", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("locked", body, StringComparison.OrdinalIgnoreCase);
        return body;
    }

    private static async Task<string> AssertConfirmThrottledAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.False(response.Headers.Contains("Retry-After"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.SignInGeneric, body);
        Assert.DoesNotContain("Too many attempts", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("locked", body, StringComparison.OrdinalIgnoreCase);
        return body;
    }

    private static void AssertLinkExpired(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal(
            AdminSignedOutExemptions.LinkExpiredPage,
            response.Headers.Location?.ToString());
        AssertNoStoreNoReferrer(response);
    }

    private static void AssertNoStoreNoReferrer(HttpResponseMessage response)
    {
        var cacheParts = new List<string>();
        if (response.Headers.CacheControl is { } parsed)
        {
            cacheParts.Add(parsed.ToString());
        }

        if (response.Headers.TryGetValues("Cache-Control", out var headerCc))
        {
            cacheParts.AddRange(headerCc);
        }

        if (response.Content.Headers.TryGetValues("Cache-Control", out var contentCc))
        {
            cacheParts.AddRange(contentCc);
        }

        Assert.Contains("no-store", string.Join(",", cacheParts), StringComparison.OrdinalIgnoreCase);
        Assert.True(
            response.Headers.TryGetValues("Referrer-Policy", out var values)
            && values.Contains("no-referrer"),
            "Referrer-Policy: no-referrer");
    }

    private static string PadBase64Url(string value)
    {
        var std = value.Replace('-', '+').Replace('_', '/');
        return std.PadRight(std.Length + (4 - std.Length % 4) % 4, '=');
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (method == HttpMethod.Post)
        {
            request.Content = JsonContent.Create(new { });
        }

        return await client.SendAsync(request);
    }

    private static HttpRequestMessage AdminGet(string path, Guid sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        return request;
    }

    private async Task<bool> CredentialStillMatchesAsync(string password)
    {
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        var store = scope.ServiceProvider.GetRequiredService<IAdminCredentialStore>();
        var credential = await store.GetByEmailAsync(CoreOwnerEmail);
        return credential is not null && hasher.Verify(password, credential.PasswordHash);
    }

    private static async Task<ThrottleStore> OpenThrottleStoreAsync(string sqliteName)
    {
        if (IsolatedWebApplicationFactory.TryBuildTestPostgresConnectionString(out var postgres))
        {
            var options = new DbContextOptionsBuilder<DealowareDbContext>()
                .UseNpgsql(postgres)
                .Options;
            await using (var created = new DealowareDbContext(options))
            {
                await created.Database.EnsureCreatedAsync();
            }

            return new ThrottleStore(options, null);
        }

        var keepAlive = new SqliteConnection($"Data Source={sqliteName};Mode=Memory;Cache=Shared");
        await keepAlive.OpenAsync();
        await using (var busy = keepAlive.CreateCommand())
        {
            busy.CommandText = "PRAGMA busy_timeout = 5000;";
            await busy.ExecuteNonQueryAsync();
        }

        var sqlite = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(keepAlive.ConnectionString)
            .Options;
        await using (var created = new DealowareDbContext(sqlite))
        {
            await created.Database.EnsureCreatedAsync();
        }

        return new ThrottleStore(sqlite, keepAlive);
    }

    private static async Task ClearThrottleIpAsync(DbContextOptions<DealowareDbContext> options, string ipHmac)
    {
        await using var db = new DealowareDbContext(options);
        await db.AdminResetIpCounters.Where(c => c.IpHmac == ipHmac).ExecuteDeleteAsync();
    }

    private static async Task SeedExpiredLockedRowAsync(
        DbContextOptions<DealowareDbContext> options,
        string ipHmac,
        DateTimeOffset windowStartedAt,
        DateTimeOffset lockedUntil,
        int failureCount)
    {
        await using var db = new DealowareDbContext(options);
        db.AdminResetIpCounters.Add(AdminResetIpCounter.Start(ipHmac, windowStartedAt));
        await db.SaveChangesAsync();
        await db.AdminResetIpCounters
            .Where(c => c.IpHmac == ipHmac)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.FailureCount, failureCount)
                .SetProperty(c => c.WindowStartedAt, windowStartedAt)
                .SetProperty(c => c.LockedUntil, lockedUntil));
    }

    private static Task<bool[]> ParallelReserveAsync(
        DbContextOptions<DealowareDbContext> options,
        string ipHmac,
        DateTimeOffset now,
        int count)
        => Task.WhenAll(Enumerable.Range(0, count).Select(async _ =>
        {
            await using var db = new DealowareDbContext(options);
            return await new AdminResetIpThrottle(db).TryReserveAsync(ipHmac, now, CancellationToken.None);
        }));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repo root.");
    }

    private sealed class ThrottleStore : IAsyncDisposable
    {
        public ThrottleStore(DbContextOptions<DealowareDbContext> options, SqliteConnection? keepAlive)
        {
            Options = options;
            _keepAlive = keepAlive;
        }

        public DbContextOptions<DealowareDbContext> Options { get; }

        private readonly SqliteConnection? _keepAlive;

        public async ValueTask DisposeAsync()
        {
            if (_keepAlive is not null)
                await _keepAlive.DisposeAsync();
        }
    }
}

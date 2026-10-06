using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Admin;
using Dealoware.Application.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
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
            totp = Factor().ValidTotp
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
        Factor().Reset();
        var token = await RequestRawTokenAsync();

        var first = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            recoveryCode = Factor().ValidRecoveryCode
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var token2 = await RequestRawTokenAsync();
        var replayCode = await PostConfirmAsync(new
        {
            token = token2,
            newPassword = "radio station third!",
            recoveryCode = Factor().ValidRecoveryCode
        });
        AssertLinkExpired(replayCode);
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
            totp = Factor().ValidTotp
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
        Assert.Empty(db.AdminSessions.ToList());
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
            totp = Factor().ValidTotp
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        var shortBody = await tooShort.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordRules.TooShortMessage, shortBody);
        Assert.Contains("too_short", shortBody);

        var same = await PostConfirmAsync(new
        {
            token,
            newPassword = CurrentPassword,
            totp = Factor().ValidTotp
        });
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Contains(AdminPasswordRules.SameAsCurrentMessage, await same.Content.ReadAsStringAsync());

        var ok = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = Factor().ValidTotp
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
            totp = Factor().ValidTotp
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var replay = await PostConfirmAsync(new
        {
            token,
            newPassword = "radio station other!",
            totp = Factor().ValidTotp
        });

        var expiredToken = await RequestRawTokenAsync();
        Clock().Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        var expired = await PostConfirmAsync(new
        {
            token = expiredToken,
            newPassword = "radio station other!",
            totp = Factor().ValidTotp
        });

        var random = await PostConfirmAsync(new
        {
            token = "totally-random-token-value",
            newPassword = "radio station other!",
            totp = Factor().ValidTotp
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
        Assert.Contains("history.replaceState", queryBody, StringComparison.Ordinal);
        Assert.Contains("location.hash", queryBody, StringComparison.Ordinal);
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
        Assert.Contains("replaceState", bareBody, StringComparison.Ordinal);
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

        var extraBody = await extraApi.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, extraApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraConfirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongMethodApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongMethodPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingAfConfirm.StatusCode);
        Assert.Equal(extraBody, await extraPage.Content.ReadAsStringAsync());
        Assert.Equal(extraBody, await wrongMethodApi.Content.ReadAsStringAsync());
        Assert.Contains("\"error\":\"Unauthorized\"", extraBody);
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
        bad.Headers.TryAddWithoutValidation(AdminCookieNames.AntiForgeryHeader, "not-the-cookie");
        bad.Content = JsonContent.Create(new { email = CoreOwnerEmail });
        using var invalid = await client.SendAsync(bad);

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
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
            totp = Factor().ValidTotp
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
            totp = Factor().ValidTotp
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
            totp = Factor().ValidTotp
        });
        AssertLinkExpired(stale);

        var ok = await PostConfirmAsync(new
        {
            token = second,
            newPassword = NextPassword,
            totp = Factor().ValidTotp
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
        using var directory = await SendAsync(HttpMethod.Get, "/admin/auth/");
        using var handlerLike = await SendAsync(HttpMethod.Get, "/admin/auth/handler");
        using var postFile = await SendAsync(HttpMethod.Post, "/admin/auth/reset.css");

        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, directory.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, handlerLike.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, postFile.StatusCode);
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
            totp = Factor().ValidTotp
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

        using var knownLocked = await PostRequestAsync(CoreOwnerEmail);
        using var unknownLocked = await PostRequestAsync("nobody@example.com");
        var knownLockedBody = await AssertThrottledAsync(knownLocked);
        var unknownLockedBody = await AssertThrottledAsync(unknownLocked);
        Assert.Equal(knownLockedBody, unknownLockedBody);
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
    }

    [Fact]
    public async Task TD_ADM_031_SC6_F2_BadToken_TwentyThen429_PasswordRuleDoesNotCount()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        await ClearResetCountersAsync();

        var tooShort = await PostConfirmAsync(new
        {
            token,
            newPassword = "short-value",
            totp = Factor().ValidTotp
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Null(await ResetCounterAsync());

        HttpResponseMessage? lastExpired = null;
        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            lastExpired?.Dispose();
            lastExpired = await PostConfirmAsync(new
            {
                token = $"bad-token-{i}",
                newPassword = NextPassword,
                totp = Factor().ValidTotp
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
            totp = Factor().ValidTotp
        });
        await AssertThrottledAsync(throttled);

        using var goodWhileLocked = await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = Factor().ValidTotp
        });
        await AssertThrottledAsync(goodWhileLocked);
        Assert.True(await CredentialStillMatchesAsync(CurrentPassword));
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
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

        var af = AdminAntiForgery.CookieOptions();
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

    private FakeAdminSecondFactorVerifier Factor()
        => _factory.Services.GetRequiredService<FakeAdminSecondFactorVerifier>();

    private FakeAdminClock Clock()
        => _factory.Services.GetRequiredService<FakeAdminClock>();

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task SeedCredentialAsync()
    {
        Factor().Reset();
        Mail().Clear();
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminResetIpCounters.RemoveRange(db.AdminResetIpCounters);
        var existing = db.AdminCredentials.SingleOrDefault(c => c.Email == CoreOwnerEmail);
        if (existing is null)
        {
            db.AdminCredentials.Add(AdminCredential.Create(
                CoreOwnerEmail,
                hasher.Hash(CurrentPassword),
                DateTimeOffset.UtcNow));
        }
        else
        {
            existing.ReplacePassword(hasher.Hash(CurrentPassword), DateTimeOffset.UtcNow);
        }

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

    private async Task<IssuedAntiForgery> IssueAntiForgeryAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, AdminSignedOutExemptions.ResetConfirmPage);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var setCookies = ReadSetCookie(response);
        var setCookie = Assert.Single(
            setCookies,
            c => c.StartsWith(AdminCookieNames.AntiForgery + "=", StringComparison.Ordinal));
        var token = setCookie.Split(';', 2)[0].Split('=', 2)[1];
        if (response.Headers.TryGetValues(AdminCookieNames.AntiForgeryHeader, out var headers))
        {
            Assert.Equal(token, headers.First());
        }

        return new IssuedAntiForgery(token, $"{AdminCookieNames.AntiForgery}={token}", setCookie);
    }

    private static void AttachAntiForgery(HttpRequestMessage request, IssuedAntiForgery issued)
    {
        request.Headers.TryAddWithoutValidation("Cookie", issued.CookieHeader);
        request.Headers.TryAddWithoutValidation(AdminCookieNames.AntiForgeryHeader, issued.Token);
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

    private sealed record IssuedAntiForgery(string Token, string CookieHeader, string SetCookie);

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

    private static async Task<string> AssertThrottledAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.False(response.Headers.Contains("Retry-After"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.Throttled, body);
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
}

using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Http;
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

        var sent = Mail().Sent;
        Assert.Equal(2, sent.Count);
        Assert.All(sent, item =>
        {
            Assert.Equal(CoreOwnerEmail, item.To);
            Assert.Equal(AdminPasswordResetEndpoints.MailSubject, item.Subject);
            Assert.Contains("/admin/reset/confirm?token=", item.TextBody);
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
        Assert.Equal(HttpStatusCode.Unauthorized, skip.StatusCode);
        var skipBody = await skip.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.CompleteFailed, skipBody);

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
        Assert.Equal(HttpStatusCode.Unauthorized, replayCode.StatusCode);
        Assert.Contains(
            AdminPasswordResetCopy.CompleteFailed,
            await replayCode.Content.ReadAsStringAsync());
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

        var client = _factory.CreateClient();
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

        var replayBody = await replay.Content.ReadAsStringAsync();
        var expiredBody = await expired.Content.ReadAsStringAsync();
        var randomBody = await random.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(replay.StatusCode, expired.StatusCode);
        Assert.Equal(replay.StatusCode, random.StatusCode);
        Assert.Equal(replayBody, expiredBody);
        Assert.Equal(replayBody, randomBody);
        Assert.Contains("\"error\":\"Unauthorized\"", replayBody);
    }

    [Fact]
    public async Task TD_ADM_031_ConfirmPage_ReplayExpiredRandom_SameDeny()
    {
        await SeedCredentialAsync();
        var token = await RequestRawTokenAsync();
        await PostConfirmAsync(new
        {
            token,
            newPassword = NextPassword,
            totp = Factor().ValidTotp
        });

        var replay = await GetConfirmPageAsync(token);
        var expiredToken = await RequestRawTokenAsync();
        Clock().Advance(TimeSpan.FromHours(2));
        var expired = await GetConfirmPageAsync(expiredToken);
        var random = await GetConfirmPageAsync("not-a-real-token");

        var replayBody = await replay.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(replay.StatusCode, expired.StatusCode);
        Assert.Equal(replay.StatusCode, random.StatusCode);
        Assert.Equal(replayBody, await expired.Content.ReadAsStringAsync());
        Assert.Equal(replayBody, await random.Content.ReadAsStringAsync());
        Assert.Contains("\"error\":\"Unauthorized\"", replayBody);
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
        using var missingTokenPage = await SendAsync(HttpMethod.Get, "/admin/reset/confirm");
        using var missingTokenApi = await SendAsync(HttpMethod.Post, "/admin/api/auth/reset/confirm");

        var extraBody = await extraApi.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, extraApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraConfirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongMethodApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongMethodPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingTokenPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingTokenApi.StatusCode);
        Assert.Equal(extraBody, await extraPage.Content.ReadAsStringAsync());
        Assert.Equal(extraBody, await wrongMethodApi.Content.ReadAsStringAsync());
        Assert.Equal(extraBody, await missingTokenPage.Content.ReadAsStringAsync());
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

        using var get = await GetConfirmPageAsync(token);
        Assert.NotEqual(HttpStatusCode.Unauthorized, get.StatusCode);

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
            Assert.DoesNotContain(NextPassword, row.ActorEmail);
            Assert.False(string.IsNullOrWhiteSpace(row.IpHmac));
        }
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

    private CapturingAdminMailSender Mail()
        => _factory.Services.GetRequiredService<CapturingAdminMailSender>();

    private FakeAdminSecondFactorVerifier Factor()
        => _factory.Services.GetRequiredService<FakeAdminSecondFactorVerifier>();

    private FakeAdminClock Clock()
        => _factory.Services.GetRequiredService<FakeAdminClock>();

    private async Task SeedCredentialAsync()
    {
        Factor().Reset();
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
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
        var link = Assert.Single(Mail().Sent).TextBody;
        var token = link[(link.IndexOf("token=", StringComparison.Ordinal) + 6)..];
        return Uri.UnescapeDataString(token);
    }

    private async Task<HttpResponseMessage> PostRequestAsync(string email)
    {
        var af = await IssueAntiForgeryAsync();
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.RequestApiPath);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(new { email });
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostConfirmAsync(object body)
    {
        var af = await IssueAntiForgeryAsync();
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AdminPasswordResetEndpoints.ConfirmApiPath);
        request.Headers.Host = AdminHost;
        AttachAntiForgery(request, af);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetConfirmPageAsync(string token)
        => await SendAsync(
            HttpMethod.Get,
            $"{AdminSignedOutExemptions.ResetConfirmPage}?token={Uri.EscapeDataString(token)}");

    private async Task<IssuedAntiForgery> IssueAntiForgeryAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, AdminSignedOutExemptions.ResetPage);
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

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path)
    {
        var client = _factory.CreateClient();
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

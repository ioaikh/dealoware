using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// A7 Step 2: first-time setup (bootstrap). TD-ADM-010 / 011 / 012 / 040 / 150 / 180
/// plus the bootstrap rows of TD-ADM-051. Every password is generated at run time.
/// </summary>
[Collection("WebAppTests")]
public class AdminBootstrapTests : IClassFixture<BootstrapWebApplicationFactory>
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = BootstrapWebApplicationFactory.OwnerEmail;
    private const string LinkExpiredSetup = "/admin/link-expired?for=setup";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminBootstrapTests(BootstrapWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TD_ADM_180_IssueLink_StoresOnlyTheHash_MailsFragmentLink_ValidAtMost24Hours()
    {
        await ResetOwnerAsync();

        var token = await IssueLinkAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = Assert.Single(db.AdminBootstrapTokens.ToList());
            Assert.Equal(AdminBootstrapToken.HashRaw(token), row.TokenHash);
            Assert.DoesNotContain(token, row.TokenHash, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(OwnerEmail, row.Email);
            Assert.Null(row.ConsumedAt);
            Assert.Equal(Clock().UtcNow, row.CreatedAt);
            Assert.Equal(TimeSpan.FromHours(24), row.ExpiresAt - row.CreatedAt);
            Assert.False(row.IsUsable(row.CreatedAt + TimeSpan.FromHours(24)));
        }

        // 32 random bytes, hex: at least 128 bits of entropy.
        Assert.Equal(64, token.Length);

        var mail = Assert.Single(Mail().Sent);
        Assert.Equal(OwnerEmail, mail.To);
        Assert.Equal(AdminMailDispatcher.BootstrapSubject, mail.Subject);
        Assert.Contains(
            "https://admin.core.dealoware.com/admin/bootstrap#token=" + Uri.EscapeDataString(token),
            mail.TextBody,
            StringComparison.Ordinal);
        Assert.DoesNotContain("?token=", mail.TextBody, StringComparison.Ordinal);
        Assert.DoesNotContain("password", mail.TextBody, StringComparison.OrdinalIgnoreCase);

        var audit = await AuditAsync();
        var issued = Assert.Single(audit, e => e.Action == AdminAuthEvents.BootstrapLinkIssued);
        Assert.Equal(OwnerEmail, issued.ActorEmail);
        Assert.Equal(AdminBootstrapService.CommandOrigin, issued.IpHmac);
        AssertNotInAudit(audit, token, AdminBootstrapToken.HashRaw(token));
    }

    [Fact]
    public async Task TD_ADM_180_NewerLink_InvalidatesOlder()
    {
        await ResetOwnerAsync();
        var older = await IssueLinkAsync();
        var newer = await IssueLinkAsync();
        var password = NewPassword();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var rows = db.AdminBootstrapTokens.ToList();
            Assert.Equal(2, rows.Count);
            Assert.NotNull(rows.Single(t => t.TokenHash == AdminBootstrapToken.HashRaw(older)).ConsumedAt);
            Assert.Null(rows.Single(t => t.TokenHash == AdminBootstrapToken.HashRaw(newer)).ConsumedAt);
        }

        using var first = await PostBootstrapAsync(Body(older, password));
        AssertInvalidLink(first);
        Assert.False(await OwnerRowsExistAsync());

        using var second = await PostBootstrapAsync(Body(newer, password));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(await StoredPasswordMatchesAsync(password));
    }

    [Fact]
    public async Task TD_ADM_010_UsedLink_CannotBeUsedTwice()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var password = NewPassword();
        var other = NewPassword();

        using var first = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var replay = await PostBootstrapAsync(Body(token, other));
        AssertInvalidLink(replay);

        Assert.True(await StoredPasswordMatchesAsync(password));
        Assert.False(await StoredPasswordMatchesAsync(other));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Single(db.AdminCredentials.ToList());
        Assert.Single(db.AdminCoreOwnerAccounts.ToList());
        Assert.NotNull(db.AdminBootstrapTokens.Single().ConsumedAt);
    }

    [Fact]
    public async Task TD_ADM_010_UnknownTruncatedExpiredSupersededAndUsedLinks_GetTheSameReply()
    {
        var password = NewPassword();

        await ResetOwnerAsync();
        var live = await IssueLinkAsync();
        using var unknown = await PostBootstrapAsync(Body(Guid.NewGuid().ToString("N"), password));
        using var truncated = await PostBootstrapAsync(Body(live[..^4], password));
        using var missing = await PostBootstrapAsync(new { newPassword = password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        var unknownReply = await ReplyAsync(unknown);
        var truncatedReply = await ReplyAsync(truncated);
        var missingReply = await ReplyAsync(missing);

        await ResetOwnerAsync();
        var aged = await IssueLinkAsync();
        Clock().Advance(AdminBootstrapToken.Lifetime);
        using var expired = await PostBootstrapAsync(Body(aged, password));
        var expiredReply = await ReplyAsync(expired);

        await ResetOwnerAsync();
        var superseded = await IssueLinkAsync();
        await IssueLinkAsync();
        using var old = await PostBootstrapAsync(Body(superseded, password));
        var supersededReply = await ReplyAsync(old);

        await ResetOwnerAsync();
        var spent = await IssueLinkAsync();
        using var ok = await PostBootstrapAsync(Body(spent, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using var again = await PostBootstrapAsync(Body(spent, password));
        var usedReply = await ReplyAsync(again);

        AssertInvalidLink(unknown);
        Assert.Equal(unknownReply, truncatedReply);
        Assert.Equal(unknownReply, missingReply);
        Assert.Equal(unknownReply, expiredReply);
        Assert.Equal(unknownReply, supersededReply);
        Assert.Equal(unknownReply, usedReply);
        Assert.Equal(string.Empty, unknownReply.Body);

        // The reply leads to the one S-A5 setup page: no form, no hint which case applied.
        using var page = await SendAsync(HttpMethod.Get, unknownReply.Location!);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains(AdminPasswordResetCopy.LinkExpiredSetupBody, html, StringComparison.Ordinal);
        Assert.DoesNotContain("<form", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TD_ADM_010_Link_WorksJustBefore24Hours_AndNotAt24Hours()
    {
        await ResetOwnerAsync();
        var late = await IssueLinkAsync();
        Clock().Advance(AdminBootstrapToken.Lifetime - TimeSpan.FromSeconds(1));
        using var inTime = await PostBootstrapAsync(Body(late, NewPassword()));
        Assert.Equal(HttpStatusCode.OK, inTime.StatusCode);

        await ResetOwnerAsync();
        var tooLate = await IssueLinkAsync();
        Clock().Advance(AdminBootstrapToken.Lifetime);
        using var atLimit = await PostBootstrapAsync(Body(tooLate, NewPassword()));
        AssertInvalidLink(atLimit);
        Assert.False(await OwnerRowsExistAsync());
    }

    [Fact]
    public async Task TD_ADM_040_BadTurnstile_IsRejectedBeforeLinkAndPassword_CountsNothing_AuditsCaptchaFailed()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var password = NewPassword();
        var fake = _factory.Services.GetRequiredService<FakeTurnstileVerifier>();
        const string badTurnstile = "expired-turnstile-response";

        using var missing = await PostBootstrapAsync(new { token, newPassword = password });
        using var invalid = await PostBootstrapAsync(new { token, newPassword = password, turnstileToken = badTurnstile });
        HttpResponseMessage unavailable;
        fake.Unavailable = true;
        try
        {
            unavailable = await PostBootstrapAsync(Body(token, password));
        }
        finally
        {
            fake.Unavailable = false;
        }

        // A bad link and a rule-breaking password behind a bad Turnstile answer look the same:
        // neither was looked at.
        using var nothingValid = await PostBootstrapAsync(new
        {
            token = Guid.NewGuid().ToString("N"),
            newPassword = TooShortPassword(),
            turnstileToken = badTurnstile
        });

        var expected = $$"""{"error":"{{AdminAuthCopy.VerificationFailed}}"}""";
        foreach (var response in new[] { missing, invalid, unavailable, nothingValid })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
            Assert.Null(response.Headers.Location);
            Assert.False(response.Headers.Contains("Retry-After"));
        }

        unavailable.Dispose();

        // 25+ bad-Turnstile submissions from one IP never throttle.
        for (var i = 0; i < AdminResetIpCounter.Threshold + 5; i++)
        {
            using var flood = await PostBootstrapAsync(new { token, newPassword = password, turnstileToken = badTurnstile });
            Assert.Equal(HttpStatusCode.Unauthorized, flood.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Empty(db.AdminResetIpCounters.ToList());
            Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);
            Assert.Empty(db.AdminCredentials.ToList());
            Assert.Empty(db.AdminCoreOwnerAccounts.ToList());
        }

        var audit = await AuditAsync();
        var captcha = audit.Where(e => e.ReasonClass == AdminAuthReason.CaptchaFailed).ToList();
        Assert.Equal(4 + AdminResetIpCounter.Threshold + 5, captcha.Count);
        Assert.All(captcha, e =>
        {
            Assert.Equal(AdminAuthEvents.BootstrapCompleteFailed, e.Action);
            Assert.Equal("anonymous", e.ActorEmail);
            AssertKeyedIpHmac(e.IpHmac);
        });
        Assert.DoesNotContain(audit, e => e.ReasonClass == AdminAuthEvents.ReasonInvalidLink);
        AssertNotInAudit(audit, token, password, badTurnstile, FakeTurnstileVerifier.ValidToken);

        // A valid Turnstile response proceeds to the next step.
        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_011_FirstPassword_CreatesAccountAndCredentialRows_AndNoSession()
    {
        await ResetOwnerAsync();
        var password = NewPassword();

        // No login before bootstrap: there is no password anywhere to match.
        using var before = await PostJsonAsync(
            AdminSignedOutExemptions.ApiSignIn,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);

        var token = await IssueLinkAsync();
        using var response = await PostBootstrapAsync(Body(token, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("""{"passwordSet":true,"next":"sign-in"}""", body);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);
        Assert.Empty(ReadSetCookie(response));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
            var account = Assert.Single(db.AdminCoreOwnerAccounts.ToList());
            var credential = Assert.Single(db.AdminCredentials.ToList());
            Assert.Equal(OwnerEmail, account.Email);
            Assert.Equal(OwnerEmail, credential.Email);
            Assert.Equal(credential.PasswordHash, account.PasswordHash);
            Assert.StartsWith(
                $"{Pbkdf2AdminPasswordHasher.AlgorithmPrefix}${Pbkdf2AdminPasswordHasher.Iterations}$",
                credential.PasswordHash,
                StringComparison.Ordinal);
            Assert.DoesNotContain(password, credential.PasswordHash, StringComparison.Ordinal);
            Assert.True(hasher.Verify(password, credential.PasswordHash));
            Assert.False(account.IsTotpEnrolled);
            Assert.NotNull(db.AdminBootstrapTokens.Single().ConsumedAt);

            // Bootstrap creates no session and no pending sign-in of any kind.
            Assert.Empty(db.AdminSessions.ToList());
            Assert.Empty(db.AdminPendingAuths.ToList());
            Assert.Empty(db.AdminAuthTokens.ToList());
        }

        using var me = await SendAsync(HttpMethod.Get, "/admin/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var audit = await AuditAsync();
        var complete = Assert.Single(audit, e => e.Action == AdminAuthEvents.BootstrapComplete);
        Assert.Equal(OwnerEmail, complete.ActorEmail);
        Assert.Null(complete.ReasonClass);
        AssertKeyedIpHmac(complete.IpHmac);
    }

    [Fact]
    public async Task TD_ADM_012_AfterBootstrap_SignInLeadsToTotpEnrolment_WithNoSessionYet()
    {
        await ResetOwnerAsync();
        var password = NewPassword();
        var token = await IssueLinkAsync();
        using var set = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        using var wrong = await PostJsonAsync(
            AdminSignedOutExemptions.ApiSignIn,
            new { email = OwnerEmail, password = NewPassword(), turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        using var signIn = await PostJsonAsync(
            AdminSignedOutExemptions.ApiSignIn,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var payload = await signIn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("enroll", payload.GetProperty("next").GetString());

        var cookies = ReadSetCookie(signIn);
        Assert.Contains(cookies, c => c.StartsWith(AdminPendingAuthCookie.Name + "=", StringComparison.Ordinal));
        Assert.DoesNotContain(cookies, c => c.StartsWith(AdminCookieNames.Session + "=", StringComparison.Ordinal));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Empty(db.AdminSessions.ToList());
    }

    [Fact]
    public async Task TD_ADM_010_PasswordRuleReject_NamesTheRule_AndLeavesTheLinkUsable()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();

        var tooShort = TooShortPassword();
        using var shortReply = await PostBootstrapAsync(Body(token, tooShort));
        await AssertRuleRejectAsync(shortReply, AdminPasswordRules.TooShortMessage, "too_short", tooShort);

        var tooLong = string.Concat(Enumerable.Repeat(Guid.NewGuid().ToString("N"), 5))[..(AdminPasswordRules.MaxCodePoints + 1)];
        using var longReply = await PostBootstrapAsync(Body(token, tooLong));
        await AssertRuleRejectAsync(longReply, AdminPasswordRules.TooLongMessage, "too_long", tooLong);

        using var commonReply = await PostBootstrapAsync(Body(token, AdminPasswordRules.BundledBlocklistFixtureValue));
        await AssertRuleRejectAsync(
            commonReply,
            AdminPasswordRules.CommonOrContextMessage,
            "common_or_context",
            AdminPasswordRules.BundledBlocklistFixtureValue);

        // Context word: the owner address local part inside an otherwise long-enough value.
        var withContextWord = OwnerEmail[..OwnerEmail.IndexOf('@')] + Guid.NewGuid().ToString("N");
        using var contextReply = await PostBootstrapAsync(Body(token, withContextWord));
        await AssertRuleRejectAsync(
            contextReply,
            AdminPasswordRules.CommonOrContextMessage,
            "common_or_context",
            withContextWord);

        Assert.False(await OwnerRowsExistAsync());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);
        }

        var password = NewPassword();
        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(await StoredPasswordMatchesAsync(password));
    }

    [Fact]
    public async Task TD_ADM_051_ThrottledIp_Gets429WithGenericSignInBody_AndAValidLinkIsNotConsumed()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var password = NewPassword();

        for (var i = 0; i < AdminResetIpCounter.Threshold; i++)
        {
            using var bad = await PostBootstrapAsync(Body(Guid.NewGuid().ToString("N"), password));
            AssertInvalidLink(bad);
        }

        var counter = await ResetCounterAsync();
        Assert.NotNull(counter);
        Assert.Equal(AdminResetIpCounter.Threshold, counter.FailureCount);
        Assert.True(counter.IsLocked(Clock().UtcNow));
        var auditBefore = (await AuditAsync()).Count;

        using var badWhileThrottled = await PostBootstrapAsync(Body(Guid.NewGuid().ToString("N"), password));
        using var validWhileThrottled = await PostBootstrapAsync(Body(token, password));
        using var pageWhileThrottled = await PostBootstrapAsync(Body(token, password), AdminBootstrapEndpoints.PagePath);
        using var badTurnstileWhileThrottled = await PostBootstrapAsync(new { token, newPassword = password });

        var expected = $$"""{"message":"{{AdminAuthCopy.SignInFailure}}"}""";
        foreach (var response in new[] { badWhileThrottled, validWhileThrottled, pageWhileThrottled, badTurnstileWhileThrottled })
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
            Assert.False(response.Headers.Contains("Retry-After"));
            Assert.Null(response.Headers.Location);
        }

        // The generic sign-in text, not the invalid-link reply, and never the word "locked".
        Assert.Equal(AdminPasswordResetCopy.SignInGeneric, AdminAuthCopy.SignInFailure);
        Assert.DoesNotContain("locked", expected, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("link", expected, StringComparison.OrdinalIgnoreCase);

        // Throttled attempts are not counted, change nothing and do not reach Turnstile or the audit log.
        Assert.Equal(AdminResetIpCounter.Threshold, (await ResetCounterAsync())!.FailureCount);
        Assert.Equal(auditBefore, (await AuditAsync()).Count);
        Assert.False(await OwnerRowsExistAsync());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);
        }

        // Flat 30 minutes later the same link (still inside its 24 hours) works.
        Clock().Advance(AdminResetIpCounter.LockDuration + TimeSpan.FromSeconds(1));
        using var afterLock = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, afterLock.StatusCode);
        Assert.True(await StoredPasswordMatchesAsync(password));
    }

    [Fact]
    public async Task TD_ADM_150_NoTokenPasswordOrHashInLogsOrAudit()
    {
        await ResetOwnerAsync();
        var password = NewPassword();
        var rejected = TooShortPassword();

        var token = await IssueLinkAsync();
        using var bad = await PostBootstrapAsync(Body(Guid.NewGuid().ToString("N"), password));
        using var ruleReject = await PostBootstrapAsync(Body(token, rejected));
        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        string passwordHash;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            passwordHash = db.AdminCredentials.Single().PasswordHash;
        }

        var secrets = new[] { token, AdminBootstrapToken.HashRaw(token), password, rejected, passwordHash };
        var logs = string.Join("\n", _factory.LogCollector.Lines);
        Assert.NotEmpty(logs);
        foreach (var secret in secrets)
        {
            Assert.DoesNotContain(secret, logs, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("#token=", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(OwnerEmail, logs, StringComparison.OrdinalIgnoreCase);

        var audit = await AuditAsync();
        Assert.Contains(audit, e => e.Action == AdminAuthEvents.BootstrapLinkIssued);
        Assert.Contains(audit, e => e.ReasonClass == AdminAuthEvents.ReasonInvalidLink);
        Assert.Contains(audit, e => e.Action == AdminAuthEvents.BootstrapComplete);
        AssertNotInAudit(audit, secrets);
        AssertNotInAudit(audit, Pbkdf2AdminPasswordHasher.AlgorithmPrefix);

        foreach (var response in new[] { bad, ruleReject, ok })
        {
            var body = await response.Content.ReadAsStringAsync();
            foreach (var secret in secrets)
            {
                Assert.DoesNotContain(secret, body, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task TD_ADM_150_MailDispatchFailure_IsReportedAsNotSent_AndLogsTypeAndTraceOnly()
    {
        await ResetOwnerAsync();
        Mail().FailWithDisabledSender = true;
        try
        {
            AdminBootstrapLinkResult result;
            using (var scope = _factory.Services.CreateScope())
            {
                result = await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>()
                    .IssueLinkAsync(CancellationToken.None);
            }

            Assert.Equal(AdminBootstrapLinkOutcome.Issued, result.Outcome);
            Assert.False(await result.MailSent.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Empty(Mail().Sent);
        }
        finally
        {
            Mail().FailWithDisabledSender = false;
        }

        var dispatch = Assert.Single(
            _factory.LogCollector.Lines,
            l => l.Contains("Admin mail dispatch failed", StringComparison.Ordinal));
        Assert.Contains("ExceptionType=System.InvalidOperationException", dispatch, StringComparison.Ordinal);
        Assert.Contains("TraceId=", dispatch, StringComparison.Ordinal);

        string storedHash;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            storedHash = db.AdminBootstrapTokens.Single().TokenHash;
        }

        var logs = string.Join("\n", _factory.LogCollector.Lines);
        Assert.DoesNotContain(storedHash, logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#token=", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminMailPagePaths.BootstrapPath, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(OwnerEmail, logs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TD_ADM_010_LinkIssuedForAnotherIdentity_IsInvalid()
    {
        await ResetOwnerAsync();
        var raw = await InsertTokenRowAsync("someone-else@example.test");

        using var response = await PostBootstrapAsync(Body(raw, NewPassword()));

        AssertInvalidLink(response);
        Assert.False(await OwnerRowsExistAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TD_ADM_011_OwnerAlreadyHasAPassword_NoLinkCanReplaceIt(bool credentialRow)
    {
        await ResetOwnerAsync();
        var existing = NewPassword();
        await SeedExistingPasswordAsync(existing, credentialRow);
        var raw = await InsertTokenRowAsync(OwnerEmail);

        using var response = await PostBootstrapAsync(Body(raw, NewPassword()));

        AssertInvalidLink(response);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        if (credentialRow)
        {
            Assert.True(hasher.Verify(existing, db.AdminCredentials.Single().PasswordHash));
            Assert.Empty(db.AdminCoreOwnerAccounts.ToList());
        }
        else
        {
            Assert.True(hasher.Verify(existing, db.AdminCoreOwnerAccounts.Single().PasswordHash!));
            Assert.Empty(db.AdminCredentials.ToList());
        }
    }

    [Fact]
    public async Task TD_ADM_011_OwnerRowWithoutAPassword_IsCompleted_NotDuplicated()
    {
        await ResetOwnerAsync();
        Guid accountId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var account = AdminCoreOwnerAccount.Create(OwnerEmail, Clock().UtcNow);
            db.AdminCoreOwnerAccounts.Add(account);
            await db.SaveChangesAsync();
            accountId = account.Id;
        }

        var token = await IssueLinkAsync();
        var password = NewPassword();
        using var response = await PostBootstrapAsync(Body(token, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await StoredPasswordMatchesAsync(password));
        using var check = _factory.Services.CreateScope();
        var after = check.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(accountId, Assert.Single(after.AdminCoreOwnerAccounts.ToList()).Id);
        Assert.Single(after.AdminCredentials.ToList());
    }

    [Fact]
    public async Task TD_ADM_011_LinkConsumeAndBothOwnerRows_AreOneTransaction()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var password = NewPassword();

        // Make the credential insert fail, as a lost race on the owner rows would.
        const string trigger = "bootstrap_test_refuse_credential";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            if (!db.Database.IsSqlite())
            {
                return;
            }

            await db.Database.ExecuteSqlRawAsync(
                $"CREATE TRIGGER \"{trigger}\" BEFORE INSERT ON \"AdminCredentials\" "
                + "BEGIN SELECT RAISE(ABORT, 'refused'); END;");
        }

        try
        {
            using var failed = await PostBootstrapAsync(Body(token, password));
            AssertInvalidLink(failed);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER \"{trigger}\";");
        }

        // Nothing was half-written: no account row, and the link was not spent.
        Assert.False(await OwnerRowsExistAsync());
        var audit = await AuditAsync();
        Assert.DoesNotContain(audit, e => e.Action == AdminAuthEvents.BootstrapComplete);
        Assert.Contains(audit, e => e.ReasonClass == AdminAuthEvents.ReasonInvalidLink);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);
        }

        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(await StoredPasswordMatchesAsync(password));
    }

    [Fact]
    public async Task TD_ADM_011_PageRoute_SetsThePassword_LikeTheApiRoute()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var password = NewPassword();

        using var bad = await PostBootstrapAsync(Body(Guid.NewGuid().ToString("N"), password), AdminBootstrapEndpoints.PagePath);
        AssertInvalidLink(bad);

        using var ok = await PostBootstrapAsync(Body(token, password), AdminBootstrapEndpoints.PagePath);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Empty(ReadSetCookie(ok));
        Assert.True(await StoredPasswordMatchesAsync(password));
    }

    [Fact]
    public async Task TD_ADM_010_BootstrapGet_TakesNoToken_AndChangesNothing()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();

        using var get = await SendAsync(
            HttpMethod.Get,
            $"{AdminBootstrapEndpoints.PagePath}?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Contains("no-store", get.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);
        var html = await get.Content.ReadAsStringAsync();
        Assert.DoesNotContain(token, html, StringComparison.Ordinal);
        var cookie = Assert.Single(ReadSetCookie(get));
        Assert.StartsWith(AdminCookieNames.AntiForgery + "=", cookie, StringComparison.Ordinal);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);
            Assert.Empty(db.AdminResetIpCounters.ToList());
        }

        // A token in the query string of the POST is ignored: body only.
        var password = NewPassword();
        using var queryOnly = await PostBootstrapAsync(
            new { newPassword = password, turnstileToken = FakeTurnstileVerifier.ValidToken },
            $"{AdminBootstrapEndpoints.ApiPath}?token={Uri.EscapeDataString(token)}");
        AssertInvalidLink(queryOnly);
        Assert.False(await OwnerRowsExistAsync());

        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_010_BootstrapRows_ExemptOnlyForExactPathAndMethod_AndNeedAntiForgery()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var body = Body(token, NewPassword());

        using var apiNoCsrf = await SendJsonWithoutAntiForgeryAsync(AdminBootstrapEndpoints.ApiPath, body);
        using var pageNoCsrf = await SendJsonWithoutAntiForgeryAsync(AdminBootstrapEndpoints.PagePath, body);
        Assert.Equal(HttpStatusCode.Forbidden, apiNoCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pageNoCsrf.StatusCode);
        Assert.Equal("""{"error":"Forbidden"}""", await apiNoCsrf.Content.ReadAsStringAsync());

        using var extraSegment = await PostBootstrapAsync(body, AdminBootstrapEndpoints.ApiPath + "/extra");
        using var extraPage = await PostBootstrapAsync(body, AdminBootstrapEndpoints.PagePath + "/extra");
        using var apiGet = await SendAsync(HttpMethod.Get, AdminBootstrapEndpoints.ApiPath);
        Assert.Equal(HttpStatusCode.Unauthorized, extraSegment.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, extraPage.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, apiGet.StatusCode);

        // C1: near-miss spellings of the page path are not the exempt row.
        foreach (var nearMiss in new[]
                 {
                     "/admin/bootstrap/", "/admin/bootstrap%2fextra", "/admin//bootstrap", "/admin/bootstrap;x"
                 })
        {
            using var denied = await SendAsync(HttpMethod.Get, nearMiss);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }

        // C7: the page exists on the admin host only.
        using var wrongHost = new HttpRequestMessage(HttpMethod.Get, AdminBootstrapEndpoints.PagePath);
        wrongHost.Headers.Host = "core.dealoware.com";
        using var otherHost = await CreateClient().SendAsync(wrongHost);
        Assert.Equal(HttpStatusCode.NotFound, otherHost.StatusCode);
        Assert.Empty(ReadSetCookie(otherHost));

        Assert.False(await OwnerRowsExistAsync());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Null(db.AdminBootstrapTokens.Single().ConsumedAt);

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>();
        Assert.Equal(1, CountHandlers(endpoints, AdminBootstrapEndpoints.PagePath, HttpMethods.Get));
        Assert.Equal(1, CountHandlers(endpoints, AdminBootstrapEndpoints.PagePath, HttpMethods.Post));
        Assert.Equal(1, CountHandlers(endpoints, AdminBootstrapEndpoints.ApiPath, HttpMethods.Post));
        Assert.Equal(0, CountHandlers(endpoints, AdminBootstrapEndpoints.ApiPath, HttpMethods.Get));
    }

    [Fact]
    public async Task TD_ADM_180_SC6_R10_PostgresParallelConfirm_SameBootstrapLink_ExactlyOneWins()
    {
        await ResetOwnerAsync();
        var token = await IssueLinkAsync();
        var passwords = new[] { NewPassword(), NewPassword() };

        var responses = await Task.WhenAll(passwords.Select(p => PostBootstrapAsync(Body(token, p))));
        try
        {
            await AssertExactlyOneWonAsync(responses, passwords);
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
        Assert.Equal(1, db.AdminBootstrapTokens.Count(t => t.ConsumedAt != null));
    }

    [Fact]
    public async Task TD_ADM_180_SC6_R10_PostgresParallelConfirm_TwoBootstrapLinks_ExactlyOneWins()
    {
        // Two usable links can only coexist if two issue commands race; the owner rows
        // (unique email) still let exactly one password through.
        await ResetOwnerAsync();
        var tokens = new[] { await InsertTokenRowAsync(OwnerEmail), await InsertTokenRowAsync(OwnerEmail) };
        var passwords = new[] { NewPassword(), NewPassword() };

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(i => PostBootstrapAsync(Body(tokens[i], passwords[i]))));
        try
        {
            await AssertExactlyOneWonAsync(responses, passwords);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        foreach (var token in tokens)
        {
            using var late = await PostBootstrapAsync(Body(token, NewPassword()));
            AssertInvalidLink(late);
        }
    }

    [Fact]
    public async Task BootstrapLinkCommand_FreshOwner_SendsTheLink_AndPrintsNoTokenOrAddress()
    {
        await ResetOwnerAsync();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(
            _factory.Services, output, error, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(AdminBootstrapLinkCommand.SentMessage, output.ToString().Trim());
        Assert.Equal(string.Empty, error.ToString());

        var mail = Assert.Single(Mail().Sent);
        var token = TokenFromMail(mail.TextBody);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Equal(AdminBootstrapToken.HashRaw(token), db.AdminBootstrapTokens.Single().TokenHash);
        }

        var printed = output.ToString();
        Assert.DoesNotContain(token, printed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#token=", printed, StringComparison.Ordinal);
        Assert.DoesNotContain(OwnerEmail, printed, StringComparison.OrdinalIgnoreCase);

        // The link the command mailed is the one the password-set request accepts.
        var password = NewPassword();
        using var ok = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BootstrapLinkCommand_OwnerHasAPassword_ReportsAlreadySetUp_AndDoesNothing(bool credentialRow)
    {
        await ResetOwnerAsync();
        await SeedExistingPasswordAsync(NewPassword(), credentialRow);
        var auditBefore = (await AuditAsync()).Count;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(
            _factory.Services, output, error, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(AdminBootstrapLinkCommand.AlreadySetUpMessage, output.ToString().Trim());
        Assert.Contains("already set up", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());

        // Give a wrongly queued mail time to leave the queue before asserting none did.
        await Task.Delay(100);
        Assert.Empty(Mail().Sent);
        Assert.Equal(auditBefore, (await AuditAsync()).Count);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Empty(db.AdminBootstrapTokens.ToList());
    }

    [Fact]
    public async Task BootstrapLinkCommand_AfterSetup_RefusesAndLeavesThePasswordAlone()
    {
        await ResetOwnerAsync();
        var password = NewPassword();
        var token = await IssueLinkAsync();
        using var set = await PostBootstrapAsync(Body(token, password));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Mail().Clear();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(
            _factory.Services, output, error, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(AdminBootstrapLinkCommand.AlreadySetUpMessage, output.ToString().Trim());
        Assert.True(await StoredPasswordMatchesAsync(password));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.DoesNotContain(db.AdminBootstrapTokens.ToList(), t => t.ConsumedAt is null);
    }

    [Fact]
    public async Task BootstrapLinkCommand_MailCannotBeSent_ExitsWithFailure()
    {
        await ResetOwnerAsync();
        Mail().FailWithDisabledSender = true;
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode;
        try
        {
            exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(
                _factory.Services, output, error, CancellationToken.None);
        }
        finally
        {
            Mail().FailWithDisabledSender = false;
        }

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(AdminBootstrapLinkCommand.MailFailedMessage, error.ToString().Trim());
        Assert.DoesNotContain("#token=", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(OwnerEmail, error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapLinkCommand_OwnServices_IssueOnce_ThenReportAlreadySetUp()
    {
        // The command's own service graph (no web host), on a private in-memory SQLite store.
        var connectionString = $"Data Source=BootstrapCommand_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["CoreOwner:Email"] = "  Owner@Example.Test "
            })
            .Build();
        var mail = new RecordingMailSender();
        var services = new ServiceCollection();
        services.AddLogging();
        AdminBootstrapLinkCommand.AddServices(services, configuration, _ => null, strictNonDevelopment: false);
        services.AddSingleton<IAdminMailSender>(mail);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DealowareDbContext>().Database.EnsureCreatedAsync();
        }

        var hosted = provider.GetServices<IHostedService>().ToList();
        Assert.IsType<AdminMailSendQueue>(Assert.Single(hosted));
        await hosted[0].StartAsync(CancellationToken.None);
        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Assert.Equal(0, await AdminBootstrapLinkCommand.ExecuteAsync(provider, output, error, CancellationToken.None));
            Assert.Equal(AdminBootstrapLinkCommand.SentMessage, output.ToString().Trim());
            var sent = Assert.Single(mail.Sent);
            Assert.Equal(OwnerEmail, sent.To);

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
                Assert.Equal(AdminBootstrapToken.HashRaw(TokenFromMail(sent.TextBody)), db.AdminBootstrapTokens.Single().TokenHash);
                db.AdminCredentials.Add(AdminCredential.Create(OwnerEmail, hasher.Hash(NewPassword()), DateTimeOffset.UtcNow));
                await db.SaveChangesAsync();
            }

            using var secondOutput = new StringWriter();
            Assert.Equal(0, await AdminBootstrapLinkCommand.ExecuteAsync(provider, secondOutput, error, CancellationToken.None));
            Assert.Equal(AdminBootstrapLinkCommand.AlreadySetUpMessage, secondOutput.ToString().Trim());
            Assert.Single(mail.Sent);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            await hosted[0].StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task BootstrapLinkCommand_OwnerNotConfigured_FailsClosed()
    {
        var connectionString = $"Data Source=BootstrapNoOwner_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["CoreOwner:Email"] = " "
            })
            .Build();
        var mail = new RecordingMailSender();
        var services = new ServiceCollection();
        services.AddLogging();
        AdminBootstrapLinkCommand.AddServices(services, configuration, _ => null, strictNonDevelopment: false);
        services.AddSingleton<IAdminMailSender>(mail);
        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DealowareDbContext>().Database.EnsureCreatedAsync();
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(provider, output, error, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Equal(AdminBootstrapLinkCommand.OwnerNotConfiguredMessage, error.ToString().Trim());
        Assert.Empty(mail.Sent);
        await using var check = provider.CreateAsyncScope();
        Assert.Empty(check.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminBootstrapTokens.ToList());
    }

    [Theory]
    [InlineData(new[] { "bootstrap-link" }, true)]
    [InlineData(new[] { "BOOTSTRAP-LINK" }, true)]
    [InlineData(new[] { "bootstrap-link", "--environment", "Production" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "migrate" }, false)]
    [InlineData(new[] { "--bootstrap-link" }, false)]
    [InlineData(new[] { "run", "bootstrap-link" }, false)]
    public void IsBootstrapLinkArgs_DetectsTheCommand(string[] args, bool expected)
    {
        Assert.Equal(expected, AdminBootstrapLinkCommand.IsBootstrapLinkArgs(args));
    }

    [Fact]
    public void Program_BootstrapLinkCommand_ReturnsBeforeTheWebHost_AndIsTheOnlyCaller()
    {
        var root = RepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "Dealoware.Api", "Program.cs"));
        var command = program.IndexOf(
            "Environment.ExitCode = await AdminBootstrapLinkCommand.RunAsync(args);",
            StringComparison.Ordinal);
        var earlyReturn = program.IndexOf("return;", command, StringComparison.Ordinal);
        var webHost = program.IndexOf("WebApplication.CreateBuilder", StringComparison.Ordinal);
        Assert.True(command >= 0, "Program.cs must keep the bootstrap-link entrypoint.");
        Assert.True(earlyReturn > command && earlyReturn < webHost, "bootstrap-link must return before the web host.");

        // No request path issues a link: the command is the only production trigger.
        var callers = Directory
            .GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains(".IssueLinkAsync(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal(new[] { "AdminBootstrapLinkCommand.cs" }, callers);
    }

    [Fact]
    public void BootstrapToken_IsHashedLikeResetTokens_AndUsableOnlyInsideItsLifetime()
    {
        var raw = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var hash = AdminBootstrapToken.HashRaw(raw);
        Assert.Equal(AdminPasswordResetToken.HashRaw(raw), hash);
        Assert.Equal(64, hash.Length);

        var token = AdminBootstrapToken.Create(OwnerEmail, hash, now);
        Assert.Equal(TimeSpan.FromHours(24), AdminBootstrapToken.Lifetime);
        Assert.Equal(now + AdminBootstrapToken.Lifetime, token.ExpiresAt);
        Assert.True(token.IsUsable(now));
        Assert.True(token.IsUsable(token.ExpiresAt - TimeSpan.FromTicks(1)));
        Assert.False(token.IsUsable(token.ExpiresAt));
        Assert.Throws<ArgumentException>(() => AdminBootstrapToken.Create(" ", hash, now));
        Assert.Throws<ArgumentException>(() => AdminBootstrapToken.Create(OwnerEmail, " ", now));
    }

    private static object Body(string token, string password)
        => new { token, newPassword = password, turnstileToken = FakeTurnstileVerifier.ValidToken };

    /// <summary>Run-time password that meets the rules: 40 hex characters, no context word.</summary>
    private static string NewPassword()
        => $"{Guid.NewGuid():N}{Guid.NewGuid():N}"[..40];

    private static string TooShortPassword()
        => Guid.NewGuid().ToString("N")[..(AdminPasswordRules.MinCodePoints - 1)];

    private RecordingMailSender Mail()
        => _factory.Services.GetRequiredService<RecordingMailSender>();

    private FakeAdminClock Clock()
        => _factory.Services.GetRequiredService<FakeAdminClock>();

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    /// <summary>A CoreOwner with no rows at all, as before first-time setup.</summary>
    private async Task ResetOwnerAsync()
    {
        Clock().UtcNow = DateTimeOffset.UtcNow;
        _factory.Services.GetRequiredService<FakeTurnstileVerifier>().Unavailable = false;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.AdminBootstrapTokens.RemoveRange(db.AdminBootstrapTokens);
            db.AdminResetIpCounters.RemoveRange(db.AdminResetIpCounters);
            db.AdminCredentials.RemoveRange(db.AdminCredentials);
            db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes);
            db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths);
            db.AdminSessions.RemoveRange(db.AdminSessions);
            db.AdminCoreOwnerAccounts.RemoveRange(db.AdminCoreOwnerAccounts);
            db.AdminAuthTokens.RemoveRange(db.AdminAuthTokens);
            db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
            db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
            db.AdminAuditLog.RemoveRange(db.AdminAuditLog);
            await db.SaveChangesAsync();
        }

        Mail().Clear();
        _factory.LogCollector.Clear();
    }

    /// <summary>Issues a link the way the one-shot command does and reads the token from the mail.</summary>
    private async Task<string> IssueLinkAsync()
    {
        Mail().Clear();
        AdminBootstrapLinkResult result;
        using (var scope = _factory.Services.CreateScope())
        {
            result = await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>()
                .IssueLinkAsync(CancellationToken.None);
        }

        Assert.Equal(AdminBootstrapLinkOutcome.Issued, result.Outcome);
        Assert.True(await result.MailSent.WaitAsync(TimeSpan.FromSeconds(5)));
        return TokenFromMail(Assert.Single(Mail().Sent).TextBody);
    }

    private static string TokenFromMail(string body)
    {
        const string marker = "#token=";
        var at = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0, "bootstrap mail must carry the token in the URL fragment");
        var token = body[(at + marker.Length)..];
        var end = token.IndexOfAny(['\r', '\n', ' ', '&']);
        return Uri.UnescapeDataString(end >= 0 ? token[..end] : token);
    }

    /// <summary>A usable token row written straight to the store, bypassing the issue path.</summary>
    private async Task<string> InsertTokenRowAsync(string email)
    {
        var raw = AdminAuthService.CreateOpaqueToken();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminBootstrapTokens.Add(
            AdminBootstrapToken.Create(email, AdminBootstrapToken.HashRaw(raw), Clock().UtcNow));
        await db.SaveChangesAsync();
        return raw;
    }

    private async Task SeedExistingPasswordAsync(string password, bool credentialRow)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>().Hash(password);
        if (credentialRow)
        {
            db.AdminCredentials.Add(AdminCredential.Create(OwnerEmail, hash, Clock().UtcNow));
        }
        else
        {
            var account = AdminCoreOwnerAccount.Create(OwnerEmail, Clock().UtcNow);
            account.SetPasswordHash(hash);
            db.AdminCoreOwnerAccounts.Add(account);
        }

        await db.SaveChangesAsync();
    }

    private async Task<bool> OwnerRowsExistAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminCredentials.AnyAsync() || await db.AdminCoreOwnerAccounts.AnyAsync();
    }

    /// <summary>True when both owner rows hold a hash of this password.</summary>
    private async Task<bool> StoredPasswordMatchesAsync(string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
        var credential = db.AdminCredentials.SingleOrDefault(c => c.Email == OwnerEmail);
        var account = db.AdminCoreOwnerAccounts.SingleOrDefault(a => a.Email == OwnerEmail);
        return credential is not null
               && account?.PasswordHash is not null
               && hasher.Verify(password, credential.PasswordHash)
               && hasher.Verify(password, account.PasswordHash);
    }

    private async Task AssertExactlyOneWonAsync(HttpResponseMessage[] responses, string[] passwords)
    {
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.SeeOther));
        AssertInvalidLink(responses.Single(r => r.StatusCode == HttpStatusCode.SeeOther));

        var winner = Array.FindIndex(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(await StoredPasswordMatchesAsync(passwords[winner]));
        Assert.False(await StoredPasswordMatchesAsync(passwords[1 - winner]));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Single(db.AdminCredentials.ToList());
        Assert.Single(db.AdminCoreOwnerAccounts.ToList());
        Assert.Single(db.AdminAuditLog.ToList(), e => e.Action == AdminAuthEvents.BootstrapComplete);
    }

    private async Task<AdminResetIpCounter?> ResetCounterAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminResetIpCounters.AsNoTracking().SingleOrDefaultAsync();
    }

    private async Task<List<AdminAuditEntry>> AuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuditLog.AsNoTracking().ToListAsync();
    }

    private static void AssertNotInAudit(IEnumerable<AdminAuditEntry> rows, params string[] secrets)
    {
        foreach (var row in rows)
        {
            var cells = new[]
            {
                row.Action, row.ActorEmail, row.IpHmac, row.ReasonClass, row.EntityType,
                row.BeforeSnapshot, row.AfterSnapshot
            };
            foreach (var cell in cells)
            {
                foreach (var secret in secrets)
                {
                    Assert.DoesNotContain(secret, cell ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    /// <summary>Base64 HMAC-SHA256 (32 bytes), never a raw address.</summary>
    private static void AssertKeyedIpHmac(string ipHmac)
    {
        Assert.Equal(32, Convert.FromBase64String(ipHmac).Length);
        Assert.False(IPAddress.TryParse(ipHmac, out _));
    }

    private static void AssertInvalidLink(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal(LinkExpiredSetup, response.Headers.Location?.ToString());
        Assert.Equal(AdminPasswordResetEndpoints.LinkExpiredSetupLocation, LinkExpiredSetup);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);
        Assert.True(
            response.Headers.TryGetValues("Referrer-Policy", out var values) && values.Contains("no-referrer"),
            "Referrer-Policy: no-referrer");
        Assert.Empty(ReadSetCookie(response));
    }

    private static async Task AssertRuleRejectAsync(
        HttpResponseMessage response,
        string message,
        string reason,
        string submitted)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(message, payload.GetProperty("error").GetString());
        Assert.Equal(reason, payload.GetProperty("reason").GetString());
        Assert.DoesNotContain(submitted, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private sealed record Reply(HttpStatusCode Status, string? Location, string Body);

    private static async Task<Reply> ReplyAsync(HttpResponseMessage response)
        => new(
            response.StatusCode,
            response.Headers.Location?.ToString(),
            await response.Content.ReadAsStringAsync());

    private Task<HttpResponseMessage> PostBootstrapAsync(object body, string path = AdminBootstrapEndpoints.ApiPath)
        => PostJsonAsync(path, body);

    /// <summary>POST with the keyed anti-forgery token the bootstrap GET issues.</summary>
    private async Task<HttpResponseMessage> PostJsonAsync(string path, object body)
    {
        var (token, cookie) = await IssueAntiForgeryAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, token);
        request.Content = JsonContent.Create(body);
        return await CreateClient().SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendJsonWithoutAntiForgeryAsync(string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        request.Content = JsonContent.Create(body);
        return await CreateClient().SendAsync(request);
    }

    private async Task<(string Token, string Cookie)> IssueAntiForgeryAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, AdminBootstrapEndpoints.PagePath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("bootstrap", payload.GetProperty("screen").GetString());
        var token = payload.GetProperty("csrfToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        var setCookie = Assert.Single(
            ReadSetCookie(response),
            c => c.StartsWith(AdminCookieNames.AntiForgery + "=", StringComparison.Ordinal));
        var nonce = setCookie.Split(';', 2)[0].Split('=', 2)[1];
        Assert.NotEqual(nonce, token);
        return (token!, $"{AdminCookieNames.AntiForgery}={nonce}");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        return await CreateClient().SendAsync(request);
    }

    private static IReadOnlyList<string> ReadSetCookie(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    private static int CountHandlers(EndpointDataSource source, string path, string method)
        => source.Endpoints.OfType<RouteEndpoint>().Count(endpoint =>
            string.Equals(endpoint.RoutePattern.RawText, path, StringComparison.OrdinalIgnoreCase)
            && (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
                .Contains(method, StringComparer.OrdinalIgnoreCase) ?? false));

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

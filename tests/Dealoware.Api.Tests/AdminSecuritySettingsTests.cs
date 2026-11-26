using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dealoware.Api.Tests;

/// <summary>A CoreOwner set through test config, on its own PostgreSQL database when DB_HOST is set.</summary>
public sealed class SecuritySettingsWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_secsettings";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// T04 S-A12 security settings server actions, through the real endpoints and
/// the real session gate. Each test names the TD-ADM case it covers:
/// TD-ADM-170 (TOTP replay), TD-ADM-171 (recovery codes), TD-ADM-172 (failure
/// counting), TD-ADM-173 (no-store, nothing in a URL), TD-ADM-174 (step-up
/// within 5 minutes, other sessions end), TD-ADM-UI-auth-13 / -18 / -19 (what
/// the page needs from the server), and the S-A12 password-change note v1.4.
/// </summary>
[Collection("WebAppTests")]
public class AdminSecuritySettingsTests : IClassFixture<SecuritySettingsWebApplicationFactory>
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = SecuritySettingsWebApplicationFactory.OwnerEmail;
    private const string CurrentPassword = "radio station prior!";
    private const string NextPassword = "radio station later!";

    private readonly SecuritySettingsWebApplicationFactory _factory;
    private byte[] _totpSecret = [];
    private IReadOnlyList<string> _recoveryCodes = [];

    public AdminSecuritySettingsTests(SecuritySettingsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- Password change (S-A12 note v1.4) ----------------------------------

    // TD-ADM-UI-auth-13 (server part), note v1.4 "On success": every session ends, this one included.
    [Fact]
    public async Task Sa12Password_Success_ChangesPassword_EndsEverySession_KeepsAuthenticator()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var cipherBefore = (await AccountAsync()).TotpSecretCipher;
        var pending = await SeedPendingSignInAsync();
        var before = await AuditIdsAsync();
        var code = Totp();

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = code },
            a);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        var body = await response.Content.ReadAsStringAsync();
        using (var json = JsonDocument.Parse(body))
        {
            Assert.Equal(AdminSecuritySettingsCopy.PasswordChanged, json.RootElement.GetProperty("message").GetString());
            Assert.True(json.RootElement.GetProperty("signedOut").GetBoolean());
            Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        }
        AssertSessionCookieCleared(response);

        Assert.False(await SessionExistsAsync(a));
        Assert.False(await SessionExistsAsync(b));
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/admin/api/me", a)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/admin/api/me", b)).StatusCode);

        var account = await AccountAsync();
        Assert.True(AdminPasswordHasher.Verify(NextPassword, account.PasswordHash!));
        Assert.False(AdminPasswordHasher.Verify(CurrentPassword, account.PasswordHash!));
        Assert.True(AdminPasswordHasher.Verify(NextPassword, (await CredentialAsync())!.PasswordHash));
        Assert.Equal(cipherBefore, account.TotpSecretCipher);
        Assert.True(account.IsTotpEnrolled);
        Assert.Equal(CurrentStep(), account.LastUsedTotpTimestep);
        Assert.True(await PendingConsumedAsync(pending));

        var rows = await NewAuditAsync(before);
        var changed = Assert.Single(rows, r => r.Action == AdminAuditActions.PasswordChanged);
        Assert.Equal(OwnerEmail, changed.ActorEmail);
        AssertNoSecretIn(rows, CurrentPassword, NextPassword, code);
        AssertLogsClean(CurrentPassword, NextPassword, code);
    }

    // TD-ADM-171 (burned on use, each use audited), note v1.4 "recovery burned atomically".
    [Fact]
    public async Task Sa12Password_RecoveryCode_BurnedWithTheChange_AndAudited()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var before = await AuditIdsAsync();
        var recovery = _recoveryCodes[3];

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, recoveryCode = recovery },
            a);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AdminRecoveryCodes.Count - 1, await UnusedCodeCountAsync());
        Assert.True(await RecoveryCodeUsedAsync(recovery));
        Assert.True(AdminPasswordHasher.Verify(NextPassword, (await AccountAsync()).PasswordHash!));
        Assert.Null((await AccountAsync()).LastUsedTotpTimestep);
        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuditActions.PasswordChanged);
        Assert.Single(rows, r => r.Action == AdminAuditActions.RecoveryCodeUse);
        AssertNoSecretIn(rows, recovery, CurrentPassword, NextPassword);
    }

    public static TheoryData<string, string?, string?, string> WrongStepUps => new()
    {
        // currentPassword, totp ("T" = a valid one), recovery ("R" = a valid one), audit reason
        { "wrong current password!", "T", null, AdminAuthReason.BadPassword },
        { CurrentPassword, "000000", null, AdminAuthReason.Bad2Fa },
        { CurrentPassword, null, null, AdminAuthReason.Bad2Fa },
        { CurrentPassword, "T", "R", AdminAuthReason.Bad2Fa },
        { CurrentPassword, null, "AAAA-BBBB-CCCC", AdminAuthReason.Bad2Fa },
        { string.Empty, "T", null, AdminAuthReason.BadPassword }
    };

    // Note v1.4 "Wrong current password or code" + Security confirm (a); TD-ADM-172 (one shared counter).
    [Theory]
    [MemberData(nameof(WrongStepUps))]
    public async Task Sa12Password_FailedStepUp_OneGenericReply_CountsOnce_Audited(
        string currentPassword,
        string? totp,
        string? recovery,
        string reason)
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var before = await AuditIdsAsync();
        var totpCode = totp == "T" ? Totp() : totp;
        var recoveryCode = recovery == "R" ? _recoveryCodes[0] : recovery;

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword, newPassword = NextPassword, totpCode, recoveryCode },
            a);

        await AssertStepUpFailedAsync(response);
        Assert.Equal(1, await AccountFailuresAsync());
        Assert.Equal(0, await IpFailuresAsync());
        Assert.True(await SessionExistsAsync(a));
        var account = await AccountAsync();
        Assert.True(AdminPasswordHasher.Verify(CurrentPassword, account.PasswordHash!));
        Assert.Null(account.LastUsedTotpTimestep);
        Assert.Equal(AdminRecoveryCodes.Count, await UnusedCodeCountAsync());

        var rows = await NewAuditAsync(before);
        var failed = Assert.Single(rows);
        Assert.Equal(AdminAuditActions.PasswordChangeFailed, failed.Action);
        Assert.Equal(reason, failed.ReasonClass);
        AssertNoSecretIn(rows, currentPassword, NextPassword, totpCode, recoveryCode);
    }

    public static TheoryData<string, string> RuleRejects => new()
    {
        { "short pass 1!", "too_short" },
        { new string('x', 120) + " quite long!", "too_long" },
        { "my dealoware password two!", "common_or_context" },
        { CurrentPassword, "same_as_current" },
        // The same as the current password after NFKC (full-width letters and "!").
        { "ｒａｄｉｏ ｓｔａｔｉｏｎ ｐｒｉｏｒ！", "same_as_current" }
    };

    // Note v1.4 "New password": rules; rule rejects count on nothing, write no failure event, spend no code.
    [Theory]
    [MemberData(nameof(RuleRejects))]
    public async Task Sa12Password_RuleReject_NamedReason_CountsNothing_SpendsNoCode(string proposed, string reason)
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var before = await AuditIdsAsync();
        var code = Totp();

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = proposed, totpCode = code },
            a);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(reason, json.RootElement.GetProperty("reason").GetString());
        Assert.Equal(0, await AccountFailuresAsync());
        Assert.Empty(await NewAuditAsync(before));
        Assert.Null((await AccountAsync()).LastUsedTotpTimestep);
        Assert.True(await SessionExistsAsync(a));

        // The same code still works: it was not spent by the reject.
        using var ok = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = code },
            a);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    // Note v1.4 Security confirm (b) + Chief UI/UX (a); TD-ADM-172: sign-in and S-A12 failures share one counter.
    [Fact]
    public async Task Sa12Password_FailureThatLocks_EndsEverySession_IncludingThisOne()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var pending = await SeedPendingSignInAsync();
        await SeedAccountFailuresAsync(AdminAuthService.AccountFailureLimit - 1);
        var before = await AuditIdsAsync();

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = "wrong current password!", newPassword = NextPassword, totpCode = Totp() },
            a);

        await AssertSignedOutAsync(response);
        Assert.False(await SessionExistsAsync(a));
        Assert.False(await SessionExistsAsync(b));
        Assert.True(await PendingConsumedAsync(pending));
        Assert.True(await AccountLockedAsync());
        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuthAction.LockStart);
        Assert.Single(rows, r => r.Action == AdminAuditActions.PasswordChangeFailed);

        // Locked: a new session is refused the same way and nothing is evaluated or counted.
        var c = await NewSessionAsync();
        var failuresAtLock = await AccountFailuresAsync();
        using var again = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = Totp() },
            c);
        await AssertSignedOutAsync(again);
        Assert.False(await SessionExistsAsync(c));
        Assert.Equal(failuresAtLock, await AccountFailuresAsync());
        Assert.True(AdminPasswordHasher.Verify(CurrentPassword, (await AccountAsync()).PasswordHash!));
    }

    // TD-ADM-170 (last-accepted step blocks replay) on this submit, note v1.4 "TOTP replay".
    [Fact]
    public async Task Sa12Password_ReplayedOrOlderTotpStep_RefusedAndCounted()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var code = Totp();
        using (var stepUp = await PostAsync(
                   AdminSecuritySettingsEndpoints.StepUpPath,
                   new { currentPassword = CurrentPassword, totpCode = code },
                   a))
        {
            Assert.Equal(HttpStatusCode.OK, stepUp.StatusCode);
        }

        using var replay = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = code },
            a);
        await AssertStepUpFailedAsync(replay);

        using var older = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = Totp(-1) },
            a);
        await AssertStepUpFailedAsync(older);

        Assert.Equal(2, await AccountFailuresAsync());
        Assert.True(AdminPasswordHasher.Verify(CurrentPassword, (await AccountAsync()).PasswordHash!));
    }

    // Note v1.4: the burn and the step are in the same transaction as the password update.
    [Fact]
    public async Task Sa12Password_FailureAfterTheBurn_RollsBackBurnStepAndPassword()
    {
        await SeedAsync();
        await using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminCredentialStore>();
            services.AddScoped<IAdminCredentialStore, ThrowingCredentialStore>();
        }));
        failing.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var recovery = _recoveryCodes[5];

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.PasswordPath,
            new { currentPassword = CurrentPassword, newPassword = NextPassword, recoveryCode = recovery },
            a,
            failing);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(recovery, await response.Content.ReadAsStringAsync());
        Assert.False(await RecoveryCodeUsedAsync(recovery));
        Assert.Equal(AdminRecoveryCodes.Count, await UnusedCodeCountAsync());
        Assert.True(AdminPasswordHasher.Verify(CurrentPassword, (await AccountAsync()).PasswordHash!));
        Assert.True(await SessionExistsAsync(a));
        Assert.True(await SessionExistsAsync(b));
        Assert.Equal(0, await AccountFailuresAsync());
        AssertLogsClean(recovery, CurrentPassword, NextPassword);
    }

    // Double submit: the second identical POST neither burns again nor changes again.
    [Fact]
    public async Task Sa12Password_SecondIdenticalSubmit_RefusedByTheGate_NoSecondBurn()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var before = await AuditIdsAsync();
        var body = new { currentPassword = CurrentPassword, newPassword = NextPassword, recoveryCode = _recoveryCodes[1] };

        using var first = await PostAsync(AdminSecuritySettingsEndpoints.PasswordPath, body, a);
        using var second = await PostAsync(AdminSecuritySettingsEndpoints.PasswordPath, body, a);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal("""{"error":"Unauthorized"}""", await second.Content.ReadAsStringAsync());
        Assert.Equal(AdminRecoveryCodes.Count - 1, await UnusedCodeCountAsync());
        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuditActions.PasswordChanged);
        Assert.Single(rows, r => r.Action == AdminAuditActions.RecoveryCodeUse);
        Assert.Equal(0, await AccountFailuresAsync());
    }

    // Note v1.4 "Transport": POST with anti-forgery; the existing gates apply; no exemption was added.
    [Fact]
    public async Task Sa12_EveryPost_BehindSessionOriginAndAntiForgery_NoExemption()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var body = new { currentPassword = CurrentPassword, newPassword = NextPassword, totpCode = Totp() };
        string[] posts =
        [
            AdminSecuritySettingsEndpoints.PasswordPath,
            AdminSecuritySettingsEndpoints.StepUpPath,
            AdminSecuritySettingsEndpoints.RecoveryCodesPath,
            AdminSecuritySettingsEndpoints.AuthenticatorPath,
            AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath
        ];

        foreach (var path in posts)
        {
            using var noSession = await PostAsync(path, body, session: null);
            Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
            Assert.Equal("""{"error":"Unauthorized"}""", await noSession.Content.ReadAsStringAsync());

            using var noToken = await PostAsync(path, body, a, antiForgery: false);
            Assert.Equal(HttpStatusCode.Forbidden, noToken.StatusCode);
            Assert.Equal("""{"error":"Forbidden"}""", await noToken.Content.ReadAsStringAsync());

            using var otherSite = await PostAsync(path, body, a, origin: "https://evil.example");
            Assert.Equal(HttpStatusCode.Forbidden, otherSite.StatusCode);

            Assert.DoesNotContain(AdminSignedOutExemptions.Rules, r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
        }

        Assert.Equal(0, await AccountFailuresAsync());
        Assert.True(AdminPasswordHasher.Verify(CurrentPassword, (await AccountAsync()).PasswordHash!));
        Assert.Equal(AdminRecoveryCodes.Count, await UnusedCodeCountAsync());
    }

    // ---- Step-up and new recovery codes (TD-ADM-171, TD-ADM-174) -------------

    // TD-ADM-174 step 1: a live session alone is not enough.
    [Fact]
    public async Task Sa12Regenerate_AndReenrol_WithTheSessionOnly_Refused()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var hashes = await CodeHashesAsync();

        using var regenerate = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a);
        await AssertStepUpRequiredAsync(regenerate);
        using var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a);
        await AssertStepUpRequiredAsync(start);
        using var confirm = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath, new { code = Totp() }, a);
        await AssertStepUpRequiredAsync(confirm);

        Assert.Equal(hashes, await CodeHashesAsync());
        Assert.Equal(0, await AccountFailuresAsync());
        Assert.True(await SessionExistsAsync(a));
    }

    // TD-ADM-174 step 2: wrong password + valid code, valid password + wrong code: refused, no factor named, counted.
    [Theory]
    [MemberData(nameof(WrongStepUps))]
    public async Task Sa12StepUp_Wrong_Refused_Counted_Audited(
        string currentPassword,
        string? totp,
        string? recovery,
        string reason)
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var before = await AuditIdsAsync();
        var totpCode = totp == "T" ? Totp() : totp;
        var recoveryCode = recovery == "R" ? _recoveryCodes[0] : recovery;

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.StepUpPath,
            new { currentPassword, totpCode, recoveryCode },
            a);

        await AssertStepUpFailedAsync(response);
        Assert.Equal(1, await AccountFailuresAsync());
        Assert.Equal(0, await IpFailuresAsync());
        Assert.Null(await StepUpRowAsync(a));
        var failed = Assert.Single(await NewAuditAsync(before));
        Assert.Equal(AdminAuditActions.StepUpFailed, failed.Action);
        Assert.Equal(reason, failed.ReasonClass);

        using var regenerate = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a);
        await AssertStepUpRequiredAsync(regenerate);
    }

    // TD-ADM-171 + TD-ADM-174: 10 new codes from a CSPRNG (≥50 bits), stored hashed, the set replaced,
    // old codes refused, shown once in the POST reply, every other session ended, audited.
    [Fact]
    public async Task Sa12Regenerate_AfterStepUp_ReplacesTheSet_ShownOnce_EndsOtherSessions()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var c = await NewSessionAsync();
        var oldCodes = _recoveryCodes;
        var code = Totp();
        using (var stepUp = await PostAsync(
                   AdminSecuritySettingsEndpoints.StepUpPath,
                   new { currentPassword = CurrentPassword, totpCode = code },
                   a))
        {
            Assert.Equal(HttpStatusCode.OK, stepUp.StatusCode);
            using var stepJson = JsonDocument.Parse(await stepUp.Content.ReadAsStringAsync());
            Assert.Equal(
                Clock().UtcNow + AdminSessionStepUp.Window,
                stepJson.RootElement.GetProperty("stepUpExpiresAt").GetDateTimeOffset());
        }

        var before = await AuditIdsAsync();
        using var response = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        Assert.False(response.Headers.Contains("Location"));
        var text = await response.Content.ReadAsStringAsync();
        var codes = ReadCodes(text);
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, item => Assert.Matches("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$", item));
        Assert.True(12 * Math.Log2(32) >= 50);
        Assert.Empty(codes.Intersect(oldCodes));
        foreach (var header in response.Headers.Concat(response.Content.Headers))
            Assert.DoesNotContain(header.Value, v => codes.Any(item => v.Contains(item, StringComparison.Ordinal)));

        // Stored as keyed hashes only: the new set, and nothing of the old one.
        var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
        var stored = await CodeHashesAsync();
        Assert.Equal(AdminRecoveryCodes.Count, stored.Count);
        Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
        Assert.All(oldCodes, item => Assert.DoesNotContain(hasher.Hash(item), stored));
        Assert.DoesNotContain(stored, s => codes.Any(item => s.Contains(item, StringComparison.Ordinal)));
        Assert.Null((await AccountAsync()).RecoveryCodesRevealCipher);

        // Every other session ended; this one did not.
        Assert.True(await SessionExistsAsync(a));
        Assert.False(await SessionExistsAsync(b));
        Assert.False(await SessionExistsAsync(c));

        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuditActions.RecoveryCodesRegenerated);
        AssertNoSecretIn(rows, codes.ToArray());

        // Never retrievable afterwards: no GET answers with them.
        foreach (var path in new[] { AdminSecuritySettingsEndpoints.BasePath, AdminSecuritySettingsEndpoints.ActivityPath })
        {
            using var get = await GetAsync(path, a);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var getText = await get.Content.ReadAsStringAsync();
            Assert.DoesNotContain(codes, item => getText.Contains(item, StringComparison.Ordinal));
        }

        // An old code no longer passes a step-up; a new one does.
        Clock().Advance(TimeSpan.FromSeconds(30));
        using (var old = await PostAsync(
                   AdminSecuritySettingsEndpoints.StepUpPath,
                   new { currentPassword = CurrentPassword, recoveryCode = oldCodes[2] },
                   a))
        {
            await AssertStepUpFailedAsync(old);
        }

        using (var fresh = await PostAsync(
                   AdminSecuritySettingsEndpoints.StepUpPath,
                   new { currentPassword = CurrentPassword, recoveryCode = codes[2] },
                   a))
        {
            Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
        }

        Assert.Equal(AdminRecoveryCodes.Count - 1, await UnusedCodeCountAsync());
        AssertLogsClean(codes.Concat(oldCodes).Append(code).Append(CurrentPassword).ToArray());
    }

    // Double submit: one step-up issues one set; the second POST issues nothing.
    [Fact]
    public async Task Sa12Regenerate_SecondSubmitUnderTheSameStepUp_IssuesNothing()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);

        using var first = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var codes = ReadCodes(await first.Content.ReadAsStringAsync());
        var stored = await CodeHashesAsync();

        using var second = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a);
        await AssertStepUpRequiredAsync(second);
        Assert.Equal(stored, await CodeHashesAsync());
        var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
        Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
    }

    // Double submit, at the same time: exactly one set is issued and it is the one in the 200 reply.
    [Fact]
    public async Task Sa12Regenerate_TwoSubmitsAtOnce_ExactlyOneSetIssued()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);

        var responses = await Task.WhenAll(
            PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a),
            PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a));
        try
        {
            var ok = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            var refused = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);
            await AssertStepUpRequiredAsync(refused);
            var codes = ReadCodes(await ok.Content.ReadAsStringAsync());
            var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
            var stored = await CodeHashesAsync();
            Assert.Equal(AdminRecoveryCodes.Count, stored.Count);
            Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    // Lock order: a password change spending a recovery code and a new set of codes, at the same moment.
    // Both take the account row first, so on PostgreSQL neither deadlocks (40P01 would be a 500), and
    // exactly one of them wins whichever runs first; the state is the winner's alone.
    [Fact]
    public async Task Sa12PasswordWithRecoveryCode_AndRegenerate_AtOnce_OneWins_NoDeadlock()
    {
        for (var round = 0; round < 5; round++)
        {
            await SeedAsync();
            var a = await NewSessionAsync();
            var b = await NewSessionAsync();
            await StepUpAsync(a);
            var recovery = _recoveryCodes[round];

            var responses = await Task.WhenAll(
                PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a),
                PostAsync(
                    AdminSecuritySettingsEndpoints.PasswordPath,
                    new { currentPassword = CurrentPassword, newPassword = NextPassword, recoveryCode = recovery },
                    b));
            try
            {
                var (regenerate, password) = (responses[0], responses[1]);
                Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);
                Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);

                var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
                var stored = await CodeHashesAsync();
                var passwordHash = (await AccountAsync()).PasswordHash!;
                if (regenerate.StatusCode == HttpStatusCode.OK)
                {
                    var codes = ReadCodes(await regenerate.Content.ReadAsStringAsync());
                    Assert.Equal(AdminRecoveryCodes.Count, stored.Count);
                    Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
                    Assert.True(AdminPasswordHasher.Verify(CurrentPassword, passwordHash));
                }
                else
                {
                    Assert.True(AdminPasswordHasher.Verify(NextPassword, passwordHash));
                    Assert.Equal(AdminRecoveryCodes.Count - 1, stored.Count);
                    Assert.DoesNotContain(hasher.Hash(recovery), stored);
                    Assert.All(_recoveryCodes.Where(c => c != recovery), item => Assert.Contains(hasher.Hash(item), stored));
                    Assert.False(await SessionExistsAsync(a));
                }
            }
            finally
            {
                foreach (var response in responses)
                    response.Dispose();
            }
        }
    }

    // TD-ADM-UI-auth-18 step 2: "Create new codes" with the password and a recovery code sent with it.
    [Fact]
    public async Task Sa12Regenerate_ProofSentWithTheAction_RecoveryCode_Works()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var before = await AuditIdsAsync();

        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.RecoveryCodesPath,
            new { currentPassword = CurrentPassword, recoveryCode = _recoveryCodes[4] },
            a);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AdminRecoveryCodes.Count, ReadCodes(await response.Content.ReadAsStringAsync()).Count);
        Assert.Equal(AdminRecoveryCodes.Count, await UnusedCodeCountAsync());
        Assert.False(await SessionExistsAsync(b));
        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuditActions.RecoveryCodeUse);
        Assert.Single(rows, r => r.Action == AdminAuditActions.RecoveryCodesRegenerated);

        // With a wrong proof sent along, nothing is issued and the failure counts.
        using var wrong = await PostAsync(
            AdminSecuritySettingsEndpoints.RecoveryCodesPath,
            new { currentPassword = "wrong current password!", totpCode = Totp() },
            a);
        await AssertStepUpFailedAsync(wrong);
        Assert.Equal(1, await AccountFailuresAsync());
    }

    // TD-ADM-174 steps 3 and 4: within 5 minutes one step-up serves both actions; 5 min 1 s later it is asked again.
    [Fact]
    public async Task Sa12StepUp_LastsFiveMinutes_ForBothActions_ThenIsAskedAgain()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);

        Clock().Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
        using (var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a))
        {
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        }

        using (var regenerate = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a))
        {
            Assert.Equal(HttpStatusCode.OK, regenerate.StatusCode);
        }

        await StepUpAsync(a);
        Clock().Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        using (var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a))
        {
            await AssertStepUpRequiredAsync(start);
        }

        using (var regenerate = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, a))
        {
            await AssertStepUpRequiredAsync(regenerate);
        }

        using var state = await GetAsync(AdminSecuritySettingsEndpoints.BasePath, a);
        using var json = JsonDocument.Parse(await state.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("stepUpExpiresAt").ValueKind);
    }

    // TD-ADM-174: the step-up belongs to the session that gave it.
    [Fact]
    public async Task Sa12StepUp_DoesNotCarryToAnotherSession()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        await StepUpAsync(a);

        using var regenerate = await PostAsync(AdminSecuritySettingsEndpoints.RecoveryCodesPath, new { }, b);
        await AssertStepUpRequiredAsync(regenerate);
        using var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, b);
        await AssertStepUpRequiredAsync(start);
    }

    // ---- Authenticator re-enrolment (TD-ADM-174, TD-ADM-UI-auth-18, TD-ADM-172) ---

    // The old key works until confirm and stops at once after it; a new set of codes; other sessions end; audited.
    [Fact]
    public async Task Sa12Reenrol_OldKeyUntilConfirm_NewKeyAfter_NewCodes_OtherSessionsEnd()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        var oldSecret = _totpSecret;
        var oldCodes = _recoveryCodes;
        var cipherBefore = (await AccountAsync()).TotpSecretCipher;
        await StepUpAsync(a);

        using var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        AssertNoStore(start);
        var (key, uri, svg) = ReadKey(await start.Content.ReadAsStringAsync());
        Assert.Matches("^[A-Z2-7]+$", key);
        Assert.Equal(Rfc6238Totp.BuildOtpAuthUri(OwnerEmail, key), uri);
        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        var newSecret = Rfc6238Totp.FromBase32(key);
        Assert.NotEqual(oldSecret, newSecret);

        // Started, not confirmed: nothing about the enrolled key changed, and it still works.
        Assert.Equal(cipherBefore, (await AccountAsync()).TotpSecretCipher);
        Assert.True(await FactorAcceptedAsync(Rfc6238Totp.ComputeCode(oldSecret, CurrentStep() + 1)));
        Assert.False(await FactorAcceptedAsync(Rfc6238Totp.ComputeCode(newSecret, CurrentStep() + 1)));
        Assert.True(await SessionExistsAsync(b));

        var before = await AuditIdsAsync();
        var confirmCode = Rfc6238Totp.ComputeCode(newSecret, CurrentStep());
        using var confirm = await PostAsync(
            AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath,
            new { code = confirmCode },
            a);

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        AssertNoStore(confirm);
        var confirmText = await confirm.Content.ReadAsStringAsync();
        using (var json = JsonDocument.Parse(confirmText))
            Assert.True(json.RootElement.GetProperty("changed").GetBoolean());
        var codes = ReadCodes(confirmText);
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);
        Assert.DoesNotContain(key, confirmText, StringComparison.Ordinal);

        // The old key stopped at once; the new one works from the next step; its confirm step is the replay floor.
        Assert.False(await FactorAcceptedAsync(Rfc6238Totp.ComputeCode(oldSecret, CurrentStep() + 1)));
        Assert.True(await FactorAcceptedAsync(Rfc6238Totp.ComputeCode(newSecret, CurrentStep() + 1)));
        Assert.False(await FactorAcceptedAsync(confirmCode));
        var account = await AccountAsync();
        Assert.NotEqual(cipherBefore, account.TotpSecretCipher);
        Assert.Equal(CurrentStep(), account.LastUsedTotpTimestep);

        // A new set replaced the old one.
        var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
        var stored = await CodeHashesAsync();
        Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
        Assert.All(oldCodes, item => Assert.DoesNotContain(hasher.Hash(item), stored));

        Assert.True(await SessionExistsAsync(a));
        Assert.False(await SessionExistsAsync(b));
        Assert.Null((await StepUpRowAsync(a))!.PendingTotpSecretCipher);

        var rows = await NewAuditAsync(before);
        Assert.Single(rows, r => r.Action == AdminAuditActions.TotpChange);
        AssertNoSecretIn(rows, codes.Append(key).Append(confirmCode).Append(uri).ToArray());
        AssertLogsClean(codes.Append(key).Append(confirmCode).ToArray());

        // Never returned by a GET.
        using var state = await GetAsync(AdminSecuritySettingsEndpoints.BasePath, a);
        var stateText = await state.Content.ReadAsStringAsync();
        Assert.DoesNotContain(key, stateText, StringComparison.Ordinal);
        Assert.DoesNotContain("cipher", stateText, StringComparison.OrdinalIgnoreCase);
    }

    // The key is new each time it is issued; an earlier key no longer confirms (it was replaced).
    [Fact]
    public async Task Sa12Reenrol_EachStartIssuesANewKey_AnEarlierKeyNoLongerConfirms()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);

        using var first = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a);
        var firstKey = ReadKey(await first.Content.ReadAsStringAsync()).Key;
        using var second = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a);
        var secondKey = ReadKey(await second.Content.ReadAsStringAsync()).Key;
        Assert.NotEqual(firstKey, secondKey);

        using var stale = await PostAsync(
            AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath,
            new { code = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(firstKey), CurrentStep()) },
            a);
        await AssertCodeFailedAsync(stale);

        using var ok = await PostAsync(
            AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath,
            new { code = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secondKey), CurrentStep()) },
            a);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    // TD-ADM-172 step 2: re-enrol confirm failures count on the one shared account counter, never on the IP throttle.
    // A missing code is a wrong code.
    [Theory]
    [InlineData("000000")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345")]
    public async Task Sa12Reenrol_WrongOrMissingCode_CountsOnTheAccountOnly(string? code)
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);
        using (var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a))
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var cipherBefore = (await AccountAsync()).TotpSecretCipher;
        var before = await AuditIdsAsync();

        using var response = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath, new { code }, a);

        await AssertCodeFailedAsync(response);
        Assert.Equal(1, await AccountFailuresAsync());
        Assert.Equal(0, await IpFailuresAsync());
        Assert.Equal(cipherBefore, (await AccountAsync()).TotpSecretCipher);
        var failed = Assert.Single(await NewAuditAsync(before));
        Assert.Equal(AdminAuditActions.TotpChangeFailed, failed.Action);
        Assert.Equal(AdminAuthReason.Bad2Fa, failed.ReasonClass);
        Assert.NotNull((await StepUpRowAsync(a))!.PendingTotpSecretCipher);
    }

    // TD-ADM-172 step 4: reaching the threshold by re-enrol confirm failures locks and ends every session.
    [Fact]
    public async Task Sa12Reenrol_ConfirmFailureThatLocks_EndsEverySession()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        var b = await NewSessionAsync();
        await StepUpAsync(a);
        using (var start = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, a))
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        await SeedAccountFailuresAsync(AdminAuthService.AccountFailureLimit - 1);

        using var response = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath, new { code = "000000" }, a);

        await AssertSignedOutAsync(response);
        Assert.True(await AccountLockedAsync());
        Assert.False(await SessionExistsAsync(a));
        Assert.False(await SessionExistsAsync(b));
        Assert.Null(await StepUpRowAsync(a));
    }

    // A waiting key expires with its step-up and is dropped by a new step-up or the end of the session.
    [Fact]
    public async Task Sa12Reenrol_AbandonedKey_ExpiresOrIsDropped()
    {
        await SeedAsync();
        var a = await NewSessionAsync();

        // Expires: 5 min 1 s after the step-up the confirm is refused, uncounted.
        await StepUpAsync(a);
        var key = await StartKeyAsync(a);
        Clock().Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        using (var late = await PostAsync(
                   AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath,
                   new { code = Rfc6238Totp.ComputeCode(key, CurrentStep()) },
                   a))
        {
            await AssertStepUpRequiredAsync(late);
        }

        // Dropped by a new step-up.
        await StepUpAsync(a);
        Assert.Null((await StepUpRowAsync(a))!.PendingTotpSecretCipher);
        using (var dropped = await PostAsync(
                   AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath,
                   new { code = Rfc6238Totp.ComputeCode(key, CurrentStep()) },
                   a))
        {
            await AssertStepUpRequiredAsync(dropped);
        }

        // Goes with the session.
        await StartKeyAsync(a);
        Assert.NotNull((await StepUpRowAsync(a))!.PendingTotpSecretCipher);
        await DeleteSessionAsync(a);
        Assert.Null(await StepUpRowAsync(a));

        Assert.Equal(0, await AccountFailuresAsync());
        Assert.True(await FactorAcceptedAsync(Totp(1)));
    }

    // Double submit: a second identical confirm changes nothing and issues nothing.
    [Fact]
    public async Task Sa12Reenrol_SecondIdenticalConfirm_ChangesNothing()
    {
        await SeedAsync();
        var a = await NewSessionAsync();
        await StepUpAsync(a);
        var key = await StartKeyAsync(a);
        var body = new { code = Rfc6238Totp.ComputeCode(key, CurrentStep()) };

        using var first = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath, body, a);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var codes = ReadCodes(await first.Content.ReadAsStringAsync());
        var cipher = (await AccountAsync()).TotpSecretCipher;
        var stored = await CodeHashesAsync();

        using var second = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath, body, a);
        await AssertStepUpRequiredAsync(second);
        Assert.Equal(cipher, (await AccountAsync()).TotpSecretCipher);
        Assert.Equal(stored, await CodeHashesAsync());
        var hasher = _factory.Services.GetRequiredService<AdminRecoveryCodeHasher>();
        Assert.All(codes, item => Assert.Contains(hasher.Hash(item), stored));
    }

    // ---- Reads (TD-ADM-UI-auth-16 count, TD-ADM-UI-auth-19 activity and lock notice) ---

    [Fact]
    public async Task Sa12State_CountMatchesTheStore_AndShowsTheStepUpWindow()
    {
        await SeedAsync();
        var a = await NewSessionAsync();

        using (var first = await GetAsync(AdminSecuritySettingsEndpoints.BasePath, a))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            AssertNoStore(first);
            using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            Assert.Equal(AdminRecoveryCodes.Count, json.RootElement.GetProperty("recoveryCodesLeft").GetInt32());
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("stepUpExpiresAt").ValueKind);
            Assert.False(json.RootElement.GetProperty("newCodesAvailable").GetBoolean());
            Assert.False(json.RootElement.GetProperty("authenticatorPending").GetBoolean());
        }

        using (var stepUp = await PostAsync(
                   AdminSecuritySettingsEndpoints.StepUpPath,
                   new { currentPassword = CurrentPassword, recoveryCode = _recoveryCodes[7] },
                   a))
        {
            Assert.Equal(HttpStatusCode.OK, stepUp.StatusCode);
        }

        using var after = await GetAsync(AdminSecuritySettingsEndpoints.BasePath, a);
        using var afterJson = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.Equal(await UnusedCodeCountAsync(), afterJson.RootElement.GetProperty("recoveryCodesLeft").GetInt32());
        Assert.Equal(AdminRecoveryCodes.Count - 1, afterJson.RootElement.GetProperty("recoveryCodesLeft").GetInt32());
        Assert.Equal(
            Clock().UtcNow + AdminSessionStepUp.Window,
            afterJson.RootElement.GetProperty("stepUpExpiresAt").GetDateTimeOffset());
        Assert.True(afterJson.RootElement.GetProperty("newCodesAvailable").GetBoolean());

        // GETs change nothing.
        var hashes = await CodeHashesAsync();
        var row = await StepUpRowAsync(a);
        using (await GetAsync(AdminSecuritySettingsEndpoints.BasePath, a))
        using (await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, a))
        {
        }

        Assert.Equal(hashes, await CodeHashesAsync());
        Assert.Equal(row!.SteppedUpAt, (await StepUpRowAsync(a))!.SteppedUpAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(AdminSecuritySettingsEndpoints.BasePath, session: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, session: null)).StatusCode);
    }

    // TD-ADM-UI-auth-19: the owner's own last 20 events, newest first, no raw IP, and the lock notice for 24 hours.
    [Fact]
    public async Task Sa12Activity_OwnLast20_NoRawIp_AndTheLockNotice()
    {
        await SeedAsync();
        var now = Clock().UtcNow;
        var ipHasher = _factory.Services.GetRequiredService<IIpHasher>();
        var ipHmac = ipHasher.Hash($"203.0.113.{RandomNumberGenerator.GetInt32(1, 250)}");
        var network = ipHmac[..AdminAuditEndpoints.IpHmacPrefixLength];
        var actor = $"actor-{Guid.NewGuid():N}@example.test";

        // Another actor's event and an entity event by the owner: neither is the owner's sign-in activity.
        await AddAuditAsync(
            AdminAuditEntry.CreateAuthEvent(AdminAuditActions.LoginFailure, actor, ipHmac, AdminAuthReason.BadPassword, now),
            AdminAuditEntry.CreateEntityEvent(AdminAuditActions.EntityEdit, OwnerEmail, ipHmac, "Offer", Guid.NewGuid(), timestamp: now));
        using (var first = await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, await NewSessionAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            AssertNoStore(first);
            using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            Assert.DoesNotContain(
                json.RootElement.GetProperty("items").EnumerateArray(),
                i => i.GetProperty("network").GetString() == network);
        }

        // 24 of the owner's events a day ahead (so they are the newest), and a lock that started 30 minutes ahead.
        var seeded = new List<AdminAuditEntry>();
        for (var i = 0; i < 24; i++)
        {
            var (action, reason) = i switch
            {
                20 => (AdminAuthEvents.SecondFactorFailed, AdminAuthReason.Bad2Fa),
                21 => (AdminAuditActions.StepUpFailed, AdminAuthReason.Bad2Fa),
                22 => (AdminAuditActions.PasswordChangeFailed, AdminAuthReason.Bad2Fa),
                23 => (AdminAuditActions.StepUpFailed, AdminAuthReason.BadPassword),
                _ => (AdminAuditActions.LoginSuccess, (string?)null)
            };
            seeded.Add(AdminAuditEntry.CreateAuthEvent(
                action,
                OwnerEmail,
                ipHmac,
                reason,
                now.AddDays(1).AddMinutes(i)));
        }

        var lockStarted = now.AddMinutes(30);
        seeded.Add(AdminAuditEntry.CreateAuthEvent(
            AdminAuthAction.LockStart, OwnerEmail, ipHmac, AdminAuthReason.Locked, lockStarted));
        await AddAuditAsync(seeded.ToArray());

        Clock().UtcNow = now.AddDays(1).AddHours(2);
        using (var response = await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, await NewSessionAsync()))
        {
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(AdminSecuritySettingsService.RecentActivityLimit, items.Count);
            var times = items.Select(i => i.GetProperty("timestamp").GetDateTimeOffset()).ToList();
            Assert.Equal(times.OrderByDescending(t => t).ToList(), times);
            Assert.Equal(now.AddDays(1).AddMinutes(23), times[0]);
            Assert.All(items, i => Assert.Equal(network, i.GetProperty("network").GetString()));
            var secondFactor = Assert.Single(items, i => i.GetProperty("event").GetString() == AdminAuthEvents.SecondFactorFailed);
            Assert.Equal(AdminAuthReason.Bad2Fa, secondFactor.GetProperty("reasonClass").GetString());

            // A failed S-A12 step-up shows no reason: the feed must not tell whether a guessed password was right.
            var stepUpFailures = items
                .Where(i => i.GetProperty("event").GetString() is AdminAuditActions.StepUpFailed or AdminAuditActions.PasswordChangeFailed)
                .ToList();
            Assert.Equal(3, stepUpFailures.Count);
            Assert.All(stepUpFailures, i => Assert.Equal(JsonValueKind.Null, i.GetProperty("reasonClass").ValueKind));
            Assert.DoesNotContain(AdminAuthReason.BadPassword, text, StringComparison.Ordinal);

            // The audit log keeps it.
            var logged = (await NewAuditAsync([]))
                .Where(e => e.IpHmac == ipHmac && e.Action is AdminAuditActions.StepUpFailed or AdminAuditActions.PasswordChangeFailed)
                .ToList();
            Assert.Equal(3, logged.Count);
            Assert.Contains(logged, e => e.ReasonClass == AdminAuthReason.BadPassword);
            Assert.All(logged, e => Assert.NotNull(e.ReasonClass));
            Assert.DoesNotContain("203.0.113.", text, StringComparison.Ordinal);
            Assert.DoesNotContain(ipHmac, text, StringComparison.Ordinal);
            Assert.DoesNotContain(actor, text, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminAuditActions.EntityEdit, text, StringComparison.Ordinal);

            // That lock ended more than 24 hours ago: no notice.
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("lock").ValueKind);
        }

        // Two hours after the lock started it has ended, and is shown as ended with the server's times.
        Clock().UtcNow = now.AddHours(2);
        using (var response = await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, await NewSessionAsync()))
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var notice = json.RootElement.GetProperty("lock");
            Assert.False(notice.GetProperty("active").GetBoolean());
            Assert.Equal(lockStarted, notice.GetProperty("startedAt").GetDateTimeOffset());
            Assert.Equal(lockStarted + AdminAuthService.LockDuration, notice.GetProperty("endsAt").GetDateTimeOffset());
        }

        // An active lock is shown as active.
        var viewer = await NewSessionAsync();
        await SeedAccountFailuresAsync(AdminAuthService.AccountFailureLimit, activate: true);
        using (var response = await GetAsync(AdminSecuritySettingsEndpoints.ActivityPath, viewer))
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var notice = json.RootElement.GetProperty("lock");
            Assert.True(notice.GetProperty("active").GetBoolean());
            Assert.Equal(Clock().UtcNow, notice.GetProperty("startedAt").GetDateTimeOffset());
            Assert.Equal(Clock().UtcNow + AdminAuthService.LockDuration, notice.GetProperty("endsAt").GetDateTimeOffset());
        }
    }

    // ---- Helpers ---------------------------------------------------------------

    private FakeAdminClock Clock() => _factory.Services.GetRequiredService<FakeAdminClock>();

    private long CurrentStep() => Rfc6238Totp.TimestepAt(Clock().UtcNow);

    private string Totp(int offset = 0) => Rfc6238Totp.ComputeCode(_totpSecret, CurrentStep() + offset);

    private async Task SeedAsync()
    {
        // Mid-step, so ±1-step tests never straddle a boundary while they run.
        var now = DateTimeOffset.UtcNow;
        Clock().UtcNow = DateTimeOffset.FromUnixTimeSeconds(Rfc6238Totp.TimestepAt(now) * 30 + 15);
        _factory.LogCollector.Clear();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();

        await db.AdminSessionStepUps.ExecuteDeleteAsync();
        await db.AdminSessions.ExecuteDeleteAsync();
        await db.AdminRecoveryCodes.ExecuteDeleteAsync();
        await db.AdminPendingAuths.ExecuteDeleteAsync();
        await db.AdminAuthTokens.ExecuteDeleteAsync();
        await db.AdminCoreOwnerAccounts.ExecuteDeleteAsync();
        await db.AdminCredentials.ExecuteDeleteAsync();
        await db.AdminAuthFailureEvents.ExecuteDeleteAsync();
        await db.AdminAuthLockouts.ExecuteDeleteAsync();

        _totpSecret = Rfc6238Totp.GenerateSecret();
        _recoveryCodes = AdminRecoveryCodes.Generate();
        var passwordHash = AdminPasswordHasher.Hash(AdminPasswordRules.Normalize(CurrentPassword));
        var account = AdminCoreOwnerAccount.Create(OwnerEmail, Clock().UtcNow);
        account.SetPasswordHash(passwordHash);
        foreach (var code in _recoveryCodes)
            db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(code)));
        account.CompleteEnrollment(protector.Encrypt(_totpSecret), now: Clock().UtcNow);
        db.AdminCoreOwnerAccounts.Add(account);
        db.AdminCredentials.Add(AdminCredential.Create(OwnerEmail, passwordHash, Clock().UtcNow));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> NewSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", Clock().UtcNow);
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task DeleteSessionAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IAdminSessionRepository>();
        await sessions.DeleteAsync(id);
        await sessions.SaveChangesAsync();
    }

    private async Task<Guid> SeedPendingSignInAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var pending = AdminPendingAuth.Create(
            OwnerEmail,
            AdminPendingToken.Hash(AdminPendingToken.Create()),
            Clock().UtcNow,
            returnPath: null);
        db.AdminPendingAuths.Add(pending);
        await db.SaveChangesAsync();
        return pending.Id;
    }

    private async Task<bool> PendingConsumedAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return (await db.AdminPendingAuths.AsNoTracking().SingleAsync(p => p.Id == id)).ConsumedAt is not null;
    }

    private async Task StepUpAsync(Guid session)
    {
        // A new step each time, so a step-up never replays the last one.
        Clock().Advance(TimeSpan.FromSeconds(30));
        using var response = await PostAsync(
            AdminSecuritySettingsEndpoints.StepUpPath,
            new { currentPassword = CurrentPassword, totpCode = Totp() },
            session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<byte[]> StartKeyAsync(Guid session)
    {
        using var response = await PostAsync(AdminSecuritySettingsEndpoints.AuthenticatorPath, new { }, session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Rfc6238Totp.FromBase32(ReadKey(await response.Content.ReadAsStringAsync()).Key);
    }

    private async Task SeedAccountFailuresAsync(int count, bool activate = false)
    {
        using var scope = _factory.Services.CreateScope();
        var lockouts = scope.ServiceProvider.GetRequiredService<IAdminLockoutStore>();
        var now = Clock().UtcNow;
        for (var i = 0; i < count; i++)
        {
            var reserved = await lockouts.TryIncrementFailureAsync(
                AdminAuthScopes.Account,
                OwnerEmail,
                now,
                AdminAuthService.FailureWindow,
                AdminAuthService.AccountFailureLimit,
                AdminAuthService.LockDuration,
                CancellationToken.None);
            Assert.True(reserved.Incremented);
        }

        if (activate)
        {
            Assert.True(await lockouts.TryActivateLockAsync(
                AdminAuthScopes.Account,
                OwnerEmail,
                now,
                AdminAuthService.FailureWindow,
                AdminAuthService.AccountFailureLimit,
                AdminAuthService.LockDuration,
                CancellationToken.None));
        }
    }

    private async Task<int> AccountFailuresAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var lockouts = scope.ServiceProvider.GetRequiredService<IAdminLockoutStore>();
        return await lockouts.CountFailuresAsync(
            AdminAuthScopes.Account,
            OwnerEmail,
            Clock().UtcNow - AdminAuthService.FailureWindow - TimeSpan.FromHours(1),
            CancellationToken.None);
    }

    private async Task<int> IpFailuresAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuthFailureEvents.CountAsync(e => e.Scope == AdminAuthScopes.LoginIp);
    }

    private async Task<bool> AccountLockedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var lockouts = scope.ServiceProvider.GetRequiredService<IAdminLockoutStore>();
        return await lockouts.GetActiveLockoutAsync(
            AdminAuthScopes.Account, OwnerEmail, Clock().UtcNow, CancellationToken.None) is not null;
    }

    private async Task<AdminCoreOwnerAccount> AccountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminCoreOwnerAccounts.AsNoTracking().SingleAsync(a => a.Email == OwnerEmail);
    }

    private async Task<AdminCredential?> CredentialAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminCredentials.AsNoTracking().SingleOrDefaultAsync(c => c.Email == OwnerEmail);
    }

    private async Task<AdminSessionStepUp?> StepUpRowAsync(Guid session)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessionStepUps.AsNoTracking().SingleOrDefaultAsync(s => s.SessionId == session);
    }

    private async Task<bool> SessionExistsAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessions.AsNoTracking().AnyAsync(s => s.Id == id);
    }

    private async Task<List<string>> CodeHashesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return (await db.AdminRecoveryCodes.AsNoTracking().Where(c => c.UsedAt == null).Select(c => c.CodeHash).ToListAsync())
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<int> UnusedCodeCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminRecoveryCodes.AsNoTracking().CountAsync(c => c.UsedAt == null);
    }

    private async Task<bool> RecoveryCodeUsedAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>().Hash(code);
        return (await db.AdminRecoveryCodes.AsNoTracking().SingleAsync(c => c.CodeHash == hash)).UsedAt is not null;
    }

    /// <summary>Whether the enrolled factor accepts this code now (evaluation only; nothing recorded).</summary>
    private async Task<bool> FactorAcceptedAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var verifier = scope.ServiceProvider.GetRequiredService<IAdminSecondFactorVerifier>();
        return (await verifier.EvaluateWithoutCommitAsync(OwnerEmail, code, null)).Succeeded;
    }

    private async Task<HashSet<Guid>> AuditIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return (await db.AdminAuditLog.AsNoTracking().Select(e => e.Id).ToListAsync()).ToHashSet();
    }

    private async Task<List<AdminAuditEntry>> NewAuditAsync(HashSet<Guid> before)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return (await db.AdminAuditLog.AsNoTracking().ToListAsync()).Where(e => !before.Contains(e.Id)).ToList();
    }

    private async Task AddAuditAsync(params AdminAuditEntry[] entries)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminAuditLog.AddRange(entries);
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        object? body,
        Guid? session,
        WebApplicationFactory<Program>? factory = null,
        bool antiForgery = true,
        string? origin = AdminHostOptions.ProductionAdminOrigin)
    {
        factory ??= _factory;
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Remove("Origin");

        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        var cookies = new List<string>();
        if (session is { } id)
            cookies.Add($"{AdminSessionCookie.Name}={id:D}");
        if (antiForgery)
        {
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            cookies.Add($"{AdminAntiForgeryCookie.Name}={nonce}");
            request.Headers.TryAddWithoutValidation(
                AdminAntiForgeryCookie.HeaderName,
                factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(nonce));
        }

        if (cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        if (origin is not null)
            request.Headers.TryAddWithoutValidation("Origin", origin);
        request.Content = JsonContent.Create(body ?? new { });
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, Guid? session)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (session is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return await client.SendAsync(request);
    }

    private static List<string> ReadCodes(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("codes").EnumerateArray().Select(c => c.GetString()!).ToList();
    }

    private static (string Key, string Uri, string Svg) ReadKey(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return (
            doc.RootElement.GetProperty("key").GetString()!,
            doc.RootElement.GetProperty("otpauthUri").GetString()!,
            doc.RootElement.GetProperty("qrSvg").GetString()!);
    }

    /// <summary>Exactly <c>{"error": ..., "reason": ...}</c>; returns the raw body.</summary>
    private static async Task<string> AssertErrorBodyAsync(HttpResponseMessage response, string error, string reason)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(error, json.RootElement.GetProperty("error").GetString());
        Assert.Equal(reason, json.RootElement.GetProperty("reason").GetString());
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        return body;
    }

    private static async Task AssertStepUpFailedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertNoStore(response);
        await AssertErrorBodyAsync(response, AdminSecuritySettingsCopy.StepUpFailed, AdminSecuritySettingsCopy.ReasonStepUpFailed);
    }

    private static async Task AssertStepUpRequiredAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertErrorBodyAsync(response, AdminSecuritySettingsCopy.StepUpRequired, AdminSecuritySettingsCopy.ReasonStepUpRequired);
    }

    private static async Task AssertCodeFailedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorBodyAsync(response, AdminSecuritySettingsCopy.NewCodeFailed, AdminSecuritySettingsCopy.ReasonCodeFailed);
    }

    private static async Task AssertSignedOutAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await AssertErrorBodyAsync(response, AdminAuthCopy.SignInFailure, AdminSecuritySettingsCopy.ReasonSignedOut);
        Assert.DoesNotContain("locked", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minute", body, StringComparison.OrdinalIgnoreCase);
        AssertSessionCookieCleared(response);
    }

    private static void AssertSessionCookieCleared(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cleared = Assert.Single(cookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
        Assert.Contains("expires=Thu, 01 Jan 1970", cleared, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/admin", cleared, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoStore(HttpResponseMessage response)
        => Assert.Contains(
            "no-store",
            string.Join(",", response.Headers.TryGetValues("Cache-Control", out var values) ? values : []),
            StringComparison.OrdinalIgnoreCase);

    private static void AssertNoSecretIn(IEnumerable<AdminAuditEntry> rows, params string?[] secrets)
    {
        foreach (var row in rows)
        {
            var cells = string.Join(
                "|",
                row.Action,
                row.ActorEmail,
                row.IpHmac,
                row.ReasonClass,
                row.EntityType,
                row.BeforeSnapshot,
                row.AfterSnapshot);
            foreach (var secret in secrets.Where(s => !string.IsNullOrEmpty(s)))
                Assert.DoesNotContain(secret!, cells, StringComparison.Ordinal);
            Assert.Null(row.BeforeSnapshot);
            Assert.Null(row.AfterSnapshot);
        }
    }

    private void AssertLogsClean(params string?[] secrets)
    {
        var lines = _factory.LogCollector.Lines;
        foreach (var secret in secrets.Where(s => !string.IsNullOrEmpty(s)))
            Assert.DoesNotContain(lines, line => line.Contains(secret!, StringComparison.Ordinal));
    }

    /// <summary>Fails after the recovery code was burned in the transaction, so the burn must roll back.</summary>
    private sealed class ThrowingCredentialStore : IAdminCredentialStore
    {
        public Task<AdminCredential?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Credential store unavailable.");

        public Task AddAsync(AdminCredential credential, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Credential store unavailable.");

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Credential store unavailable.");
    }
}

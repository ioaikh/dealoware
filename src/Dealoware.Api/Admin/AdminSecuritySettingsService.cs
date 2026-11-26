using System.Security.Cryptography;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// S-A12 security settings actions (T04). See <see cref="AdminSecuritySettingsEndpoints"/>
/// for the routes and the replies.
/// <para>
/// Failure counting: a failed step-up (wrong current password or code, a
/// missing code, a replayed authenticator step, a spent recovery code) and a
/// wrong code from a new key while re-enrolling each take one slot of the one
/// shared account counter that sign-in uses (<see cref="AdminAuthScopes.Account"/>,
/// reserve before evaluate, <see cref="AdminLockoutFlow"/>). Nothing here
/// counts on the sign-in IP throttle. New-password rule rejects count on
/// nothing. A lock, whichever request causes it, ends every session of the
/// owner, this one included.
/// </para>
/// <para>
/// Secrets: no password, code, key or recovery code is logged, audited,
/// written to an exception, a header or a URL. The key of a re-enrolment and a
/// new set of recovery codes are returned once, in the body of the POST that
/// made them, with <c>Cache-Control: no-store</c>; no GET returns them.
/// </para>
/// </summary>
public sealed class AdminSecuritySettingsService
{
    /// <summary>"Last 20" (TD-ADM-UI-auth-19).</summary>
    public const int RecentActivityLimit = 20;

    /// <summary>The lock notice shows an active lock or one that ended less than 24 hours ago.</summary>
    public static readonly TimeSpan LockNoticeWindow = TimeSpan.FromHours(24);

    /// <summary>
    /// Sign-in activity the S-A12 panel lists: the owner's own auth events.
    /// Closed list; the lockout note's list is not in this repository, so it is
    /// the auth events the code writes for the owner.
    /// </summary>
    public static readonly IReadOnlyList<string> ActivityActions =
    [
        AdminAuthEvents.LoginSuccess,
        AdminAuthEvents.LoginFailure,
        AdminAuthEvents.SecondFactorFailed,
        AdminAuthAction.LockStart,
        AdminAuthEvents.RecoveryCodeUse,
        AdminAuditActions.ResetComplete,
        AdminAuditActions.TotpEnroll,
        AdminAuditActions.TotpChange,
        AdminAuditActions.TotpChangeFailed,
        AdminAuditActions.PasswordChanged,
        AdminAuditActions.PasswordChangeFailed,
        AdminAuditActions.StepUpFailed,
        AdminAuditActions.RecoveryCodesRegenerated
    ];

    private readonly IAdminSessionRepository _sessions;
    private readonly IAdminCoreOwnerAccountRepository _accounts;
    private readonly IAdminSecondFactorVerifier _secondFactor;
    private readonly IAdminLockoutStore _lockouts;
    private readonly IAdminAuthTokenStore _authTokens;
    private readonly IAdminAuditRepository _audit;
    private readonly IAdminCredentialStore _credentials;
    private readonly IAdminPasswordHasher _hasher;
    private readonly IIpHasher _ipHasher;
    private readonly IAdminClock _clock;
    private readonly IOptions<CoreOwnerOptions> _coreOwner;
    private readonly TotpSecretProtector _protector;
    private readonly AdminRecoveryCodeHasher _recoveryHasher;
    private readonly AdminSecuritySettingsStore _store;
    private readonly DealowareDbContext _db;

    public AdminSecuritySettingsService(
        IAdminSessionRepository sessions,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSecondFactorVerifier secondFactor,
        IAdminLockoutStore lockouts,
        IAdminAuthTokenStore authTokens,
        IAdminAuditRepository audit,
        IAdminCredentialStore credentials,
        IAdminPasswordHasher hasher,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        TotpSecretProtector protector,
        AdminRecoveryCodeHasher recoveryHasher,
        AdminSecuritySettingsStore store,
        DealowareDbContext db)
    {
        _sessions = sessions;
        _accounts = accounts;
        _secondFactor = secondFactor;
        _lockouts = lockouts;
        _authTokens = authTokens;
        _audit = audit;
        _credentials = credentials;
        _hasher = hasher;
        _ipHasher = ipHasher;
        _clock = clock;
        _coreOwner = coreOwner;
        _protector = protector;
        _recoveryHasher = recoveryHasher;
        _store = store;
        _db = db;
    }

    // ---- Reads ----------------------------------------------------------

    /// <summary>
    /// GET: what the page and the recovery-code banner need. No secret, no
    /// side effect (the session gate's idle renewal is the gate's own).
    /// </summary>
    public async Task<IResult> GetStateAsync(HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var ct = http.RequestAborted;
        var left = await _store.CountUnusedRecoveryCodesAsync(owner.Account.Id, ct);
        var stepUp = await _store.GetStepUpAsync(owner.Session.Id, ct);
        var fresh = stepUp is not null && stepUp.IsFresh(owner.Now);
        return Results.Json(new
        {
            recoveryCodesLeft = left,
            stepUpExpiresAt = fresh ? stepUp!.ExpiresAt : (DateTimeOffset?)null,
            newCodesAvailable = fresh && stepUp!.CodesIssuedAt is null,
            authenticatorPending = fresh && !string.IsNullOrEmpty(stepUp!.PendingTotpSecretCipher)
        });
    }

    /// <summary>
    /// GET: "Recent sign-in activity" and the 24-hour lock notice
    /// (TD-ADM-UI-auth-19). The network is a short prefix of the keyed hash
    /// the audit row holds; no address is stored, so none is returned.
    /// </summary>
    public async Task<IResult> GetActivityAsync(HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var ct = http.RequestAborted;
        var rows = await _store.ListRecentAuthEventsAsync(owner.Email, ActivityActions, RecentActivityLimit, ct);
        var items = rows.Select(e => new
        {
            timestamp = e.Timestamp,
            @event = e.Action,
            reasonClass = FeedReasonClass(e),
            network = NetworkPrefix(e.IpHmac)
        }).ToArray();

        return Results.Json(new { items, @lock = await LockNoticeAsync(owner, ct) });
    }

    /// <summary>
    /// The S-A12 step-up failures carry no reason in the feed: their audit
    /// rows say whether the password or the code was wrong, and the feed is
    /// read with the same session that sent the guess, so showing it would
    /// tell a holder of a stolen session cookie that a guessed password was
    /// right. The audit log keeps the reason.
    /// </summary>
    private static string? FeedReasonClass(AdminAuditEntry entry)
        => entry.Action is AdminAuditActions.StepUpFailed or AdminAuditActions.PasswordChangeFailed
            ? null
            : entry.ReasonClass;

    private async Task<object?> LockNoticeAsync(Owner owner, CancellationToken ct)
    {
        var now = owner.Now;
        var candidates = new List<(DateTimeOffset Start, DateTimeOffset End, bool Active)>();

        var gate = await _lockouts.GetActiveLockoutAsync(AdminAuthScopes.Account, owner.Email, now, ct);
        if (gate is not null)
            candidates.Add((gate.StartedAt, gate.ExpiresAt, true));

        if (await _store.LatestLockStartAsync(owner.Email, ct) is { } started)
        {
            var end = started + AdminAuthService.LockDuration;
            candidates.Add((started, end, gate is not null && now < end));
        }

        if (owner.Account.FactorLockedUntil is { } until)
        {
            candidates.Add((
                until - TimeSpan.FromMinutes(AdminCoreOwnerAccount.FactorLockoutMinutes),
                until,
                now < until));
        }

        if (candidates.Count == 0)
            return null;

        var latest = candidates
            .OrderByDescending(c => c.Active)
            .ThenByDescending(c => c.End)
            .First();
        if (!latest.Active && latest.End <= now - LockNoticeWindow)
            return null;

        return new { active = latest.Active, startedAt = latest.Start, endsAt = latest.End };
    }

    // ---- Password change (S-A12 note v1.4) ------------------------------

    public async Task<IResult> ChangePasswordAsync(AdminSecuritySettingsEndpoints.PasswordChangeBody? body, HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var ct = http.RequestAborted;

        // Step-up first, counted: the new-password rules include "same as
        // current", which must never answer about the current password to a
        // request that has not proved it.
        var check = await CheckStepUpAsync(owner, body, AdminAuditActions.PasswordChangeFailed, ct);
        if (check.Outcome == StepUpOutcome.Locked)
            return SignedOut(http);
        if (check.Outcome == StepUpOutcome.Failed)
            return StepUpFailedResult();

        // Rule rejects count on nothing, write no failure event, and spend no code.
        var proposed = body?.NewPassword ?? string.Empty;
        var currentHash = owner.Account.PasswordHash;
        var matchesCurrent = !string.IsNullOrEmpty(currentHash)
                             && proposed.Length > 0
                             && (AdminPasswordHasher.Verify(AdminPasswordRules.Normalize(proposed), currentHash)
                                 || AdminPasswordHasher.Verify(proposed, currentHash));
        var rules = AdminPasswordRules.Evaluate(proposed, owner.Email, matchesCurrent);
        if (!rules.Accepted)
        {
            return Results.Json(
                new { error = rules.Message, reason = AdminPasswordResetEndpoints.ReasonName(rules.Reason) },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var newHash = _hasher.Hash(AdminPasswordRules.Normalize(proposed));

        // One transaction: the recovery-code burn or the accepted step, the
        // password, the end of every session, and the audit rows.
        await using (var tx = await _db.Database.BeginTransactionAsync(ct))
        {
            if (!await CommitFactorAsync(owner, check.Factor, ct))
            {
                // Lost a race with an identical submit: nothing written, nothing counted.
                await tx.RollbackAsync(ct);
                return StepUpFailedResult();
            }

            // Read again: the lockout store clears the change tracker, so the
            // entity read before the step-up is no longer tracked.
            var account = await _accounts.GetByEmailAsync(owner.Email, ct);
            if (account is null)
            {
                await tx.RollbackAsync(ct);
                return AdminDeny.UnauthorizedResult();
            }

            account.SetPasswordHash(newHash);
            var credential = await _credentials.GetByEmailAsync(owner.Email, ct);
            if (credential is null)
                await _credentials.AddAsync(AdminCredential.Create(owner.Email, newHash, owner.Now), ct);
            else
                credential.ReplacePassword(newHash, owner.Now);
            await _db.SaveChangesAsync(ct);

            await EndEverySessionAsync(owner, ct);
            await _audit.AddAsync(Event(owner, AdminAuditActions.PasswordChanged), ct);
            if (check.Factor.UsedRecoveryCode)
                await _audit.AddAsync(Event(owner, AdminAuditActions.RecoveryCodeUse), ct);
            await _audit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        ClearSessionCookie(http);
        return Results.Json(new { message = AdminSecuritySettingsCopy.PasswordChanged, signedOut = true });
    }

    // ---- Step-up (TD-ADM-174) -------------------------------------------

    public async Task<IResult> StepUpAsync(AdminSecuritySettingsEndpoints.StepUpBody? body, HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        return await GiveStepUpAsync(owner, body, http) ?? Results.Json(new
        {
            stepUpExpiresAt = owner.Now + AdminSessionStepUp.Window
        });
    }

    /// <summary>
    /// Checks the proof and, when it holds, records the step-up on this
    /// session in the same transaction as the code it spent. Null on success,
    /// otherwise the reply to send.
    /// </summary>
    private async Task<IResult?> GiveStepUpAsync(Owner owner, AdminSecuritySettingsEndpoints.StepUpBody? body, HttpContext http)
    {
        var ct = http.RequestAborted;
        var check = await CheckStepUpAsync(owner, body, AdminAuditActions.StepUpFailed, ct);
        if (check.Outcome == StepUpOutcome.Locked)
            return SignedOut(http);
        if (check.Outcome == StepUpOutcome.Failed)
            return StepUpFailedResult();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (!await CommitFactorAsync(owner, check.Factor, ct))
        {
            await tx.RollbackAsync(ct);
            return StepUpFailedResult();
        }

        await _store.RecordStepUpAsync(owner.Session.Id, owner.Now, ct);
        if (check.Factor.UsedRecoveryCode)
        {
            await _audit.AddAsync(Event(owner, AdminAuditActions.RecoveryCodeUse), ct);
            await _audit.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return null;
    }

    /// <summary>An action may carry its own proof; then it is checked and recorded first.</summary>
    private async Task<IResult?> StepUpIfProofSentAsync(Owner owner, AdminSecuritySettingsEndpoints.StepUpBody? body, HttpContext http)
    {
        var sent = !string.IsNullOrEmpty(body?.CurrentPassword)
                   || !string.IsNullOrEmpty(body?.TotpCode)
                   || !string.IsNullOrEmpty(body?.RecoveryCode);
        return sent ? await GiveStepUpAsync(owner, body, http) : null;
    }

    // ---- New recovery codes (TD-ADM-171, TD-ADM-174) ---------------------

    public async Task<IResult> RegenerateRecoveryCodesAsync(AdminSecuritySettingsEndpoints.StepUpBody? body, HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var early = await StepUpIfProofSentAsync(owner, body, http);
        if (early is not null)
            return early;

        var ct = http.RequestAborted;
        var (plain, hashed) = NewRecoveryCodes(owner);

        await using (var tx = await _db.Database.BeginTransactionAsync(ct))
        {
            // Lock order, as everywhere here: account row, recovery codes,
            // sessions and step-ups. The claim on this step-up comes last: one
            // set per step-up, so a repeated submit issues nothing.
            if (!await _store.LockAccountRowAsync(owner.Account.Id, ct)
                || !await _store.ReplaceRecoveryCodesAsync(owner.Account.Id, hashed, ct)
                || !await _store.TryClaimCodesIssueAsync(owner.Session.Id, owner.Now, ct))
            {
                await tx.RollbackAsync(ct);
                return StepUpRequiredResult();
            }

            await _store.DeleteOtherSessionsAsync(owner.Session.Email, owner.Session.Id, ct);
            await _audit.AddAsync(Event(owner, AdminAuditActions.RecoveryCodesRegenerated), ct);
            await _audit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        return Results.Json(new { codes = plain, recoveryCodesLeft = plain.Count });
    }

    // ---- Authenticator re-enrolment (TD-ADM-174, TD-ADM-UI-auth-18) ------

    /// <summary>
    /// A new key, every time, held for this session only and returned once.
    /// The enrolled key is not touched: it keeps working until a confirm.
    /// </summary>
    public async Task<IResult> StartReenrolmentAsync(AdminSecuritySettingsEndpoints.StepUpBody? body, HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var early = await StepUpIfProofSentAsync(owner, body, http);
        if (early is not null)
            return early;

        var secret = Rfc6238Totp.GenerateSecret();
        string cipher;
        string base32;
        try
        {
            cipher = _protector.Encrypt(secret);
            base32 = Rfc6238Totp.ToBase32(secret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }

        if (!await _store.TrySetPendingKeyAsync(owner.Session.Id, cipher, owner.Now, http.RequestAborted))
            return StepUpRequiredResult();

        var stepUp = await _store.GetStepUpAsync(owner.Session.Id, http.RequestAborted);
        var uri = Rfc6238Totp.BuildOtpAuthUri(owner.Account.Email, base32);
        return Results.Json(new
        {
            key = base32,
            otpauthUri = uri,
            qrSvg = AdminEnrollmentPages.QrSvg(AdminQrCode.Encode(uri)),
            stepUpExpiresAt = stepUp?.ExpiresAt
        });
    }

    /// <summary>
    /// A code from the new key replaces the enrolled key with it and issues a
    /// new set of recovery codes, in one transaction. The old key and the old
    /// codes stop working at once; every other session ends.
    /// </summary>
    public async Task<IResult> ConfirmReenrolmentAsync(AdminSecuritySettingsEndpoints.ReenrolConfirmBody? body, HttpContext http)
    {
        NoStore(http);
        var owner = await LoadOwnerAsync(http);
        if (owner is null)
            return AdminDeny.UnauthorizedResult();

        var ct = http.RequestAborted;
        var stepUp = await _store.GetStepUpAsync(owner.Session.Id, ct);
        var pendingCipher = stepUp?.PendingTotpSecretCipher;
        if (stepUp is null || !stepUp.IsFresh(owner.Now) || string.IsNullOrEmpty(pendingCipher))
            return StepUpRequiredResult();

        byte[] secret;
        try
        {
            secret = _protector.Decrypt(pendingCipher);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return StepUpRequiredResult();
        }

        long step;
        try
        {
            // Counted on the one shared account counter, never on the IP
            // throttle (TD-ADM-172). A missing code is a wrong code.
            if (owner.Account.IsFactorLocked(owner.Now))
                return await LockedAsync(owner, AdminAuditActions.TotpChangeFailed, http);

            var reserved = await ReserveAsync(owner, ct);
            if (!reserved.Incremented)
                return await LockedAsync(owner, AdminAuditActions.TotpChangeFailed, http);

            if (!Rfc6238Totp.TryVerify(secret, body?.Code ?? string.Empty, owner.Now, lastUsedTimestep: null, out step))
            {
                var locked = await RecordFailureAsync(
                    owner, reserved, AdminAuditActions.TotpChangeFailed, AdminAuthReason.Bad2Fa, ct);
                return locked
                    ? SignedOut(http)
                    : Results.Json(
                        new
                        {
                            error = AdminSecuritySettingsCopy.NewCodeFailed,
                            reason = AdminSecuritySettingsCopy.ReasonCodeFailed
                        },
                        statusCode: StatusCodes.Status400BadRequest);
            }

            await ReleaseAsync(owner, reserved, ct);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }

        var (plain, hashed) = NewRecoveryCodes(owner);
        await using (var tx = await _db.Database.BeginTransactionAsync(ct))
        {
            // Lock order, as everywhere here: account row, recovery codes,
            // then this session's step-up. The key is taken once, so a
            // repeated confirm changes nothing.
            if (!await _store.LockAccountRowAsync(owner.Account.Id, ct)
                || !await _store.TryReplaceAuthenticatorAsync(owner.Account.Id, pendingCipher, step, owner.Now, ct)
                || !await _store.ReplaceRecoveryCodesAsync(owner.Account.Id, hashed, ct)
                || !await _store.TryTakePendingKeyAsync(owner.Session.Id, pendingCipher, owner.Now, ct))
            {
                await tx.RollbackAsync(ct);
                return StepUpRequiredResult();
            }

            await _store.DeleteOtherSessionsAsync(owner.Session.Email, owner.Session.Id, ct);
            await _audit.AddAsync(Event(owner, AdminAuditActions.TotpChange), ct);
            await _audit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        return Results.Json(new { changed = true, codes = plain, recoveryCodesLeft = plain.Count });
    }

    // ---- The shared step-up check ----------------------------------------

    private enum StepUpOutcome
    {
        Passed,
        Failed,
        Locked
    }

    private readonly record struct StepUpCheck(StepUpOutcome Outcome, AdminSecondFactorVerifyResult Factor);

    /// <summary>
    /// The current password and exactly one current code, evaluated together
    /// (both always run, so the reply and its timing do not say which one
    /// failed). Commits nothing: the caller records the accepted step or burns
    /// the recovery code in its own transaction.
    /// </summary>
    private async Task<StepUpCheck> CheckStepUpAsync(
        Owner owner,
        AdminSecuritySettingsEndpoints.StepUpBody? body,
        string failureAction,
        CancellationToken ct)
    {
        if (owner.Account.IsFactorLocked(owner.Now))
        {
            await LockedEndAsync(owner, failureAction, ct);
            return new StepUpCheck(StepUpOutcome.Locked, AdminSecondFactorVerifyResult.Failed);
        }

        var reserved = await ReserveAsync(owner, ct);
        if (!reserved.Incremented)
        {
            await LockedEndAsync(owner, failureAction, ct);
            return new StepUpCheck(StepUpOutcome.Locked, AdminSecondFactorVerifyResult.Failed);
        }

        var password = AdminPasswordRules.Normalize(body?.CurrentPassword ?? string.Empty);
        var hash = owner.Account.PasswordHash;
        var passwordOk = !string.IsNullOrEmpty(hash) && password.Length > 0
            ? AdminPasswordHasher.Verify(password, hash)
            : AdminPasswordHasher.DummyVerify(password);

        var factor = await _secondFactor.EvaluateWithoutCommitAsync(
            owner.Email,
            body?.TotpCode,
            body?.RecoveryCode,
            ct);
        // A step at or before the last accepted one is a replay, refused like a wrong code.
        var factorOk = factor.Succeeded
                       && !(factor.TotpTimestep is { } step
                            && owner.Account.LastUsedTotpTimestep is { } last
                            && step <= last);

        if (passwordOk && factorOk)
        {
            await ReleaseAsync(owner, reserved, ct);
            return new StepUpCheck(StepUpOutcome.Passed, factor);
        }

        var locked = await RecordFailureAsync(
            owner,
            reserved,
            failureAction,
            passwordOk ? AdminAuthReason.Bad2Fa : AdminAuthReason.BadPassword,
            ct);
        return new StepUpCheck(locked ? StepUpOutcome.Locked : StepUpOutcome.Failed, AdminSecondFactorVerifyResult.Failed);
    }

    private Task<FailureIncrementResult> ReserveAsync(Owner owner, CancellationToken ct)
        => AdminLockoutFlow.ReserveAsync(
            _lockouts,
            AdminAuthScopes.Account,
            owner.Email,
            owner.Now,
            AdminAuthService.AccountFailureLimit,
            ct);

    private Task ReleaseAsync(Owner owner, FailureIncrementResult reserved, CancellationToken ct)
        => AdminLockoutFlow.ReleaseSuccessAsync(
            _lockouts,
            reserved,
            AdminAuthScopes.Account,
            owner.Email,
            owner.Now,
            AdminAuthService.AccountFailureLimit,
            ct);

    /// <summary>
    /// Keeps the reserved slot as a failure and writes the failure event. True
    /// when the account is now locked, in which case every session has ended.
    /// </summary>
    private async Task<bool> RecordFailureAsync(
        Owner owner,
        FailureIncrementResult reserved,
        string failureAction,
        string reasonClass,
        CancellationToken ct)
    {
        var lockCreated = false;
        await AdminLockoutFlow.ConfirmFailureAsync(
            _lockouts,
            reserved,
            AdminAuthScopes.Account,
            owner.Email,
            owner.Now,
            AdminAuthService.AccountFailureLimit,
            async () =>
            {
                lockCreated = true;
                await _audit.AddAsync(Event(owner, AdminAuthAction.LockStart, AdminAuthReason.Locked), ct);
                await _audit.SaveChangesAsync(ct);
            },
            ct);

        await _audit.AddAsync(Event(owner, failureAction, reasonClass), ct);
        await _audit.SaveChangesAsync(ct);

        if (!lockCreated
            && await _lockouts.GetActiveLockoutAsync(AdminAuthScopes.Account, owner.Email, owner.Now, ct) is null)
        {
            return false;
        }

        await EndEverySessionAsync(owner, ct);
        return true;
    }

    private async Task LockedEndAsync(Owner owner, string failureAction, CancellationToken ct)
    {
        await _audit.AddAsync(Event(owner, failureAction, AdminAuthReason.Locked), ct);
        await _audit.SaveChangesAsync(ct);
        await EndEverySessionAsync(owner, ct);
    }

    private async Task<IResult> LockedAsync(Owner owner, string failureAction, HttpContext http)
    {
        await LockedEndAsync(owner, failureAction, http.RequestAborted);
        return SignedOut(http);
    }

    /// <summary>Every session of the owner, this one included, and any sign-in still in progress.</summary>
    private async Task EndEverySessionAsync(Owner owner, CancellationToken ct)
    {
        await _accounts.ConsumeOutstandingPendingAsync(owner.Email, owner.Now, ct);
        await _authTokens.ConsumeOutstandingPendingAsync(owner.Email, owner.Now, ct);
        await _sessions.RevokeAllForEmailAsync(owner.Session.Email, ct);
    }

    /// <summary>Burns the recovery code, or records the accepted step, in the caller's transaction.</summary>
    /// <remarks>
    /// The account row is locked first, as in every S-A12 transaction
    /// (<see cref="AdminSecuritySettingsStore.LockAccountRowAsync"/>): a burn
    /// that took the recovery-code row before the account row could deadlock
    /// with a replacement of the set, which takes them the other way round.
    /// </remarks>
    private async Task<bool> CommitFactorAsync(Owner owner, AdminSecondFactorVerifyResult factor, CancellationToken ct)
    {
        if (!await _store.LockAccountRowAsync(owner.Account.Id, ct))
            return false;

        if (factor.UsedRecoveryCode)
        {
            return !string.IsNullOrEmpty(factor.RecoveryCodeHash)
                   && await _accounts.TryRedeemRecoveryCodeAsync(owner.Account.Id, factor.RecoveryCodeHash, owner.Now, ct);
        }

        return factor.TotpTimestep is { } step
               && await _accounts.TryRecordTotpTimestepAsync(owner.Account.Id, step, ct);
    }

    // ---- Helpers ----------------------------------------------------------

    private sealed record Owner(AdminSession Session, AdminCoreOwnerAccount Account, string Email, string IpHmac, DateTimeOffset Now);

    /// <summary>
    /// The signed-in owner of this request. The session gate has already let
    /// it through; this reads the same session again and the enrolled account.
    /// </summary>
    private async Task<Owner?> LoadOwnerAsync(HttpContext http)
    {
        var now = _clock.UtcNow;
        var raw = http.Request.Cookies[AdminSessionCookie.Name];
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var sessionId))
            return null;

        var session = await _sessions.GetByIdAsync(sessionId, http.RequestAborted);
        var ownerEmail = AdminAuthService.NormalizeEmail(_coreOwner.Value.Email);
        if (session is null
            || !session.IsValid(now)
            || ownerEmail.Length == 0
            || !string.Equals(AdminAuthService.NormalizeEmail(session.Email), ownerEmail, StringComparison.Ordinal))
        {
            return null;
        }

        var account = await _accounts.GetByEmailAsync(ownerEmail, http.RequestAborted);
        if (account is null || !account.IsTotpEnrolled)
            return null;

        return new Owner(session, account, ownerEmail, HashIp(http), now);
    }

    private (IReadOnlyList<string> Plain, IReadOnlyList<AdminRecoveryCode> Hashed) NewRecoveryCodes(Owner owner)
    {
        var plain = AdminRecoveryCodes.Generate();
        var hashed = plain
            .Select(code => AdminRecoveryCode.Create(owner.Account.Id, _recoveryHasher.Hash(code), owner.Now))
            .ToList();
        return (plain, hashed);
    }

    private static AdminAuditEntry Event(Owner owner, string action, string? reasonClass = null)
        => AdminAuditEntry.CreateAuthEvent(action, owner.Email, owner.IpHmac, reasonClass, owner.Now);

    private string HashIp(HttpContext http)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
            ip = "unknown";
        return _ipHasher.Hash(ip);
    }

    private static string NetworkPrefix(string ipHmac)
        => string.IsNullOrEmpty(ipHmac) || ipHmac.Length <= AdminAuditEndpoints.IpHmacPrefixLength
            ? ipHmac ?? string.Empty
            : ipHmac[..AdminAuditEndpoints.IpHmacPrefixLength];

    private static void NoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
    }

    private static void ClearSessionCookie(HttpContext http)
        => http.Response.Cookies.Delete(AdminSessionCookie.Name, AdminSessionCookie.ExpiredOptions());

    private static IResult StepUpFailedResult()
        => Results.Json(
            new { error = AdminSecuritySettingsCopy.StepUpFailed, reason = AdminSecuritySettingsCopy.ReasonStepUpFailed },
            statusCode: StatusCodes.Status400BadRequest);

    private static IResult StepUpRequiredResult()
        => Results.Json(
            new { error = AdminSecuritySettingsCopy.StepUpRequired, reason = AdminSecuritySettingsCopy.ReasonStepUpRequired },
            statusCode: StatusCodes.Status403Forbidden);

    /// <summary>
    /// The account locked: every session has ended, this one included. The
    /// generic UX1-A03 sentence, no duration and no "locked" (note v1.4 (b)).
    /// </summary>
    private static IResult SignedOut(HttpContext http)
    {
        ClearSessionCookie(http);
        return Results.Json(
            new { error = AdminAuthCopy.SignInFailure, reason = AdminSecuritySettingsCopy.ReasonSignedOut },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}

using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Storage for the S-A12 security settings actions (T04): the per-session
/// step-up, the re-enrolment key waiting for its confirming code, the
/// replacement of the recovery-code set and of the authenticator key, and the
/// narrow reads the page needs. Every write that decides something is one
/// conditional statement (rows == 1), so two identical submits cannot both
/// win. The callers own the transactions.
/// </summary>
public sealed class AdminSecuritySettingsStore
{
    private readonly DealowareDbContext _db;

    public AdminSecuritySettingsStore(DealowareDbContext db)
    {
        _db = db;
    }

    /// <summary>The step-up of one session, untracked. Never handed out as is: it holds a cipher.</summary>
    public Task<AdminSessionStepUp?> GetStepUpAsync(Guid sessionId, CancellationToken ct)
        => _db.AdminSessionStepUps.AsNoTracking().SingleOrDefaultAsync(s => s.SessionId == sessionId, ct);

    /// <summary>
    /// A new step-up for this session: a fresh window, no set issued under it,
    /// and any re-enrolment key still waiting dropped.
    /// </summary>
    public async Task RecordStepUpAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct)
    {
        if (await RenewAsync(sessionId, now, ct) == 1)
            return;

        var row = AdminSessionStepUp.Create(sessionId, now);
        _db.AdminSessionStepUps.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another submit on this session inserted it first: renew that row instead.
            _db.Entry(row).State = EntityState.Detached;
            await RenewAsync(sessionId, now, ct);
        }
        finally
        {
            _db.Entry(row).State = EntityState.Detached;
        }
    }

    private Task<int> RenewAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct)
        => _db.AdminSessionStepUps
            .Where(s => s.SessionId == sessionId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.SteppedUpAt, now)
                    .SetProperty(x => x.CodesIssuedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.PendingTotpSecretCipher, (string?)null),
                ct);

    /// <summary>
    /// Claims the one new recovery-code set a step-up allows: only while the
    /// step-up is within its window and nothing was issued under it yet.
    /// </summary>
    public async Task<bool> TryClaimCodesIssueAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = AdminSessionStepUp.FreshCutoff(now);
        var rows = await _db.AdminSessionStepUps
            .Where(s => s.SessionId == sessionId && s.SteppedUpAt >= cutoff && s.CodesIssuedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CodesIssuedAt, now), ct);
        return rows == 1;
    }

    /// <summary>
    /// Holds a new re-enrolment key for this session, replacing any key an
    /// earlier start left waiting. Only while the step-up is within its window.
    /// </summary>
    public async Task<bool> TrySetPendingKeyAsync(
        Guid sessionId,
        string pendingCipher,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingCipher);
        var cutoff = AdminSessionStepUp.FreshCutoff(now);
        var rows = await _db.AdminSessionStepUps
            .Where(s => s.SessionId == sessionId && s.SteppedUpAt >= cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PendingTotpSecretCipher, pendingCipher), ct);
        return rows == 1;
    }

    /// <summary>
    /// Takes the waiting key exactly once: only the key that was read, only
    /// while the step-up is within its window.
    /// </summary>
    public async Task<bool> TryTakePendingKeyAsync(
        Guid sessionId,
        string pendingCipher,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var cutoff = AdminSessionStepUp.FreshCutoff(now);
        var rows = await _db.AdminSessionStepUps
            .Where(s =>
                s.SessionId == sessionId
                && s.SteppedUpAt >= cutoff
                && s.PendingTotpSecretCipher == pendingCipher)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PendingTotpSecretCipher, (string?)null), ct);
        return rows == 1;
    }

    /// <summary>
    /// Takes the account row lock (PostgreSQL holds it to the end of the
    /// caller's transaction) without changing anything. Every S-A12
    /// transaction takes it first, before any recovery-code row, session row
    /// or step-up row, so two of them always wait on the same row in the same
    /// order and never deadlock.
    /// </summary>
    public async Task<bool> LockAccountRowAsync(Guid accountId, CancellationToken ct)
    {
        var rows = await _db.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.RecoveryCodesIssued, a => a.RecoveryCodesIssued), ct);
        return rows == 1;
    }

    /// <summary>
    /// Replaces the whole recovery-code set, in the caller's transaction. The
    /// account row is written first, which holds its row lock to the end of
    /// the transaction on PostgreSQL: a second replacement waits, then deletes
    /// this set before adding its own, so two sets are never valid together.
    /// The S-A7 copy of a first-enrolment set, if still held, goes too.
    /// </summary>
    public async Task<bool> ReplaceRecoveryCodesAsync(
        Guid accountId,
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken ct)
    {
        var rows = await _db.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId && a.TotpEnrolledAt != null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.RecoveryCodesIssued, true)
                    .SetProperty(a => a.RecoveryCodesRevealCipher, (string?)null),
                ct);
        if (rows != 1)
            return false;

        await _db.AdminRecoveryCodes
            .Where(c => c.AccountId == accountId)
            .ExecuteDeleteAsync(ct);
        _db.AdminRecoveryCodes.AddRange(codes);
        await _db.SaveChangesAsync(ct);
        foreach (var code in codes)
            _db.Entry(code).State = EntityState.Detached;
        return true;
    }

    /// <summary>
    /// The confirmed re-enrolment key replaces the enrolled one, in the
    /// caller's transaction. The accepted step of the new key becomes the
    /// replay floor, as at first enrolment.
    /// </summary>
    public async Task<bool> TryReplaceAuthenticatorAsync(
        Guid accountId,
        string totpSecretCipher,
        long acceptedStep,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var rows = await _db.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId && a.TotpEnrolledAt != null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.TotpSecretCipher, totpSecretCipher)
                    .SetProperty(a => a.TotpEnrolledAt, now)
                    .SetProperty(a => a.LastUsedTotpTimestep, acceptedStep)
                    .SetProperty(a => a.PendingTotpSecretCipher, (string?)null),
                ct);
        return rows == 1;
    }

    public Task<int> CountUnusedRecoveryCodesAsync(Guid accountId, CancellationToken ct)
        => _db.AdminRecoveryCodes.AsNoTracking().CountAsync(c => c.AccountId == accountId && c.UsedAt == null, ct);

    /// <summary>Ends every session of the owner but the one given (its step-ups go with them).</summary>
    public Task<int> DeleteOtherSessionsAsync(string sessionEmail, Guid keepSessionId, CancellationToken ct)
        => _db.AdminSessions
            .Where(s => s.Email == sessionEmail && s.Id != keepSessionId)
            .ExecuteDeleteAsync(ct);

    /// <summary>
    /// The owner's own latest auth events from a closed list of actions,
    /// newest first. Read only; the audit row holds a keyed hash of the
    /// address, never the address.
    /// </summary>
    public async Task<IReadOnlyList<AdminAuditEntry>> ListRecentAuthEventsAsync(
        string actorEmail,
        IReadOnlyCollection<string> actions,
        int take,
        CancellationToken ct)
    {
        var names = actions.ToList();
        return await _db.AdminAuditLog.AsNoTracking()
            .Where(e => e.ActorEmail == actorEmail && e.EntityType == null && names.Contains(e.Action))
            .OrderByDescending(e => e.Timestamp)
            .ThenBy(e => e.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    /// <summary>When the owner's account was last locked (the newest lock_start row), if ever.</summary>
    public async Task<DateTimeOffset?> LatestLockStartAsync(string actorEmail, CancellationToken ct)
    {
        var rows = await _db.AdminAuditLog.AsNoTracking()
            .Where(e => e.ActorEmail == actorEmail && e.Action == AdminAuthAction.LockStart)
            .OrderByDescending(e => e.Timestamp)
            .Select(e => e.Timestamp)
            .Take(1)
            .ToListAsync(ct);
        return rows.Count == 0 ? null : rows[0];
    }
}

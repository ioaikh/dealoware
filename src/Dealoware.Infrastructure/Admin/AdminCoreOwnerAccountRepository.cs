using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminCoreOwnerAccountRepository : IAdminCoreOwnerAccountRepository
{
    private readonly DealowareDbContext _context;

    public AdminCoreOwnerAccountRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public Task<AdminCoreOwnerAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.AdminCoreOwnerAccounts
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);
    }

    public async Task AddAsync(AdminCoreOwnerAccount account, CancellationToken cancellationToken = default)
    {
        await _context.AdminCoreOwnerAccounts.AddAsync(account, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminRecoveryCode>> GetRecoveryCodesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AdminRecoveryCodes
            .Where(c => c.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRecoveryCodesAsync(
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default)
    {
        if (codes.Count == 0)
            return;

        var accountId = codes[0].AccountId;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.AdminRecoveryCodes
            .Where(c => c.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        await _context.AdminRecoveryCodes.AddRangeAsync(codes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<AdminPendingAuth?> GetPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.AdminPendingAuths
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddPendingAsync(AdminPendingAuth pending, CancellationToken cancellationToken = default)
    {
        await _context.AdminPendingAuths.AddAsync(pending, cancellationToken);
    }

    public async Task ConsumeOutstandingPendingAsync(
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await _context.AdminPendingAuths
            .Where(p => p.Email == email && p.ConsumedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.ConsumedAt, now),
                cancellationToken);
    }

    public async Task<bool> TryIncrementPendingFailureAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = AdminPendingAuth.MaxFailedCodeAttempts;
        var rows = await _context.AdminPendingAuths
            .Where(p =>
                p.Id == pendingId
                && p.ConsumedAt == null
                && p.ExpiresAt > now
                && p.FailedCodeAttempts < maxAttempts)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.FailedCodeAttempts, p => p.FailedCodeAttempts + 1),
                cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryConsumePendingAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AdminPendingAuths
            .Where(p =>
                p.Id == pendingId
                && p.ConsumedAt == null
                && p.ExpiresAt > now)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.ConsumedAt, now),
                cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryRedeemRecoveryCodeAsync(
        Guid accountId,
        string codeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AdminRecoveryCodes
            .Where(c =>
                c.AccountId == accountId
                && c.CodeHash == codeHash
                && c.UsedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.UsedAt, now),
                cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryRecordTotpTimestepAsync(
        Guid accountId,
        long step,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AdminCoreOwnerAccounts
            .Where(a =>
                a.Id == accountId
                && (a.LastUsedTotpTimestep == null || a.LastUsedTotpTimestep < step))
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.LastUsedTotpTimestep, step),
                cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryCompleteEnrollmentAsync(
        Guid accountId,
        string totpSecretCipher,
        string? recoveryCodesRevealCipher,
        long step,
        DateTimeOffset now,
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var rows = await _context.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId && a.TotpEnrolledAt == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.TotpSecretCipher, totpSecretCipher)
                    .SetProperty(a => a.TotpEnrolledAt, now)
                    .SetProperty(a => a.RecoveryCodesIssued, true)
                    .SetProperty(a => a.PendingTotpSecretCipher, (string?)null)
                    .SetProperty(a => a.RecoveryCodesRevealCipher, recoveryCodesRevealCipher)
                    .SetProperty(a => a.LastUsedTotpTimestep, step),
                cancellationToken);
        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        DetachTrackedAccount(accountId);
        await _context.AdminRecoveryCodes
            .Where(c => c.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        await _context.AdminRecoveryCodes.AddRangeAsync(codes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRecordFailedFactorAttemptAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var windowStartCutoff = now.AddMinutes(-AdminCoreOwnerAccount.FactorAttemptWindowMinutes);
        var lockUntil = now.AddMinutes(AdminCoreOwnerAccount.FactorLockoutMinutes);
        const int maxAttempts = AdminCoreOwnerAccount.MaxFailedFactorAttempts;

        var rows = await _context.AdminCoreOwnerAccounts
            .Where(a =>
                a.Id == accountId
                && (a.FactorLockedUntil == null || a.FactorLockedUntil <= now)
                && (a.FailedFactorAttempts < maxAttempts
                    || a.FactorAttemptWindowStartedAt == null
                    || a.FactorAttemptWindowStartedAt <= windowStartCutoff))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(
                        a => a.FailedFactorAttempts,
                        a => a.FactorAttemptWindowStartedAt == null
                             || a.FactorAttemptWindowStartedAt <= windowStartCutoff
                            ? 1
                            : a.FailedFactorAttempts + 1)
                    .SetProperty(
                        a => a.FactorAttemptWindowStartedAt,
                        a => a.FactorAttemptWindowStartedAt == null
                             || a.FactorAttemptWindowStartedAt <= windowStartCutoff
                            ? now
                            : a.FactorAttemptWindowStartedAt)
                    .SetProperty(
                        a => a.FactorLockedUntil,
                        a => (a.FactorAttemptWindowStartedAt == null
                              || a.FactorAttemptWindowStartedAt <= windowStartCutoff
                            ? 1
                            : a.FailedFactorAttempts + 1) >= maxAttempts
                            ? lockUntil
                            : a.FactorLockedUntil),
                cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryReserveSecondFactorAttemptAsync(
        Guid pendingId,
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryIncrementPendingFailureAsync(pendingId, now, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (!await TryRecordFailedFactorAttemptAsync(accountId, now, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ClearFactorLockAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _context.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.FailedFactorAttempts, 0)
                    .SetProperty(a => a.FactorAttemptWindowStartedAt, (DateTimeOffset?)null)
                    .SetProperty(a => a.FactorLockedUntil, (DateTimeOffset?)null),
                cancellationToken);
    }

    public async Task ClearRecoveryCodesRevealAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _context.AdminCoreOwnerAccounts
            .Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.RecoveryCodesRevealCipher, (string?)null),
                cancellationToken);
    }

    public async Task<bool> IsFactorLockedAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var until = await _context.AdminCoreOwnerAccounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => a.FactorLockedUntil)
            .FirstOrDefaultAsync(cancellationToken);
        return until is { } lockedUntil && now < lockedUntil;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    private void DetachTrackedAccount(Guid accountId)
    {
        var tracked = _context.AdminCoreOwnerAccounts.Local.FirstOrDefault(a => a.Id == accountId);
        if (tracked is not null)
            _context.Entry(tracked).State = EntityState.Detached;
    }
}

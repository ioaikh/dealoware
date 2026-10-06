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
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM AdminRecoveryCodes
            WHERE AccountId = {accountId}
            """,
            cancellationToken);
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
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminPendingAuths
            SET ConsumedAt = {now}
            WHERE Email = {email}
              AND ConsumedAt IS NULL
            """,
            cancellationToken);
    }

    public async Task<bool> TryIncrementPendingFailureAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = AdminPendingAuth.MaxFailedCodeAttempts;
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminPendingAuths
            SET FailedCodeAttempts = FailedCodeAttempts + 1,
                ConsumedAt = CASE
                    WHEN FailedCodeAttempts + 1 >= {maxAttempts} THEN {now}
                    ELSE ConsumedAt
                END
            WHERE Id = {pendingId}
              AND ConsumedAt IS NULL
              AND ExpiresAt > {now}
              AND FailedCodeAttempts < {maxAttempts}
            """,
            cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryConsumePendingAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminPendingAuths
            SET ConsumedAt = {now}
            WHERE Id = {pendingId}
              AND ConsumedAt IS NULL
              AND ExpiresAt > {now}
            """,
            cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryRedeemRecoveryCodeAsync(
        Guid accountId,
        string codeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminRecoveryCodes
            SET UsedAt = {now}
            WHERE AccountId = {accountId}
              AND CodeHash = {codeHash}
              AND UsedAt IS NULL
            """,
            cancellationToken);
        return rows == 1;
    }

    public async Task<bool> TryRecordTotpTimestepAsync(
        Guid accountId,
        long step,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminCoreOwnerAccounts
            SET LastUsedTotpTimestep = {step}
            WHERE Id = {accountId}
              AND (LastUsedTotpTimestep IS NULL OR LastUsedTotpTimestep < {step})
            """,
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
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminCoreOwnerAccounts
            SET TotpSecretCipher = {totpSecretCipher},
                TotpEnrolledAt = {now},
                RecoveryCodesIssued = {true},
                PendingTotpSecretCipher = NULL,
                RecoveryCodesRevealCipher = {recoveryCodesRevealCipher},
                LastUsedTotpTimestep = {step}
            WHERE Id = {accountId}
              AND TotpEnrolledAt IS NULL
            """,
            cancellationToken);
        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        DetachTrackedAccount(accountId);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM AdminRecoveryCodes
            WHERE AccountId = {accountId}
            """,
            cancellationToken);
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

        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminCoreOwnerAccounts
            SET FailedFactorAttempts = CASE
                    WHEN FactorAttemptWindowStartedAt IS NULL
                         OR FactorAttemptWindowStartedAt <= {windowStartCutoff}
                    THEN 1
                    ELSE FailedFactorAttempts + 1
                END,
                FactorAttemptWindowStartedAt = CASE
                    WHEN FactorAttemptWindowStartedAt IS NULL
                         OR FactorAttemptWindowStartedAt <= {windowStartCutoff}
                    THEN {now}
                    ELSE FactorAttemptWindowStartedAt
                END,
                FactorLockedUntil = CASE
                    WHEN (
                        CASE
                            WHEN FactorAttemptWindowStartedAt IS NULL
                                 OR FactorAttemptWindowStartedAt <= {windowStartCutoff}
                            THEN 1
                            ELSE FailedFactorAttempts + 1
                        END
                    ) >= {maxAttempts}
                    THEN {lockUntil}
                    ELSE FactorLockedUntil
                END
            WHERE Id = {accountId}
              AND (FactorLockedUntil IS NULL OR FactorLockedUntil <= {now})
              AND (
                    FailedFactorAttempts < {maxAttempts}
                    OR FactorAttemptWindowStartedAt IS NULL
                    OR FactorAttemptWindowStartedAt <= {windowStartCutoff}
                  )
            """,
            cancellationToken);
        return rows == 1;
    }

    public async Task ClearFactorLockAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminCoreOwnerAccounts
            SET FailedFactorAttempts = 0,
                FactorAttemptWindowStartedAt = NULL,
                FactorLockedUntil = NULL
            WHERE Id = {accountId}
            """,
            cancellationToken);
    }

    public async Task ClearRecoveryCodesRevealAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE AdminCoreOwnerAccounts
            SET RecoveryCodesRevealCipher = NULL
            WHERE Id = {accountId}
            """,
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

namespace Dealoware.Domain.Admin;

public interface IAdminCoreOwnerAccountRepository
{
    Task<AdminCoreOwnerAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(AdminCoreOwnerAccount account, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminRecoveryCode>> GetRecoveryCodesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddRecoveryCodesAsync(
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default);

    Task<AdminPendingAuth?> GetPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddPendingAsync(AdminPendingAuth pending, CancellationToken cancellationToken = default);

    Task ConsumeOutstandingPendingAsync(string email, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<bool> TryIncrementPendingFailureAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumePendingAsync(
        Guid pendingId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> TryRedeemRecoveryCodeAsync(
        Guid accountId,
        string codeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> TryRecordTotpTimestepAsync(
        Guid accountId,
        long step,
        CancellationToken cancellationToken = default);

    Task<bool> TryCompleteEnrollmentAsync(
        Guid accountId,
        string totpSecretCipher,
        string? recoveryCodesRevealCipher,
        long step,
        DateTimeOffset now,
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default);

    Task<bool> TryRecordFailedFactorAttemptAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims one second-factor evaluation slot on both the pending
    /// row and the account row. Both updates must affect exactly one row.
    /// </summary>
    Task<bool> TryReserveSecondFactorAttemptAsync(
        Guid pendingId,
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task ClearFactorLockAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task ClearRecoveryCodesRevealAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<bool> IsFactorLockedAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

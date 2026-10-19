namespace Dealoware.Domain.Admin;

/// <summary>
/// Cross-instance lockout counters and locks (SC-6). Durable store only.
/// Account keys are emails. IP keys are the trusted client address
/// (or its keyed HMAC); never a client-supplied left-most XFF hop.
/// </summary>
public interface IAdminLockoutStore
{
    Task<int> CountFailuresAsync(string scope, string subjectKey, DateTimeOffset windowStartExclusive, CancellationToken ct);

    Task AddFailureAsync(AdminAuthFailureEvent failure, CancellationToken ct);

    Task<AdminAuthLockout?> GetActiveLockoutAsync(string scope, string subjectKey, DateTimeOffset now, CancellationToken ct);

    Task AddLockoutAsync(AdminAuthLockout lockout, CancellationToken ct);

    /// <summary>
    /// Atomically reserve one attempt. Refuses when the gate is already
    /// locked or the sliding window is at <paramref name="limit"/>. Does
    /// not activate the lock — the caller locks only after a failed
    /// evaluation, or releases <see cref="FailureIncrementResult.ReservationId"/>
    /// on success.
    /// </summary>
    Task<FailureIncrementResult> TryIncrementFailureAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct);

    Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct);

    /// <summary>
    /// Activate the gate when the window count is at least
    /// <paramref name="limit"/>. Version compare-and-set; never an
    /// unconditional write.
    /// </summary>
    Task<bool> TryActivateLockAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct);

    /// <summary>
    /// Deactivate the gate only when it is active and the window count is
    /// below <paramref name="limit"/>. Version compare-and-set.
    /// </summary>
    Task<bool> TryReleaseLockIfBelowLimitAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        CancellationToken ct);

    Task ClearAccountFailuresAsync(string email, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

public readonly record struct FailureIncrementResult(
    bool Incremented,
    bool LockCreated,
    bool AlreadyLocked,
    int Count,
    Guid? ReservationId);

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
    /// Atomically: refuse if already locked, else increment and lock when
    /// the window count reaches <paramref name="limit"/>. Parallel callers
    /// cannot pass the threshold.
    /// </summary>
    Task<FailureIncrementResult> TryIncrementFailureAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct);

    Task ClearAccountFailuresAsync(string email, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

public readonly record struct FailureIncrementResult(
    bool Incremented,
    bool LockCreated,
    bool AlreadyLocked,
    int Count);

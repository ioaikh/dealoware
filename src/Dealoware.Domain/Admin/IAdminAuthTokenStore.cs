namespace Dealoware.Domain.Admin;

public interface IAdminAuthTokenStore
{
    Task<AdminAuthToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(AdminAuthToken token, CancellationToken ct);
    Task CancelUnusedAsync(string kind, string email, DateTimeOffset now, CancellationToken ct);

    Task ConsumeOutstandingPendingAsync(string email, DateTimeOffset now, CancellationToken ct);

    /// <summary>
    /// Conditional single-use consume: <c>WHERE TokenHash = @hash AND ConsumedAt IS NULL</c>,
    /// rows == 1.
    /// </summary>
    Task<bool> TryConsumeAsync(string tokenHash, DateTimeOffset now, CancellationToken ct);

    /// <summary>
    /// Step-2 pending cap as a conditional write: increment only when
    /// <c>ConsumedAt IS NULL AND AttemptCount &lt; max AND ExpiresAt &gt; now</c>, rows == 1.
    /// </summary>
    Task<bool> TryIncrementPendingAttemptAsync(
        Guid id,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

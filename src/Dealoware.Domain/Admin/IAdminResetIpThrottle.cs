namespace Dealoware.Domain.Admin;

public interface IAdminResetIpThrottle
{
    /// <summary>
    /// Atomically reserves one slot under the Spec §8.6 budget
    /// (fixed 15-minute window from <c>WindowStartedAt</c>, ruling
    /// <c>e48f4581</c>, and not locked). Succeeds only when the conditional
    /// update or insert affects exactly one row (SC-6 / R5). In-lock events
    /// are not counted.
    /// </summary>
    Task<bool> TryReserveAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Read-only: true while this IP is inside its 30-minute lock. Counts
    /// nothing, so a caller can refuse a locked IP before Turnstile and still
    /// keep rejected Turnstile submissions off the counter (TD-ADM-040).
    /// </summary>
    Task<bool> IsLockedAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

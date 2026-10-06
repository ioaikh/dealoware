namespace Dealoware.Domain.Admin;

public interface IAdminResetIpThrottle
{
    Task<bool> IsThrottledAsync(string ipHmac, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically reserves one slot under the Spec §8.6 budget
    /// (FailureCount &lt; Threshold and not locked). Succeeds only when the
    /// conditional update or insert affects exactly one row (SC-6).
    /// In-lock events are not counted.
    /// </summary>
    Task<bool> TryReserveAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

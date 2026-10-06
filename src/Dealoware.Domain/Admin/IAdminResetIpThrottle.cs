namespace Dealoware.Domain.Admin;

public sealed record AdminResetThrottleRecord(bool Throttled, int FailureCount);

public interface IAdminResetIpThrottle
{
    Task<bool> IsThrottledAsync(string ipHmac, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically records one reset-IP event. In-lock events are not counted.
    /// </summary>
    Task<AdminResetThrottleRecord> RecordFailureAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

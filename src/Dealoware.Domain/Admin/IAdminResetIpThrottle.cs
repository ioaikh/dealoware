namespace Dealoware.Domain.Admin;

public interface IAdminResetIpThrottle
{
    /// <summary>
    /// Atomically reserves one slot under the Spec §8.6 budget
    /// (events still inside the exclusive 15-minute window, and not locked).
    /// Succeeds only when the conditional update or insert affects exactly
    /// one row (SC-6 / R5). In-lock events are not counted.
    /// </summary>
    Task<bool> TryReserveAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

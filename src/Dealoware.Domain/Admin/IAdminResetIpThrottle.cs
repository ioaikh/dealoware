namespace Dealoware.Domain.Admin;

/// <summary>
/// Step 5 owns the reset/bootstrap IP throttle. Step 4 records a failure
/// so that implementation can count it. This PR ships a no-op.
/// </summary>
public interface IAdminResetIpThrottle
{
    Task RecordFailureAsync(string ipHmac, CancellationToken cancellationToken);
}

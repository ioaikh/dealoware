using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Fail-open no-op until Step 5 stores the reset/bootstrap IP counter.
/// </summary>
public sealed class PendingStep5ResetIpThrottle : IAdminResetIpThrottle
{
    public Task RecordFailureAsync(string ipHmac, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Clock seam so reset expiry tests can advance time without wall-clock waits.
/// </summary>
public interface IAdminClock
{
    DateTimeOffset UtcNow { get; }
}

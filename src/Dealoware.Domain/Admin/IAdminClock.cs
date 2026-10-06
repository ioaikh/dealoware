namespace Dealoware.Domain.Admin;

/// <summary>
/// Clock for bootstrap token lifetime. Tests substitute a controllable clock.
/// </summary>
public interface IAdminClock
{
    DateTimeOffset UtcNow { get; }
}

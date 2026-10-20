namespace Dealoware.Domain.Admin;

/// <summary>
/// Clock used by lockout windows and session timers so tests can advance time.
/// </summary>
public interface IAdminClock
{
    DateTimeOffset UtcNow { get; }
}

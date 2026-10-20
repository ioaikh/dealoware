namespace Dealoware.Domain.Admin;

/// <summary>
/// Clock used by lockout windows, session timers, and reset expiry so tests can advance time.
/// </summary>
public interface IAdminClock
{
    DateTimeOffset UtcNow { get; }
}

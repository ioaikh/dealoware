namespace Dealoware.Domain.Admin;

/// <summary>
/// Clock used by admin delete confirm tokens so tests can advance time.
/// </summary>
public interface IAdminClock
{
    DateTimeOffset UtcNow { get; }
}

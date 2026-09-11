namespace Dealoware.Domain.Participants;

/// <summary>
/// Repository for Participant persistence.
/// </summary>
public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Participant?> GetBySubAsync(string sub, CancellationToken cancellationToken = default);
    Task AddAsync(Participant participant, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

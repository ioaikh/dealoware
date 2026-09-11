namespace Dealoware.Domain.Artifacts;

/// <summary>
/// Repository interface for Artifact persistence.
/// </summary>
public interface IArtifactRepository
{
    Task<Artifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Artifact>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Artifact artifact, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

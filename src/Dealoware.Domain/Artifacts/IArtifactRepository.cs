namespace Dealoware.Domain.Artifacts;

/// <summary>
/// Repository interface for Artifact persistence.
/// </summary>
public interface IArtifactRepository
{
    Task<Artifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an artifact by ID only if the caller is the owner.
    /// Returns null if the artifact doesn't exist OR if the caller is not the owner.
    /// This enforces owner-scope authorization at the query plane (not fetch-then-filter).
    /// </summary>
    Task<Artifact?> GetByIdForOwnerAsync(Guid id, string ownerParticipantId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Artifact>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Artifact artifact, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

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

    /// <summary>
    /// Searches discoverable artifacts by query string across D1-D5 fields.
    /// 
    /// SECURITY: This is a DISCOVERY surface, SEPARATE from owner inventory (GetByOwnerAsync).
    /// Returns artifacts matching the query regardless of ownership - for authenticated
    /// Participants to discover others' artifacts on the Platform.
    /// 
    /// Query-plane filter: applies search predicate at DB level, not fetch-all-then-filter.
    /// Caller must authenticate (#5 principal required); this method does NOT filter by owner.
    /// </summary>
    /// <param name="query">Search query to match against artifact fields (Subject/Intent/Location).</param>
    /// <param name="callerParticipantId">The authenticated caller's participant ID (for audit, not filtering).</param>
    /// <param name="limit">Maximum results to return (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discoverable artifacts matching the query, ordered by relevance/recency.</returns>
    Task<IReadOnlyList<Artifact>> SearchDiscoverableAsync(
        string query, 
        string callerParticipantId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}

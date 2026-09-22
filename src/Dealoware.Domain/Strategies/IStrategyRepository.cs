namespace Dealoware.Domain.Strategies;

/// <summary>
/// Repository interface for Strategy persistence.
/// Owner-scoped query-plane authorization (OwnerParticipantId == principal sub).
/// </summary>
public interface IStrategyRepository
{
    /// <summary>
    /// Gets a strategy by ID only if the caller is the owner.
    /// Returns null if the strategy doesn't exist OR if the caller is not the owner.
    /// This enforces owner-scope authorization at the query plane (not fetch-then-filter).
    /// </summary>
    Task<Strategy?> GetByIdForOwnerAsync(Guid id, string ownerParticipantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all strategies owned by the specified participant.
    /// Returns only strategies where OwnerParticipantId matches.
    /// </summary>
    Task<IReadOnlyList<Strategy>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new strategy.
    /// </summary>
    Task AddAsync(Strategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing strategy.
    /// </summary>
    void Update(Strategy strategy);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Repository interface for Negotiation persistence.
/// </summary>
public interface INegotiationRepository
{
    /// <summary>
    /// Gets a negotiation by ID with its offers loaded.
    /// </summary>
    Task<Negotiation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a negotiation by ID with its offers loaded for a specific party.
    /// Returns null if the participant is not a party to the negotiation.
    /// This supports the "non-party → 404" security requirement.
    /// </summary>
    Task<Negotiation?> GetByIdForPartyAsync(Guid id, string participantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all negotiations for a participant (as either party).
    /// </summary>
    Task<IReadOnlyList<Negotiation>> GetByParticipantAsync(string participantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new negotiation.
    /// </summary>
    Task AddAsync(Negotiation negotiation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing negotiation.
    /// </summary>
    void Update(Negotiation negotiation);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

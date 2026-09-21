namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Repository interface for Offer persistence.
/// </summary>
public interface IOfferRepository
{
    /// <summary>
    /// Gets an offer by ID.
    /// </summary>
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an offer by ID only if the caller is a party to the parent negotiation.
    /// Returns null if offer doesn't exist OR if the caller is not a party.
    /// This enforces party-scope authorization at the query plane.
    /// </summary>
    Task<Offer?> GetByIdForPartyAsync(Guid id, string participantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all offers where the caller is a party to the parent negotiation.
    /// Returns offers scoped to negotiations where participantId is party A or B.
    /// </summary>
    Task<IReadOnlyList<Offer>> GetByParticipantAsync(string participantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all offers for a negotiation.
    /// </summary>
    Task<IReadOnlyList<Offer>> GetByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open offers for a negotiation.
    /// </summary>
    Task<IReadOnlyList<Offer>> GetOpenByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new offer.
    /// </summary>
    Task AddAsync(Offer offer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing offer.
    /// </summary>
    void Update(Offer offer);

    /// <summary>
    /// Updates multiple offers.
    /// </summary>
    void UpdateRange(IEnumerable<Offer> offers);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

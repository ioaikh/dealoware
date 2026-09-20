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

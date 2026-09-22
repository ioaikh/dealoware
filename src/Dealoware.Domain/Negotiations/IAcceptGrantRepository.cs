namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Repository interface for AcceptGrant persistence.
/// Stage B (#42): Manages Accept grants for contact-on-accept.
/// </summary>
public interface IAcceptGrantRepository
{
    /// <summary>
    /// Gets an AcceptGrant by offer ID and grantee sub.
    /// Returns the grant if the grantee has been authorized contact access.
    /// </summary>
    Task<AcceptGrant?> GetByOfferAndGranteeAsync(Guid offerId, string granteeSub, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an AcceptGrant by negotiation ID and grantee sub.
    /// Returns the grant if there's an accepted offer authorizing contact access.
    /// </summary>
    Task<AcceptGrant?> GetByNegotiationAndGranteeAsync(Guid negotiationId, string granteeSub, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a grantee has an Accept grant for a negotiation.
    /// </summary>
    Task<bool> HasGrantForNegotiationAsync(Guid negotiationId, string granteeSub, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new AcceptGrant.
    /// </summary>
    Task AddAsync(AcceptGrant grant, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds multiple AcceptGrants.
    /// </summary>
    Task AddRangeAsync(IEnumerable<AcceptGrant> grants, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

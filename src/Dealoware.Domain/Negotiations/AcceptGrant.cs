namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Represents an Accept grant for ShareOutbound(ContactEmail).
/// Stage B (#42): Persists when Accept is recorded, enabling contact exchange.
/// 
/// Invariants:
/// - One AcceptGrant per accepted Offer
/// - GrantorSub is the accepting party (ToParticipantId of the offer)
/// - GranteeSub is the counterparty (FromParticipantId of the offer)
/// - Created atomically with Offer.Accept()
/// </summary>
public sealed class AcceptGrant
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// The offer this grant is associated with.
    /// </summary>
    public Guid OfferId { get; private set; }
    
    /// <summary>
    /// The negotiation this grant is associated with.
    /// </summary>
    public Guid NegotiationId { get; private set; }
    
    /// <summary>
    /// The subject identifier of the grantor (party who accepted the offer).
    /// This party grants their ContactEmail to the grantee.
    /// </summary>
    public string GrantorSub { get; private set; } = string.Empty;
    
    /// <summary>
    /// The subject identifier of the grantee (counterparty who made the offer).
    /// This party receives the grantor's ContactEmail.
    /// </summary>
    public string GranteeSub { get; private set; } = string.Empty;
    
    /// <summary>
    /// When the grant was created (Accept time).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
    
    private AcceptGrant() { }
    
    /// <summary>
    /// Creates a mutual AcceptGrant pair for both parties on Accept.
    /// Stage B: Both parties receive each other's ContactEmail after Accept.
    /// </summary>
    /// <param name="offerId">The accepted offer ID</param>
    /// <param name="negotiationId">The negotiation ID</param>
    /// <param name="acceptorSub">The party who accepted (ToParticipantId)</param>
    /// <param name="offerorSub">The party who made the offer (FromParticipantId)</param>
    /// <returns>Two AcceptGrant records - one for each direction of contact sharing</returns>
    public static (AcceptGrant AcceptorToOfferor, AcceptGrant OfferorToAcceptor) CreatePair(
        Guid offerId,
        Guid negotiationId,
        string acceptorSub,
        string offerorSub)
    {
        var now = DateTimeOffset.UtcNow;
        
        var acceptorToOfferor = new AcceptGrant
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            NegotiationId = negotiationId,
            GrantorSub = acceptorSub,
            GranteeSub = offerorSub,
            CreatedAt = now
        };
        
        var offerorToAcceptor = new AcceptGrant
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            NegotiationId = negotiationId,
            GrantorSub = offerorSub,
            GranteeSub = acceptorSub,
            CreatedAt = now
        };
        
        return (acceptorToOfferor, offerorToAcceptor);
    }
    
    /// <summary>
    /// Checks if this grant authorizes contact sharing to a specific party.
    /// </summary>
    public bool AuthorizesShareTo(string requestingSub)
    {
        return GranteeSub == requestingSub;
    }
}

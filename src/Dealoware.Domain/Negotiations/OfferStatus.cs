namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Status of an Offer within a Negotiation.
/// </summary>
public enum OfferStatus
{
    /// <summary>
    /// Offer is active and can be accepted, declined, or countered.
    /// </summary>
    Open = 0,
    
    /// <summary>
    /// Offer was accepted by the recipient.
    /// Other open offers in the negotiation are cancelled.
    /// </summary>
    Accepted = 1,
    
    /// <summary>
    /// Offer was explicitly declined by the recipient.
    /// </summary>
    Declined = 2,
    
    /// <summary>
    /// Offer was superseded by a counter-offer.
    /// </summary>
    Superseded = 3,
    
    /// <summary>
    /// Offer was cancelled (negotiation closed/expired or another offer accepted).
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Offer was withdrawn by the maker. First-class offer-only status (int 5).
    /// Admin cannot set this; only <see cref="Offer.Withdraw"/> can.
    /// </summary>
    Withdrawn = 5
}

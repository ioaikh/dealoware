namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Status of a 1:1 Negotiation between two parties.
/// </summary>
public enum NegotiationStatus
{
    /// <summary>
    /// Negotiation is active and accepting offers.
    /// </summary>
    Open = 0,
    
    /// <summary>
    /// Negotiation was explicitly closed by a party.
    /// All open offers are cancelled when closed.
    /// </summary>
    Closed = 1,
    
    /// <summary>
    /// Negotiation expired after endsAt time passed.
    /// All open offers are cancelled when expired.
    /// </summary>
    Expired = 2
}

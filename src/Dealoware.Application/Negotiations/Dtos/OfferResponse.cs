namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Response representing an offer.
/// 
/// Identity-seal invariant (PoC stub - precursor to MVP P7/A9):
/// - FromParticipantId and ToParticipantId are opaque identifiers only
/// - No contact PII (email, phone, address) is exposed, even on accept
/// - IdentitySealed flag indicates contact is protected; contact release is MVP (P7/A9)
/// </summary>
public class OfferResponse
{
    public Guid Id { get; set; }
    public Guid NegotiationId { get; set; }
    
    /// <summary>
    /// Opaque participant identifier for offer sender. No contact PII.
    /// </summary>
    public string FromParticipantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Opaque participant identifier for offer recipient. No contact PII.
    /// </summary>
    public string ToParticipantId { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    
    /// <summary>
    /// Optional terms text. Must NOT contain contact PII.
    /// </summary>
    public string? Terms { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Identity-seal stub flag (PoC).
    /// Always true in PoC - counterparty contact is protected.
    /// Contact exchange on accept is MVP (P7/A9), not PoC.
    /// </summary>
    public bool IdentitySealed { get; set; } = true;
}

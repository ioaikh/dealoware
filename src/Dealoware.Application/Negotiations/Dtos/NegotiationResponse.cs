namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Response representing a negotiation.
/// 
/// Identity-seal invariant (PoC stub - precursor to MVP P7/A9):
/// - PartyAParticipantId and PartyBParticipantId are opaque identifiers only
/// - No contact PII (email, phone, address) is exposed in this DTO
/// - IdentitySealed flag indicates contact is protected; contact release is MVP (P7/A9)
/// </summary>
public class NegotiationResponse
{
    public Guid Id { get; set; }
    public Guid ArtifactId { get; set; }
    
    /// <summary>
    /// Opaque participant identifier for Party A. No contact PII.
    /// </summary>
    public string PartyAParticipantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Opaque participant identifier for Party B. No contact PII.
    /// </summary>
    public string PartyBParticipantId { get; set; } = string.Empty;
    
    public string PartyAIntent { get; set; } = string.Empty;
    public string PartyBIntent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Identity-seal stub flag (PoC).
    /// Always true in PoC - counterparty contact is protected.
    /// Contact exchange on accept is MVP (P7/A9), not PoC.
    /// </summary>
    public bool IdentitySealed { get; set; } = true;
    
    /// <summary>
    /// Optional embedded offers (included on GET /negotiations/{id}).
    /// </summary>
    public List<OfferResponse>? Offers { get; set; }
}

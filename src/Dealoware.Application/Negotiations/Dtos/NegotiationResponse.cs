namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Response representing a negotiation.
/// </summary>
public class NegotiationResponse
{
    public Guid Id { get; set; }
    public Guid ArtifactId { get; set; }
    public string PartyAParticipantId { get; set; } = string.Empty;
    public string PartyBParticipantId { get; set; } = string.Empty;
    public string PartyAIntent { get; set; } = string.Empty;
    public string PartyBIntent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Optional embedded offers (included on GET /negotiations/{id}).
    /// </summary>
    public List<OfferResponse>? Offers { get; set; }
}

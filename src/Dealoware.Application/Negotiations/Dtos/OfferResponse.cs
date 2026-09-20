namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Response representing an offer.
/// NOTE: No contact information or PII is included, even on accept.
/// </summary>
public class OfferResponse
{
    public Guid Id { get; set; }
    public Guid NegotiationId { get; set; }
    public string FromParticipantId { get; set; } = string.Empty;
    public string ToParticipantId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Terms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

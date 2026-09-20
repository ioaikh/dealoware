namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Request to place a new offer in a negotiation.
/// Caller becomes FromParticipant, other party becomes ToParticipant.
/// </summary>
public class CreateOfferRequest
{
    /// <summary>
    /// Optional monetary amount (e.g., 1500.00).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Currency code (3-letter ISO). Required if Amount is specified.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Optional terms describing the offer. NO contact info or PII.
    /// </summary>
    public string? Terms { get; set; }
}

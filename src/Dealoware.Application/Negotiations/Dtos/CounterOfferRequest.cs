namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Request to counter an offer.
/// The prior offer becomes Superseded, a new offer is created from counterparty.
/// </summary>
public class CounterOfferRequest
{
    /// <summary>
    /// Optional new monetary amount.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Currency code (3-letter ISO). Required if Amount is specified.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Optional terms for the counter-offer. NO contact info or PII.
    /// </summary>
    public string? Terms { get; set; }
}

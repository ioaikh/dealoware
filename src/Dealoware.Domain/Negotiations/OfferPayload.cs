namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Thin payload for an Offer.
/// Contains monetary amount and/or terms string.
/// 
/// NO contact information or PII.
/// NO Strategy/AI fields.
/// </summary>
public sealed class OfferPayload
{
    /// <summary>
    /// Optional monetary amount (e.g., 1500.00).
    /// </summary>
    public decimal? Amount { get; private set; }

    /// <summary>
    /// Optional currency code (e.g., "USD", "EUR").
    /// Normalized to uppercase.
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>
    /// Optional terms string describing the offer conditions.
    /// Free-form text, NO contact info or PII.
    /// </summary>
    public string? Terms { get; private set; }

    private OfferPayload() { }

    /// <summary>
    /// Creates an offer payload with validation.
    /// At least one of amount or terms must be provided.
    /// </summary>
    public static (OfferPayload? Payload, List<string> Errors) Create(
        decimal? amount,
        string? currency,
        string? terms)
    {
        var errors = new List<string>();

        if (!amount.HasValue && string.IsNullOrWhiteSpace(terms))
            errors.Add("At least one of Amount or Terms must be provided");

        if (amount.HasValue && amount.Value < 0)
            errors.Add("Amount cannot be negative");

        if (amount.HasValue && string.IsNullOrWhiteSpace(currency))
            errors.Add("Currency is required when Amount is specified");

        if (!string.IsNullOrWhiteSpace(currency) && currency.Trim().Length != 3)
            errors.Add("Currency must be a 3-letter ISO code");

        if (errors.Count > 0)
            return (null, errors);

        var payload = new OfferPayload
        {
            Amount = amount,
            Currency = currency?.Trim().ToUpperInvariant(),
            Terms = terms?.Trim()
        };

        return (payload, errors);
    }
}

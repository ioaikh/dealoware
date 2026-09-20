namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Offer aggregate representing a proposal in a 1:1 Negotiation.
/// 
/// Invariants:
/// - Belongs to exactly one Negotiation
/// - FromParticipantId must be a party to the negotiation
/// - ToParticipantId must be the other party
/// - Only Open offers can be accepted, declined, or countered
/// - Accept/Decline/Counter can only be done by ToParticipantId
/// - One-open-per-side: each party can have at most one open offer
/// </summary>
public sealed class Offer
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The negotiation this offer belongs to.
    /// </summary>
    public Guid NegotiationId { get; private set; }

    /// <summary>
    /// The participant who made this offer.
    /// </summary>
    public string FromParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// The participant who receives this offer.
    /// Must be the other party in the negotiation.
    /// </summary>
    public string ToParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the offer.
    /// </summary>
    public OfferStatus Status { get; private set; }

    /// <summary>
    /// Optional monetary amount.
    /// </summary>
    public decimal? Amount { get; private set; }

    /// <summary>
    /// Optional currency code (3-letter ISO).
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>
    /// Optional terms string. NO contact/PII.
    /// </summary>
    public string? Terms { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Offer() { }

    /// <summary>
    /// Creates a new offer.
    /// </summary>
    public static (Offer? Offer, List<string> Errors) Create(
        Guid negotiationId,
        string fromParticipantId,
        string toParticipantId,
        decimal? amount,
        string? currency,
        string? terms)
    {
        var errors = new List<string>();

        if (negotiationId == Guid.Empty)
            errors.Add("NegotiationId is required");

        if (string.IsNullOrWhiteSpace(fromParticipantId))
            errors.Add("FromParticipantId is required");

        if (string.IsNullOrWhiteSpace(toParticipantId))
            errors.Add("ToParticipantId is required");

        if (fromParticipantId == toParticipantId)
            errors.Add("FromParticipantId and ToParticipantId must be different");

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

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            NegotiationId = negotiationId,
            FromParticipantId = fromParticipantId,
            ToParticipantId = toParticipantId,
            Status = OfferStatus.Open,
            Amount = amount,
            Currency = currency?.Trim().ToUpperInvariant(),
            Terms = terms?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return (offer, errors);
    }

    /// <summary>
    /// Accepts this offer. Can only be done by ToParticipantId when Open.
    /// </summary>
    /// <returns>True if successfully accepted</returns>
    public bool Accept(string callerParticipantId)
    {
        if (Status != OfferStatus.Open)
            return false;

        if (callerParticipantId != ToParticipantId)
            return false;

        Status = OfferStatus.Accepted;
        return true;
    }

    /// <summary>
    /// Declines this offer. Can only be done by ToParticipantId when Open.
    /// </summary>
    /// <returns>True if successfully declined</returns>
    public bool Decline(string callerParticipantId)
    {
        if (Status != OfferStatus.Open)
            return false;

        if (callerParticipantId != ToParticipantId)
            return false;

        Status = OfferStatus.Declined;
        return true;
    }

    /// <summary>
    /// Marks this offer as superseded by a counter-offer.
    /// </summary>
    /// <returns>True if successfully superseded</returns>
    public bool Supersede()
    {
        if (Status != OfferStatus.Open)
            return false;

        Status = OfferStatus.Superseded;
        return true;
    }

    /// <summary>
    /// Cancels this offer (negotiation closed/expired or another offer accepted).
    /// </summary>
    /// <returns>True if successfully cancelled</returns>
    public bool Cancel()
    {
        if (Status != OfferStatus.Open)
            return false;

        Status = OfferStatus.Cancelled;
        return true;
    }
}

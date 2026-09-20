namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Negotiation aggregate root representing a 1:1 negotiation between two parties around an Artifact.
/// 
/// Invariants:
/// - Exactly one Artifact (artifactId is immutable after creation)
/// - Exactly two distinct parties (partyA ≠ partyB)
/// - Parties must have complementary intents at creation
/// - Status transitions: Open → Closed | Expired (terminal states)
/// - When Closed or Expired, all open offers are cancelled
/// - If endsAt is set and now ≥ endsAt, status becomes Expired on any mutating operation
/// </summary>
public sealed class Negotiation
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The Artifact being negotiated. Immutable after creation.
    /// Must reference an existing Artifact.
    /// </summary>
    public Guid ArtifactId { get; private set; }

    /// <summary>
    /// The initiating party's participant ID (from principal sub claim).
    /// </summary>
    public string PartyAParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// The counterparty's participant ID. Must be distinct from PartyA.
    /// </summary>
    public string PartyBParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// Intent of Party A (from their Artifact or stated at creation).
    /// Used for complementary intent validation.
    /// </summary>
    public string PartyAIntent { get; private set; } = string.Empty;

    /// <summary>
    /// Intent of Party B (from their Artifact or stated at creation).
    /// Must be complementary to PartyAIntent.
    /// </summary>
    public string PartyBIntent { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the negotiation.
    /// </summary>
    public NegotiationStatus Status { get; private set; }

    /// <summary>
    /// Optional: When the negotiation becomes active.
    /// If null, immediately active upon creation.
    /// </summary>
    public DateTimeOffset? StartsAt { get; private set; }

    /// <summary>
    /// Optional: When the negotiation expires (D10).
    /// If set and now ≥ endsAt, status becomes Expired.
    /// </summary>
    public DateTimeOffset? EndsAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Navigation property for Offers in this negotiation.
    /// </summary>
    private readonly List<Offer> _offers = new();
    public IReadOnlyList<Offer> Offers => _offers;

    private Negotiation() { }

    /// <summary>
    /// Creates a new 1:1 negotiation with complementary intent validation.
    /// </summary>
    /// <param name="artifactId">The artifact being negotiated (must exist)</param>
    /// <param name="partyAParticipantId">Initiating party's participant ID</param>
    /// <param name="partyBParticipantId">Counterparty's participant ID (must be distinct)</param>
    /// <param name="partyAIntent">Intent of party A</param>
    /// <param name="partyBIntent">Intent of party B (must be complementary)</param>
    /// <param name="startsAt">Optional start time</param>
    /// <param name="endsAt">Optional expiration time (D10)</param>
    /// <returns>Result with the negotiation or validation errors</returns>
    public static (Negotiation? Negotiation, List<string> Errors) Create(
        Guid artifactId,
        string partyAParticipantId,
        string partyBParticipantId,
        string partyAIntent,
        string partyBIntent,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null)
    {
        var errors = new List<string>();

        if (artifactId == Guid.Empty)
            errors.Add("ArtifactId is required");

        if (string.IsNullOrWhiteSpace(partyAParticipantId))
            errors.Add("PartyAParticipantId is required");

        if (string.IsNullOrWhiteSpace(partyBParticipantId))
            errors.Add("PartyBParticipantId is required");

        if (!string.IsNullOrWhiteSpace(partyAParticipantId) && 
            !string.IsNullOrWhiteSpace(partyBParticipantId) &&
            partyAParticipantId == partyBParticipantId)
            errors.Add("PartyA and PartyB must be distinct participants");

        if (string.IsNullOrWhiteSpace(partyAIntent))
            errors.Add("PartyAIntent is required");

        if (string.IsNullOrWhiteSpace(partyBIntent))
            errors.Add("PartyBIntent is required");

        if (!string.IsNullOrWhiteSpace(partyAIntent) && 
            !string.IsNullOrWhiteSpace(partyBIntent) &&
            !IntentComplement.AreComplementary(partyAIntent, partyBIntent))
            errors.Add($"Intents are not complementary: '{partyAIntent}' and '{partyBIntent}'. See Complementary Intent Pairs in product documentation.");

        if (startsAt.HasValue && endsAt.HasValue && startsAt.Value >= endsAt.Value)
            errors.Add("EndsAt must be after StartsAt");

        if (errors.Count > 0)
            return (null, errors);

        var negotiation = new Negotiation
        {
            Id = Guid.NewGuid(),
            ArtifactId = artifactId,
            PartyAParticipantId = partyAParticipantId,
            PartyBParticipantId = partyBParticipantId,
            PartyAIntent = partyAIntent.Trim().ToLowerInvariant(),
            PartyBIntent = partyBIntent.Trim().ToLowerInvariant(),
            Status = NegotiationStatus.Open,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return (negotiation, errors);
    }

    /// <summary>
    /// Checks if the given participant is a party to this negotiation.
    /// </summary>
    public bool IsParty(string participantId)
    {
        return PartyAParticipantId == participantId || PartyBParticipantId == participantId;
    }

    /// <summary>
    /// Gets the other party's participant ID given one party.
    /// </summary>
    public string? GetOtherParty(string participantId)
    {
        if (participantId == PartyAParticipantId)
            return PartyBParticipantId;
        if (participantId == PartyBParticipantId)
            return PartyAParticipantId;
        return null;
    }

    /// <summary>
    /// Checks if the negotiation has expired based on endsAt.
    /// If expired, updates status to Expired.
    /// </summary>
    /// <returns>True if negotiation is or has become expired</returns>
    public bool CheckAndApplyExpiration()
    {
        if (Status != NegotiationStatus.Open)
            return Status == NegotiationStatus.Expired;

        if (EndsAt.HasValue && DateTimeOffset.UtcNow >= EndsAt.Value)
        {
            Status = NegotiationStatus.Expired;
            CancelAllOpenOffers();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Closes the negotiation. Cancels all open offers.
    /// </summary>
    /// <returns>True if successfully closed, false if already closed/expired</returns>
    public bool Close()
    {
        CheckAndApplyExpiration();

        if (Status != NegotiationStatus.Open)
            return false;

        Status = NegotiationStatus.Closed;
        CancelAllOpenOffers();
        return true;
    }

    /// <summary>
    /// Adds an offer to this negotiation (for EF navigation).
    /// </summary>
    internal void AddOffer(Offer offer)
    {
        _offers.Add(offer);
    }

    /// <summary>
    /// Cancels all open offers in this negotiation.
    /// Called when negotiation is closed or expired.
    /// </summary>
    private void CancelAllOpenOffers()
    {
        foreach (var offer in _offers.Where(o => o.Status == OfferStatus.Open))
        {
            offer.Cancel();
        }
    }

    /// <summary>
    /// Gets all open offers for cancellation when an offer is accepted.
    /// </summary>
    public IEnumerable<Offer> GetOpenOffersExcept(Guid acceptedOfferId)
    {
        return _offers.Where(o => o.Status == OfferStatus.Open && o.Id != acceptedOfferId);
    }

    /// <summary>
    /// Checks if a party already has an open offer.
    /// One-open-per-side rule: each party can have at most one open offer at a time.
    /// </summary>
    public bool HasOpenOfferFrom(string participantId)
    {
        return _offers.Any(o => o.Status == OfferStatus.Open && o.FromParticipantId == participantId);
    }
}

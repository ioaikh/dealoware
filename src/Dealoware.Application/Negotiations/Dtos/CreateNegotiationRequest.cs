namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Request to create a new 1:1 negotiation.
/// Caller becomes PartyA (from principal sub claim).
/// </summary>
public class CreateNegotiationRequest
{
    /// <summary>
    /// The artifact being negotiated. Must exist.
    /// </summary>
    public Guid ArtifactId { get; set; }

    /// <summary>
    /// The counterparty's participant ID. Must be distinct from caller.
    /// </summary>
    public string CounterpartyParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// Intent of the caller (party A). E.g., "buy", "sell", "provide", "consume", "rent".
    /// Must be complementary to CounterpartyIntent.
    /// </summary>
    public string CallerIntent { get; set; } = string.Empty;

    /// <summary>
    /// Expected intent of the counterparty (party B).
    /// Must be complementary to CallerIntent.
    /// </summary>
    public string CounterpartyIntent { get; set; } = string.Empty;

    /// <summary>
    /// Optional: When the negotiation becomes active.
    /// </summary>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>
    /// Optional: When the negotiation expires (D10).
    /// </summary>
    public DateTimeOffset? EndsAt { get; set; }
}

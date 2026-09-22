namespace Dealoware.Application.Negotiations.Dtos;

/// <summary>
/// Response for Accept offer action with counterparty contact sharing.
/// Stage B (#42): Contact on accept - counterparty ContactEmail revealed.
/// 
/// Security invariants:
/// - CounterpartyContactEmail: ContactEmail of the offer sender (FromParticipantId)
/// - LoginEmail: NEVER included - User-only field
/// - Only returned to accepting party (ToParticipantId) after Accept recorded
/// - IdentitySealed becomes false when ContactEmail is shared
/// </summary>
public class AcceptOfferResponse
{
    public Guid Id { get; set; }
    public Guid NegotiationId { get; set; }
    
    /// <summary>
    /// Opaque participant identifier for offer sender.
    /// </summary>
    public string FromParticipantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Opaque participant identifier for offer recipient (acceptor).
    /// </summary>
    public string ToParticipantId { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Terms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Identity seal status post-Accept.
    /// Stage B (#42): False when Accept grant enables ContactEmail sharing.
    /// </summary>
    public bool IdentitySealed { get; set; }
    
    /// <summary>
    /// Counterparty's ContactEmail revealed after Accept.
    /// Stage B (#42): ShareOutbound(ContactEmail) enabled by HasAcceptGrant.
    /// Only set when Accept grant is active; null otherwise.
    /// LoginEmail is NEVER included here.
    /// </summary>
    public string? CounterpartyContactEmail { get; set; }
    
    /// <summary>
    /// Counterparty's DisplayName if available.
    /// </summary>
    public string? CounterpartyDisplayName { get; set; }
    
    /// <summary>
    /// Whether ContactEmail was included in this response.
    /// True when Accept grant enabled ShareOutbound(ContactEmail).
    /// </summary>
    public bool IncludesContactEmail { get; set; }
}

namespace Dealoware.Application.Strategies.Dtos;

/// <summary>
/// Response representing a strategy.
/// Only returned to the owning participant.
/// Never exposed via Negotiation DTOs to counterparty.
/// </summary>
public class StrategyResponse
{
    /// <summary>
    /// Strategy identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owning participant's subject identifier.
    /// </summary>
    public string OwnerParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Strategy body content (only visible to owner/OwnAgent).
    /// FieldClass: StrategyBody - Counterparty/Stranger/Unauth Deny.
    /// </summary>
    public string? StrategyBody { get; set; }

    /// <summary>
    /// Whether StrategyBody is included (for ACL transparency).
    /// </summary>
    public bool IncludesStrategyBody { get; set; }

    /// <summary>
    /// When this strategy was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this strategy was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

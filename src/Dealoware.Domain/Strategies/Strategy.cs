namespace Dealoware.Domain.Strategies;

/// <summary>
/// Negotiation Strategy entity bound to owning Participant.
/// MVP Stage B minimal CRUD (P3 partial) - opaque/text StrategyBody.
/// 
/// Ownership: OwnerParticipantId == principal sub (query-plane enforced).
/// FieldClass ACL: StrategyBody - User R/W; OwnAgent R/W; Counterparty/Stranger/Unauth Deny.
/// 
/// OUT: Free-form condition engine (V1); Strategy sandbox A5 (V4);
///      Assistant runtime (Stage C/X1).
/// </summary>
public sealed class Strategy
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Owning Participant's subject identifier.
    /// Query-plane authorization: OwnerParticipantId == principal sub.
    /// </summary>
    public string OwnerParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// Optional human-readable name for this strategy.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// The strategy body content (opaque/text for MVP).
    /// FieldClass: StrategyBody - User R/W; OwnAgent R/W; Counterparty/Stranger Deny.
    /// 
    /// MVP delivers minimal CRUD only, not full free-form evaluation engine.
    /// Free-form conditions → V1; sandbox (A5) → V4.
    /// </summary>
    public string? StrategyBody { get; private set; }

    /// <summary>
    /// When this strategy was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When this strategy was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Whether this strategy is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    private Strategy() { }

    /// <summary>
    /// Creates a new Strategy for the specified owner.
    /// </summary>
    /// <param name="ownerParticipantId">The owning participant's sub.</param>
    /// <param name="name">Optional name for the strategy.</param>
    /// <param name="strategyBody">Optional strategy body content.</param>
    /// <returns>Tuple of (Strategy, Errors).</returns>
    public static (Strategy? Strategy, IList<string> Errors) Create(
        string ownerParticipantId,
        string? name = null,
        string? strategyBody = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ownerParticipantId))
        {
            errors.Add("OwnerParticipantId is required");
        }

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var now = DateTimeOffset.UtcNow;
        var strategy = new Strategy
        {
            Id = Guid.NewGuid(),
            OwnerParticipantId = ownerParticipantId,
            Name = name,
            StrategyBody = strategyBody,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        return (strategy, errors);
    }

    /// <summary>
    /// Updates the strategy name.
    /// </summary>
    public void UpdateName(string? name)
    {
        Name = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the strategy body (owner-only operation).
    /// </summary>
    public void UpdateStrategyBody(string? strategyBody)
    {
        StrategyBody = strategyBody;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deactivates this strategy.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

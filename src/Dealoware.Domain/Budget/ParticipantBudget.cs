namespace Dealoware.Domain.Budget;

/// <summary>
/// Per-Participant budget for MVP-metered Assistant/LLM usage.
/// Stage C #68: A8-minimum meters + hard cutoff (fail-closed).
/// 
/// Security enforcement:
/// 1. Scoped to Participant — each Participant has own budget counter
/// 2. Hard cutoff fail-closed — when exhausted, further invocations deny server-side
/// 3. Cross-tenant isolation — wrong principal cannot consume another's budget
/// 4. Metered path wall-bound (#67) — budget status not a leak/escalation channel
/// 
/// OUT: Mature metering/analytics (V3); billing/escrow; #66/#67/#69 impl.
/// </summary>
public sealed class ParticipantBudget
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The Participant this budget belongs to (matches Participant.Sub).
    /// Budget is strictly scoped to this Participant — cross-tenant access denied.
    /// </summary>
    public string ParticipantSub { get; private set; } = string.Empty;

    /// <summary>
    /// Budget limit in usage units.
    /// A8-minimum: minimum viable counters sufficient for cutoff.
    /// Mature analytics / cost UI → V3.
    /// </summary>
    public long LimitUnits { get; private set; }

    /// <summary>
    /// Current usage in units consumed.
    /// Incremented on each metered Assistant/LLM invocation.
    /// </summary>
    public long UsedUnits { get; private set; }

    /// <summary>
    /// When this budget was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When this budget was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private ParticipantBudget() { }

    /// <summary>
    /// Creates a new ParticipantBudget with the specified limit.
    /// Default limit is A8-minimum sufficient for MVP testing.
    /// </summary>
    public static ParticipantBudget Create(string participantSub, long limitUnits = 1000)
    {
        if (string.IsNullOrWhiteSpace(participantSub))
            throw new ArgumentException("Participant subject identifier required", nameof(participantSub));
        
        if (limitUnits < 0)
            throw new ArgumentException("Budget limit cannot be negative", nameof(limitUnits));

        var now = DateTimeOffset.UtcNow;
        return new ParticipantBudget
        {
            Id = Guid.NewGuid(),
            ParticipantSub = participantSub,
            LimitUnits = limitUnits,
            UsedUnits = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Remaining units available before cutoff.
    /// </summary>
    public long RemainingUnits => Math.Max(0, LimitUnits - UsedUnits);

    /// <summary>
    /// Whether this budget is exhausted (at or over limit).
    /// Hard cutoff: when true, further metered invocations DENY.
    /// </summary>
    public bool IsExhausted => UsedUnits >= LimitUnits;

    /// <summary>
    /// Attempts to consume units from the budget.
    /// Returns true if consumption succeeded (under budget).
    /// Returns false if budget is exhausted — hard cutoff applies.
    /// 
    /// Security: This is the server-side enforcement point.
    /// Soft-warn-only is REJECTED as sole control.
    /// </summary>
    public bool TryConsume(long units)
    {
        if (units < 0)
            throw new ArgumentException("Consumption units cannot be negative", nameof(units));

        if (IsExhausted)
            return false;

        if (UsedUnits + units > LimitUnits)
            return false;

        UsedUnits += units;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// Records usage without pre-check (for post-invocation metering).
    /// Updates usage counter and timestamp.
    /// </summary>
    public void RecordUsage(long units)
    {
        if (units < 0)
            throw new ArgumentException("Usage units cannot be negative", nameof(units));

        UsedUnits += units;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Resets the usage counter (for testing or admin reset).
    /// Does NOT change the limit.
    /// </summary>
    internal void ResetUsage()
    {
        UsedUnits = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the budget limit (for admin operations).
    /// </summary>
    internal void UpdateLimit(long newLimit)
    {
        if (newLimit < 0)
            throw new ArgumentException("Budget limit cannot be negative", nameof(newLimit));

        LimitUnits = newLimit;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

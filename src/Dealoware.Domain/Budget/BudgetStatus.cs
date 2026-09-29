namespace Dealoware.Domain.Budget;

/// <summary>
/// Budget status DTO for API responses.
/// Stage C #68: A8-minimum budget status display.
/// 
/// Security: This is the ONLY budget information exposed via API.
/// Must NOT contain:
/// - Private FieldClass values
/// - Other Participants' budget details
/// - Auth secrets or credentials
/// - Internal implementation details
/// 
/// Budget status must NOT be a privilege escalation or field-leak channel.
/// </summary>
public sealed class BudgetStatus
{
    /// <summary>
    /// Budget limit in usage units.
    /// </summary>
    public long LimitUnits { get; }

    /// <summary>
    /// Current usage in units consumed.
    /// </summary>
    public long UsedUnits { get; }

    /// <summary>
    /// Remaining units before cutoff.
    /// </summary>
    public long RemainingUnits { get; }

    /// <summary>
    /// Whether the budget is exhausted (hard cutoff applies).
    /// </summary>
    public bool IsExhausted { get; }

    /// <summary>
    /// Usage percentage (0-100+).
    /// </summary>
    public int UsagePercentage { get; }

    private BudgetStatus(long limitUnits, long usedUnits, long remainingUnits, bool isExhausted, int usagePercentage)
    {
        LimitUnits = limitUnits;
        UsedUnits = usedUnits;
        RemainingUnits = remainingUnits;
        IsExhausted = isExhausted;
        UsagePercentage = usagePercentage;
    }

    /// <summary>
    /// Creates a BudgetStatus from a ParticipantBudget entity.
    /// </summary>
    public static BudgetStatus FromBudget(ParticipantBudget budget)
    {
        var percentage = budget.LimitUnits > 0
            ? (int)Math.Min(100, (budget.UsedUnits * 100) / budget.LimitUnits)
            : 100;

        return new BudgetStatus(
            budget.LimitUnits,
            budget.UsedUnits,
            budget.RemainingUnits,
            budget.IsExhausted,
            percentage);
    }
}

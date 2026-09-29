namespace Dealoware.Application.Budget.Dtos;

/// <summary>
/// Response DTO for budget status endpoint.
/// Stage C #68: A8-minimum budget status display.
/// 
/// Security: This is the ONLY budget information exposed via API.
/// Must NOT contain private FieldClass values, other Participants' data,
/// or auth secrets. Budget status must NOT be a leak/escalation channel.
/// </summary>
public sealed class BudgetStatusResponse
{
    /// <summary>
    /// Budget limit in usage units.
    /// </summary>
    public long LimitUnits { get; set; }

    /// <summary>
    /// Current usage in units consumed.
    /// </summary>
    public long UsedUnits { get; set; }

    /// <summary>
    /// Remaining units before cutoff.
    /// </summary>
    public long RemainingUnits { get; set; }

    /// <summary>
    /// Whether the budget is exhausted (hard cutoff applies).
    /// </summary>
    public bool IsExhausted { get; set; }

    /// <summary>
    /// Usage percentage (0-100+).
    /// </summary>
    public int UsagePercentage { get; set; }
}

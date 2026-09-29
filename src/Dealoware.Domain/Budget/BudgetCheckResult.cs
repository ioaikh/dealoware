namespace Dealoware.Domain.Budget;

/// <summary>
/// Result of a budget check operation.
/// Stage C #68: Used for hard cutoff enforcement.
/// 
/// Security: Error messages and status must NOT leak:
/// - Private FieldClass values (LoginEmail, StrategyBody, etc.)
/// - Other Participants' budget details
/// - Auth secrets or credentials
/// </summary>
public sealed class BudgetCheckResult
{
    /// <summary>
    /// Whether the operation is allowed (budget available).
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// Remaining units after this check.
    /// Only populated for successful checks by the owning principal.
    /// </summary>
    public long? RemainingUnits { get; }

    /// <summary>
    /// Error code if denied.
    /// Uniform codes — no private data leakage.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// User-facing error message if denied.
    /// MUST NOT contain private fields, secrets, or other Participants' data.
    /// </summary>
    public string? ErrorMessage { get; }

    private BudgetCheckResult(bool isAllowed, long? remainingUnits, string? errorCode, string? errorMessage)
    {
        IsAllowed = isAllowed;
        RemainingUnits = remainingUnits;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Budget check passed — invocation allowed.
    /// </summary>
    public static BudgetCheckResult Allowed(long remainingUnits)
        => new(true, remainingUnits, null, null);

    /// <summary>
    /// Budget exhausted — hard cutoff; further invocations DENIED.
    /// Message is safe to return to client (no private data).
    /// </summary>
    public static BudgetCheckResult Exhausted()
        => new(false, 0, "BUDGET_EXHAUSTED", "Budget exhausted. Further metered invocations are denied.");

    /// <summary>
    /// No budget found for participant — fail-closed; create budget first.
    /// </summary>
    public static BudgetCheckResult NoBudget()
        => new(false, null, "NO_BUDGET", "No budget configured. Metered invocations are denied.");

    /// <summary>
    /// Unauthenticated request — 401; cannot consume any budget.
    /// </summary>
    public static BudgetCheckResult Unauthenticated()
        => new(false, null, "UNAUTHENTICATED", "Authentication required.");

    /// <summary>
    /// Cross-tenant / wrong principal — 403/404; cannot access or consume budget.
    /// Uniform deny — does not reveal whether budget exists for another participant.
    /// </summary>
    public static BudgetCheckResult AccessDenied()
        => new(false, null, "ACCESS_DENIED", "Access denied.");
}

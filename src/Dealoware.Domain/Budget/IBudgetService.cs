namespace Dealoware.Domain.Budget;

/// <summary>
/// Service for per-Participant budget operations.
/// Stage C #68: A8-minimum meters + hard cutoff (fail-closed).
/// 
/// Security enforcement points:
/// 1. CheckBudgetAsync — Verifies principal can invoke metered operations
/// 2. RecordUsageAsync — Records consumption after successful invocation
/// 3. GetBudgetStatusAsync — Returns budget status (owner-only, no cross-tenant leak)
/// 
/// Hard cutoff: When budget exhausted, further metered invocations DENY server-side.
/// Soft-warn-only is REJECTED as sole control.
/// 
/// Cross-tenant: Wrong principal cannot burn another Participant's budget.
/// Fail-closed on all meter/budget/cutoff paths.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Checks if the principal has budget available for a metered invocation.
    /// This is the PRE-CHECK before allowing Assistant/tool invocation.
    /// 
    /// Security:
    /// - Requires valid ownerSub (unauthenticated → Unauthenticated result)
    /// - Returns Exhausted if budget is at/over limit (hard cutoff)
    /// - Returns NoBudget if no budget exists (fail-closed)
    /// </summary>
    /// <param name="ownerSub">The authenticated principal's subject identifier</param>
    /// <param name="unitsToConsume">Units that will be consumed if allowed (default 1)</param>
    /// <returns>BudgetCheckResult indicating allow/deny</returns>
    Task<BudgetCheckResult> CheckBudgetAsync(string? ownerSub, long unitsToConsume = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records usage after a successful metered invocation.
    /// Called AFTER the invocation completes successfully.
    /// 
    /// Security: Only records for the authenticated principal's own budget.
    /// </summary>
    /// <param name="ownerSub">The authenticated principal's subject identifier</param>
    /// <param name="unitsConsumed">Units consumed by the invocation</param>
    Task RecordUsageAsync(string ownerSub, long unitsConsumed = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the budget status for the authenticated principal.
    /// 
    /// Security:
    /// - Requires valid ownerSub (unauthenticated → AccessDenied)
    /// - Returns status for the principal's OWN budget only
    /// - Cross-tenant access denied (uniform deny)
    /// - Budget status must NOT be a leak/escalation channel
    /// </summary>
    /// <param name="ownerSub">The authenticated principal's subject identifier</param>
    /// <returns>Budget status DTO (safe to return to client)</returns>
    Task<BudgetStatus?> GetBudgetStatusAsync(string? ownerSub, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a budget exists for the participant, creating one with default limit if not.
    /// Used for auto-provisioning on first registration or metered invocation.
    /// </summary>
    /// <param name="participantSub">The participant's subject identifier</param>
    /// <param name="defaultLimit">Default budget limit (A8-minimum)</param>
    Task EnsureBudgetExistsAsync(string participantSub, long defaultLimit = 1000, CancellationToken cancellationToken = default);
}

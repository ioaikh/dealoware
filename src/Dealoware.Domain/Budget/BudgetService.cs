namespace Dealoware.Domain.Budget;

/// <summary>
/// Budget service implementation for per-Participant meters + hard cutoff.
/// Stage C #68: A8-minimum meters + hard budgets (fail-closed).
/// 
/// Security enforcement:
/// 1. Per-Participant meters — counters scoped to Participant
/// 2. Hard cutoff fail-closed — budget exhausted → deny server-side
/// 3. Cross-tenant / unauth cannot burn budget
/// 4. Metered path wall-bound (#67) — budget status not leak/escalation channel
/// 5. Authn fail-closed on meter APIs
/// 
/// Primary consumer: #66 thin Assistant (cross-ref only).
/// Metered path remains behind #67 hard wall (cross-ref only).
/// 
/// OUT: Mature metering/analytics (V3); billing/escrow; #66/#67/#69 impl.
/// Soft audit/OTel: weave only if hooks on path (no 5th Story).
/// </summary>
public sealed class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _repository;

    public BudgetService(IBudgetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Checks if the principal has budget available for a metered invocation.
    /// This is the hard cutoff enforcement point — server-side deny.
    /// 
    /// Security:
    /// - Unauthenticated → fail-closed (cannot burn any budget)
    /// - Budget exhausted → hard cutoff (deny further invocations)
    /// - No budget → fail-closed (must have budget configured)
    /// </summary>
    public async Task<BudgetCheckResult> CheckBudgetAsync(
        string? ownerSub, 
        long unitsToConsume = 1, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerSub))
        {
            return BudgetCheckResult.Unauthenticated();
        }

        var budget = await _repository.GetByParticipantSubAsync(ownerSub, cancellationToken);
        
        if (budget is null)
        {
            return BudgetCheckResult.NoBudget();
        }

        if (budget.IsExhausted)
        {
            return BudgetCheckResult.Exhausted();
        }

        if (budget.RemainingUnits < unitsToConsume)
        {
            return BudgetCheckResult.Exhausted();
        }

        return BudgetCheckResult.Allowed(budget.RemainingUnits);
    }

    /// <summary>
    /// Records usage after a successful metered invocation.
    /// Only records for the authenticated principal's own budget.
    /// </summary>
    public async Task RecordUsageAsync(
        string ownerSub, 
        long unitsConsumed = 1, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerSub))
        {
            return;
        }

        var budget = await _repository.GetByParticipantSubAsync(ownerSub, cancellationToken);
        
        if (budget is null)
        {
            return;
        }

        budget.RecordUsage(unitsConsumed);
        _repository.Update(budget);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the budget status for the authenticated principal.
    /// Returns null if no budget exists or principal is invalid.
    /// 
    /// Security:
    /// - Returns ONLY the principal's own budget
    /// - Cross-tenant access returns null (uniform deny)
    /// - Budget status does not leak private fields
    /// </summary>
    public async Task<BudgetStatus?> GetBudgetStatusAsync(
        string? ownerSub, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerSub))
        {
            return null;
        }

        var budget = await _repository.GetByParticipantSubAsync(ownerSub, cancellationToken);
        
        if (budget is null)
        {
            return null;
        }

        return BudgetStatus.FromBudget(budget);
    }

    /// <summary>
    /// Ensures a budget exists for the participant.
    /// Creates one with default limit if not present.
    /// </summary>
    public async Task EnsureBudgetExistsAsync(
        string participantSub, 
        long defaultLimit = 1000, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(participantSub))
        {
            return;
        }

        await _repository.GetOrCreateAsync(participantSub, defaultLimit, cancellationToken);
    }
}

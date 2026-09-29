namespace Dealoware.Domain.Budget;

/// <summary>
/// Repository for ParticipantBudget persistence.
/// Stage C #68: A8-minimum per-Participant meters.
/// 
/// Security: All operations require valid participantSub.
/// Cross-tenant access is denied at the service layer.
/// </summary>
public interface IBudgetRepository
{
    /// <summary>
    /// Gets the budget for a specific participant.
    /// Returns null if no budget exists.
    /// </summary>
    Task<ParticipantBudget?> GetByParticipantSubAsync(string participantSub, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new budget.
    /// </summary>
    Task AddAsync(ParticipantBudget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing budget.
    /// </summary>
    void Update(ParticipantBudget budget);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a budget for a participant with default limit.
    /// Used for auto-provisioning on first metered invocation.
    /// </summary>
    Task<ParticipantBudget> GetOrCreateAsync(string participantSub, long defaultLimit = 1000, CancellationToken cancellationToken = default);
}

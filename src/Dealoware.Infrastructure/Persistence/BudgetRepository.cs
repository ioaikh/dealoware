using Dealoware.Domain.Budget;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// EF Core repository for ParticipantBudget persistence.
/// Stage C #68: A8-minimum per-Participant meters.
/// 
/// Security: Operations require valid participantSub.
/// Cross-tenant access denied at the service layer.
/// </summary>
public sealed class BudgetRepository : IBudgetRepository
{
    private readonly DealowareDbContext _context;

    public BudgetRepository(DealowareDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ParticipantBudget?> GetByParticipantSubAsync(
        string participantSub, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(participantSub))
            return null;

        return await _context.ParticipantBudgets
            .FirstOrDefaultAsync(b => b.ParticipantSub == participantSub, cancellationToken);
    }

    public async Task AddAsync(ParticipantBudget budget, CancellationToken cancellationToken = default)
    {
        await _context.ParticipantBudgets.AddAsync(budget, cancellationToken);
    }

    public void Update(ParticipantBudget budget)
    {
        _context.ParticipantBudgets.Update(budget);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ParticipantBudget> GetOrCreateAsync(
        string participantSub, 
        long defaultLimit = 1000, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(participantSub))
            throw new ArgumentException("Participant subject identifier required", nameof(participantSub));

        var existing = await GetByParticipantSubAsync(participantSub, cancellationToken);
        if (existing is not null)
            return existing;

        var budget = ParticipantBudget.Create(participantSub, defaultLimit);
        await AddAsync(budget, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        
        return budget;
    }
}

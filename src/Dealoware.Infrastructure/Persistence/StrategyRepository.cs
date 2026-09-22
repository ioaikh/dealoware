using Dealoware.Domain.Strategies;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Repository for Strategy persistence.
/// All queries enforce owner-scope at query plane (not fetch-then-filter).
/// </summary>
public class StrategyRepository : IStrategyRepository
{
    private readonly DealowareDbContext _context;

    public StrategyRepository(DealowareDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a strategy by ID only if the caller is the owner.
    /// Query-plane authorization: WHERE Id = @id AND OwnerParticipantId = @ownerParticipantId.
    /// </summary>
    public async Task<Strategy?> GetByIdForOwnerAsync(Guid id, string ownerParticipantId, CancellationToken cancellationToken = default)
    {
        return await _context.Strategies
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerParticipantId == ownerParticipantId && s.IsActive, cancellationToken);
    }

    /// <summary>
    /// Gets all strategies owned by the specified participant.
    /// Query-plane authorization: WHERE OwnerParticipantId = @ownerParticipantId.
    /// </summary>
    public async Task<IReadOnlyList<Strategy>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default)
    {
        var strategies = await _context.Strategies
            .Where(s => s.OwnerParticipantId == ownerParticipantId && s.IsActive)
            .ToListAsync(cancellationToken);

        return strategies.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public async Task AddAsync(Strategy strategy, CancellationToken cancellationToken = default)
    {
        await _context.Strategies.AddAsync(strategy, cancellationToken);
    }

    public void Update(Strategy strategy)
    {
        _context.Strategies.Update(strategy);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

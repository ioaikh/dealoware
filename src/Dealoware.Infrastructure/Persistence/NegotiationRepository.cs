using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <remarks>
/// The engine's party lookups. A soft-deleted negotiation (admin delete,
/// Spec §11) is not found, exactly like an id that does not exist, and the
/// soft-deleted offers of a negotiation are left out of its offers.
/// </remarks>
public class NegotiationRepository : INegotiationRepository
{
    private readonly DealowareDbContext _context;

    public NegotiationRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<Negotiation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Negotiations
            .Include(n => n.Offers.Where(o => o.DeletedAt == null))
            .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null, cancellationToken);
    }

    public async Task<Negotiation?> GetByIdForPartyAsync(Guid id, string participantId, CancellationToken cancellationToken = default)
    {
        return await _context.Negotiations
            .Include(n => n.Offers.Where(o => o.DeletedAt == null))
            .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null &&
                (n.PartyAParticipantId == participantId || n.PartyBParticipantId == participantId), 
                cancellationToken);
    }

    public async Task<IReadOnlyList<Negotiation>> GetByParticipantAsync(string participantId, CancellationToken cancellationToken = default)
    {
        var negotiations = await _context.Negotiations
            .Include(n => n.Offers.Where(o => o.DeletedAt == null))
            .Where(n => n.DeletedAt == null
                        && (n.PartyAParticipantId == participantId || n.PartyBParticipantId == participantId))
            .ToListAsync(cancellationToken);

        return negotiations.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task AddAsync(Negotiation negotiation, CancellationToken cancellationToken = default)
    {
        await _context.Negotiations.AddAsync(negotiation, cancellationToken);
    }

    public void Update(Negotiation negotiation)
    {
        _context.Negotiations.Update(negotiation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

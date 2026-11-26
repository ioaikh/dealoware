using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <remarks>
/// The engine's party lookups. A soft-deleted offer, or an offer of a
/// soft-deleted negotiation (admin delete, Spec §11), is not found, exactly
/// like an id that does not exist, and is left out of every list.
/// </remarks>
public class OfferRepository : IOfferRepository
{
    private readonly DealowareDbContext _context;

    public OfferRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
    }

    public async Task<Offer?> GetByIdForPartyAsync(Guid id, string participantId, CancellationToken cancellationToken = default)
    {
        return await (from o in _context.Offers
                      join n in _context.Negotiations on o.NegotiationId equals n.Id
                      where o.Id == id && o.DeletedAt == null && n.DeletedAt == null &&
                            (n.PartyAParticipantId == participantId || n.PartyBParticipantId == participantId)
                      select o)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetByParticipantAsync(string participantId, CancellationToken cancellationToken = default)
    {
        var offers = await (from o in _context.Offers
                            join n in _context.Negotiations on o.NegotiationId equals n.Id
                            where o.DeletedAt == null && n.DeletedAt == null
                                  && (n.PartyAParticipantId == participantId || n.PartyBParticipantId == participantId)
                            select o)
            .ToListAsync(cancellationToken);
        
        return offers.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<IReadOnlyList<Offer>> GetByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.NegotiationId == negotiationId && o.DeletedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetOpenByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.NegotiationId == negotiationId && o.Status == OfferStatus.Open && o.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await _context.Offers.AddAsync(offer, cancellationToken);
    }

    public void Update(Offer offer)
    {
        _context.Offers.Update(offer);
    }

    public void UpdateRange(IEnumerable<Offer> offers)
    {
        _context.Offers.UpdateRange(offers);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

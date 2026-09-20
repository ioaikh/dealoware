using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

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
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.NegotiationId == negotiationId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetOpenByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.NegotiationId == negotiationId && o.Status == OfferStatus.Open)
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

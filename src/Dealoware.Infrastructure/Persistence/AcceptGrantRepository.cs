using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for AcceptGrant persistence.
/// Stage B (#42): Manages Accept grants for contact-on-accept.
/// </summary>
public class AcceptGrantRepository : IAcceptGrantRepository
{
    private readonly DealowareDbContext _context;

    public AcceptGrantRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<AcceptGrant?> GetByOfferAndGranteeAsync(Guid offerId, string granteeSub, CancellationToken cancellationToken = default)
    {
        return await _context.AcceptGrants
            .FirstOrDefaultAsync(g => g.OfferId == offerId && g.GranteeSub == granteeSub, cancellationToken);
    }

    public async Task<AcceptGrant?> GetByNegotiationAndGranteeAsync(Guid negotiationId, string granteeSub, CancellationToken cancellationToken = default)
    {
        return await _context.AcceptGrants
            .FirstOrDefaultAsync(g => g.NegotiationId == negotiationId && g.GranteeSub == granteeSub, cancellationToken);
    }

    public async Task<bool> HasGrantForNegotiationAsync(Guid negotiationId, string granteeSub, CancellationToken cancellationToken = default)
    {
        return await _context.AcceptGrants
            .AnyAsync(g => g.NegotiationId == negotiationId && g.GranteeSub == granteeSub, cancellationToken);
    }

    public async Task AddAsync(AcceptGrant grant, CancellationToken cancellationToken = default)
    {
        await _context.AcceptGrants.AddAsync(grant, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<AcceptGrant> grants, CancellationToken cancellationToken = default)
    {
        await _context.AcceptGrants.AddRangeAsync(grants, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

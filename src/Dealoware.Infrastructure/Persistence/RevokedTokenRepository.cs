using Dealoware.Domain.Participants;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

public class RevokedTokenRepository : IRevokedTokenRepository
{
    private readonly DealowareDbContext _context;

    public RevokedTokenRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return await _context.RevokedTokens
            .AnyAsync(t => t.Jti == jti, cancellationToken);
    }

    public async Task AddAsync(RevokedToken revokedToken, CancellationToken cancellationToken = default)
    {
        await _context.RevokedTokens.AddAsync(revokedToken, cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = await _context.RevokedTokens
            .Where(t => t.ExpiresAt < now)
            .ToListAsync(cancellationToken);
        
        _context.RevokedTokens.RemoveRange(expired);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

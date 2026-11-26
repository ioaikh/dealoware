using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminBootstrapTokenRepository : IAdminBootstrapTokenRepository
{
    private readonly DealowareDbContext _context;

    public AdminBootstrapTokenRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminBootstrapToken token, CancellationToken cancellationToken = default)
    {
        await _context.AdminBootstrapTokens.AddAsync(token, cancellationToken);
    }

    public async Task<AdminBootstrapToken?> FindUsableByHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.AdminBootstrapTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (row is null || !row.IsUsable(now))
        {
            return null;
        }

        return row;
    }

    public async Task<bool> TryConsumeAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // One conditional write: a parallel consume of the same link cannot
        // also match ConsumedAt IS NULL, so exactly one caller sees one row.
        var updated = await _context.AdminBootstrapTokens
            .Where(t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.ConsumedAt, now),
                cancellationToken);
        return updated == 1;
    }

    public async Task InvalidateUnusedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await _context.AdminBootstrapTokens
            .Where(t => t.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.ConsumedAt, now),
                cancellationToken);
    }
}

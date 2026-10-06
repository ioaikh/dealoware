using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminPasswordResetTokenRepository : IAdminPasswordResetTokenRepository
{
    private readonly DealowareDbContext _context;

    public AdminPasswordResetTokenRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminPasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.AdminPasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminPasswordResetToken?> FindUsableByHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.AdminPasswordResetTokens
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
        var updated = await _context.AdminPasswordResetTokens
            .Where(t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.ConsumedAt, now),
                cancellationToken);
        return updated == 1;
    }

    public async Task InvalidateUnusedForEmailAsync(
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await _context.AdminPasswordResetTokens
            .Where(t => t.Email == email && t.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.ConsumedAt, now),
                cancellationToken);
    }
}

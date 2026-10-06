using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminDeleteConfirmTokenRepository : IAdminDeleteConfirmTokenRepository
{
    private readonly DealowareDbContext _context;

    public AdminDeleteConfirmTokenRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminDeleteConfirmToken token, CancellationToken cancellationToken = default)
    {
        await _context.AdminDeleteConfirmTokens.AddAsync(token, cancellationToken);
    }

    public Task<AdminDeleteConfirmToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.AdminDeleteConfirmTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

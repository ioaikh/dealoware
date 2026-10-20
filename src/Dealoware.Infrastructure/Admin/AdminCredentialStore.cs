using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminCredentialStore : IAdminCredentialStore
{
    private readonly DealowareDbContext _context;

    public AdminCredentialStore(DealowareDbContext context)
    {
        _context = context;
    }

    public Task<AdminCredential?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.AdminCredentials
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task AddAsync(AdminCredential credential, CancellationToken cancellationToken = default)
    {
        await _context.AdminCredentials.AddAsync(credential, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

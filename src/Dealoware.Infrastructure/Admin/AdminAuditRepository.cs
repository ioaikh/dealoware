using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// EF Core implementation of IAdminAuditRepository.
/// Append-only: provides only Add, no Update or Delete.
/// </summary>
public sealed class AdminAuditRepository : IAdminAuditRepository
{
    private readonly DealowareDbContext _context;

    public AdminAuditRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.AdminAuditLog.AddAsync(entry, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

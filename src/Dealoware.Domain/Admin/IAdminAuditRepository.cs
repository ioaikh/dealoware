namespace Dealoware.Domain.Admin;

/// <summary>
/// Repository for admin audit entries. Append-only - no update or delete.
/// </summary>
public interface IAdminAuditRepository
{
    Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Repository for admin audit entries. Append-only - no update or delete.
/// </summary>
public interface IAdminAuditRepository
{
    Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Paged read: filter, sort, Skip/Take and total count all run in SQL.
    /// </summary>
    Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListPageAsync(
        AdminAuditListCriteria criteria,
        CancellationToken cancellationToken = default);
}

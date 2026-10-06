namespace Dealoware.Domain.Admin;

/// <summary>
/// Repository for admin session persistence.
/// </summary>
public interface IAdminSessionRepository
{
    Task<AdminSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AdminSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdminSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

namespace Dealoware.Domain.Admin;

public interface IAdminCredentialStore
{
    Task<AdminCredential?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(AdminCredential credential, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

namespace Dealoware.Domain.Admin;

public interface IAdminDeleteConfirmTokenRepository
{
    Task AddAsync(AdminDeleteConfirmToken token, CancellationToken cancellationToken = default);

    Task<AdminDeleteConfirmToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

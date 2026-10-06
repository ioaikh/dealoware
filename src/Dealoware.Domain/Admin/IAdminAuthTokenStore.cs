namespace Dealoware.Domain.Admin;

public interface IAdminAuthTokenStore
{
    Task<AdminAuthToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(AdminAuthToken token, CancellationToken ct);
    Task CancelUnusedAsync(string kind, string email, DateTimeOffset now, CancellationToken ct);

    Task ConsumeOutstandingPendingAsync(string email, DateTimeOffset now, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

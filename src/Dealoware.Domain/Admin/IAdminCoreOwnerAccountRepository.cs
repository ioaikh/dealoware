namespace Dealoware.Domain.Admin;

public interface IAdminCoreOwnerAccountRepository
{
    Task<AdminCoreOwnerAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(AdminCoreOwnerAccount account, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminRecoveryCode>> GetRecoveryCodesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddRecoveryCodesAsync(
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default);

    Task<AdminPendingAuth?> GetPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddPendingAsync(AdminPendingAuth pending, CancellationToken cancellationToken = default);

    Task ConsumeOutstandingPendingAsync(string email, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

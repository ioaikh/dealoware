using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminCoreOwnerAccountRepository : IAdminCoreOwnerAccountRepository
{
    private readonly DealowareDbContext _context;

    public AdminCoreOwnerAccountRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public Task<AdminCoreOwnerAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.AdminCoreOwnerAccounts
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);
    }

    public async Task AddAsync(AdminCoreOwnerAccount account, CancellationToken cancellationToken = default)
    {
        await _context.AdminCoreOwnerAccounts.AddAsync(account, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminRecoveryCode>> GetRecoveryCodesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AdminRecoveryCodes
            .Where(c => c.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRecoveryCodesAsync(
        IReadOnlyList<AdminRecoveryCode> codes,
        CancellationToken cancellationToken = default)
    {
        await _context.AdminRecoveryCodes.AddRangeAsync(codes, cancellationToken);
    }

    public Task<AdminPendingAuth?> GetPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.AdminPendingAuths
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddPendingAsync(AdminPendingAuth pending, CancellationToken cancellationToken = default)
    {
        await _context.AdminPendingAuths.AddAsync(pending, cancellationToken);
    }

    public async Task ConsumeOutstandingPendingAsync(
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var outstanding = await _context.AdminPendingAuths
            .Where(p => p.Email == email && p.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var pending in outstanding)
        {
            pending.Consume(now);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

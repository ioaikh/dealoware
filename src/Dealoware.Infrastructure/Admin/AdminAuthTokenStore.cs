using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminAuthTokenStore : IAdminAuthTokenStore
{
    private readonly DealowareDbContext _db;

    public AdminAuthTokenStore(DealowareDbContext db)
    {
        _db = db;
    }

    public Task<AdminAuthToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
    {
        return _db.AdminAuthTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public async Task AddAsync(AdminAuthToken token, CancellationToken ct)
    {
        await _db.AdminAuthTokens.AddAsync(token, ct).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

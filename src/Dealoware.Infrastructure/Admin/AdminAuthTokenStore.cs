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

    public async Task CancelUnusedAsync(string kind, string email, DateTimeOffset now, CancellationToken ct)
    {
        await _db.AdminAuthTokens
            .Where(t => t.Kind == kind && t.Email == email && t.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct)
            .ConfigureAwait(false);
    }

    public async Task ConsumeOutstandingPendingAsync(string email, DateTimeOffset now, CancellationToken ct)
    {
        await _db.AdminAuthTokens
            .Where(t => t.Kind == AdminAuthToken.KindPending && t.Email == email && t.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> TryConsumeAsync(string tokenHash, DateTimeOffset now, CancellationToken ct)
    {
        var rows = await _db.AdminAuthTokens
            .Where(t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct)
            .ConfigureAwait(false);
        return rows == 1;
    }

    public async Task<bool> TryIncrementPendingAttemptAsync(
        Guid id,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken ct)
    {
        var rows = await _db.AdminAuthTokens
            .Where(t =>
                t.Id == id
                && t.ConsumedAt == null
                && t.ExpiresAt > now
                && t.AttemptCount < maxAttempts)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.AttemptCount, t => t.AttemptCount + 1),
                ct)
            .ConfigureAwait(false);
        return rows == 1;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

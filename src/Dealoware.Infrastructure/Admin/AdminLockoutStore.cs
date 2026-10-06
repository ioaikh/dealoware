using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminLockoutStore : IAdminLockoutStore
{
    private readonly DealowareDbContext _db;

    public AdminLockoutStore(DealowareDbContext db)
    {
        _db = db;
    }

    public async Task<int> CountFailuresAsync(
        string scope,
        string subjectKey,
        DateTimeOffset windowStartExclusive,
        CancellationToken ct)
    {
        // DateTimeOffset comparisons are not translated: the global UTC converter
        // uses ToUniversalTime(). Filter by key in SQL, apply the exclusive
        // 15-minute window in memory (event at exactly windowStart is outside).
        var rows = await _db.AdminAuthFailureEvents
            .Where(e => e.Scope == scope && e.SubjectKey == subjectKey)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Count(e => e.OccurredAt > windowStartExclusive);
    }

    public async Task AddFailureAsync(AdminAuthFailureEvent failure, CancellationToken ct)
    {
        await _db.AdminAuthFailureEvents.AddAsync(failure, ct).ConfigureAwait(false);
    }

    public async Task<AdminAuthLockout?> GetActiveLockoutAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var rows = await _db.AdminAuthLockouts
            .Where(l => l.Scope == scope && l.SubjectKey == subjectKey)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows
            .Where(l => now < l.ExpiresAt)
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefault();
    }

    public async Task AddLockoutAsync(AdminAuthLockout lockout, CancellationToken ct)
    {
        await _db.AdminAuthLockouts.AddAsync(lockout, ct).ConfigureAwait(false);
    }

    public async Task ClearAccountFailuresAsync(string email, CancellationToken ct)
    {
        var rows = await _db.AdminAuthFailureEvents
            .Where(e => e.Scope == AdminAuthScopes.Account && e.SubjectKey == email)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        _db.AdminAuthFailureEvents.RemoveRange(rows);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

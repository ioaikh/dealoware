using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminLockoutStore : IAdminLockoutStore
{
    private const int MaxAttempts = 8;
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
        var rows = await _db.AdminAuthFailureEvents.AsNoTracking()
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
        var row = await _db.AdminAuthLockouts.AsNoTracking()
            .SingleOrDefaultAsync(l => l.Scope == scope && l.SubjectKey == subjectKey, ct)
            .ConfigureAwait(false);
        return row is not null && row.IsActive(now) ? row : null;
    }

    public async Task AddLockoutAsync(AdminAuthLockout lockout, CancellationToken ct)
    {
        await _db.AdminAuthLockouts.AddAsync(lockout, ct).ConfigureAwait(false);
    }

    public async Task<FailureIncrementResult> TryIncrementFailureAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return await IncrementOnceAsync(scope, subjectKey, now, window, limit, lockDuration, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (IsConcurrencyConflict(ex) && attempt < MaxAttempts - 1)
            {
                last = ex;
                _db.ChangeTracker.Clear();
                await Task.Delay(8 * (attempt + 1), ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            "Could not increment lockout counter after concurrency retries.", last);
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

    private async Task<FailureIncrementResult> IncrementOnceAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct)
    {
        _db.ChangeTracker.Clear();
        var gate = await GetOrCreateGateAsync(scope, subjectKey, ct).ConfigureAwait(false);
        if (gate.IsActive(now))
        {
            var lockedCount = await CountFailuresAsync(scope, subjectKey, now - window, ct)
                .ConfigureAwait(false);
            return new FailureIncrementResult(false, false, true, lockedCount);
        }

        var prior = await CountFailuresAsync(scope, subjectKey, now - window, ct).ConfigureAwait(false);
        var count = prior + 1;
        await _db.AdminAuthFailureEvents
            .AddAsync(AdminAuthFailureEvent.Create(scope, subjectKey, now), ct)
            .ConfigureAwait(false);

        var created = false;
        if (count >= limit)
        {
            gate.Activate(now, lockDuration);
            created = true;
        }

        // Conditional UPDATE: SaveChanges sends WHERE Version = original.
        gate.BumpVersion();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new FailureIncrementResult(true, created, false, count);
    }

    private async Task<AdminAuthLockout> GetOrCreateGateAsync(
        string scope,
        string subjectKey,
        CancellationToken ct)
    {
        var existing = await _db.AdminAuthLockouts
            .SingleOrDefaultAsync(l => l.Scope == scope && l.SubjectKey == subjectKey, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var gate = AdminAuthLockout.CreateGate(scope, subjectKey);
        await _db.AdminAuthLockouts.AddAsync(gate, ct).ConfigureAwait(false);
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return gate;
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            _db.ChangeTracker.Clear();
            return await _db.AdminAuthLockouts
                .SingleAsync(l => l.Scope == scope && l.SubjectKey == subjectKey, ct)
                .ConfigureAwait(false);
        }
    }

    private static bool IsConcurrencyConflict(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException)
        {
            return true;
        }

        for (var current = ex; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique", StringComparison.Ordinal)
                || message.Contains("constraint", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("Scope", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

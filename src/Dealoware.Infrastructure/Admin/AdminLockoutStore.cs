using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminLockoutStore : IAdminLockoutStore
{
    private const int MaxAttempts = 48;
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
            catch (Exception ex) when (IsTransientLockoutConflict(ex) && attempt < MaxAttempts - 1)
            {
                last = ex;
                await ResetAfterConflictAsync(ct).ConfigureAwait(false);
                await Task.Delay(BackoffMs(attempt), ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            "Could not increment lockout counter after concurrency retries.", last);
    }

    public Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct)
        => _db.AdminAuthFailureEvents
            .Where(e => e.Id == reservationId)
            .ExecuteDeleteAsync(ct);

    public async Task<bool> TryActivateLockAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        TimeSpan lockDuration,
        CancellationToken ct)
    {
        return await MutateGateAsync(
            scope,
            subjectKey,
            now,
            window,
            limit,
            ct,
            (gate, count) =>
            {
                if (gate.IsActive(now) || count < limit)
                {
                    return false;
                }

                gate.Activate(now, lockDuration);
                gate.BumpVersion();
                return true;
            }).ConfigureAwait(false);
    }

    public async Task<bool> TryReleaseLockIfBelowLimitAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        CancellationToken ct)
    {
        return await MutateGateAsync(
            scope,
            subjectKey,
            now,
            window,
            limit,
            ct,
            (gate, count) =>
            {
                if (!gate.IsActive(now) || count >= limit)
                {
                    return false;
                }

                gate.Deactivate();
                gate.BumpVersion();
                return true;
            }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListAccountFailureIdsAsync(string email, CancellationToken ct)
    {
        return await _db.AdminAuthFailureEvents.AsNoTracking()
            .Where(e => e.Scope == AdminAuthScopes.Account && e.SubjectKey == email)
            .Select(e => e.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteAccountFailuresByIdsAsync(
        string email,
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        return await _db.AdminAuthFailureEvents
            .Where(e => e.Scope == AdminAuthScopes.Account
                        && e.SubjectKey == email
                        && ids.Contains(e.Id))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
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
        _ = lockDuration;
        _db.ChangeTracker.Clear();
        await EnsureGateExistsAsync(scope, subjectKey, ct).ConfigureAwait(false);

        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var gate = await LoadGateForUpdateAsync(scope, subjectKey, ct).ConfigureAwait(false);
        var prior = await CountFailuresAsync(scope, subjectKey, now - window, ct).ConfigureAwait(false);
        if (gate.IsActive(now) || prior >= limit)
        {
            // Refuse only. ConfirmFailureAsync activates the lock so on-lock
            // session revoke, pending consume, and the lock-start audit run.
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new FailureIncrementResult(false, false, true, prior, null);
        }

        var failure = AdminAuthFailureEvent.Create(scope, subjectKey, now);
        await _db.AdminAuthFailureEvents.AddAsync(failure, ct).ConfigureAwait(false);
        gate.BumpVersion();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return new FailureIncrementResult(true, false, false, prior + 1, failure.Id);
    }

    private async Task<bool> MutateGateAsync(
        string scope,
        string subjectKey,
        DateTimeOffset now,
        TimeSpan window,
        int limit,
        CancellationToken ct,
        Func<AdminAuthLockout, int, bool> apply)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                _db.ChangeTracker.Clear();
                await EnsureGateExistsAsync(scope, subjectKey, ct).ConfigureAwait(false);
                await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
                var gate = await LoadGateForUpdateAsync(scope, subjectKey, ct).ConfigureAwait(false);
                var count = await CountFailuresAsync(scope, subjectKey, now - window, ct)
                    .ConfigureAwait(false);
                if (!apply(gate, count))
                {
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    return false;
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (IsTransientLockoutConflict(ex) && attempt < MaxAttempts - 1)
            {
                last = ex;
                await ResetAfterConflictAsync(ct).ConfigureAwait(false);
                await Task.Delay(BackoffMs(attempt), ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            "Could not update lockout gate after concurrency retries.", last);
    }

    private async Task EnsureGateExistsAsync(string scope, string subjectKey, CancellationToken ct)
    {
        if (_db.Database.IsNpgsql())
        {
            var id = Guid.NewGuid();
            var epoch = DateTimeOffset.UnixEpoch;
            var version = 0;
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""AdminAuthLockouts"" (""Id"", ""Scope"", ""SubjectKey"", ""StartedAt"", ""ExpiresAt"", ""Version"")
                VALUES ({id}, {scope}, {subjectKey}, {epoch}, {epoch}, {version})
                ON CONFLICT (""Scope"", ""SubjectKey"") DO NOTHING")
                .ConfigureAwait(false);
            return;
        }

        var existing = await _db.AdminAuthLockouts.AsNoTracking()
            .SingleOrDefaultAsync(l => l.Scope == scope && l.SubjectKey == subjectKey, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        await _db.AdminAuthLockouts.AddAsync(AdminAuthLockout.CreateGate(scope, subjectKey), ct)
            .ConfigureAwait(false);
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsTransientLockoutConflict(ex))
        {
            _db.ChangeTracker.Clear();
        }
    }

    private async Task<AdminAuthLockout> LoadGateForUpdateAsync(
        string scope,
        string subjectKey,
        CancellationToken ct)
    {
        if (_db.Database.IsNpgsql())
        {
            // ExecuteSql keeps FOR UPDATE on this connection. Do not compose
            // FromSql + Single — EF wraps that as a subquery and the lock is lost.
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM ""AdminAuthLockouts""
                WHERE ""Scope"" = {scope} AND ""SubjectKey"" = {subjectKey}
                FOR UPDATE")
                .ConfigureAwait(false);
        }

        var gate = await _db.AdminAuthLockouts
            .SingleOrDefaultAsync(l => l.Scope == scope && l.SubjectKey == subjectKey, ct)
            .ConfigureAwait(false);
        if (gate is null)
        {
            throw new DbUpdateConcurrencyException(
                "Lockout gate row was not visible after insert.");
        }

        return gate;
    }

    private async Task ResetAfterConflictAsync(CancellationToken ct)
    {
        _db.ChangeTracker.Clear();
        if (_db.Database.CurrentTransaction is not null)
        {
            await _db.Database.RollbackTransactionAsync(ct).ConfigureAwait(false);
        }
    }

    private static int BackoffMs(int attempt) => Math.Min(80, 4 + (attempt * 4));

    private static bool IsTransientLockoutConflict(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException)
        {
            return true;
        }

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg
                && pg.SqlState is "23505" or "40001" or "40P01")
            {
                return true;
            }

            var message = current.Message;
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique", StringComparison.Ordinal)
                || message.Contains("constraint", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("Scope", StringComparison.Ordinal)
                || message.Contains("not visible after insert", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

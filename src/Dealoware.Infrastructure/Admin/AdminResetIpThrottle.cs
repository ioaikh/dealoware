using System.Data;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminResetIpThrottle : IAdminResetIpThrottle
{
    private readonly DealowareDbContext _context;

    public AdminResetIpThrottle(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryReserveAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            DetachTracked();
            await using var tx = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                var reserved = await TryReserveOnceAsync(ipHmac, now, cancellationToken);
                if (reserved is null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    continue;
                }

                await tx.CommitAsync(cancellationToken);
                return reserved.Value;
            }
            catch (DbUpdateException)
            {
                await TryRollbackAsync(tx, cancellationToken);
            }
            catch (SqliteException)
            {
                await TryRollbackAsync(tx, cancellationToken);
            }
        }

        return false;
    }

    private async Task<bool?> TryReserveOnceAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await _context.AdminResetIpCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IpHmac == ipHmac, cancellationToken);

        if (row is null)
        {
            _context.AdminResetIpCounters.Add(AdminResetIpCounter.Start(ipHmac, now));
            _context.AdminResetIpEvents.Add(AdminResetIpEvent.Create(ipHmac, now));
            await _context.SaveChangesAsync(cancellationToken);
            DetachTracked();
            return true;
        }

        if (row.IsLocked(now))
            return false;

        var cutoff = now - AdminResetIpCounter.Window;
        await _context.AdminResetIpEvents
            .Where(e => e.IpHmac == ipHmac && e.OccurredAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var inWindow = await _context.AdminResetIpEvents
            .CountAsync(
                e => e.IpHmac == ipHmac && e.OccurredAt > cutoff,
                cancellationToken);

        if (inWindow >= AdminResetIpCounter.Threshold)
        {
            var lockUntil = now.Add(AdminResetIpCounter.LockDuration);
            var locked = await Matching(row)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.FailureCount, inWindow)
                        .SetProperty(c => c.LockedUntil, lockUntil),
                    cancellationToken);
            return locked == 1 ? false : null;
        }

        var next = inWindow + 1;
        var nextWindow = row.WindowStartedAt > cutoff ? row.WindowStartedAt : now;
        var nextLock = next >= AdminResetIpCounter.Threshold
            ? now.Add(AdminResetIpCounter.LockDuration)
            : (DateTimeOffset?)null;

        var updated = await Matching(row)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.FailureCount, next)
                    .SetProperty(c => c.WindowStartedAt, nextWindow)
                    .SetProperty(c => c.LockedUntil, nextLock),
                cancellationToken);
        if (updated != 1)
            return null;

        _context.AdminResetIpEvents.Add(AdminResetIpEvent.Create(ipHmac, now));
        await _context.SaveChangesAsync(cancellationToken);
        DetachTracked();
        return true;
    }

    private IQueryable<AdminResetIpCounter> Matching(AdminResetIpCounter row)
        => _context.AdminResetIpCounters.Where(c =>
            c.IpHmac == row.IpHmac
            && c.FailureCount == row.FailureCount
            && c.WindowStartedAt == row.WindowStartedAt
            && c.LockedUntil == row.LockedUntil);

    private void DetachTracked()
    {
        foreach (var entry in _context.ChangeTracker.Entries()
                     .Where(e => e.Entity is AdminResetIpCounter or AdminResetIpEvent)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static async Task TryRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx,
        CancellationToken cancellationToken)
    {
        try
        {
            await tx.RollbackAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Already rolled back with the failed command.
        }
    }
}

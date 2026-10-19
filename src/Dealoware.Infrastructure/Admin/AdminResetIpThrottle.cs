using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
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
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var row = await _context.AdminResetIpCounters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IpHmac == ipHmac, cancellationToken);

            if (row is null)
            {
                try
                {
                    _context.AdminResetIpCounters.Add(AdminResetIpCounter.Start(ipHmac, now));
                    await _context.SaveChangesAsync(cancellationToken);
                    DetachTracked();
                    return true;
                }
                catch (DbUpdateException)
                {
                    DetachTracked();
                    continue;
                }
            }

            // Count >= Threshold is locked only while LockedUntil is still in the future.
            if (row.IsLocked(now))
            {
                return false;
            }

            if (!row.WindowOpen(now))
            {
                var reset = await Matching(row)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(c => c.FailureCount, 1)
                            .SetProperty(c => c.WindowStartedAt, now)
                            .SetProperty(c => c.LockedUntil, (DateTimeOffset?)null),
                        cancellationToken);
                if (reset == 1)
                {
                    return true;
                }

                continue;
            }

            var next = row.FailureCount + 1;
            var lockUntil = next >= AdminResetIpCounter.Threshold
                ? now.Add(AdminResetIpCounter.LockDuration)
                : row.LockedUntil;
            var updated = await Matching(row)
                .Where(c => c.FailureCount < AdminResetIpCounter.Threshold)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.FailureCount, next)
                        .SetProperty(c => c.LockedUntil, lockUntil),
                    cancellationToken);
            if (updated == 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// R5 compare-and-set: a delayed reset cannot clear a newer lock, and a
    /// burst against an expired row cannot pass the threshold.
    /// </summary>
    private IQueryable<AdminResetIpCounter> Matching(AdminResetIpCounter row)
        => _context.AdminResetIpCounters.Where(c =>
            c.IpHmac == row.IpHmac
            && c.FailureCount == row.FailureCount
            && c.WindowStartedAt == row.WindowStartedAt
            && c.LockedUntil == row.LockedUntil);

    private void DetachTracked()
    {
        foreach (var entry in _context.ChangeTracker.Entries<AdminResetIpCounter>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}

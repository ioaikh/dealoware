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

    public async Task<bool> IsThrottledAsync(
        string ipHmac,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await _context.AdminResetIpCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IpHmac == ipHmac, cancellationToken);
        return row is not null && row.IsLocked(now);
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
                catch (Exception)
                {
                    DetachTracked();
                    continue;
                }
            }

            if (row.IsLocked(now) || row.FailureCount >= AdminResetIpCounter.Threshold)
            {
                return false;
            }

            try
            {
                if (!row.WindowOpen(now))
                {
                    var reset = await _context.AdminResetIpCounters
                        .Where(c => c.IpHmac == ipHmac
                                    && c.FailureCount == row.FailureCount
                                    && c.FailureCount < AdminResetIpCounter.Threshold)
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
                var updated = await _context.AdminResetIpCounters
                    .Where(c => c.IpHmac == ipHmac
                                && c.FailureCount == row.FailureCount
                                && c.FailureCount < AdminResetIpCounter.Threshold)
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
            catch (Exception)
            {
                continue;
            }
        }

        return false;
    }

    private void DetachTracked()
    {
        foreach (var entry in _context.ChangeTracker.Entries<AdminResetIpCounter>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}

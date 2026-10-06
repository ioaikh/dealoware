namespace Dealoware.Domain.Admin;

/// <summary>
/// Reset/bootstrap IP throttle (Spec §8.6): 20 events in 15 minutes → 30 minute lock.
/// SC-6: persisted in Postgres; increments are atomic conditional updates.
/// </summary>
public sealed class AdminResetIpCounter
{
    public const int Threshold = 20;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(30);

    public string IpHmac { get; private set; } = string.Empty;

    public DateTimeOffset WindowStartedAt { get; private set; }

    public int FailureCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    private AdminResetIpCounter() { }

    public static AdminResetIpCounter Start(string ipHmac, DateTimeOffset now)
        => new()
        {
            IpHmac = ipHmac,
            WindowStartedAt = now,
            FailureCount = 1,
            LockedUntil = null
        };

    public bool IsLocked(DateTimeOffset now)
        => LockedUntil is { } until && until > now;

    public bool WindowOpen(DateTimeOffset now)
        => now - WindowStartedAt < Window;
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Per-IP bootstrap password-set failure counter. Spec 20/15min → 30min.
/// The key is HMAC-SHA256 of the trusted client address after ForwardedHeaders
/// (right-most ALB hop). Never store a raw IP. Never keep this counter in process memory.
/// </summary>
public sealed class AdminBootstrapIpThrottle
{
    public const int AttemptLimit = 20;
    public const int WindowMinutes = 15;
    public const int LockMinutes = 30;

    public Guid Id { get; private set; }

    public string IpKey { get; private set; } = string.Empty;

    public DateTimeOffset WindowStartedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    private AdminBootstrapIpThrottle()
    {
    }

    public static AdminBootstrapIpThrottle StartWindow(string ipKey, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipKey);
        return new AdminBootstrapIpThrottle
        {
            Id = Guid.NewGuid(),
            IpKey = ipKey,
            WindowStartedAt = now,
            AttemptCount = 1,
            LockedUntil = null
        };
    }

    public bool IsLocked(DateTimeOffset now)
        => LockedUntil is { } until && now < until;

    public bool WindowExpired(DateTimeOffset now)
        => now - WindowStartedAt >= TimeSpan.FromMinutes(WindowMinutes);
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Single-use bootstrap token. The raw token is never stored; only the hash
/// is persisted. Lifetime is at most 24 hours.
/// </summary>
public sealed class AdminBootstrapToken
{
    public const int MaxLifetimeHours = 24;

    public Guid Id { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    private AdminBootstrapToken()
    {
    }

    public static AdminBootstrapToken Create(string tokenHash, DateTimeOffset now, TimeSpan? lifetime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        var window = lifetime ?? TimeSpan.FromHours(MaxLifetimeHours);
        if (window <= TimeSpan.Zero || window > TimeSpan.FromHours(MaxLifetimeHours))
        {
            window = TimeSpan.FromHours(MaxLifetimeHours);
        }

        return new AdminBootstrapToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(window)
        };
    }

    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && now < ExpiresAt;

    public void MarkConsumed(DateTimeOffset now)
    {
        ConsumedAt = now;
    }
}

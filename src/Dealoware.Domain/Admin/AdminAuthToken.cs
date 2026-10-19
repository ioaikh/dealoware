namespace Dealoware.Domain.Admin;

/// <summary>
/// Pending-auth or one-time email-link token. The raw token is never persisted.
/// </summary>
public sealed class AdminAuthToken
{
    public const string KindPending = "pending";
    public const string KindReset = "reset";
    public const string KindBootstrap = "bootstrap";

    public Guid Id { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private AdminAuthToken()
    {
    }

    public static AdminAuthToken Create(
        string kind,
        string tokenHash,
        string email,
        DateTimeOffset createdAt,
        TimeSpan lifetime)
    {
        return new AdminAuthToken
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            TokenHash = tokenHash,
            Email = email,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.Add(lifetime),
            AttemptCount = 0
        };
    }

    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt is null && now < ExpiresAt;

    public void IncrementAttempt() => AttemptCount++;

    public void Consume(DateTimeOffset now) => ConsumedAt = now;
}

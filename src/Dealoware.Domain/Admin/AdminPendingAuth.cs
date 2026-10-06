namespace Dealoware.Domain.Admin;

/// <summary>
/// Opaque pending-auth token issued after a correct password (step 1).
/// Grants only TOTP enroll / step-2 verify. Never a CoreOwner session.
/// </summary>
public sealed class AdminPendingAuth
{
    public const int LifetimeMinutes = 5;

    public const int MaxFailedCodeAttempts = 5;

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the opaque cookie value. The raw token is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public int FailedCodeAttempts { get; private set; }

    /// <summary>Validated return path from r5 / C6. Never a raw query value.</summary>
    public string ReturnPath { get; private set; } = "/admin/";

    private AdminPendingAuth() { }

    public static AdminPendingAuth Create(
        string email,
        string tokenHash,
        DateTimeOffset? now = null,
        string? returnPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        var timestamp = now ?? DateTimeOffset.UtcNow;
        return new AdminPendingAuth
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            TokenHash = tokenHash,
            CreatedAt = timestamp,
            ExpiresAt = timestamp.AddMinutes(LifetimeMinutes),
            ReturnPath = string.IsNullOrWhiteSpace(returnPath) ? "/admin/" : returnPath.Trim()
        };
    }

    public bool IsUsable(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        return ConsumedAt is null && timestamp < ExpiresAt;
    }

    public void Consume(DateTimeOffset? now = null)
    {
        ConsumedAt = now ?? DateTimeOffset.UtcNow;
    }
}

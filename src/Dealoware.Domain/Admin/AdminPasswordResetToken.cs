using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Domain.Admin;

/// <summary>
/// Single-use password-reset token. Only the hash is stored.
/// Lifetime is 1 hour from creation. Consumed atomically on successful reset.
/// </summary>
public sealed class AdminPasswordResetToken
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the opaque token. Raw token is never persisted.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    private AdminPasswordResetToken() { }

    public static AdminPasswordResetToken Create(
        string email,
        string tokenHash,
        DateTimeOffset now)
    {
        return new AdminPasswordResetToken
        {
            Id = Guid.NewGuid(),
            Email = email,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddHours(1),
            ConsumedAt = null
        };
    }

    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && now < ExpiresAt;

    public void MarkConsumed(DateTimeOffset now)
    {
        ConsumedAt = now;
    }

    public static string HashRaw(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}

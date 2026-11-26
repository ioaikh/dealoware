using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Domain.Admin;

/// <summary>
/// Single-use bootstrap (first-time setup) token for the CoreOwner. Only the
/// hash is stored. Lifetime is 24 hours from creation (Spec §8.2). Consumed
/// atomically when the first password is set, or when a newer link is issued.
/// </summary>
public sealed class AdminBootstrapToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public Guid Id { get; private set; }

    /// <summary>The owner identity the link was issued for.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the opaque token. Raw token is never persisted.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    private AdminBootstrapToken() { }

    public static AdminBootstrapToken Create(
        string email,
        string tokenHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new AdminBootstrapToken
        {
            Id = Guid.NewGuid(),
            Email = email,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
            ConsumedAt = null
        };
    }

    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && now < ExpiresAt;

    /// <summary>Same hash as password-reset tokens: hex SHA-256 of the UTF-8 token.</summary>
    public static string HashRaw(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}

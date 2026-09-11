namespace Dealoware.Domain.Participants;

/// <summary>
/// Tracks revoked JWT tokens by their JTI (JWT ID) claim.
/// Simple revocation list for token invalidation.
/// </summary>
public sealed class RevokedToken
{
    /// <summary>
    /// The JWT ID (jti claim) of the revoked token.
    /// </summary>
    public string Jti { get; private set; } = string.Empty;

    /// <summary>
    /// The participant who owned this token.
    /// </summary>
    public string Sub { get; private set; } = string.Empty;

    /// <summary>
    /// When this token was revoked.
    /// </summary>
    public DateTimeOffset RevokedAt { get; private set; }

    /// <summary>
    /// When this token expires (for cleanup).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private RevokedToken() { }

    /// <summary>
    /// Creates a revoked token record.
    /// </summary>
    public static RevokedToken Create(string jti, string sub, DateTimeOffset expiresAt)
    {
        return new RevokedToken
        {
            Jti = jti,
            Sub = sub,
            RevokedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
    }
}

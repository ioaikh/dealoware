using System.Security.Cryptography;

namespace Dealoware.Domain.Participants;

/// <summary>
/// API key credential for a Participant.
/// Stores only the hashed key - raw key is returned once at creation and never stored.
/// </summary>
public sealed class ApiKeyCredential
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The participant this credential belongs to.
    /// </summary>
    public Guid ParticipantId { get; private set; }

    /// <summary>
    /// Key prefix for identification (first 8 chars of the key).
    /// Allows users to identify which key without exposing the full key.
    /// </summary>
    public string KeyPrefix { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the API key. Never store raw keys.
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// When this credential was issued.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When this credential was revoked (null if active).
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Whether this credential is currently valid for authentication.
    /// </summary>
    public bool IsValid => RevokedAt is null;

    private ApiKeyCredential() { }

    /// <summary>
    /// Creates a new API key credential.
    /// Returns the raw API key - this is the only time it's available.
    /// Format: "dlw_{prefix}_{random}" where prefix is 8 chars for identification.
    /// </summary>
    public static (ApiKeyCredential Credential, string RawApiKey) Create(Guid participantId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        var prefix = randomPart[..8];
        var rawKey = $"dlw_{prefix}_{randomPart[8..]}";

        var credential = new ApiKeyCredential
        {
            Id = Guid.NewGuid(),
            ParticipantId = participantId,
            KeyPrefix = prefix,
            KeyHash = HashKey(rawKey),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return (credential, rawKey);
    }

    /// <summary>
    /// Revokes this credential, preventing further authentication.
    /// </summary>
    public void Revoke()
    {
        if (RevokedAt is null)
        {
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Verifies if the provided raw key matches this credential's hash.
    /// </summary>
    public bool VerifyKey(string rawKey)
    {
        if (!IsValid) return false;
        return HashKey(rawKey) == KeyHash;
    }

    /// <summary>
    /// Computes SHA-256 hash of an API key.
    /// </summary>
    public static string HashKey(string rawKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

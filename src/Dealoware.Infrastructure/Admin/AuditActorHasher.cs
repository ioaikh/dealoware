using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Derives a dedicated audit HMAC key with HKDF-SHA256 and hashes
/// normalized emails. The IKM is DEALOWARE_ADMIN_IP_HMAC_KEY; that raw
/// key is never logged.
/// </summary>
public sealed class AuditActorHasher : IAuditActorHasher
{
    public const string InfoLabel = "dw-admin-audit-email-v1";

    private readonly byte[] _derivedKey;

    public AuditActorHasher(byte[] ipHmacKey)
    {
        if (ipHmacKey is null || ipHmacKey.Length < 32)
        {
            throw new ArgumentException("HMAC key must be at least 32 bytes", nameof(ipHmacKey));
        }

        _derivedKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm: ipHmacKey,
            outputLength: 32,
            salt: [],
            info: Encoding.UTF8.GetBytes(InfoLabel));
    }

    public string HashEmail(string normalizedEmail)
    {
        if (string.IsNullOrEmpty(normalizedEmail))
        {
            throw new ArgumentException("Email must not be null or empty", nameof(normalizedEmail));
        }

        using var hmac = new HMACSHA256(_derivedKey);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail)));
    }
}

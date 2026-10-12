using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// HMAC-SHA256 hasher for recovery codes. Key from DEALOWARE_ADMIN_RECOVERY_HMAC_KEY
/// (base64, ≥32 bytes). Missing or short key fails closed at startup in every environment.
/// </summary>
public sealed class AdminRecoveryCodeHasher
{
    public const string KeyEnvironmentVariable = "DEALOWARE_ADMIN_RECOVERY_HMAC_KEY";
    public const int MinimumKeyBytes = 32;

    private readonly byte[] _key;

    public AdminRecoveryCodeHasher(byte[] key)
    {
        if (key is null || key.Length < MinimumKeyBytes)
            throw new ArgumentException("Recovery HMAC key must be at least 32 bytes.", nameof(key));
        _key = key;
    }

    public string Hash(string code)
    {
        var normalized = AdminRecoveryCodes.Normalize(code);
        var bytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    public bool FixedTimeEquals(string storedHash, string presentedCode)
        => AdminRecoveryCodes.FixedTimeEquals(storedHash, Hash(presentedCode));

    public static AdminRecoveryCodeHasher Create(Func<string, string?> getEnvVar)
    {
        var keyValue = getEnvVar(KeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} is required. " +
                "Generate a 32+ byte key and encode it as base64.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(keyValue);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} must be a valid base64-encoded key.", ex);
        }

        if (key.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} must decode to at least {MinimumKeyBytes} bytes.");
        }

        return new AdminRecoveryCodeHasher(key);
    }
}

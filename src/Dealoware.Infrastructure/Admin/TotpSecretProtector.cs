using System.Security.Cryptography;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// AES-256-GCM protector for the TOTP shared secret at rest.
/// Key from DEALOWARE_ADMIN_TOTP_KEY (base64, ≥32 bytes). Fail-closed outside Development.
/// </summary>
public sealed class TotpSecretProtector
{
    public const string KeyEnvironmentVariable = "DEALOWARE_ADMIN_TOTP_KEY";

    private readonly byte[] _key;

    public TotpSecretProtector(byte[] key)
    {
        if (key is null || key.Length < 32)
            throw new ArgumentException("TOTP key must be at least 32 bytes", nameof(key));

        _key = key;
    }

    public TotpSecretProtector(string base64Key)
        : this(Convert.FromBase64String(base64Key ?? throw new ArgumentNullException(nameof(base64Key))))
    {
    }

    public string Encrypt(ReadOnlySpan<byte> plaintext)
    {
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var packed = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(packed, 0);
        tag.CopyTo(packed, nonce.Length);
        ciphertext.CopyTo(packed, nonce.Length + tag.Length);
        return Convert.ToBase64String(packed);
    }

    public byte[] Decrypt(string packedBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packedBase64);
        var packed = Convert.FromBase64String(packedBase64);
        if (packed.Length < 12 + 16)
            throw new CryptographicException("Invalid TOTP ciphertext.");

        var nonce = packed.AsSpan(0, 12);
        var tag = packed.AsSpan(12, 16);
        var ciphertext = packed.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static TotpSecretProtector Create(Func<string, string?> getEnvVar, bool isDevelopment)
    {
        var keyValue = getEnvVar(KeyEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    $"Environment variable {KeyEnvironmentVariable} is required in non-Development environments. " +
                    "Generate a 32+ byte key and encode it as base64.");
            }

            var devKey = new byte[32];
            RandomNumberGenerator.Fill(devKey);
            return new TotpSecretProtector(devKey);
        }

        try
        {
            return new TotpSecretProtector(keyValue);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} must be a valid base64-encoded key", ex);
        }
    }
}

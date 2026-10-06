using System.Security.Cryptography;

namespace Dealoware.Domain.Admin;

/// <summary>
/// PBKDF2-HMAC-SHA512 at or above the password-rules floor (220,000
/// iterations), per-user salt. Never log the password or the derived key.
/// </summary>
public static class AdminPasswordHasher
{
    public const int Iterations = 220_000;
    public const int SaltSize = 16;
    public const int KeySize = 32;
    public const string AlgorithmLabel = "pbkdf2-sha512";

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            KeySize);
        return $"{AlgorithmLabel}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string encoded)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encoded))
        {
            return false;
        }

        var parts = encoded.Split('$');
        if (parts.Length != 4
            || !string.Equals(parts[0], AlgorithmLabel, StringComparison.Ordinal)
            || !int.TryParse(parts[1], out var iterations)
            || iterations < Iterations)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

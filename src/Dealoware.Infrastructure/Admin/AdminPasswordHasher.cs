using System.Security.Cryptography;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// PBKDF2-HMAC-SHA512 password hasher at the password-rules v2.3 floor (220,000 iterations).
/// Per-user random salt. Never logs the password or the hash.
/// </summary>
public static class AdminPasswordHasher
{
    public const int IterationCount = 220_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA512,
            KeySize);
        return $"pbkdf2-sha512${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedHash))
            return false;

        var parts = encodedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha512")
            return DummyVerify(password);

        if (!int.TryParse(parts[1], out var iterations) || iterations < 1)
            return DummyVerify(password);

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return DummyVerify(password);
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Burns the same work as a real verify so unknown-account timing stays flat.</summary>
    public static bool DummyVerify(string password)
    {
        var dummySalt = new byte[SaltSize];
        Rfc2898DeriveBytes.Pbkdf2(
            password ?? string.Empty,
            dummySalt,
            IterationCount,
            HashAlgorithmName.SHA512,
            KeySize);
        return false;
    }
}

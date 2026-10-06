using System.Security.Cryptography;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// PBKDF2-HMAC-SHA512 at 220,000 iterations with a 16-byte per-user salt.
/// Format: pbkdf2-sha512$220000$saltHash.
/// </summary>
public sealed class Pbkdf2AdminPasswordHasher : IAdminPasswordHasher
{
    public const int Iterations = 220_000;
    public const int SaltSize = 16;
    public const int HashSize = 32;
    public const string AlgorithmPrefix = "pbkdf2-sha512";

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return $"{AlgorithmPrefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$', 4, StringSplitOptions.None);
        if (parts.Length != 4
            || !string.Equals(parts[0], AlgorithmPrefix, StringComparison.Ordinal)
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

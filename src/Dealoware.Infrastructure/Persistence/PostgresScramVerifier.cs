using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Builds a PostgreSQL SCRAM-SHA-256 password verifier on the client (RFC 5802 /
/// RFC 7677, the form <c>psql \password</c> sends). <c>dbroles</c> sets each login's
/// password with <c>ALTER ROLE ... PASSWORD '&lt;verifier&gt;'</c>, so the plain
/// password never travels to the server and cannot appear in a server log.
/// </summary>
public static class PostgresScramVerifier
{
    /// <summary>PostgreSQL's default SCRAM iteration count.</summary>
    public const int DefaultIterations = 4096;

    private const int SaltLength = 16;

    /// <summary>
    /// <c>SCRAM-SHA-256$&lt;iterations&gt;:&lt;salt&gt;$&lt;StoredKey&gt;:&lt;ServerKey&gt;</c>
    /// with a fresh random salt.
    /// </summary>
    public static string Create(string password)
        => Create(password, RandomNumberGenerator.GetBytes(SaltLength), DefaultIterations);

    /// <summary>Same as <see cref="Create(string)"/> with a given salt (tests).</summary>
    public static string Create(string password, byte[] salt, int iterations)
    {
        ArgumentNullException.ThrowIfNull(salt);
        if (!IsAcceptablePassword(password))
        {
            // PostgreSQL applies SASLprep to the password. For printable ASCII it is the
            // identity, so the verifier built here is exactly the one the server expects.
            throw new ArgumentException("The password must be 16 to 1024 printable ASCII characters.", nameof(password));
        }

        if (salt.Length == 0 || iterations < 1)
        {
            throw new ArgumentException("A salt and a positive iteration count are required.");
        }

        var (storedKey, serverKey) = DeriveKeys(password, salt, iterations);
        return $"SCRAM-SHA-256${iterations}:{Convert.ToBase64String(salt)}" +
               $"${Convert.ToBase64String(storedKey)}:{Convert.ToBase64String(serverKey)}";
    }

    /// <summary>
    /// The StoredKey and ServerKey of RFC 5802 for this password, salt and iteration count.
    /// </summary>
    public static (byte[] StoredKey, byte[] ServerKey) DeriveKeys(string password, byte[] salt, int iterations)
    {
        var salted = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            var clientKey = HMACSHA256.HashData(salted, "Client Key"u8);
            var storedKey = SHA256.HashData(clientKey);
            var serverKey = HMACSHA256.HashData(salted, "Server Key"u8);
            CryptographicOperations.ZeroMemory(clientKey);
            return (storedKey, serverKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salted);
        }
    }

    /// <summary>
    /// 16 to 1024 printable ASCII characters. The stack's generated passwords are 32
    /// letters and digits.
    /// </summary>
    public static bool IsAcceptablePassword(string? password)
        => password is { Length: >= 16 and <= 1024 }
           && password.All(c => c is >= '!' and <= '~');
}

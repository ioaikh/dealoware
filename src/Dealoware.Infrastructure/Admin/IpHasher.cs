using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// HMAC-SHA256 IP hasher implementation.
/// Uses a keyed hash to prevent rainbow table attacks on the small IPv4 space.
/// The key is sourced from DEALOWARE_ADMIN_IP_HMAC_KEY environment variable.
/// </summary>
public sealed class IpHasher : IIpHasher
{
    private readonly byte[] _key;

    /// <summary>
    /// Environment variable name for the HMAC key.
    /// </summary>
    public const string KeyEnvironmentVariable = "DEALOWARE_ADMIN_IP_HMAC_KEY";

    /// <summary>
    /// Creates an IpHasher with the specified key.
    /// </summary>
    /// <param name="key">The HMAC key bytes. Must be at least 32 bytes for adequate security.</param>
    public IpHasher(byte[] key)
    {
        if (key is null || key.Length < 32)
            throw new ArgumentException("HMAC key must be at least 32 bytes", nameof(key));
        
        _key = key;
    }

    /// <summary>
    /// Creates an IpHasher from a base64-encoded key string.
    /// </summary>
    /// <param name="base64Key">Base64-encoded HMAC key.</param>
    public IpHasher(string base64Key)
        : this(Convert.FromBase64String(base64Key ?? throw new ArgumentNullException(nameof(base64Key))))
    {
    }

    /// <inheritdoc />
    public string Hash(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            throw new ArgumentException("IP address must not be null or empty", nameof(ipAddress));

        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Factory method that creates an IpHasher from environment configuration.
    /// Fail-closed in non-Development: throws if key is missing.
    /// </summary>
    /// <param name="getEnvVar">Function to retrieve environment variables.</param>
    /// <param name="isDevelopment">Whether the app is running in Development mode.</param>
    /// <returns>An IpHasher instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown in non-Development if DEALOWARE_ADMIN_IP_HMAC_KEY is not set.
    /// </exception>
    public static IpHasher Create(Func<string, string?> getEnvVar, bool isDevelopment)
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

            // Development fallback: generate a random key per process start.
            // This is NOT for production - it means hashes won't be consistent across restarts.
            var devKey = new byte[32];
            RandomNumberGenerator.Fill(devKey);
            return new IpHasher(devKey);
        }

        try
        {
            return new IpHasher(keyValue);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} must be a valid base64-encoded key", ex);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                $"Environment variable {KeyEnvironmentVariable} is too short. " +
                "A base64-encoded HMAC key of at least 32 bytes is required.");
        }
    }
}

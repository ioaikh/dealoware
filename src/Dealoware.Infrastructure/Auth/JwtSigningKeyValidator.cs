using System.Text;

namespace Dealoware.Infrastructure.Auth;

/// <summary>
/// Resolves and validates the JWT signing key.
///
/// Strict mode (every environment except Development: Production, Staging, Testing, custom names):
/// the key must be set, must not be the development placeholder, and must be at least
/// <see cref="MinimumKeyBytes"/> bytes (UTF-8). Otherwise startup fails fast.
/// Development: unchanged PoC behavior, falling back to the development placeholder when no key is set.
///
/// Exception messages never include the key value.
/// </summary>
public static class JwtSigningKeyValidator
{
    /// <summary>Environment variable that carries the signing key.</summary>
    public const string EnvironmentVariableName = "DEALOWARE_JWT_SIGNING_KEY";

    /// <summary>Public development-only placeholder. Never valid in Production.</summary>
    public const string DevelopmentPlaceholderKey = "DEVELOPMENT_PLACEHOLDER_KEY_CHANGE_IN_PRODUCTION_32CHARS";

    /// <summary>Minimum key length in bytes (UTF-8) for HMAC-SHA256.</summary>
    public const int MinimumKeyBytes = 32;

    /// <summary>
    /// Returns the signing key to use. When <paramref name="requireStrictKey"/> is true (callers pass
    /// <c>!IsDevelopment()</c>), throws <see cref="InvalidOperationException"/> if the key is missing, blank,
    /// the development placeholder, or shorter than <see cref="MinimumKeyBytes"/> bytes.
    /// </summary>
    public static string Validate(string? key, bool requireStrictKey)
    {
        if (!requireStrictKey)
        {
            return key ?? DevelopmentPlaceholderKey;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} is not set. A JWT signing key of at least {MinimumKeyBytes} bytes is required outside Development.");
        }

        if (string.Equals(key.Trim(), DevelopmentPlaceholderKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} is set to the public development placeholder, which is only allowed in Development. Configure a random key of at least {MinimumKeyBytes} bytes.");
        }

        if (Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} is too short. A JWT signing key of at least {MinimumKeyBytes} bytes (UTF-8) is required outside Development.");
        }

        return key;
    }
}

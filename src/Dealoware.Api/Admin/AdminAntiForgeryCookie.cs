using System.Security.Cryptography;
using System.Text;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C5 + R14/R16: anti-forgery cookie name dw_admin_af, no __Host- prefix, Path=/admin.
/// HttpOnly cookie holds a per-browser nonce; the request token is HMAC over a purpose
/// label and that nonce, keyed with HKDF-SHA256(DEALOWARE_ADMIN_IP_HMAC_KEY,
/// info dw-admin-antiforgery-v1). Never a cookie echo.
/// </summary>
public sealed class AdminAntiForgeryService
{
    public const string CookieName = "dw_admin_af";
    public const string HeaderName = "X-CSRF-TOKEN";
    public const string TokenPurpose = "dw-admin-af-token-v1";
    public const string KeyInfo = "dw-admin-antiforgery-v1";
    public const int KeySizeBytes = 32;

    private readonly byte[] _key;

    public AdminAntiForgeryService(byte[] key)
    {
        if (key is null || key.Length < KeySizeBytes)
            throw new ArgumentException("Anti-forgery key must be at least 32 bytes.", nameof(key));
        _key = key;
    }

    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = AdminHostMiddleware.AdminPathPrefix,
        IsEssential = true
    };

    public static CookieOptions ExpiredOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        return options;
    }

    /// <summary>
    /// Derive the anti-forgery HMAC key from DEALOWARE_ADMIN_IP_HMAC_KEY.
    /// No random fallback outside Development. The derived key is never logged.
    /// </summary>
    public static AdminAntiForgeryService Create(Func<string, string?> getEnvVar, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(getEnvVar);
        var keyValue = getEnvVar(IpHasher.KeyEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    $"Environment variable {IpHasher.KeyEnvironmentVariable} is required in non-Development environments. " +
                    "Generate a 32+ byte key and encode it as base64.");
            }

            var devKey = new byte[KeySizeBytes];
            RandomNumberGenerator.Fill(devKey);
            return new AdminAntiForgeryService(DeriveKey(devKey));
        }

        byte[] sourceKey;
        try
        {
            sourceKey = Convert.FromBase64String(keyValue);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Environment variable {IpHasher.KeyEnvironmentVariable} must be a valid base64-encoded key.", ex);
        }

        if (sourceKey.Length < KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"Environment variable {IpHasher.KeyEnvironmentVariable} is too short. " +
                "A base64-encoded HMAC key of at least 32 bytes is required.");
        }

        return new AdminAntiForgeryService(DeriveKey(sourceKey));
    }

    public static byte[] DeriveKey(byte[] sourceKey)
    {
        ArgumentNullException.ThrowIfNull(sourceKey);
        return HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            sourceKey,
            KeySizeBytes,
            salt: null,
            info: Encoding.UTF8.GetBytes(KeyInfo));
    }

    public static string ComputeRequestToken(byte[] key, string cookieNonce, string purpose = TokenPurpose)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrEmpty(cookieNonce);
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        var payload = Encoding.UTF8.GetBytes(purpose + "\0" + cookieNonce);
        return Convert.ToHexString(HMACSHA256.HashData(key, payload));
    }

    public string ComputeRequestToken(string cookieNonce)
        => ComputeRequestToken(_key, cookieNonce, TokenPurpose);

    public string Issue(HttpContext context)
    {
        var cookieToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(CookieName, cookieToken, CreateOptions());
        return ComputeRequestToken(cookieToken);
    }

    public IResult IssuePage(HttpContext context, string screen)
        => Results.Json(new { screen, csrfToken = Issue(context) });

    public bool TryValidate(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookie)
            || string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var header)
            || string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var expected = ComputeRequestToken(cookie);
        var presented = header.ToString();
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var presentedBytes = Encoding.ASCII.GetBytes(presented);
        return expectedBytes.Length == presentedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, presentedBytes);
    }
}

/// <summary>Name aliases so tests and cookie-clearing use the r3 C5 identifiers.</summary>
public static class AdminAntiForgeryCookie
{
    public const string Name = AdminAntiForgeryService.CookieName;
    public const string HeaderName = AdminAntiForgeryService.HeaderName;

    public static CookieOptions CreateOptions() => AdminAntiForgeryService.CreateOptions();

    public static CookieOptions ExpiredOptions() => AdminAntiForgeryService.ExpiredOptions();
}

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

    /// <summary>
    /// For the signed-in pages: the request token for the anti-forgery
    /// cookie this browser already holds, so a page opened in one tab does
    /// not invalidate the token of a page open in another. Only a cookie of
    /// the exact shape <see cref="Issue"/> makes is used; without one a new
    /// pair is issued. The token is the same keyed HMAC either way.
    /// </summary>
    public string IssueOrReuse(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var cookie) && IsIssuedShape(cookie))
            return ComputeRequestToken(cookie);

        return Issue(context);
    }

    private static bool IsIssuedShape(string? cookie)
    {
        if (cookie is null || cookie.Length != 64)
            return false;

        foreach (var ch in cookie)
        {
            if (ch is not ((>= '0' and <= '9') or (>= 'A' and <= 'F')))
                return false;
        }

        return true;
    }

    /// <summary>
    /// The hidden field of the shell's sign-out form. Only POST /admin/sign-out
    /// reads it (<see cref="TryValidateSignOutFormAsync"/>), so that signing
    /// out works with JavaScript off; every other route takes the header only.
    /// </summary>
    public const string SignOutFormField = "dw_csrf";

    /// <summary>The largest sign-out form read: one field of 64 hex characters needs far less.</summary>
    public const int SignOutFormMaxBytes = 1024;

    public bool TryValidate(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var header)
            || string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        return Matches(context, header.ToString());
    }

    /// <summary>
    /// The token of the sign-out form: a urlencoded POST to exactly
    /// /admin/sign-out (by the canonical path the gate compares), of known
    /// small length, carrying <see cref="SignOutFormField"/> once. The same
    /// keyed check as the header: the token must be the HMAC of this
    /// browser's cookie, compared in constant time. The origin check runs
    /// before this, as for every state-changing request.
    /// </summary>
    public async Task<bool> TryValidateSignOutFormAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method)
            || !AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical)
            || !string.Equals(canonical, AdminSignedOutExemptions.SignOut, StringComparison.OrdinalIgnoreCase)
            || request.ContentLength is not (> 0 and <= SignOutFormMaxBytes)
            || !Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            || !string.Equals(mediaType.MediaType.Value, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(context.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or FormatException)
        {
            return false;
        }

        return form.TryGetValue(SignOutFormField, out var values)
               && values.Count == 1
               && !string.IsNullOrWhiteSpace(values[0])
               && Matches(context, values[0]!);
    }

    private bool Matches(HttpContext context, string presented)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookie)
            || string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        var expected = ComputeRequestToken(cookie);
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

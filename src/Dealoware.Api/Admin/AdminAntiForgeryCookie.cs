using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C5: anti-forgery cookie name dw_admin_af, no __Host- prefix, Path=/admin.
/// HttpOnly cookie holds a random secret; the request token is HMAC(secret) and is
/// returned on GET so the client can send it as X-CSRF-TOKEN (C3).
/// </summary>
public sealed class AdminAntiForgeryService
{
    public const string CookieName = "dw_admin_af";
    public const string HeaderName = "X-CSRF-TOKEN";

    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

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

    public string Issue(HttpContext context)
    {
        var cookieToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(CookieName, cookieToken, CreateOptions());
        return CreateRequestToken(cookieToken);
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

        var expected = CreateRequestToken(cookie);
        var presented = header.ToString();
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var presentedBytes = Encoding.ASCII.GetBytes(presented);
        return expectedBytes.Length == presentedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, presentedBytes);
    }

    private string CreateRequestToken(string cookieToken)
        => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(cookieToken)));
}

/// <summary>Name aliases so tests and cookie-clearing use the r3 C5 identifiers.</summary>
public static class AdminAntiForgeryCookie
{
    public const string Name = AdminAntiForgeryService.CookieName;
    public const string HeaderName = AdminAntiForgeryService.HeaderName;

    public static CookieOptions CreateOptions() => AdminAntiForgeryService.CreateOptions();

    public static CookieOptions ExpiredOptions() => AdminAntiForgeryService.ExpiredOptions();
}

using System.Security.Cryptography;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C3 / C5: anti-forgery cookie <c>dw_admin_af</c> on exempt POSTs.
/// Host-only, Path=/admin, no __Host- prefix.
/// </summary>
public static class AdminAntiForgery
{
    public const string CookieName = "dw_admin_af";
    public const string HeaderName = "X-CSRF-TOKEN";

    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = AdminHostMiddleware.AdminPathPrefix,
        IsEssential = true
    };

    public static string Issue(HttpContext context)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(CookieName, token, CreateOptions());
        return token;
    }

    public static bool Validate(HttpContext context, string? bodyToken = null)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookie)
            || string.IsNullOrEmpty(cookie))
        {
            return false;
        }

        var header = context.Request.Headers[HeaderName].ToString();
        var presented = !string.IsNullOrEmpty(header) ? header : bodyToken;
        if (string.IsNullOrEmpty(presented) || presented.Length != cookie.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(presented),
            System.Text.Encoding.UTF8.GetBytes(cookie));
    }
}

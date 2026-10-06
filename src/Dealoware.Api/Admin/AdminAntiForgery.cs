using System.Security.Cryptography;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C3/C5: anti-forgery cookie dw_admin_af plus matching request token.
/// No __Host- prefix. Host-only, Secure, HttpOnly, SameSite=Strict, Path=/admin.
/// </summary>
public static class AdminAntiForgery
{
    public static CookieOptions CookieOptions()
    {
        var options = AdminSessionCookie.CreateOptions();
        options.Domain = null;
        return options;
    }

    public static string Issue(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(AdminCookieNames.AntiForgery, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            context.Response.Headers[AdminCookieNames.AntiForgeryHeader] = existing;
            return existing;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        context.Response.Cookies.Append(AdminCookieNames.AntiForgery, token, CookieOptions());
        context.Response.Headers[AdminCookieNames.AntiForgeryHeader] = token;
        return token;
    }

    public static bool TryValidate(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(AdminCookieNames.AntiForgery, out var cookie)
            || string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        var presented = ReadPresented(context);
        if (string.IsNullOrWhiteSpace(presented))
        {
            return false;
        }

        var cookieBytes = System.Text.Encoding.UTF8.GetBytes(cookie);
        var presentedBytes = System.Text.Encoding.UTF8.GetBytes(presented);
        return cookieBytes.Length == presentedBytes.Length
               && CryptographicOperations.FixedTimeEquals(cookieBytes, presentedBytes);
    }

    private static string? ReadPresented(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(AdminCookieNames.AntiForgeryHeader, out var header)
            && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        if (context.Request.HasFormContentType
            && context.Request.Form.TryGetValue("af", out var form)
            && !string.IsNullOrWhiteSpace(form))
        {
            return form.ToString();
        }

        return null;
    }
}

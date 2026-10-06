using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// §10 C3 / C5: exempt POSTs require dw_admin_af. GET may issue the cookie (no other side effects).
/// </summary>
public static class AdminAntiForgery
{
    public static void IssueOnGet(HttpContext context)
    {
        var existing = context.Request.Cookies[AdminAuthCookies.AntiForgery];
        var token = existing;
        if (string.IsNullOrWhiteSpace(token))
        {
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            context.Response.Cookies.Append(
                AdminAuthCookies.AntiForgery,
                token,
                AdminAuthCookies.CreateOptions());
        }

        context.Response.Headers[AdminAuthCookies.AntiForgeryHeader] = token;
    }

    public static bool TryValidate(HttpContext context)
    {
        var cookie = context.Request.Cookies[AdminAuthCookies.AntiForgery];
        if (string.IsNullOrWhiteSpace(cookie))
            return false;

        var presented = context.Request.Headers[AdminAuthCookies.AntiForgeryHeader].ToString();
        if (string.IsNullOrWhiteSpace(presented)
            && context.Request.HasFormContentType
            && context.Request.Form.TryGetValue("csrf", out var form))
        {
            presented = form.ToString();
        }

        if (string.IsNullOrWhiteSpace(presented) || presented.Length != cookie.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cookie),
            Encoding.UTF8.GetBytes(presented));
    }
}

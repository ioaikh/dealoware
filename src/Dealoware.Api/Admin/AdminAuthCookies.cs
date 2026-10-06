namespace Dealoware.Api.Admin;

/// <summary>
/// r5 sha c5e9d232 §10 C5: dw_admin_session, dw_admin_pending, dw_admin_af.
/// Host-only, Secure, HttpOnly, SameSite=Strict, Path=/admin. No __Host- prefix.
/// </summary>
public static class AdminAuthCookies
{
    public const string PendingSignIn = "dw_admin_pending";
    public const string AntiForgery = "dw_admin_af";
    public const string AntiForgeryHeader = "X-CSRF-TOKEN";

    /// <summary>Development stand-in for S-A6/S-A7 enrol pending (no extra cookie name).</summary>
    public const string EnrolPendingValue = "enrol";

    public static CookieOptions CreateOptions(int? maxAgeSeconds = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = AdminHostMiddleware.AdminPathPrefix,
            IsEssential = true
        };
        if (maxAgeSeconds is { } seconds)
            options.MaxAge = TimeSpan.FromSeconds(seconds);
        return options;
    }

    public static CookieOptions ExpireOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        return options;
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        var expire = ExpireOptions();
        response.Cookies.Append(AdminSessionCookie.Name, string.Empty, expire);
        response.Cookies.Append(PendingSignIn, string.Empty, expire);
        response.Cookies.Append(AntiForgery, string.Empty, expire);
    }
}

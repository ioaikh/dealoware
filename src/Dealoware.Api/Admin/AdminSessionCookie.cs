namespace Dealoware.Api.Admin;

/// <summary>
/// Admin session cookie flags (Spec §8.7 / SA §3.3).
/// Issued on successful sign-in (after step 2). Path stays /admin.
/// </summary>
public static class AdminSessionCookie
{
    public const string Name = "dw_admin_session";

    /// <summary>
    /// HttpOnly, Secure, SameSite=Strict, path-scoped to /admin (host-only cookie).
    /// </summary>
    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = AdminHostMiddleware.AdminPathPrefix,
        IsEssential = true
    };

    /// <summary>r2 §2.4: clear with the same name, Path, Secure, HttpOnly, SameSite, and Max-Age=0.</summary>
    public static CookieOptions CreateClearOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        return options;
    }
}

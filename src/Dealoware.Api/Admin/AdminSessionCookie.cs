namespace Dealoware.Api.Admin;

/// <summary>
/// Admin session cookie flags (Spec §8.7 / SA §3.3).
/// Issuance is a later PR; validation middleware reads this cookie name.
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
}

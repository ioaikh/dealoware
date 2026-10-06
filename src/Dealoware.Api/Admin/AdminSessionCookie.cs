using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Admin session cookie flags (Spec §8.7 / SA §3.3).
/// Path=/admin, host-only (no Domain). Issuance after step 2 TOTP/recovery.
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

    public static CookieOptions ExpiredOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        return options;
    }
}

/// <summary>
/// Pending-auth cookie after step 1. Grants only /admin/api/auth/* step-2 and enroll.
/// Path=/admin, host-only. Lifetime 5 minutes.
/// </summary>
public static class AdminPendingAuthCookie
{
    public const string Name = "dw_admin_pending";

    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = AdminHostMiddleware.AdminPathPrefix,
        IsEssential = true,
        MaxAge = TimeSpan.FromMinutes(AdminPendingAuth.LifetimeMinutes)
    };

    public static CookieOptions ExpiredOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        return options;
    }
}

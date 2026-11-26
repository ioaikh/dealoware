namespace Dealoware.Api.Admin;

/// <summary>
/// TD-ADM-177: the page the owner asked for when the session gate sent the
/// browser to sign in, carried across sign-in on the server's side and never
/// in a sign-in address.
/// <para>
/// The gate sets it on its redirect (<see cref="Remember"/>), holding only a
/// path that <see cref="AdminReturnPath"/> accepted, with the list query that
/// survived its checks. It is HttpOnly, Secure, SameSite=Strict, host-only
/// and scoped to the sign-in JSON routes
/// (<see cref="AdminSignedOutExemptions.ApiSignIn"/> and the two second-step
/// routes under it), so no page and no script ever sees it. Step 1 reads it
/// and checks it again (<see cref="Read"/>) before it goes into the pending
/// sign-in; a completed sign-in removes it (<see cref="Forget"/>). It runs
/// out on its own after <see cref="Lifetime"/>.
/// </para>
/// </summary>
public static class AdminReturnCookie
{
    public const string Name = "dw_admin_return";

    /// <summary>Sent to step 1 and to the two step 2 routes below it, and to nothing else.</summary>
    public const string Path = AdminSignedOutExemptions.ApiSignIn;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        IsEssential = true,
        MaxAge = Lifetime
    };

    public static CookieOptions ExpiredOptions()
    {
        var options = CreateOptions();
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        return options;
    }

    /// <summary>
    /// Keeps the page this request asked for, if it is one a sign-in may
    /// return to; otherwise removes whatever an earlier visit kept, so the
    /// owner lands on Stats rather than on an older page.
    /// </summary>
    public static void Remember(HttpContext context, string canonicalPath)
    {
        var candidate = canonicalPath + context.Request.QueryString.Value;
        if (AdminReturnPath.TryValidate(candidate, out var path))
            context.Response.Cookies.Append(Name, path, CreateOptions());
        else
            Forget(context);
    }

    /// <summary>The kept page, checked again, or null when there is none or it does not pass.</summary>
    public static string? Read(HttpContext context)
        => context.Request.Cookies.TryGetValue(Name, out var raw)
           && AdminReturnPath.TryValidate(raw, out var path)
            ? path
            : null;

    public static void Forget(HttpContext context)
        => context.Response.Cookies.Delete(Name, ExpiredOptions());
}

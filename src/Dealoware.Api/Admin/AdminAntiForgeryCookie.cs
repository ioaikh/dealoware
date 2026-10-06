using Microsoft.AspNetCore.Antiforgery;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C5: anti-forgery cookie name dw_admin_af, no __Host- prefix, Path=/admin.
/// </summary>
public static class AdminAntiForgeryCookie
{
    public const string Name = "dw_admin_af";
    public const string HeaderName = "X-CSRF-TOKEN";

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

    public static IResult IssuePage(HttpContext context, IAntiforgery antiforgery, string screen)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Json(new { screen, csrfToken = tokens.RequestToken });
    }
}

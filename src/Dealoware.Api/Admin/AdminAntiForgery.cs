using Microsoft.AspNetCore.Antiforgery;

namespace Dealoware.Api.Admin;

public static class AdminAntiForgery
{
    public const string CookieName = "dw_admin_af";
    public const string HeaderName = "X-CSRF-TOKEN";

    public static void Configure(AntiforgeryOptions options)
    {
        options.HeaderName = HeaderName;
        options.Cookie.Name = CookieName;
        options.Cookie.Path = AdminHostMiddleware.AdminPathPrefix;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;
    }

    public static IResult IssueTokens(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers[HeaderName] = tokens.RequestToken ?? string.Empty;
        return Results.NoContent();
    }
}

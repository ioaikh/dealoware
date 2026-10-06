using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Thin admin UI pages under /admin/... (route-prefix note r3, sha b079a814).
/// HTML has no entity data; lists and stats load through /admin/api after the session gate.
/// Signed-in shell assets are served only from these endpoints (after AdminSessionMiddleware).
/// They are not under wwwroot and are never placed in /admin/auth/.
/// </summary>
public static class AdminUiEndpoints
{
    private static readonly HashSet<string> AssetFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin.css",
        "admin.js"
    };

    public static void MapAdminUiEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/", ServePage).WithName("AdminUiStats");
        app.MapGet("/admin/participants", ServePage).WithName("AdminUiParticipants");
        app.MapGet("/admin/artifacts", ServePage).WithName("AdminUiArtifacts");
        app.MapGet("/admin/negotiations", ServePage).WithName("AdminUiNegotiations");
        app.MapGet("/admin/offers", ServePage).WithName("AdminUiOffers");
        app.MapGet("/admin/participants/{id:guid}", ServePage).WithName("AdminUiParticipantDetail");
        app.MapGet("/admin/artifacts/{id:guid}", ServePage).WithName("AdminUiArtifactDetail");
        app.MapGet("/admin/negotiations/{id:guid}", ServePage).WithName("AdminUiNegotiationDetail");
        app.MapGet("/admin/offers/{id:guid}", ServePage).WithName("AdminUiOfferDetail");
        app.MapGet("/admin/ui/{file}", ServeAsset).WithName("AdminUiAsset");
        app.MapGet("/admin/sign-out", ServePage).WithName("AdminUiSignOutGet");
        app.MapPost("/admin/sign-out", SignOut).WithName("AdminUiSignOutPost");
        // One extra segment or more — never `/admin` or `/admin/` (those collide with stats).
        app.MapGet("/admin/{first}", ServeUnknownOne).WithName("AdminUiUnknownOne");
        app.MapGet("/admin/{first}/{*rest}", ServeUnknownMore).WithName("AdminUiUnknownMore");
    }

    private static IResult ServePage(HttpContext context)
    {
        ApplyNoStore(context);
        var file = ResolveUiFile(context, "index.html");
        if (file is null)
            return AdminDeny.NotFoundResult();
        return Results.File(file, "text/html; charset=utf-8");
    }

    private static IResult ServeUnknownOne(HttpContext context, string first)
        => ServeUnknownPage(context, first);

    private static IResult ServeUnknownMore(HttpContext context, string first, string rest)
        => ServeUnknownPage(context, first);

    private static IResult ServeUnknownPage(HttpContext context, string first)
    {
        if (first.Equals("api", StringComparison.OrdinalIgnoreCase))
            return AdminDeny.NotFoundResult();
        // Signed-out auth assets live under /admin/auth/ only (Step 14). Never serve the
        // signed-in shell there.
        if (first.Equals("auth", StringComparison.OrdinalIgnoreCase))
            return AdminDeny.NotFoundResult();
        if (first.Equals("ui-test", StringComparison.OrdinalIgnoreCase))
            return AdminDeny.NotFoundResult();

        ApplyNoStore(context);
        var file = ResolveUiFile(context, "index.html");
        if (file is null)
            return AdminDeny.NotFoundResult();
        return Results.File(file, "text/html; charset=utf-8");
    }

    private static IResult ServeAsset(HttpContext context, string file)
    {
        if (!AssetFiles.Contains(file))
            return AdminDeny.NotFoundResult();

        ApplyNoStore(context);
        var path = ResolveUiFile(context, file);
        if (path is null)
            return AdminDeny.NotFoundResult();

        var contentType = file.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            ? "text/css; charset=utf-8"
            : "text/javascript; charset=utf-8";
        return Results.File(path, contentType);
    }

    private static async Task<IResult> SignOut(
        HttpContext context,
        IAdminSessionRepository sessions)
    {
        ApplyNoStore(context);
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        if (Guid.TryParse(raw, out var sessionId))
        {
            await sessions.DeleteAsync(sessionId, context.RequestAborted);
            await sessions.SaveChangesAsync(context.RequestAborted);
        }

        context.Response.Cookies.Append(
            AdminSessionCookie.Name,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = AdminHostMiddleware.AdminPathPrefix,
                IsEssential = true,
                MaxAge = TimeSpan.Zero,
                Expires = DateTimeOffset.UnixEpoch
            });

        return Results.Redirect(AdminUiRoutes.Stats);
    }

    private static void ApplyNoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    private static string? ResolveUiFile(HttpContext context, string name)
    {
        var env = context.RequestServices.GetService<IWebHostEnvironment>();
        var contentRoot = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(contentRoot, "Admin", "Ui", name),
            Path.Combine(assemblyDir, "Admin", "Ui", name),
            Path.Combine(AppContext.BaseDirectory, "Admin", "Ui", name)
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}

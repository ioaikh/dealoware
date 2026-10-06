using Microsoft.AspNetCore.Hosting;

namespace Dealoware.Api.Admin;

/// <summary>
/// Serves S-A1–S-A11 HTML after the session gate. Pages live outside wwwroot
/// so they are never served as anonymous static files except via §2.2.
/// </summary>
public static class AdminAuthPageEndpoints
{
    public const string PageFolder = "AdminUi/pages";

    private static readonly Dictionary<string, string> PageFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        [AdminAuthPaths.SignIn] = "signin.html",
        [AdminAuthPaths.SignInCode] = "signin-code.html",
        [AdminAuthPaths.SignInRecovery] = "signin-recovery.html",
        [AdminAuthPaths.Bootstrap] = "bootstrap.html",
        [AdminAuthPaths.LinkExpired] = "link-expired.html",
        [AdminAuthPaths.Authenticator] = "authenticator.html",
        [AdminAuthPaths.RecoveryCodes] = "recovery-codes.html",
        [AdminAuthPaths.Reset] = "reset.html",
        [AdminAuthPaths.ResetSent] = "reset-sent.html",
        [AdminAuthPaths.ResetConfirm] = "reset-confirm.html",
        [AdminAuthPaths.Stats] = "stats.html"
    };

    public static IServiceCollection AddAdminAuthUi(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            services.AddSingleton<IAdminAuthTokenGate, DevelopmentAdminAuthTokenGate>();
        else
            services.AddSingleton<IAdminAuthTokenGate, FailClosedAdminAuthTokenGate>();
        return services;
    }

    public static IEndpointRouteBuilder MapAdminAuthUi(this IEndpointRouteBuilder app)
    {
        foreach (var (route, file) in PageFiles)
        {
            var capturedFile = file;
            app.MapMethods(route, ["GET", "POST"], (HttpContext context, IWebHostEnvironment env) =>
                    WritePage(context, env, capturedFile))
                .WithTags("AdminAuthUi")
                .AllowAnonymous();
        }

        app.MapPost(AdminAuthPaths.SignOut, SignOutAsync)
            .WithTags("AdminAuthUi")
            .AllowAnonymous();

        app.MapGet(AdminAuthPaths.SignOut, () => Results.NotFound())
            .WithTags("AdminAuthUi");

        app.MapPost(AdminAuthPaths.ApiSignOut, SignOutAsync)
            .WithTags("AdminAuthUi")
            .AllowAnonymous();

        return app;
    }

    internal static string ResolvePageRoot(IWebHostEnvironment env)
    {
        var fromContent = Path.Combine(env.ContentRootPath, PageFolder);
        if (Directory.Exists(fromContent))
            return fromContent;

        var fromBase = Path.Combine(AppContext.BaseDirectory, PageFolder);
        return Directory.Exists(fromBase) ? fromBase : fromContent;
    }

    private static IResult WritePage(HttpContext context, IWebHostEnvironment env, string fileName)
    {
        var path = Path.Combine(ResolvePageRoot(env), fileName);
        if (!File.Exists(path))
            return Results.NotFound();

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(path, "text/html; charset=utf-8");
    }

    private static Task SignOutAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
            return Task.CompletedTask;

        AdminAuthCookies.ClearAuthCookies(context.Response);
        if (AcceptsHtml(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = AdminAuthPaths.SignIn + "?status=signed-out";
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    private static bool AcceptsHtml(HttpRequest request) =>
        request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
}

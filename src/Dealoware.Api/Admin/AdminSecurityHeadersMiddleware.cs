using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Adds HSTS, CSP, and browser hardening headers on every admin-host response
/// (pages, <c>/admin/api/...</c>, errors, 404s, signed-out <c>/admin/sign-in</c>,
/// and <c>/admin/auth/</c> static files). Does not run for other hosts.
/// Unhandled exceptions on the admin host become a generic 500 that still
/// carries the headers when the response has not started.
/// </summary>
public sealed class AdminSecurityHeadersMiddleware
{
    public const string StrictTransportSecurity = "max-age=31536000";

    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://challenges.cloudflare.com; " +
        "frame-src https://challenges.cloudflare.com; " +
        "frame-ancestors 'none'; " +
        "base-uri 'none'; " +
        "form-action 'self'; " +
        "object-src 'none'";

    public const string ReferrerPolicy = "no-referrer";
    public const string RobotsTag = "noindex, nofollow";
    public const string CacheControl = "no-store";
    public const string Nosniff = "nosniff";
    public const string FrameOptions = "DENY";
    public const string GenericErrorMessage = "Internal Server Error";

    private readonly RequestDelegate _next;

    public AdminSecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<AdminHostOptions> options,
        ILogger<AdminSecurityHeadersMiddleware> logger)
    {
        var isAdminHost = options.Value.IsAllowedHost(context.Request.Host.Host);
        if (isAdminHost)
        {
            context.Response.OnStarting(static state =>
            {
                Apply((HttpContext)state);
                return Task.CompletedTask;
            }, context);
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (isAdminHost)
        {
            // Type name and trace id only — never the exception object, message, or stack.
            logger.LogError(
                "Unhandled exception on the admin host. Type={ExceptionType} TraceId={TraceId}",
                ex.GetType().FullName,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                context.Abort();
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            Apply(context);
            await context.Response.WriteAsJsonAsync(new
            {
                error = GenericErrorMessage,
                traceId = context.TraceIdentifier
            });
        }
    }

    public static void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        if (context.Request.IsHttps)
            headers.StrictTransportSecurity = StrictTransportSecurity;

        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = Nosniff;
        headers["Referrer-Policy"] = ReferrerPolicy;
        headers["X-Robots-Tag"] = RobotsTag;
        headers.CacheControl = CacheControl;
        headers.XFrameOptions = FrameOptions;
    }
}

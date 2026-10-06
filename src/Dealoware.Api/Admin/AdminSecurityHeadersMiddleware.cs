using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Adds HSTS, CSP, and browser hardening headers on every admin-host response
/// (pages, <c>/admin/api/...</c>, errors, 404s, signed-out <c>/admin/sign-in</c>,
/// and <c>/admin/auth/</c> static files). Does not run for other hosts.
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

    private readonly RequestDelegate _next;

    public AdminSecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, IOptions<AdminHostOptions> options)
    {
        if (options.Value.IsAllowedHost(context.Request.Host.Host))
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                var headers = response.Headers;
                headers.StrictTransportSecurity = StrictTransportSecurity;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = Nosniff;
                headers["Referrer-Policy"] = ReferrerPolicy;
                headers["X-Robots-Tag"] = RobotsTag;
                headers.CacheControl = CacheControl;
                return Task.CompletedTask;
            }, context.Response);
        }

        return _next(context);
    }
}

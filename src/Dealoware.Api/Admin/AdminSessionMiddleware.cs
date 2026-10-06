using System.Security.Claims;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Fail-closed session gate for /admin routes after the host gate (C7).
/// r3 §2 + §10 C1–C8: exact exemptions, AF on POST, generic 401 otherwise.
/// </summary>
public sealed class AdminSessionMiddleware
{
    public const string AuthenticationType = "AdminSession";
    public const string CoreOwnerRole = "CoreOwner";
    public const string PolicyName = "CoreOwner";

    private readonly RequestDelegate _next;

    public AdminSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        IOptions<CoreOwnerOptions> coreOwnerOptions,
        IAdminAuthTokenGate tokenGate)
    {
        if (!context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
            await _next(context);
            return;
        }

        var raw = AdminAuthPaths.RawPath(context);
        if (string.Equals(raw, AdminAuthPaths.StatsBare, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Location = AdminAuthPaths.Stats;
            return;
        }

        if (await AdminAuthPaths.IsExemptAsync(context, tokenGate, context.RequestAborted))
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                AdminAntiForgery.IssueOnGet(context);
                await _next(context);
                return;
            }

            if (HttpMethods.IsPost(context.Request.Method) && !AdminAntiForgery.TryValidate(context))
            {
                await AdminDeny.WriteUnauthorizedAsync(context);
                return;
            }

            await _next(context);
            return;
        }

        if (!TryGetSessionId(context, out var sessionId))
        {
            await AdminDeny.WriteUnauthorizedAsync(context);
            return;
        }

        var session = await sessions.GetByIdAsync(sessionId, context.RequestAborted);
        var ownerEmail = coreOwnerOptions.Value.Email;

        if (session is null
            || session.IsExpired()
            || !session.TotpVerified
            || string.IsNullOrWhiteSpace(ownerEmail)
            || !string.Equals(session.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
        {
            await AdminDeny.WriteUnauthorizedAsync(context);
            return;
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, session.Email),
                new Claim(ClaimTypes.Email, session.Email),
                new Claim(ClaimTypes.Role, CoreOwnerRole),
                new Claim("role", CoreOwnerRole),
                new Claim("principal", CoreOwnerRole)
            ],
            AuthenticationType);
        context.User = new ClaimsPrincipal(identity);

        session.UpdateActivity();
        try
        {
            await sessions.SaveChangesAsync(context.RequestAborted);
        }
        catch
        {
            // Sliding renewal is best-effort; do not fail a valid session on activity write.
        }

        await _next(context);
    }

    private static bool TryGetSessionId(HttpContext context, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out sessionId);
    }
}

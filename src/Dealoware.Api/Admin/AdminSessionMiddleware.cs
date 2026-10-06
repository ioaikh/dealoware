using System.Security.Claims;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Fail-closed session gate for all /admin routes.
/// Missing, invalid, expired, TOTP-unverified, or non-CoreOwner session → generic 401.
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
        IOptions<CoreOwnerOptions> coreOwnerOptions)
    {
        if (!context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
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

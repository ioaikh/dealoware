using System.Security.Claims;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Antiforgery;
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
        IOptions<CoreOwnerOptions> coreOwnerOptions,
        IAdminClock clock)
    {
        if (!context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        if (AdminSessionExemptions.IsAnonymousAuthAsset(context.Request.Path, method))
        {
            await _next(context);
            return;
        }

        if (AdminSessionExemptions.TryCanonicalPath(context, out var path)
            && AdminSessionExemptions.TryMatchUnconditional(path, method))
        {
            if (!await AcceptExemptPostAsync(context, method))
            {
                return;
            }

            await _next(context);
            return;
        }

        if (AdminSessionExemptions.TryCanonicalPath(context, out path)
            && AdminSessionExemptions.TryMatchTokenRow(path, method, out var requiredKind))
        {
            var tokens = context.RequestServices.GetRequiredService<IAdminAuthTokenStore>();
            if (await AdminSessionExemptions.HasValidTokenAsync(
                    context, requiredKind, tokens, clock, context.RequestAborted)
                && await AcceptExemptPostAsync(context, method))
            {
                await _next(context);
                return;
            }

            await AdminDeny.WriteUnauthorizedAsync(context);
            return;
        }

        if (!TryGetSessionId(context, out var sessionId))
        {
            await AdminDeny.WriteUnauthorizedAsync(context);
            return;
        }

        var session = await sessions.GetByIdAsync(sessionId, context.RequestAborted);
        var ownerEmail = coreOwnerOptions.Value.Email;

        var now = clock.UtcNow;
        if (session is null
            || session.IsExpired(now)
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

        session.UpdateActivity(now);
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

    private static async Task<bool> AcceptExemptPostAsync(HttpContext context, string method)
    {
        if (!HttpMethods.IsPost(method))
        {
            return true;
        }

        try
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            await AdminDeny.WriteUnauthorizedAsync(context);
            return false;
        }
    }

    private static bool TryGetSessionId(HttpContext context, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out sessionId);
    }
}

using System.Security.Claims;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Fail-closed session gate for all /admin routes (route-prefix note r3 §2 + §10 C1–C8).
/// Consulted only after AdminHostMiddleware accepts the admin host (C7).
/// Exempts only exact canonical path+method pairs. Token rows require a server-validated
/// pending token. /admin/api/** is never exempt except the listed auth API rows.
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
        IAdminCoreOwnerAccountRepository accounts,
        IAntiforgery antiforgery,
        IOptions<CoreOwnerOptions> coreOwnerOptions)
    {
        if (!context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
            await _next(context);
            return;
        }

        if (await IsSignedOutExemptAsync(context, accounts, antiforgery))
        {
            await _next(context);
            return;
        }

        if (context.Items.ContainsKey(AntiForgeryDeniedKey))
            return;

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

    internal static async Task<bool> IsSignedOutExemptAsync(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAntiforgery antiforgery)
    {
        if (!AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical))
            return false;

        if (HttpMethods.IsGet(context.Request.Method)
            && AdminSignedOutExemptions.IsAnonymousAuthAsset(canonical))
        {
            return true;
        }

        var rule = AdminSignedOutExemptions.FindRule(canonical, context.Request.Method);
        if (rule is null)
            return false;

        if (HttpMethods.IsPost(context.Request.Method)
            && !await HasValidAntiForgeryAsync(context, canonical, antiforgery))
        {
            return false;
        }

        return rule.Value.Token switch
        {
            AdminSignedOutExemptions.TokenKind.None => true,
            AdminSignedOutExemptions.TokenKind.PendingSignIn =>
                await AdminSignedOutExemptions.HasValidPendingTokenAsync(
                    context, accounts, context.RequestAborted),
            // Bootstrap / reset tokens are owned by Steps 2 and 4. Missing token → not exempt.
            _ => false
        };
    }

    private static async Task<bool> HasValidAntiForgeryAsync(
        HttpContext context,
        string canonical,
        IAntiforgery antiforgery)
    {
        var hasSessionCookie = !string.IsNullOrWhiteSpace(context.Request.Cookies[AdminSessionCookie.Name]);
        if (AdminSignedOutExemptions.IsSignOut(canonical) && !hasSessionCookie)
            return true;

        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            if (AdminSignedOutExemptions.IsAuthPost(canonical))
                await AdminSignInDeny.WriteFailureAsync(context);
            else
                await AdminDeny.WriteUnauthorizedAsync(context);
            context.Items[AntiForgeryDeniedKey] = true;
            return false;
        }
    }

    internal const string AntiForgeryDeniedKey = "AdminAntiForgeryDenied";

    private static bool TryGetSessionId(HttpContext context, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out sessionId);
    }
}

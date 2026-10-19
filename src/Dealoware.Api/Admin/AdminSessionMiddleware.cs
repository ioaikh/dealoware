using System.Security.Claims;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Fail-closed session gate for all /admin routes (route-prefix note r3 §2 + §10 C1–C8).
/// Consulted only after AdminHostMiddleware accepts the admin host (C7).
/// State-changing methods default-deny: Origin (R13) plus keyed token (R9/R15/R16).
/// Exceptions are an explicit allow-list (empty). Sign-out with no session still
/// requires the token. Rejects are a generic 403 (R17).
/// Exempts only exact canonical path+method pairs for the session cookie.
/// Missing, invalid, expired, TOTP-unverified, or non-CoreOwner session → generic 401.
/// </summary>
public sealed class AdminSessionMiddleware
{
    public const string AuthenticationType = "AdminSession";
    public const string CoreOwnerRole = "CoreOwner";
    public const string PolicyName = "CoreOwner";
    public const string CsrfMissing = "csrf_missing";
    public const string CsrfInvalid = "csrf_invalid";

    /// <summary>
    /// State-changing /admin paths that skip the CSRF token. Origin is still required.
    /// Empty: coverage defaults to deny.
    /// </summary>
    internal static readonly HashSet<string> AntiForgeryTokenAllowList = new(StringComparer.OrdinalIgnoreCase);

    private readonly RequestDelegate _next;

    public AdminSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        IAdminCoreOwnerAccountRepository accounts,
        AdminAntiForgeryService antiforgery,
        IOptions<CoreOwnerOptions> coreOwnerOptions,
        ILogger<AdminSessionMiddleware> logger)
    {
        if (!context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
            await _next(context);
            return;
        }

        if (AdminOriginGate.IsStateChanging(context.Request))
        {
            if (!AdminOriginGate.TryValidate(context, out var originReason))
            {
                logger.LogWarning("Admin request rejected: {ReasonClass}", originReason);
                await AdminDeny.WriteForbiddenAsync(context);
                return;
            }

            if (!IsAntiForgeryAllowListed(context) && !antiforgery.TryValidate(context))
            {
                var csrfReason = HasPresentedAntiForgery(context) ? CsrfInvalid : CsrfMissing;
                logger.LogWarning("Admin request rejected: {ReasonClass}", csrfReason);
                await AdminDeny.WriteForbiddenAsync(context);
                return;
            }
        }

        if (await IsSignedOutExemptAsync(context, accounts))
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

    internal static async Task<bool> IsSignedOutExemptAsync(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts)
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

        return rule.Value.Token switch
        {
            AdminSignedOutExemptions.TokenKind.None => true,
            AdminSignedOutExemptions.TokenKind.PendingSignIn =>
                await AdminSignedOutExemptions.HasValidPendingTokenAsync(
                    context, accounts, context.RequestAborted),
            // Bootstrap tokens are owned by Step 2. Missing token → not exempt.
            _ => false
        };
    }

    private static bool IsAntiForgeryAllowListed(HttpContext context)
        => AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical)
           && AntiForgeryTokenAllowList.Contains(canonical);

    private static bool HasPresentedAntiForgery(HttpContext context)
        => context.Request.Headers.ContainsKey(AdminAntiForgeryService.HeaderName)
           || context.Request.Cookies.ContainsKey(AdminAntiForgeryService.CookieName);

    private static bool TryGetSessionId(HttpContext context, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out sessionId);
    }
}

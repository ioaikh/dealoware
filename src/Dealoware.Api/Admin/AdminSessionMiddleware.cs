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
        IAdminClock clock,
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

        if (await IsSignedOutExemptAsync(context, accounts, antiforgery, clock))
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

    internal static async Task<bool> IsSignedOutExemptAsync(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        AdminAntiForgeryService antiforgery,
        IAdminClock clock)
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

        if (rule.Value.Token == AdminSignedOutExemptions.TokenKind.PendingSignIn
            && await TryWritePendingThrottleAsync(context, clock))
        {
            return false;
        }

        return rule.Value.Token switch
        {
            AdminSignedOutExemptions.TokenKind.None => true,
            AdminSignedOutExemptions.TokenKind.PendingSignIn =>
                await AdminSignedOutExemptions.HasValidPendingTokenAsync(
                    context, accounts, context.RequestAborted)
                || await HasStep5TokenAsync(context, AdminAuthToken.KindPending, clock),
            // Bootstrap / reset tokens are owned by Steps 2 and 4. Missing token → not exempt.
            _ => false
        };
    }

    private static async Task<bool> TryWritePendingThrottleAsync(HttpContext context, IAdminClock clock)
    {
        var lockouts = context.RequestServices.GetRequiredService<IAdminLockoutStore>();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var now = clock.UtcNow;
        var active = await lockouts.GetActiveLockoutAsync(
            AdminAuthScopes.LoginIp, ip, now, context.RequestAborted);
        if (active is null)
        {
            var prior = await lockouts.CountFailuresAsync(
                AdminAuthScopes.LoginIp,
                ip,
                now - AdminAuthService.FailureWindow,
                context.RequestAborted);
            if (prior < AdminAuthService.IpFailureLimit)
            {
                return false;
            }
        }

        await AdminSignInDeny.WriteThrottledAsync(context);
        context.Items[AntiForgeryDeniedKey] = true;
        return true;
    }

    private static async Task<bool> HasStep5TokenAsync(
        HttpContext context,
        string kind,
        IAdminClock clock)
    {
        var tokens = context.RequestServices.GetService<IAdminAuthTokenStore>();
        if (tokens is null)
        {
            return false;
        }

        return await AdminSessionExemptions.HasValidTokenAsync(
            context, kind, tokens, clock, context.RequestAborted);
    }

    private static async Task<bool> HasValidAntiForgeryAsync(
        HttpContext context,
        string canonical,
        AdminAntiForgeryService antiforgery)
    {
        var hasSessionCookie = !string.IsNullOrWhiteSpace(context.Request.Cookies[AdminSessionCookie.Name]);
        if (AdminSignedOutExemptions.IsSignOut(canonical) && !hasSessionCookie)
            return true;

        if (antiforgery.TryValidate(context))
            return true;

        if (AdminSignedOutExemptions.IsAuthPost(canonical))
            await AdminSignInDeny.WriteFailureAsync(context);
        else
            await AdminDeny.WriteUnauthorizedAsync(context);
        context.Items[AntiForgeryDeniedKey] = true;
        return false;
    }

    internal const string AntiForgeryDeniedKey = "AdminAntiForgeryDenied";

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

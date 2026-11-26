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
/// An accepted request renews the idle window, save one: GET of
/// <see cref="AdminSessionEndpoints.StatusPath"/>, which only reads how long
/// the session has left (<see cref="IsNonRenewingRead"/>).
/// Two answers differ, both for a browser's GET of a page, never for
/// /admin/api/**, a static file or any other method:
/// a GET of one of the four pages that are served only during a pending
/// sign-in (the code page, the recovery page, authenticator set-up and its
/// recovery codes), asked for without one, is redirected to step 1; and a GET
/// of a signed-in page without a valid session is redirected to sign-in with
/// the "session ended" note, the page kept for after sign-in
/// (<see cref="AdminReturnCookie"/>).
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

            if (!IsAntiForgeryAllowListed(context) && !await HasAntiForgeryTokenAsync(context, antiforgery))
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

        // A page that is served only during a pending sign-in, opened with
        // none (it ran out, was used, or there never was one): send the
        // browser to step 1 rather than show it the gate's JSON. Nothing is
        // exempted by this. No handler runs, no cookie is set or cleared, and
        // the target is a constant. Only the GET of those four pages is
        // answered this way: their POSTs, every /admin/api/ path and every
        // other page keep the 401 below, and a throttled address has had its
        // 429 above.
        if (IsPendingSignInPageGet(context))
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = AdminSignInPages.SentBackLocation;
            return;
        }

        if (!TryGetSessionId(context, out var sessionId))
        {
            await DenyAsync(context);
            return;
        }

        var session = await sessions.GetByIdAsync(sessionId, context.RequestAborted);
        var ownerEmail = coreOwnerOptions.Value.Email;

        var now = clock.UtcNow;
        if (!IsAcceptedSession(session, ownerEmail, now))
        {
            await DenyAsync(context);
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

        // Every accepted request renews the idle window, except the one read
        // that only asks how long the session has left: a page's timer reads
        // it, and a timer must never keep a session alive (TD-ADM-176). That
        // read is checked exactly as above; only the renewal is skipped.
        if (!IsNonRenewingRead(context))
        {
            session!.UpdateActivity(now);
            try
            {
                await sessions.SaveChangesAsync(context.RequestAborted);
            }
            catch
            {
                // Sliding renewal is best-effort; do not fail a valid session on activity write.
            }
        }

        await _next(context);
    }

    /// <summary>
    /// GET of <see cref="AdminSessionEndpoints.StatusPath"/>, by the exact
    /// canonical path, and nothing else: the one accepted request that does
    /// not renew the idle window. It lets nothing through that the gate
    /// would refuse. Any other spelling of the path, and any other method,
    /// renews as every request does.
    /// </summary>
    internal static bool IsNonRenewingRead(HttpContext context)
        => HttpMethods.IsGet(context.Request.Method)
           && AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical)
           && string.Equals(canonical, AdminSessionEndpoints.StatusPath, StringComparison.Ordinal);

    /// <summary>
    /// The one rule for a session the gate lets through: it exists, has not run
    /// out (30 minutes idle, 8 hours in all), passed its second step, and is
    /// the configured owner's.
    /// </summary>
    internal static bool IsAcceptedSession(AdminSession? session, string? ownerEmail, DateTimeOffset now)
        => session is not null
           && !session.IsExpired(now)
           && session.TotpVerified
           && !string.IsNullOrWhiteSpace(ownerEmail)
           && string.Equals(session.Email, ownerEmail, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// No valid session. A browser's GET of a signed-in page is sent to
    /// sign-in with the "session ended" note, and the page it asked for is
    /// kept for after sign-in when it is one a sign-in may return to. Nothing
    /// is exempted by this: no handler runs, the target is a constant, and
    /// the only cookie set is the kept page (or its removal). Every other
    /// request keeps the 401: every /admin/api/ path, every static-file path,
    /// and every method but GET.
    /// </summary>
    private static Task DenyAsync(HttpContext context)
    {
        if (!IsSignedInPageGet(context, out var canonical))
            return AdminDeny.WriteUnauthorizedAsync(context);

        AdminReturnCookie.Remember(context, canonical);
        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location = AdminSignInPages.SessionEndedLocation;
        return Task.CompletedTask;
    }

    /// <summary>Under /admin and never under them: the JSON routes, and the two static-file folders.</summary>
    private static readonly string[] NotPagePrefixes =
    [
        "/admin/api",
        AdminSignedOutExemptions.AuthStaticPrefix,
        AdminSignedInPage.AssetPrefix
    ];

    /// <summary>
    /// GET of a page path under /admin, by the canonical path the exemption
    /// rules use (a path that does not canonicalise is not one).
    /// </summary>
    internal static bool IsSignedInPageGet(HttpContext context, out string canonical)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !AdminPathCanonicalizer.TryGetComparablePath(context, out canonical))
        {
            canonical = string.Empty;
            return false;
        }

        if (!string.Equals(canonical, AdminHostMiddleware.AdminPathPrefix, StringComparison.OrdinalIgnoreCase)
            && !canonical.StartsWith(AdminHostMiddleware.AdminPathPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var prefix in NotPagePrefixes)
        {
            if (string.Equals(canonical, prefix, StringComparison.OrdinalIgnoreCase)
                || canonical.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The pages whose GET the exemption rules let through only with a
    /// pending sign-in: S-A2, S-A3, S-A6 and S-A7.
    /// </summary>
    private static readonly string[] PendingSignInPages =
    [
        AdminSignedOutExemptions.SignInCode,
        AdminSignedOutExemptions.SignInRecovery,
        AdminSignedOutExemptions.SetupAuthenticator,
        AdminSignedOutExemptions.SetupRecoveryCodes
    ];

    /// <summary>
    /// GET of one of those pages, by the same exact canonical path the
    /// exemption rules use. A trailing slash, an extra segment or an encoded
    /// separator is not one of them.
    /// </summary>
    private static bool IsPendingSignInPageGet(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical))
        {
            return false;
        }

        foreach (var page in PendingSignInPages)
        {
            if (string.Equals(canonical, page, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
            // Unknown token kind → not exempt.
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

        if (await HasAntiForgeryTokenAsync(context, antiforgery))
            return true;

        if (AdminSignedOutExemptions.IsAuthPost(canonical))
            await AdminSignInDeny.WriteFailureAsync(context);
        else
            await AdminDeny.WriteUnauthorizedAsync(context);
        context.Items[AntiForgeryDeniedKey] = true;
        return false;
    }

    internal const string AntiForgeryDeniedKey = "AdminAntiForgeryDenied";

    /// <summary>
    /// The anti-forgery token in the X-CSRF-TOKEN header; for POST
    /// /admin/sign-out alone, also the sign-out form's hidden field, so that
    /// signing out works with JavaScript off. No other route reads a form token.
    /// </summary>
    private static async Task<bool> HasAntiForgeryTokenAsync(HttpContext context, AdminAntiForgeryService antiforgery)
    {
        // A sign-out let past the origin check by its Sec-Fetch-* headers is the browser's own
        // form submission: its token must be the form's field, never a header.
        if (context.Items.ContainsKey(AdminOriginGate.NullOriginSignOutKey))
            return await antiforgery.TryValidateSignOutFormAsync(context);

        return antiforgery.TryValidate(context) || await antiforgery.TryValidateSignOutFormAsync(context);
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

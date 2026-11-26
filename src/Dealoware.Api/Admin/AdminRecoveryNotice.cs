using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// The UXR-A40 recovery-code banner (TD-ADM-UI-auth-16, T04).
/// <para>
/// The signal is the server's: a session opened with a recovery code at sign-in
/// step 2 gets one <see cref="AdminSessionRecoveryNotice"/> row, written in the
/// same save as the session (<see cref="Add"/>). While that row exists, every
/// signed-in page of that session shows the banner: one sentence with the
/// number of unused recovery codes, read from the store for each page (never
/// worked out by the browser), the "Manage recovery codes" link to S-A12, and
/// "Dismiss". Dismissing deletes the row (<see cref="DismissPath"/>), so the
/// banner is gone for the rest of that session; the row goes with the session
/// in any case, so a later sign-in with an authenticator code never shows it.
/// </para>
/// <para>
/// Nothing here is exempt from the session gate: the lookup runs only for a
/// signed-in page GET the gate has let through, and the dismiss POST is behind
/// the gate, the Origin check and the anti-forgery token like every other
/// admin POST. Neither the banner nor its reply holds a code.
/// </para>
/// </summary>
public static class AdminRecoveryNotice
{
    public const string DismissPath = AdminSecuritySettingsEndpoints.BasePath + "/recovery-notice/dismiss";

    /// <summary>Dismiss, sent from any signed-in page that shows the banner.</summary>
    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/recovery-notice.js";

    private const string ItemKey = "Dealoware.Admin.RecoveryNotice";

    private sealed record Pending(int CodesLeft);

    /// <summary>Adds the banner row of a new session to the caller's unit of work; the caller saves it with the session.</summary>
    public static void Add(HttpContext http, Guid sessionId, DateTimeOffset now)
    {
        var db = http.RequestServices.GetRequiredService<DealowareDbContext>();
        db.AdminSessionRecoveryNotices.Add(AdminSessionRecoveryNotice.Create(sessionId, now));
    }

    /// <summary>
    /// After the session gate: for a signed-in page GET, finds whether this
    /// session's banner is still to be shown and, if so, how many unused codes
    /// the owner has. <see cref="AdminSignedInPage"/> reads it from the request.
    /// </summary>
    public static IApplicationBuilder UseAdminRecoveryNotice(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole)
                && AdminSessionMiddleware.IsSignedInPageGet(context, out _)
                && Guid.TryParse(context.Request.Cookies[AdminSessionCookie.Name], out var sessionId))
            {
                var ct = context.RequestAborted;
                var db = context.RequestServices.GetRequiredService<DealowareDbContext>();
                if (await db.AdminSessionRecoveryNotices.AsNoTracking().AnyAsync(n => n.SessionId == sessionId, ct))
                {
                    var email = AdminAuthService.NormalizeEmail(context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
                    var accountId = await db.AdminCoreOwnerAccounts.AsNoTracking()
                        .Where(a => a.Email == email)
                        .Select(a => (Guid?)a.Id)
                        .FirstOrDefaultAsync(ct);
                    if (accountId is { } id)
                    {
                        var store = context.RequestServices.GetRequiredService<AdminSecuritySettingsStore>();
                        context.Items[ItemKey] = new Pending(await store.CountUnusedRecoveryCodesAsync(id, ct));
                    }
                }
            }

            await next(context);
        });

    public static void MapAdminRecoveryNotice(this WebApplication app)
    {
        app.MapPost(DismissPath, DismissAsync)
            .WithName("AdminRecoveryNoticeDismiss")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>Deletes this session's banner row. The same answer whether or not there was one.</summary>
    private static async Task<IResult> DismissAsync(HttpContext http, DealowareDbContext db)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
        if (!Guid.TryParse(http.Request.Cookies[AdminSessionCookie.Name], out var sessionId))
            return AdminDeny.UnauthorizedResult();

        await db.AdminSessionRecoveryNotices
            .Where(n => n.SessionId == sessionId)
            .ExecuteDeleteAsync(http.RequestAborted);
        return Results.Json(new { dismissed = true });
    }

    /// <summary>The banner's markup, or "" when this page has none to show. Constants and a number only.</summary>
    public static string Markup(HttpContext http)
    {
        if (http.Items[ItemKey] is not Pending pending)
            return string.Empty;

        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            "<div id=\"recovery-notice\" class=\"recovery-notice\"" +
            $" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\">" +
            $"<p id=\"recovery-notice-text\">{E(AdminSecuritySettingsPageCopy.RecoveryNotice(pending.CodesLeft))}</p>" +
            "<p class=\"recovery-notice-actions\">" +
            $"<a id=\"recovery-notice-manage\" href=\"{AdminSignedInPage.SecuritySettings}\">" +
            $"{E(AdminSecuritySettingsPageCopy.ManageRecoveryCodes)}</a>" +
            // Shown by its script: without JavaScript there is nothing it could send.
            "<button type=\"button\" id=\"recovery-notice-dismiss\" class=\"secondary\" hidden" +
            " aria-describedby=\"recovery-notice-text\">" +
            $"{E(AdminSecuritySettingsPageCopy.Dismiss)}</button>" +
            "</p></div>";
    }

    /// <summary>The banner's script, when the page shows the banner.</summary>
    public static IEnumerable<string> Scripts(HttpContext http)
        => http.Items[ItemKey] is Pending ? [ScriptPath] : [];
}

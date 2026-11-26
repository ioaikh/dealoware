using System.Globalization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// How long the signed-in session has left, and "Stay signed in" (Spec §8.7;
/// TD-ADM-176, TD-ADM-UI-auth-14, TD-ADM-UI-auth-24). Both run behind the
/// session gate like every /admin/api/ route: no exemption.
/// <list type="bullet">
/// <item><c>GET <see cref="StatusPath"/></c> reads the session's two ends.
/// It is the one accepted request the gate does not count as activity
/// (<see cref="AdminSessionMiddleware.IsNonRenewingRead"/>), so a page's
/// timer can ask without keeping the session alive.</item>
/// <item><c>POST <see cref="StaySignedInPath"/></c> renews the idle window
/// only. The gate requires the Origin and the anti-forgery header for it, as
/// for every POST. The 8-hour end is never moved or issued anew, and nothing
/// the client sends (body, header, cookie) is read.</item>
/// </list>
/// Both answer 200 with:
/// <code>
/// { "idleExpiresAt": "…Z", "absoluteExpiresAt": "…Z",
///   "idleSecondsLeft": n, "absoluteSecondsLeft": m }
/// </code>
/// The session ends at whichever comes first. The seconds are whole seconds
/// left by the server's clock, rounded down and never below 0, so a page
/// needs no clock of the server's. Without a valid session the gate answers
/// its 401 first. Not audited: the test design asks for no audit of either.
/// </summary>
public static class AdminSessionEndpoints
{
    public const string StatusPath = "/admin/api/session";
    public const string StaySignedInPath = "/admin/api/session/stay-signed-in";

    public static void MapAdminSessionEndpoints(this WebApplication app)
    {
        app.MapGet(StatusPath, GetStatusAsync)
            .WithName("AdminSessionStatus")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(StaySignedInPath, StaySignedInAsync)
            .WithName("AdminStaySignedIn")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetStatusAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        IOptions<CoreOwnerOptions> coreOwner,
        IAdminClock clock)
    {
        var now = clock.UtcNow;
        var session = await AcceptedSessionAsync(context, sessions, coreOwner, now);
        return session is null ? AdminDeny.UnauthorizedResult() : Reply(session, now);
    }

    private static async Task<IResult> StaySignedInAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        IOptions<CoreOwnerOptions> coreOwner,
        IAdminClock clock)
    {
        var now = clock.UtcNow;
        var session = await AcceptedSessionAsync(context, sessions, coreOwner, now);
        if (session is null)
            return AdminDeny.UnauthorizedResult();

        // The gate has renewed it already, best-effort. Here the renewal is the
        // point of the request, so it is written, and a failed write fails it.
        session.UpdateActivity(now);
        await sessions.SaveChangesAsync(context.RequestAborted);
        return Reply(session, now);
    }

    /// <summary>The session the gate let through, checked again by the gate's own rule.</summary>
    private static async Task<AdminSession?> AcceptedSessionAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        IOptions<CoreOwnerOptions> coreOwner,
        DateTimeOffset now)
    {
        var raw = context.Request.Cookies[AdminSessionCookie.Name];
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var id))
            return null;

        var session = await sessions.GetByIdAsync(id, context.RequestAborted);
        return AdminSessionMiddleware.IsAcceptedSession(session, coreOwner.Value.Email, now) ? session : null;
    }

    private static IResult Reply(AdminSession session, DateTimeOffset now)
        => Results.Json(new
        {
            idleExpiresAt = Utc(session.IdleExpiresAt),
            absoluteExpiresAt = Utc(session.AbsoluteExpiresAt),
            idleSecondsLeft = SecondsLeft(session.IdleExpiresAt, now),
            absoluteSecondsLeft = SecondsLeft(session.AbsoluteExpiresAt, now)
        });

    internal static long SecondsLeft(DateTimeOffset end, DateTimeOffset now)
        => end <= now ? 0 : (long)Math.Floor((end - now).TotalSeconds);

    private static string Utc(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
}

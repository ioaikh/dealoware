using System.Globalization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Admin;

/// <summary>
/// The signed-in console pages of T04 piece 1, each one call to the shared
/// shell (<see cref="AdminSignedInPage"/>):
/// <list type="bullet">
/// <item>S-B1 Stats at /admin/ (and /admin, which is sent there): the five
/// Spec §6 totals, written into the page by the server, with the time they
/// were counted in ET; each total links to its list, filtered as it counts
/// (TD-ADM-UI-na-B09), Refresh counts again, and a total that cannot be
/// counted says so with Retry while the others stay;</item>
/// <item>the four lists and their detail pages (<see cref="AdminListPages"/>);</item>
/// <item>S-A12 Security settings, routed and in the nav, saying it is not
/// built yet (a later piece of T04 fills it);</item>
/// <item>S-D3 for any other page path under /admin/ that no route answers:
/// "We can't find that page." with a 404 (<see cref="UseAdminPageNotFound"/>).</item>
/// </list>
/// Every one of them is reached only through the session gate: without a
/// valid session the gate sends a browser's GET to sign-in before any of
/// these handlers runs, and nothing else gets past it.
/// S-D4 ("You can't do that.") is not built: neither the Spec nor the Story
/// says when it shows, and no signed-in page here is refused.
/// </summary>
public static class AdminConsolePages
{
    public static void MapAdminConsolePages(this WebApplication app)
    {
        // "/admin/" also matches "/admin": the handler sends that one to "/admin/".
        Page(app.MapGet(AdminSignedInPage.Stats, GetStats));
        Page(app.MapGet(AdminSignedInPage.SecuritySettings, (HttpContext http, AdminAntiForgeryService af)
            => NotBuiltYet(http, af, AdminConsoleCopy.NavSecuritySettings, AdminConsoleSection.SecuritySettings)));
        app.MapAdminListPages();
    }

    /// <summary>
    /// S-D3. Runs after the session gate and routing: a GET of a page path
    /// that no route answers, from the signed-in owner, gets the page. Not a
    /// route of its own, so every route keeps its answers exactly (a JSON
    /// route asked with the wrong method still gets its 405), and
    /// /admin/api/, /admin/auth/ and /admin/assets/ keep their plain 404.
    /// Without a session the gate has already sent the browser to sign-in.
    /// </summary>
    public static IApplicationBuilder UseAdminPageNotFound(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is null
                && context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole)
                && AdminSessionMiddleware.IsSignedInPageGet(context, out _))
            {
                var antiforgery = context.RequestServices.GetRequiredService<AdminAntiForgeryService>();
                await NotFound(context, antiforgery).ExecuteAsync(context);
                return;
            }

            await next(context);
        });

    private static void Page(RouteHandlerBuilder builder)
        => builder
            .WithTags("AdminConsole")
            .ExcludeFromDescription()
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

    private static async Task<IResult> GetStats(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        DealowareDbContext db,
        IAdminClock clock,
        ILoggerFactory logs,
        CancellationToken cancellationToken)
    {
        if (!(http.Request.Path.Value ?? string.Empty).EndsWith('/'))
        {
            AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
            return Results.Redirect(AdminSignedInPage.Stats);
        }

        // Each total on its own: one that cannot be counted does not take the others with it.
        var counts = new int?[AdminReadEndpoints.TotalCounts.Count];
        for (var i = 0; i < counts.Length; i++)
        {
            try
            {
                counts[i] = await AdminReadEndpoints.TotalCounts[i](db, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logs.CreateLogger(typeof(AdminConsolePages)).LogError("Admin stats total {Index} could not be counted: {Fault}", i, ex.GetType().Name);
            }
        }

        var asOf = clock.UtcNow;
        return AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(AdminConsoleCopy.StatsHeading),
            Heading = AdminConsoleCopy.StatsHeading,
            Section = AdminConsoleSection.Stats,
            BodyHtml = StatsBody(counts, asOf)
        });
    }

    /// <summary>The five totals: id, label, and the list each opens, filtered as it counts (TD-ADM-UI-na-B09).</summary>
    internal static readonly IReadOnlyList<(string Id, string Label, string List)> StatTiles =
    [
        ("stat-participants", AdminConsoleCopy.StatParticipants, AdminSignedInPage.Participants),
        ("stat-open-negotiations", AdminConsoleCopy.StatOpenNegotiations, AdminSignedInPage.Negotiations + "?status=Open"),
        ("stat-offers", AdminConsoleCopy.StatOffers, AdminSignedInPage.Offers),
        ("stat-accepts", AdminConsoleCopy.StatAccepts, AdminSignedInPage.Offers + "?status=Accepted"),
        ("stat-declines", AdminConsoleCopy.StatDeclines, AdminSignedInPage.Offers + "?status=Declined")
    ];

    private static string StatsBody(IReadOnlyList<int?> counts, DateTimeOffset asOf)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);
        static string Count(int n) => n.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

        var html = new System.Text.StringBuilder("<dl class=\"stats\">");
        for (var i = 0; i < StatTiles.Count; i++)
        {
            var (id, label, list) = StatTiles[i];
            html.Append($"<div class=\"stat\"><dt id=\"{id}-label\"><a href=\"{list}\">{E(label)}</a></dt>");
            html.Append(counts[i] is { } count
                ? $"<dd id=\"{id}\">{Count(count)}</dd>"
                : $"<dd id=\"{id}\" class=\"stat-error\">{E(AdminConsoleCopy.CouldNotLoad)} <a href=\"{AdminSignedInPage.Stats}\">{E(AdminConsoleCopy.Retry)}</a></dd>");
            html.Append("</div>");
        }

        html.Append("</dl>");
        html.Append($"<p id=\"stats-as-of\" class=\"as-of\">{E(AdminConsoleCopy.AsOfPrefix)}{AdminListPages.TimeHtml(asOf)}</p>");
        html.Append($"<p><a id=\"stats-refresh\" class=\"button-link secondary\" href=\"{AdminSignedInPage.Stats}\">{E(AdminConsoleCopy.Refresh)}</a></p>");
        return html.ToString();
    }

    private static IResult NotBuiltYet(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        string screen,
        AdminConsoleSection section)
        => AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(screen),
            Heading = screen,
            Section = section,
            BodyHtml = $"<p class=\"lede\">{AdminSignedOutPage.Encode(AdminConsoleCopy.NotBuiltYet)}</p>"
        });

    internal static IResult NotFound(HttpContext http, AdminAntiForgeryService antiforgery)
        => AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.NotFoundTitle,
            Heading = AdminConsoleCopy.NotFoundHeading,
            Section = AdminConsoleSection.None,
            StatusCode = StatusCodes.Status404NotFound,
            BodyHtml =
                $"<p><a id=\"go-to-stats\" href=\"{AdminSignedInPage.Stats}\">" +
                $"{AdminSignedOutPage.Encode(AdminConsoleCopy.GoToStats)}</a></p>"
        });
}

/// <summary>
/// A time as the console shows it: America/New_York with the "ET" label,
/// for example "Oct 5, 2026, 7:55 PM ET" (TD-ADM-UI-na-B15). Where the
/// system has no time-zone data the US rule since 2007 is applied.
/// </summary>
public static class EasternTime
{
    private static readonly CultureInfo UsEnglish = CultureInfo.GetCultureInfo("en-US");

    public static string Format(DateTimeOffset at)
        => ToEastern(at).ToString("MMM d, yyyy, h:mm tt", UsEnglish) + " ET";

    /// <summary>The instant an ET calendar day begins (midnight in New York), in UTC.</summary>
    public static DateTimeOffset StartOfDayUtc(DateOnly day)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // New York is four hours behind UTC in daylight time, five otherwise: the one that maps back is the one.
        foreach (var hours in new[] { 4, 5 })
        {
            var utc = new DateTimeOffset(midnight.AddHours(hours), TimeSpan.Zero);
            if (ToEastern(utc) == midnight)
                return utc;
        }

        return new DateTimeOffset(midnight.AddHours(5), TimeSpan.Zero);
    }

    public static DateTime ToEastern(DateTimeOffset at)
    {
        TimeZoneInfo? zone = null;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        if (zone is not null)
            return TimeZoneInfo.ConvertTime(at, zone).DateTime;

        var utc = at.UtcDateTime;
        // Daylight time: from 2:00 local on the second Sunday of March (07:00 UTC)
        // to 2:00 local on the first Sunday of November (06:00 UTC).
        var start = NthSunday(utc.Year, 3, 2).AddHours(7);
        var end = NthSunday(utc.Year, 11, 1).AddHours(6);
        var offset = utc >= start && utc < end ? -4 : -5;
        return DateTime.SpecifyKind(utc.AddHours(offset), DateTimeKind.Unspecified);
    }

    private static DateTime NthSunday(int year, int month, int n)
    {
        var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toSunday = ((int)DayOfWeek.Sunday - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(toSunday + 7 * (n - 1));
    }
}

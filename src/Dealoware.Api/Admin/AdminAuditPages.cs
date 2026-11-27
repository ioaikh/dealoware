using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Domain.Admin;
using Dealoware.Domain.FieldAcl;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// What the audit list shows: its filters (several actions, one entity type,
/// a range of ET calendar days), its page and its page size, as read from its
/// address. Every value has passed <see cref="AdminReturnQuery.Filter"/>, so
/// a hand-edited address that names an action, an entity type or a value the
/// list does not take shows the list without it, never an error. The list has
/// one order: Time, newest first (TD-ADM-UI-na-X02).
/// </summary>
public sealed record AdminAuditState
{
    public static readonly int[] PageSizes = [50, 100, 200];

    public IReadOnlyList<string> Actions { get; init; } = [];
    public string? EntityType { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public int Offset { get; init; }
    public int Limit { get; init; } = AdminAuditListCriteria.DefaultLimit;

    /// <summary>Any filter set: an empty result is then a filter miss, not an empty log.</summary>
    public bool HasFilters => Actions.Count > 0 || EntityType is not null || From is not null || To is not null;

    /// <summary>The same list with every filter dropped; the page size stays.</summary>
    public AdminAuditState Cleared() => new() { Limit = Limit };

    /// <summary>
    /// Reads the audit list's address (the entry page carries the same
    /// query, for its way back). Checkboxes sent by a form without
    /// JavaScript repeat <c>action</c>: their values are joined first. A page
    /// size above the maximum is clamped to it. Then only what
    /// <see cref="AdminReturnQuery.Filter"/> keeps is read.
    /// </summary>
    public static AdminAuditState Read(HttpRequest request)
    {
        var raw = request.QueryString.HasValue ? request.QueryString.Value![1..] : string.Empty;
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> given;
        try
        {
            given = QueryHelpers.ParseQuery(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or UriFormatException)
        {
            return new AdminAuditState();
        }

        var pairs = new List<string>();
        foreach (var (key, values) in given)
        {
            var list = values.Where(v => v is not null).Select(v => v!).ToList();
            if (key == "action" && list.Count > 1)
                list = [string.Join(',', list)];
            foreach (var value in list)
            {
                var v = value;
                if (key == "limit"
                    && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var asked)
                    && asked > AdminAuditListCriteria.MaxLimit)
                {
                    v = AdminAuditListCriteria.MaxLimit.ToString(CultureInfo.InvariantCulture);
                }

                pairs.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(v));
            }
        }

        var kept = QueryHelpers.ParseQuery(AdminReturnQuery.Filter(AdminAuditPages.ListPath, string.Join('&', pairs)));
        string? One(string key) => kept.TryGetValue(key, out var v) ? v.ToString() : null;

        var limit = int.TryParse(One("limit"), NumberStyles.None, CultureInfo.InvariantCulture, out var l)
            ? PageSizes.First(size => l <= size || size == AdminAuditListCriteria.MaxLimit)
            : AdminAuditListCriteria.DefaultLimit;
        var offset = int.TryParse(One("offset"), NumberStyles.None, CultureInfo.InvariantCulture, out var o) ? o : 0;

        return new AdminAuditState
        {
            Actions = One("action") is { } a ? a.Split(',') : [],
            EntityType = One("entityType"),
            From = One("from"),
            To = One("to"),
            Limit = limit,
            Offset = offset / limit * limit
        };
    }

    /// <summary>
    /// The address of this state: only values that differ from the default,
    /// in the page's key order, each encoded. "" for the default list.
    /// </summary>
    public string ToQueryString()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Actions.Count > 0)
            values["action"] = string.Join(',', Actions);
        if (EntityType is not null)
            values["entityType"] = EntityType;
        if (From is not null)
            values["from"] = From;
        if (To is not null)
            values["to"] = To;
        if (Offset > 0)
            values["offset"] = Offset.ToString(CultureInfo.InvariantCulture);
        if (Limit != AdminAuditListCriteria.DefaultLimit)
            values["limit"] = Limit.ToString(CultureInfo.InvariantCulture);

        var parts = AdminAuditPages.QueryKeys
            .Where(values.ContainsKey)
            .Select(key => key + "=" + Uri.EscapeDataString(values[key]))
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    public string Url() => AdminAuditPages.ListPath + ToQueryString();

    /// <summary>The audit read's query for this state. ET calendar dates become the instants that bound them.</summary>
    public AdminAuditListCriteria ToCriteria() => new()
    {
        Offset = Offset,
        Limit = Limit,
        Sort = "-timestamp",
        Actions = Actions,
        EntityType = EntityType,
        From = Bound(From, endOfDay: false),
        To = Bound(To, endOfDay: true)
    };

    /// <summary>A kept date value as an instant: an ET date from its start, or through its end; an instant as it is.</summary>
    private static DateTimeOffset? Bound(string? value, bool endOfDay)
    {
        if (value is null)
            return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            // AdminReturnQuery keeps only these years; outside them a bound would be no bound.
            if (date.Year is < AdminReturnQuery.MinYear or > AdminReturnQuery.MaxYear)
                return null;
            return endOfDay
                ? EasternTime.StartOfDayUtc(date.AddDays(1)).AddTicks(-1)
                : EasternTime.StartOfDayUtc(date);
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? at.ToUniversalTime()
            : null;
    }
}

/// <summary>
/// T07 (A7 Step 15): the audit log viewer, two pages on the shared shell
/// (<see cref="AdminSignedInPage"/>), both reached only through the session gate.
/// <para>
/// S-D1, /admin/audit: the log as a table, written by the server from its
/// address through the audit read API's own read
/// (<see cref="AdminAuditEndpoints.ListAsync"/>), so the paging (50 by
/// default, never more than 200), the IP rule and the snapshot rules are the
/// API's. Columns Time (ET), Actor, Action, Entity type, Entity ID, Outcome;
/// newest first. Filters: action (several), entity type, a range of days.
/// There is no search box (test design OQ15). Filters, chips and paging are
/// plain links and one GET form, so the page works without JavaScript; its
/// script (<see cref="ScriptPath"/>) only moves to the new address sooner,
/// keeps the focus where it was and says "N results".
/// </para>
/// <para>
/// S-D2, /admin/audit/{id}: one entry, with its before and after snapshots
/// as text, field by field, and the word "Changed" on each field that
/// differs. The first characters of the keyed hash of the IP show here only.
/// An id no entry has, like an address no route answers, is S-D3 "We can't
/// find that page." with a 404.
/// </para>
/// Both pages only read: there is no edit or delete control, and no route
/// that changes an entry. The actor (which can be an email address typed at a
/// failed sign-in) and snapshot values (which can hold names) are shown as
/// encoded text only: never in an address, a title or a log line.
/// </summary>
public static class AdminAuditPages
{
    public const string ListPath = AdminSignedInPage.AuditLog;

    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/console-audit.js";

    /// <summary>The entity types an entry can name, with the list each one's record page is under.</summary>
    public static readonly IReadOnlyList<(string Type, string RecordPath)> EntityTypes =
    [
        ("Participant", AdminSignedInPage.Participants),
        ("Artifact", AdminSignedInPage.Artifacts),
        ("Negotiation", AdminSignedInPage.Negotiations),
        ("Offer", AdminSignedInPage.Offers)
    ];

    public static readonly IReadOnlyList<string> EntityTypeNames = EntityTypes.Select(t => t.Type).ToArray();

    /// <summary>
    /// The query keys the list reads, in the order it writes them. The same
    /// as <see cref="AdminReturnQuery.KeysFor"/> for its path, which a test holds.
    /// </summary>
    public static readonly IReadOnlyList<string> QueryKeys = ["action", "entityType", "from", "to", "offset", "limit"];

    private static readonly string[] Columns =
    [
        AdminConsoleCopy.AuditColumnTime,
        AdminConsoleCopy.AuditColumnActor,
        AdminConsoleCopy.AuditColumnAction,
        AdminConsoleCopy.AuditColumnEntityType,
        AdminConsoleCopy.AuditColumnEntityId,
        AdminConsoleCopy.AuditColumnOutcome
    ];

    public static void MapAdminAuditPages(this WebApplication app)
    {
        Page(app.MapGet(ListPath, ListPageAsync));
        Page(app.MapGet(ListPath + "/{id:guid}", EntryPageAsync));
    }

    private static void Page(RouteHandlerBuilder builder)
        => builder
            .WithTags("AdminConsole")
            .ExcludeFromDescription()
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

    private static string E(string? value) => AdminSignedOutPage.Encode(value);

    // ---- S-D1: the list ----

    private static async Task<IResult> ListPageAsync(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        IAdminAuditRepository audit,
        DealowareDbContext db,
        IFieldPolicy policy,
        ILoggerFactory logs,
        CancellationToken ct)
    {
        var state = AdminAuditState.Read(http.Request);
        var principal = AdminReadProjection.CoreOwnerFrom(http);
        IReadOnlyList<AdminAuditView>? rows = null;
        var total = 0;
        var records = RecordStates.None;
        try
        {
            (rows, total) = await AdminAuditEndpoints.ListAsync(audit, policy, principal, state.ToCriteria(), ct);
            // A page past the last one shows the last one.
            if (rows.Count == 0 && total > 0 && state.Offset > 0)
            {
                state = state with { Offset = (total - 1) / state.Limit * state.Limit };
                (rows, total) = await AdminAuditEndpoints.ListAsync(audit, policy, principal, state.ToCriteria(), ct);
            }

            // Nothing to page through: the address says page 1.
            if (total == 0)
                state = state with { Offset = 0 };
            records = await RecordStatesAsync(db, rows, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The kind of fault only.
            logs.CreateLogger(typeof(AdminAuditPages)).LogError("Admin audit log could not be read: {Fault}", ex.GetType().Name);
            rows = null;
        }

        return AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(AdminConsoleCopy.NavAuditLog),
            Heading = AdminConsoleCopy.NavAuditLog,
            Section = AdminConsoleSection.AuditLog,
            BodyHtml = ListBody(state, rows, total, records),
            Scripts = [ScriptPath],
            StatusCode = rows is null ? StatusCodes.Status500InternalServerError : StatusCodes.Status200OK,
            // The count console-audit.js says after a move: its space is kept, so nothing jumps.
            ReservesLiveRegion = rows is not null
        });
    }

    /// <summary>
    /// Which of the records a page of entries names still exist, and which
    /// of those are soft-deleted: an Entity ID links to its record only when
    /// that page would open (TD-ADM-UI-na-X02). Read once for the page, by key.
    /// </summary>
    private sealed record RecordStates(IReadOnlySet<(string Type, Guid Id)> Live, IReadOnlySet<(string Type, Guid Id)> Deleted)
    {
        public static readonly RecordStates None = new(new HashSet<(string, Guid)>(), new HashSet<(string, Guid)>());
    }

    private static async Task<RecordStates> RecordStatesAsync(
        DealowareDbContext db,
        IReadOnlyList<AdminAuditView> rows,
        CancellationToken ct)
    {
        var live = new HashSet<(string, Guid)>();
        var deleted = new HashSet<(string, Guid)>();
        foreach (var type in EntityTypeNames)
        {
            var ids = rows.Where(r => r.EntityType == type && r.EntityId is not null)
                .Select(r => r.EntityId!.Value).Distinct().ToArray();
            if (ids.Length == 0)
                continue;
            var found = type switch
            {
                "Participant" => await db.Participants.AsNoTracking().Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, IsDeleted = x.DeletedAt != null }).ToListAsync(ct),
                "Artifact" => await db.Artifacts.AsNoTracking().Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, IsDeleted = x.DeletedAt != null }).ToListAsync(ct),
                "Negotiation" => await db.Negotiations.AsNoTracking().Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, IsDeleted = x.DeletedAt != null }).ToListAsync(ct),
                _ => await db.Offers.AsNoTracking().Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, IsDeleted = x.DeletedAt != null }).ToListAsync(ct)
            };
            foreach (var record in found)
                (record.IsDeleted ? deleted : live).Add((type, record.Id));
        }

        return new RecordStates(live, deleted);
    }

    private static string ListBody(AdminAuditState state, IReadOnlyList<AdminAuditView>? rows, int total, RecordStates records)
    {
        var html = new StringBuilder(16384);
        var canonical = state.Url();
        html.Append($"<p class=\"lede\">{E(AdminConsoleCopy.AuditLede)}</p>");

        // The filters and the page size: one GET form.
        html.Append($"<form id=\"list-form\" class=\"list-form\" method=\"get\" action=\"{ListPath}\" data-canonical=\"{E(canonical)}\">");
        html.Append("<div class=\"filters audit-filters\">");
        html.Append($"<fieldset class=\"field statuses wide\"><legend>{E(AdminConsoleCopy.AuditColumnAction)}</legend><ul class=\"checks\">");
        foreach (var action in AdminAuditOutcome.Actions)
        {
            var id = ActionBoxId(action);
            var on = state.Actions.Contains(action) ? " checked" : string.Empty;
            html.Append($"<li><input type=\"checkbox\" id=\"{id}\" name=\"action\" value=\"{E(action)}\"{on} data-auto>");
            html.Append($"<label for=\"{id}\">{E(action)}</label></li>");
        }

        html.Append("</ul></fieldset>");

        html.Append($"<div class=\"field\"><label for=\"entityType\">{E(AdminConsoleCopy.AuditColumnEntityType)}</label>");
        html.Append("<select id=\"entityType\" name=\"entityType\" data-auto>");
        html.Append($"<option value=\"\">{E(AdminConsoleCopy.AuditAllTypes)}</option>");
        foreach (var type in EntityTypeNames)
        {
            var selected = type == state.EntityType ? " selected" : string.Empty;
            html.Append($"<option value=\"{type}\"{selected}>{type}</option>");
        }

        html.Append("</select></div>");
        foreach (var (key, label, value) in new[]
                 {
                     ("from", AdminConsoleCopy.AuditFilterFrom, state.From),
                     ("to", AdminConsoleCopy.AuditFilterTo, state.To)
                 })
        {
            html.Append($"<div class=\"field\"><label for=\"{key}\">{E(label)}</label>");
            html.Append($"<input type=\"date\" id=\"{key}\" name=\"{key}\" value=\"{E(DateInputValue(value))}\"></div>");
        }

        html.Append("</div>");
        html.Append("<div class=\"list-options\">");
        html.Append("<div class=\"field page-size\">");
        html.Append($"<label for=\"limit\">{E(AdminConsoleCopy.PageSize)}</label><select id=\"limit\" name=\"limit\" data-auto>");
        foreach (var size in AdminAuditState.PageSizes)
        {
            var selected = size == state.Limit ? " selected" : string.Empty;
            html.Append($"<option value=\"{size}\"{selected}>{size}</option>");
        }

        html.Append("</select></div>");
        html.Append($"<button type=\"submit\" id=\"apply-filters\">{E(AdminConsoleCopy.ApplyFilters)}</button>");
        html.Append("</div></form>");

        html.Append(Chips(state));

        // The table, in a region that scrolls sideways by itself on a narrow screen.
        html.Append("<div id=\"list-region\" class=\"table-wrap\" role=\"region\" aria-labelledby=\"list-caption\" tabindex=\"0\"");
        if (rows is not null)
            html.Append($" data-announce=\"{E(AdminConsoleCopy.Results(total))}\"");
        html.Append('>');
        html.Append($"<table id=\"list-table\" class=\"list\"><caption id=\"list-caption\">{E(AdminConsoleCopy.NavAuditLog)}</caption>");
        html.Append("<thead><tr>");
        for (var i = 0; i < Columns.Length; i++)
        {
            // The one order the log has: Time, newest first.
            html.Append(i == 0
                ? $"<th scope=\"col\" aria-sort=\"descending\">{E(Columns[i])}<span class=\"visually-hidden\">{E(AdminConsoleCopy.SortedDescending)}</span></th>"
                : $"<th scope=\"col\">{E(Columns[i])}</th>");
        }

        html.Append("</tr></thead><tbody>");
        if (rows is null)
        {
            html.Append($"<tr><td colspan=\"{Columns.Length}\" class=\"state error\"><p id=\"list-error\">{E(AdminConsoleCopy.CouldNotLoadList(AdminConsoleCopy.AuditLogType))}</p>");
            html.Append($"<a id=\"retry\" class=\"button-link\" href=\"{E(canonical)}\">{E(AdminConsoleCopy.Retry)}</a></td></tr>");
        }
        else if (total == 0 && !state.HasFilters)
        {
            html.Append($"<tr><td colspan=\"{Columns.Length}\" class=\"state\"><p id=\"list-empty\">{E(AdminConsoleCopy.NoneYet(AdminConsoleCopy.AuditEntriesType))}</p></td></tr>");
        }
        else if (total == 0)
        {
            html.Append($"<tr><td colspan=\"{Columns.Length}\" class=\"state\"><p id=\"list-miss\">{E(AdminConsoleCopy.NoResults)}</p>");
            html.Append($"<a id=\"clear-filters\" class=\"button-link\" href=\"{E(state.Cleared().Url())}\" data-focus=\"main\">{E(AdminConsoleCopy.ClearFilters)}</a></td></tr>");
        }
        else
        {
            var entryQuery = state.ToQueryString();
            foreach (var row in rows)
            {
                html.Append("<tr>");
                html.Append($"<td>{AdminListPages.TimeHtml(row.Timestamp)}</td>");
                html.Append($"<td>{Text(row.ActorEmail)}</td>");
                html.Append($"<td><a class=\"entry\" href=\"{E($"{ListPath}/{row.Id:D}{entryQuery}")}\">{E(row.Action)}</a></td>");
                html.Append($"<td>{Text(row.EntityType)}</td>");
                html.Append($"<td>{RecordLink(row, records)}</td>");
                html.Append($"<td>{OutcomeBadge(row.Outcome)}</td>");
                html.Append("</tr>");
            }
        }

        html.Append("</tbody></table></div>");
        if (rows is not null && total > 0)
            html.Append(Pager(state, total));
        return html.ToString();
    }

    /// <summary>The id of an action's checkbox: a dot in the name becomes a dash.</summary>
    public static string ActionBoxId(string action) => "action-" + action.Replace('.', '-');

    private static string Text(string? value) => string.IsNullOrEmpty(value) ? E(AdminConsoleCopy.NoValue) : E(value);

    private static string Badge(string text, string kind) => $"<span class=\"badge badge-{kind}\">{E(text)}</span>";

    /// <summary>The outcome as a word (never colour alone).</summary>
    private static string OutcomeBadge(string outcome) => outcome switch
    {
        AdminAuditOutcome.Failure => Badge(AdminConsoleCopy.AuditOutcomeFailure, "failure"),
        AdminAuditOutcome.Denied => Badge(AdminConsoleCopy.AuditOutcomeDenied, "denied"),
        _ => Badge(AdminConsoleCopy.AuditOutcomeSuccess, "success")
    };

    /// <summary>
    /// The Entity ID: a link to the record when it exists and is not
    /// soft-deleted; otherwise the id as text (with "Deleted" beside a
    /// soft-deleted one), which opens nothing.
    /// </summary>
    private static string RecordLink(AdminAuditView row, RecordStates records)
    {
        if (row.EntityId is not { } id)
            return E(AdminConsoleCopy.NoValue);
        var type = row.EntityType ?? string.Empty;
        var path = EntityTypes.FirstOrDefault(t => t.Type == type).RecordPath;
        if (path is not null && records.Live.Contains((type, id)))
            return $"<a class=\"id\" href=\"{path}/{id:D}\">{id:D}</a>";
        var text = $"<span class=\"id\">{id:D}</span>";
        return records.Deleted.Contains((type, id)) ? text + " " + Badge(AdminConsoleCopy.Deleted, "deleted") : text;
    }

    /// <summary>A date filter as the date box shows it: the ET calendar date of what is kept.</summary>
    private static string? DateInputValue(string? value)
    {
        if (value is null)
            return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return value;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? EasternTime.ToEastern(at).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    private static string DateChip(string value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"))
            : DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
                ? EasternTime.Format(at)
                : value;

    /// <summary>One removable chip per active filter, then "Clear all filters".</summary>
    private static string Chips(AdminAuditState state)
    {
        var chips = new List<(string Label, string Value, AdminAuditState Without)>();
        var page1 = state with { Offset = 0 };
        if (state.Actions.Count > 0)
            chips.Add((AdminConsoleCopy.AuditColumnAction, string.Join(", ", state.Actions), page1 with { Actions = [] }));
        if (state.EntityType is { } type)
            chips.Add((AdminConsoleCopy.AuditColumnEntityType, type, page1 with { EntityType = null }));
        if (state.From is { } from)
            chips.Add((AdminConsoleCopy.AuditFilterFrom, DateChip(from), page1 with { From = null }));
        if (state.To is { } to)
            chips.Add((AdminConsoleCopy.AuditFilterTo, DateChip(to), page1 with { To = null }));
        if (chips.Count == 0)
            return string.Empty;

        var html = new StringBuilder("<div id=\"chips\" class=\"chips-bar\"><ul class=\"chips\">");
        foreach (var (label, value, without) in chips)
        {
            html.Append($"<li class=\"chip\"><span>{E(label)}: {E(value)}</span>");
            html.Append($"<a href=\"{E(without.Url())}\" aria-label=\"{E(AdminConsoleCopy.RemoveChipName(label))}\" data-focus=\"main\">{E(AdminConsoleCopy.RemoveChip)}</a></li>");
        }

        html.Append("</ul>");
        html.Append($"<a id=\"clear-all\" class=\"button-link secondary\" href=\"{E(state.Cleared().Url())}\" data-focus=\"main\">{E(AdminConsoleCopy.ClearAllFilters)}</a>");
        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>"Showing a–b of N", Previous, numbered pages and Next, each a link to its page.</summary>
    private static string Pager(AdminAuditState state, int total)
    {
        var pages = (total + state.Limit - 1) / state.Limit;
        var page = state.Offset / state.Limit + 1;
        var first = state.Offset + 1;
        var last = Math.Min(state.Offset + state.Limit, total);
        string Href(int p) => E((state with { Offset = (p - 1) * state.Limit }).Url());

        var html = new StringBuilder("<div id=\"pager\" class=\"pager\">");
        html.Append($"<p id=\"list-range\" class=\"range\">{E(AdminConsoleCopy.Showing(first, last, total))}</p>");
        html.Append("<ul class=\"pages\">");
        html.Append(page > 1
            ? $"<li><a id=\"page-prev\" href=\"{Href(page - 1)}\" rel=\"prev\" data-focus=\"page-prev\">{E(AdminConsoleCopy.Previous)}</a></li>"
            : $"<li><span class=\"page-off\">{E(AdminConsoleCopy.Previous)}</span></li>");

        var shown = new SortedSet<int> { 1, pages, page - 1, page, page + 1 };
        var previous = 0;
        foreach (var p in shown.Where(p => p >= 1 && p <= pages))
        {
            if (p > previous + 1)
                html.Append("<li><span class=\"gap\" aria-hidden=\"true\">…</span></li>");
            var current = p == page ? " aria-current=\"page\"" : string.Empty;
            html.Append($"<li><a id=\"page-{p}\" href=\"{Href(p)}\" aria-label=\"{E(AdminConsoleCopy.PageName(p))}\"{current} data-focus=\"page-{p}\">{p}</a></li>");
            previous = p;
        }

        html.Append(page < pages
            ? $"<li><a id=\"page-next\" href=\"{Href(page + 1)}\" rel=\"next\" data-focus=\"page-next\">{E(AdminConsoleCopy.Next)}</a></li>"
            : $"<li><span class=\"page-off\">{E(AdminConsoleCopy.Next)}</span></li>");
        html.Append("</ul></div>");
        return html.ToString();
    }

    // ---- S-D2: one entry ----

    private static async Task<IResult> EntryPageAsync(
        HttpContext http,
        Guid id,
        AdminAntiForgeryService antiforgery,
        IAdminAuditRepository audit,
        DealowareDbContext db,
        IFieldPolicy policy,
        CancellationToken ct)
    {
        // The list's state rides along, so "Back to audit log" restores it.
        var state = AdminAuditState.Read(http.Request);
        var entry = await AdminAuditEndpoints.GetAsync(audit, policy, AdminReadProjection.CoreOwnerFrom(http), id, ct);
        if (entry is null)
            return AdminConsolePages.NotFound(http, antiforgery);

        var records = await RecordStatesAsync(db, [entry], ct);
        return AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(AdminConsoleCopy.AuditEntry),
            Heading = AdminConsoleCopy.AuditEntry,
            Section = AdminConsoleSection.AuditLog,
            BodyHtml = EntryBody(state, entry, records)
        });
    }

    private static string EntryBody(AdminAuditState state, AdminAuditView entry, RecordStates records)
    {
        var html = new StringBuilder(4096);
        html.Append($"<p class=\"back\"><a id=\"back-to-list\" href=\"{E(state.Url())}\">{E(AdminConsoleCopy.BackToAuditLog)}</a></p>");
        html.Append("<section class=\"audit-section\" aria-labelledby=\"entry-heading\">");
        html.Append($"<h2 id=\"entry-heading\">{E(AdminConsoleCopy.AuditEntryHeading)}</h2>");
        html.Append("<dl id=\"entry\" class=\"details audit-details\">");

        void Item(string label, string valueHtml)
            => html.Append($"<div class=\"detail\"><dt>{E(label)}</dt><dd>{valueHtml}</dd></div>");

        Item(AdminConsoleCopy.AuditColumnTime, AdminListPages.TimeHtml(entry.Timestamp));
        Item(AdminConsoleCopy.AuditColumnActor, Text(entry.ActorEmail));
        Item(AdminConsoleCopy.AuditColumnAction, E(entry.Action));
        Item(AdminConsoleCopy.AuditColumnEntityType, Text(entry.EntityType));
        Item(AdminConsoleCopy.AuditColumnEntityId, RecordLink(entry, records));
        Item(AdminConsoleCopy.AuditColumnOutcome, OutcomeBadge(entry.Outcome));
        if (!string.IsNullOrEmpty(entry.ReasonClass))
            Item(AdminConsoleCopy.AuditReason, E(entry.ReasonClass));
        // The first characters of the keyed hash: on this page only, never in the list. An entry an
        // operator command wrote has no address to hash (its stored value is a marker, not a hash): a dash.
        if (entry.IpHmacPrefix == AdminAuditEntry.CommandOrigin)
            Item(AdminConsoleCopy.AuditIpPrefix, E(AdminConsoleCopy.NoValue));
        else if (!string.IsNullOrEmpty(entry.IpHmacPrefix))
            Item(AdminConsoleCopy.AuditIpPrefix, $"<span id=\"ip-prefix\" class=\"id\">{E(entry.IpHmacPrefix)}</span>");
        html.Append("</dl></section>");
        html.Append(Snapshots(entry));
        return html.ToString();
    }

    /// <summary>
    /// Before and after, each field as text. With both snapshots there, every
    /// field of either shows on both sides, and one whose values differ
    /// carries the word "Changed" on both. An entry without snapshots (an
    /// authentication event) has no such section.
    /// </summary>
    private static string Snapshots(AdminAuditView entry)
    {
        var before = Fields(entry.BeforeSnapshot);
        var after = Fields(entry.AfterSnapshot);
        if (before is null && after is null)
            return string.Empty;

        var keys = new List<string>();
        foreach (var key in (before?.Keys.AsEnumerable() ?? []).Concat(after?.Keys.AsEnumerable() ?? []))
        {
            if (!keys.Contains(key, StringComparer.Ordinal))
                keys.Add(key);
        }

        var compare = before is not null && after is not null;
        bool Changed(string key)
            => compare
               && (!before!.TryGetValue(key, out var b) | !after!.TryGetValue(key, out var a)
                   || !string.Equals(b.Json, a.Json, StringComparison.Ordinal));

        var html = new StringBuilder("<section id=\"snapshots\" class=\"audit-section\" aria-labelledby=\"snapshots-heading\">");
        html.Append($"<h2 id=\"snapshots-heading\">{E(AdminConsoleCopy.AuditBeforeAfter)}</h2>");
        html.Append("<div class=\"snapshots\">");
        foreach (var (id, heading, side) in new[]
                 {
                     ("snapshot-before", AdminConsoleCopy.AuditBefore, before),
                     ("snapshot-after", AdminConsoleCopy.AuditAfter, after)
                 })
        {
            html.Append($"<div id=\"{id}\" class=\"snapshot\"><h3>{E(heading)}</h3>");
            var shown = compare ? keys : side?.Keys.ToList() ?? [];
            if (side is null || shown.Count == 0)
            {
                html.Append($"<p class=\"none\">{E(AdminConsoleCopy.AuditNoFields)}</p></div>");
                continue;
            }

            html.Append("<dl class=\"details\">");
            foreach (var key in shown)
            {
                var text = side.TryGetValue(key, out var value) ? value.Text : AdminConsoleCopy.NoValue;
                html.Append($"<div class=\"detail\"><dt>{E(key)}</dt><dd><span class=\"free-text\">{E(text)}</span>");
                if (Changed(key))
                    html.Append(Badge(AdminConsoleCopy.AuditChanged, "changed"));
                html.Append("</dd></div>");
            }

            html.Append("</dl></div>");
        }

        html.Append("</div></section>");
        return html.ToString();
    }

    /// <summary>
    /// A snapshot's fields in their order: each value as the text to show and
    /// as JSON to compare. Null when the entry has no such snapshot. A
    /// snapshot that is not a JSON object is one unnamed field.
    /// </summary>
    private static Dictionary<string, (string Text, string Json)>? Fields(string? snapshot)
    {
        if (snapshot is null)
            return null;
        var fields = new Dictionary<string, (string Text, string Json)>(StringComparer.Ordinal);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(snapshot);
        }
        catch (JsonException)
        {
            // The read model gives JSON only; anything else is not shown.
            return fields;
        }

        if (root is JsonObject obj)
        {
            foreach (var (key, value) in obj)
                fields[key] = (ValueText(value), value?.ToJsonString() ?? "null");
        }
        else
        {
            fields[AdminConsoleCopy.NoValue] = (ValueText(root), root?.ToJsonString() ?? "null");
        }

        return fields;
    }

    /// <summary>A value as text: a string as it is, nothing as a dash, anything else as its JSON.</summary>
    private static string ValueText(JsonNode? value)
    {
        if (value is null)
            return AdminConsoleCopy.NoValue;
        if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text))
            return text.Length == 0 ? AdminConsoleCopy.NoValue : text;
        return value.ToJsonString();
    }
}

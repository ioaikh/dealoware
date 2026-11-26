using System.Globalization;
using System.Text;
using Dealoware.Domain.FieldAcl;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>Which of the four lists.</summary>
public enum AdminListKind
{
    Participants,
    Artifacts,
    Negotiations,
    Offers
}

/// <summary>One column of a list (TD-ADM-UI-na-B01): its heading and, if it sorts, its sort key.</summary>
public sealed record AdminListColumn(string Key, string Label, string? Sort = null);

/// <summary>
/// One list page (S-B2 to S-B5) and its detail page (S-B6): the columns in
/// order, what sorts, the statuses its filter offers, and which filters it has
/// (TD-ADM-UI-na-B01 to B05). The columns, search labels and filters are draft
/// #39's list definitions.
/// </summary>
public sealed record AdminListDefinition(
    AdminListKind Kind,
    string Path,
    string Screen,
    string TypeLabel,
    string DetailScreen,
    AdminConsoleSection Section,
    string SearchLabel,
    IReadOnlyList<AdminListColumn> Columns,
    IReadOnlyList<string> Statuses,
    string? ParticipantFilter,
    bool ArtifactFilter,
    bool NegotiationIdFilter,
    bool AmountFilter)
{
    /// <summary>The sort keys a header offers. Any other sort in the address is dropped.</summary>
    public IReadOnlyList<string> SortKeys => Columns.Where(c => c.Sort is not null).Select(c => c.Sort!).ToArray();

    /// <summary>
    /// The query keys this page reads, in the order it writes them. The same
    /// as <see cref="AdminReturnQuery.KeysFor"/> for its path, which a test holds.
    /// </summary>
    public IReadOnlyList<string> QueryKeys
    {
        get
        {
            var keys = new List<string> { "sort", "q", "dir", "offset", "limit", "includeDeleted" };
            if (ParticipantFilter is not null)
                keys.Add("participant");
            if (Kind == AdminListKind.Participants)
                keys.Add("status");
            keys.AddRange(["createdFrom", "createdTo", "updatedFrom", "updatedTo"]);
            if (Kind != AdminListKind.Participants && Statuses.Count > 0)
                keys.Add("status");
            if (ArtifactFilter)
                keys.Add("artifact");
            if (NegotiationIdFilter)
                keys.Add("negotiationId");
            if (AmountFilter)
                keys.AddRange(["amountMin", "amountMax"]);
            return keys;
        }
    }
}

/// <summary>
/// What a list page shows: search, sort, filters, page, page size and the
/// "Show deleted" toggle, as read from its address. Every value has passed
/// <see cref="AdminReturnQuery.Filter"/> and this page's own rules, so a
/// hand-edited address that names a status, sort or value this list does not
/// take shows the list without it (TD-ADM-062, TD-ADM-UI-na-B04), never an
/// error.
/// </summary>
public sealed record AdminListState
{
    public const string DefaultSort = AdminListQuery.DefaultSort;
    public static readonly int[] PageSizes = [50, 100, 200];

    public string? Q { get; init; }
    public string Sort { get; init; } = DefaultSort;
    public bool Descending { get; init; } = true;
    public int Offset { get; init; }
    public int Limit { get; init; } = AdminListQuery.DefaultLimit;
    public bool IncludeDeleted { get; init; }
    public IReadOnlyList<string> Statuses { get; init; } = [];
    public string? Participant { get; init; }
    public Guid? Artifact { get; init; }
    public Guid? NegotiationId { get; init; }
    public decimal? AmountMin { get; init; }
    public decimal? AmountMax { get; init; }
    public string? CreatedFrom { get; init; }
    public string? CreatedTo { get; init; }
    public string? UpdatedFrom { get; init; }
    public string? UpdatedTo { get; init; }

    /// <summary>Any search or filter set: an empty result is then a filter miss, not an empty table (TD-ADM-UI-na-B14).</summary>
    public bool HasFilters
        => !string.IsNullOrEmpty(Q) || Statuses.Count > 0 || Participant is not null || Artifact is not null
           || NegotiationId is not null || AmountMin is not null || AmountMax is not null
           || CreatedFrom is not null || CreatedTo is not null || UpdatedFrom is not null || UpdatedTo is not null;

    /// <summary>The same list with every search and filter dropped; sort, page size and the toggle stay.</summary>
    public AdminListState Cleared() => new()
    {
        Sort = Sort,
        Descending = Descending,
        Limit = Limit,
        IncludeDeleted = IncludeDeleted
    };

    /// <summary>
    /// Reads a list page's address. Checkboxes sent by a form without
    /// JavaScript repeat <c>status</c>: their values are joined first. A page
    /// size above the maximum is clamped to it (TD-ADM-UI-na-B06). Then only
    /// what <see cref="AdminReturnQuery.Filter"/> keeps is read, and of that
    /// only a sort this page's headers offer.
    /// </summary>
    public static AdminListState Read(AdminListDefinition list, HttpRequest request)
    {
        var raw = request.QueryString.HasValue ? request.QueryString.Value![1..] : string.Empty;
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> given;
        try
        {
            given = QueryHelpers.ParseQuery(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or UriFormatException)
        {
            return new AdminListState();
        }

        var pairs = new List<string>();
        foreach (var (key, values) in given)
        {
            var list2 = values.Where(v => v is not null).Select(v => v!).ToList();
            if (key == "status" && list2.Count > 1)
                list2 = [string.Join(',', list2)];
            foreach (var value in list2)
            {
                var v = value;
                if (key == "limit"
                    && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var asked)
                    && asked > AdminListQuery.MaxLimit)
                {
                    v = AdminListQuery.MaxLimit.ToString(CultureInfo.InvariantCulture);
                }

                pairs.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(v));
            }
        }

        var kept = QueryHelpers.ParseQuery(AdminReturnQuery.Filter(list.Path, string.Join('&', pairs)));
        string? One(string key) => kept.TryGetValue(key, out var v) ? v.ToString() : null;

        var sortGiven = One("sort");
        var sortOk = sortGiven is not null && list.SortKeys.Contains(sortGiven, StringComparer.Ordinal);
        var sortDropped = given.ContainsKey("sort") && !sortOk;
        var limit = int.TryParse(One("limit"), NumberStyles.None, CultureInfo.InvariantCulture, out var l)
            ? PageSizes.First(size => l <= size || size == AdminListQuery.MaxLimit)
            : AdminListQuery.DefaultLimit;
        var offset = int.TryParse(One("offset"), NumberStyles.None, CultureInfo.InvariantCulture, out var o) ? o : 0;

        return new AdminListState
        {
            Q = One("q"),
            Sort = sortOk ? sortGiven! : DefaultSort,
            // A dropped sort takes its direction with it: the default list.
            Descending = sortDropped || One("dir") != "asc",
            Limit = limit,
            Offset = offset / limit * limit,
            IncludeDeleted = One("includeDeleted") == "true",
            Statuses = One("status") is { } s ? s.Split(',') : [],
            Participant = One("participant"),
            Artifact = Guid.TryParse(One("artifact"), out var a) ? a : null,
            NegotiationId = Guid.TryParse(One("negotiationId"), out var n) ? n : null,
            AmountMin = decimal.TryParse(One("amountMin"), NumberStyles.Number, CultureInfo.InvariantCulture, out var min) ? min : null,
            AmountMax = decimal.TryParse(One("amountMax"), NumberStyles.Number, CultureInfo.InvariantCulture, out var max) ? max : null,
            CreatedFrom = One("createdFrom"),
            CreatedTo = One("createdTo"),
            UpdatedFrom = One("updatedFrom"),
            UpdatedTo = One("updatedTo")
        };
    }

    /// <summary>
    /// The address of this state: only values that differ from the default,
    /// in the page's key order, each encoded. "" for the default list.
    /// </summary>
    public string ToQueryString(AdminListDefinition list)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Sort != DefaultSort)
            values["sort"] = Sort;
        if (!string.IsNullOrEmpty(Q))
            values["q"] = Q;
        if (!Descending)
            values["dir"] = "asc";
        if (Offset > 0)
            values["offset"] = Offset.ToString(CultureInfo.InvariantCulture);
        if (Limit != AdminListQuery.DefaultLimit)
            values["limit"] = Limit.ToString(CultureInfo.InvariantCulture);
        if (IncludeDeleted)
            values["includeDeleted"] = "true";
        if (Statuses.Count > 0)
            values["status"] = string.Join(',', Statuses);
        if (Participant is not null)
            values["participant"] = Participant;
        if (Artifact is { } artifact)
            values["artifact"] = artifact.ToString("D");
        if (NegotiationId is { } negotiation)
            values["negotiationId"] = negotiation.ToString("D");
        if (AmountMin is { } amountMin)
            values["amountMin"] = amountMin.ToString(CultureInfo.InvariantCulture);
        if (AmountMax is { } amountMax)
            values["amountMax"] = amountMax.ToString(CultureInfo.InvariantCulture);
        if (CreatedFrom is not null)
            values["createdFrom"] = CreatedFrom;
        if (CreatedTo is not null)
            values["createdTo"] = CreatedTo;
        if (UpdatedFrom is not null)
            values["updatedFrom"] = UpdatedFrom;
        if (UpdatedTo is not null)
            values["updatedTo"] = UpdatedTo;

        var parts = list.QueryKeys
            .Where(values.ContainsKey)
            .Select(key => key + "=" + Uri.EscapeDataString(values[key]))
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    public string Url(AdminListDefinition list) => list.Path + ToQueryString(list);

    /// <summary>The read API's query for this state. ET calendar dates become the instants that bound them.</summary>
    public AdminListQuery ToQuery() => new()
    {
        Q = Q,
        Sort = Sort,
        Descending = Descending,
        Offset = Offset,
        Limit = Limit,
        IncludeDeleted = IncludeDeleted,
        Status = Statuses.Count == 0 ? null : string.Join(',', Statuses),
        Statuses = Statuses,
        Participant = Participant,
        ArtifactId = Artifact,
        NegotiationId = NegotiationId,
        AmountMin = AmountMin,
        AmountMax = AmountMax,
        CreatedFrom = Bound(CreatedFrom, endOfDay: false),
        CreatedTo = Bound(CreatedTo, endOfDay: true),
        UpdatedFrom = Bound(UpdatedFrom, endOfDay: false),
        UpdatedTo = Bound(UpdatedTo, endOfDay: true)
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
/// T04 piece 2: the four list pages (S-B2 Participants, S-B3 Artifacts, S-B4
/// Negotiations, S-B5 Offers) and their detail pages (S-B6), each one call to
/// the shared shell (<see cref="AdminSignedInPage"/>).
/// <para>
/// A list page is written by the server from its address: the first page of
/// rows for the search, sort, filters, page, page size and toggle the address
/// names, through the read API's own query functions
/// (<see cref="AdminReadEndpoints"/>), so the field policy, the server-side
/// paging (50 by default, never more than 200) and the name search are the
/// API's. Every value is HTML-encoded. Sorting, filters, chips and paging are
/// plain links and GET forms, so the page works without JavaScript; its
/// script (<see cref="ScriptPath"/>) only moves to the new address sooner,
/// keeps the focus where it was, says "N results", and drives the two
/// pickers, which ask the read API for one page of matches as the owner types.
/// </para>
/// <para>
/// A detail page shows one record's fields as text, free text (an artifact's
/// descriptions, an offer's terms) included; strategy text never appears (the
/// field policy denies it). A soft-deleted record is shown only with the
/// toggle on (<c>includeDeleted=true</c>, carried from the list); otherwise,
/// like an unknown id, it is S-D3 "We can't find that page." with a 404.
/// </para>
/// A detail page has an edit mode on its own address (Step 9,
/// <see cref="AdminEditPage"/>). A detail page and each list row of a record
/// that is not soft-deleted have a Delete that opens the delete dialog (Step
/// 10, <see cref="AdminDeletePage"/>).
/// </summary>
public static class AdminListPages
{
    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/console-list.js";

    public static readonly AdminListDefinition Participants = new(
        AdminListKind.Participants,
        AdminSignedInPage.Participants,
        AdminConsoleCopy.NavParticipants,
        "participants",
        AdminConsoleCopy.ParticipantDetails,
        AdminConsoleSection.Participants,
        AdminConsoleCopy.SearchParticipants,
        [
            new("name", AdminConsoleCopy.ColumnName, "name"),
            new("id", AdminConsoleCopy.ColumnId),
            new("status", AdminConsoleCopy.ColumnStatus),
            new("created", AdminConsoleCopy.ColumnCreated, "created"),
            new("updated", AdminConsoleCopy.ColumnUpdated, "updated")
        ],
        AdminReadEndpoints.ParticipantStatuses,
        ParticipantFilter: null,
        ArtifactFilter: false,
        NegotiationIdFilter: false,
        AmountFilter: false);

    public static readonly AdminListDefinition Artifacts = new(
        AdminListKind.Artifacts,
        AdminSignedInPage.Artifacts,
        AdminConsoleCopy.NavArtifacts,
        "artifacts",
        AdminConsoleCopy.ArtifactDetails,
        AdminConsoleSection.Artifacts,
        AdminConsoleCopy.SearchArtifacts,
        [
            new("name", AdminConsoleCopy.ColumnName, "name"),
            new("owner", AdminConsoleCopy.ColumnOwner),
            new("created", AdminConsoleCopy.ColumnCreated, "created"),
            new("updated", AdminConsoleCopy.ColumnUpdated, "updated")
        ],
        [],
        ParticipantFilter: AdminConsoleCopy.FilterOwner,
        ArtifactFilter: false,
        NegotiationIdFilter: false,
        AmountFilter: false);

    public static readonly AdminListDefinition Negotiations = new(
        AdminListKind.Negotiations,
        AdminSignedInPage.Negotiations,
        AdminConsoleCopy.NavNegotiations,
        "negotiations",
        AdminConsoleCopy.NegotiationDetails,
        AdminConsoleSection.Negotiations,
        AdminConsoleCopy.SearchNegotiations,
        [
            new("id", AdminConsoleCopy.ColumnId),
            new("artifact", AdminConsoleCopy.ColumnArtifact),
            new("participants", AdminConsoleCopy.ColumnParticipants),
            new("status", AdminConsoleCopy.ColumnStatus, "status"),
            new("created", AdminConsoleCopy.ColumnCreated, "created"),
            new("updated", AdminConsoleCopy.ColumnUpdated, "updated")
        ],
        AdminReadEndpoints.NegotiationStatuses,
        ParticipantFilter: AdminConsoleCopy.FilterParticipant,
        ArtifactFilter: true,
        NegotiationIdFilter: true,
        AmountFilter: false);

    public static readonly AdminListDefinition Offers = new(
        AdminListKind.Offers,
        AdminSignedInPage.Offers,
        AdminConsoleCopy.NavOffers,
        "offers",
        AdminConsoleCopy.OfferDetails,
        AdminConsoleSection.Offers,
        AdminConsoleCopy.SearchOffers,
        [
            new("id", AdminConsoleCopy.ColumnId),
            new("negotiationId", AdminConsoleCopy.ColumnNegotiationId),
            new("artifact", AdminConsoleCopy.ColumnArtifact),
            new("offering", AdminConsoleCopy.ColumnOffering),
            new("amount", AdminConsoleCopy.ColumnAmount, "amount"),
            new("status", AdminConsoleCopy.ColumnStatus, "status"),
            new("created", AdminConsoleCopy.ColumnCreated, "created"),
            new("updated", AdminConsoleCopy.ColumnUpdated, "updated")
        ],
        AdminReadEndpoints.OfferStatuses,
        ParticipantFilter: AdminConsoleCopy.FilterParticipant,
        ArtifactFilter: true,
        NegotiationIdFilter: true,
        AmountFilter: true);

    public static readonly IReadOnlyList<AdminListDefinition> All = [Participants, Artifacts, Negotiations, Offers];

    public static void MapAdminListPages(this WebApplication app)
    {
        foreach (var list in All)
        {
            var def = list;
            Page(app.MapGet(def.Path, (HttpContext http, AdminAntiForgeryService af, DealowareDbContext db,
                    IFieldPolicy policy, ILoggerFactory logs, CancellationToken ct)
                => ListPageAsync(http, af, db, policy, logs, def, ct)));
            Page(app.MapGet(def.Path + "/{id:guid}", (HttpContext http, Guid id, AdminAntiForgeryService af,
                    DealowareDbContext db, IFieldPolicy policy, CancellationToken ct)
                => DetailPageAsync(http, id, af, db, policy, def, ct)));
        }
    }

    private static void Page(RouteHandlerBuilder builder)
        => builder
            .WithTags("AdminConsole")
            .ExcludeFromDescription()
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

    private static string E(string? value) => AdminSignedOutPage.Encode(value);

    // ---- the list page ----

    private static Task<AdminReadEndpoints.ListResult> RunAsync(
        AdminListDefinition list,
        DealowareDbContext db,
        IFieldPolicy policy,
        FieldPrincipal principal,
        AdminListQuery query,
        CancellationToken ct)
        => list.Kind switch
        {
            AdminListKind.Participants => AdminReadEndpoints.ListParticipantsAsync(db, policy, principal, query, ct),
            AdminListKind.Artifacts => AdminReadEndpoints.ListArtifactsAsync(db, policy, principal, query, ct),
            AdminListKind.Negotiations => AdminReadEndpoints.ListNegotiationsAsync(db, policy, principal, query, ct),
            _ => AdminReadEndpoints.ListOffersAsync(db, policy, principal, query, ct)
        };

    private static async Task<IResult> ListPageAsync(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        DealowareDbContext db,
        IFieldPolicy policy,
        ILoggerFactory logs,
        AdminListDefinition list,
        CancellationToken ct)
    {
        var state = AdminListState.Read(list, http.Request);
        var principal = AdminReadProjection.CoreOwnerFrom(http);
        AdminReadEndpoints.ListResult? result = null;
        var chips = new ChipNames(null, null);
        try
        {
            result = await RunAsync(list, db, policy, principal, state.ToQuery(), ct);
            // A page past the last one shows the last one (TD-ADM-UI-na-B06).
            if (result.Items.Count == 0 && result.Total > 0 && state.Offset > 0)
            {
                state = state with { Offset = (result.Total - 1) / state.Limit * state.Limit };
                result = await RunAsync(list, db, policy, principal, state.ToQuery(), ct);
            }

            chips = await ChipNamesAsync(list, state, db, policy, principal, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The kind of fault only: never the query, which can hold the owner's search text.
            logs.CreateLogger(typeof(AdminListPages)).LogError("Admin list {List} could not be read: {Fault}", list.TypeLabel, ex.GetType().Name);
            result = null;
        }

        return AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(list.Screen),
            Heading = list.Screen,
            Section = list.Section,
            BodyHtml = ListBody(list, state, result, chips),
            // The delete script first: it reads what a delete just did before the list script tidies the address.
            Scripts = [AdminSignedOutPage.DomScriptPath, AdminDeletePage.ScriptPath, ScriptPath],
            StatusCode = result is null ? StatusCodes.Status500InternalServerError : StatusCodes.Status200OK
        });
    }

    /// <summary>
    /// The names the participant and artifact filters show, read through the
    /// field policy. A soft-deleted one gives no name while "Show deleted" is
    /// off: its chip shows its identifier and "Deleted".
    /// </summary>
    private sealed record ChipNames(string? Participant, string? Artifact, bool ParticipantDeleted = false, bool ArtifactDeleted = false);

    private static async Task<ChipNames> ChipNamesAsync(
        AdminListDefinition list,
        AdminListState state,
        DealowareDbContext db,
        IFieldPolicy policy,
        FieldPrincipal principal,
        CancellationToken ct)
    {
        string? participant = null;
        string? artifact = null;
        var participantDeleted = false;
        var artifactDeleted = false;
        if (list.ParticipantFilter is not null && state.Participant is { } sub)
        {
            var ctx = FieldResourceContext.ForSelfProfile(sub);
            var row = await db.Participants.AsNoTracking()
                .Where(p => p.Sub == sub)
                .Select(p => new { p.DisplayName, p.DeletedAt })
                .FirstOrDefaultAsync(ct);
            participantDeleted = row?.DeletedAt is not null
                                 && policy.Evaluate(principal, FieldClass.SoftDeletedAt, FieldAction.Read, ctx);
            if (row is not null && (row.DeletedAt is null || state.IncludeDeleted)
                && policy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx))
            {
                participant = row.DisplayName;
            }
        }

        if (list.ArtifactFilter && state.Artifact is { } artifactId)
        {
            var row = await db.Artifacts.AsNoTracking().Include(a => a.Entities)
                .FirstOrDefaultAsync(a => a.Id == artifactId, ct);
            var ctx = FieldResourceContext.ForSelfProfile(row?.OwnerParticipantId ?? string.Empty);
            artifactDeleted = row?.DeletedAt is not null
                              && policy.Evaluate(principal, FieldClass.SoftDeletedAt, FieldAction.Read, ctx);
            if (row is not null && (row.DeletedAt is null || state.IncludeDeleted)
                && policy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx))
            {
                artifact = AdminReadProjection.ArtifactNameOf(row);
            }
        }

        return new ChipNames(participant, artifact, participantDeleted && participant is null, artifactDeleted && artifact is null);
    }

    private static string ListBody(
        AdminListDefinition list,
        AdminListState state,
        AdminReadEndpoints.ListResult? result,
        ChipNames names)
    {
        var html = new StringBuilder(16384);
        var canonical = state.Url(list);

        // The search, the filters, the toggle and the page size: one GET form.
        html.Append($"<form id=\"list-form\" class=\"list-form\" method=\"get\" action=\"{list.Path}\" data-canonical=\"{E(canonical)}\">");
        html.Append("<div class=\"search\">");
        html.Append($"<label for=\"q\">{E(list.SearchLabel)}</label>");
        html.Append("<div class=\"search-row\">");
        html.Append($"<input type=\"search\" id=\"q\" name=\"q\" value=\"{E(state.Q)}\" maxlength=\"{AdminListQuery.MaxQueryLength}\" autocomplete=\"off\">");
        html.Append($"<a id=\"search-clear\" class=\"button-link secondary\" href=\"{E((state with { Q = null, Offset = 0 }).Url(list))}\" data-focus=\"q\">{E(AdminConsoleCopy.ClearSearch)}</a>");
        html.Append("</div></div>");

        html.Append("<div class=\"filters\">");
        if (list.Statuses.Count > 0)
        {
            html.Append($"<fieldset class=\"field statuses\"><legend>{E(AdminConsoleCopy.FilterStatus)}</legend><ul class=\"checks\">");
            foreach (var status in list.Statuses)
            {
                var id = "status-" + status;
                var on = state.Statuses.Contains(status) ? " checked" : string.Empty;
                html.Append($"<li><input type=\"checkbox\" id=\"{id}\" name=\"status\" value=\"{status}\"{on} data-auto>");
                html.Append($"<label for=\"{id}\">{E(StatusLabel(status))}</label></li>");
            }

            html.Append("</ul></fieldset>");
        }

        if (list.ParticipantFilter is not null)
        {
            html.Append(Picker("participant", list.ParticipantFilter, "/admin/api/participants", "sub", "displayName",
                state.Participant, names.Participant ?? state.Participant));
        }

        if (list.ArtifactFilter)
        {
            html.Append(Picker("artifact", AdminConsoleCopy.FilterArtifact, "/admin/api/artifacts", "id", "name",
                state.Artifact?.ToString("D"), names.Artifact ?? state.Artifact?.ToString("D")));
        }

        if (list.NegotiationIdFilter)
        {
            html.Append(Field("negotiationId", AdminConsoleCopy.FilterNegotiationId,
                $"<input type=\"text\" id=\"negotiationId\" name=\"negotiationId\" value=\"{E(state.NegotiationId?.ToString("D"))}\" maxlength=\"36\" spellcheck=\"false\" autocomplete=\"off\">"));
        }

        if (list.AmountFilter)
        {
            foreach (var (key, label, value) in new[]
                     {
                         ("amountMin", AdminConsoleCopy.FilterAmountMin, state.AmountMin),
                         ("amountMax", AdminConsoleCopy.FilterAmountMax, state.AmountMax)
                     })
            {
                html.Append(Field(key, label,
                    $"<input type=\"number\" id=\"{key}\" name=\"{key}\" value=\"{E(value?.ToString(CultureInfo.InvariantCulture))}\" min=\"0\" step=\"0.01\" inputmode=\"decimal\">"));
            }
        }

        foreach (var (key, label, value) in new[]
                 {
                     ("createdFrom", AdminConsoleCopy.FilterCreatedFrom, state.CreatedFrom),
                     ("createdTo", AdminConsoleCopy.FilterCreatedTo, state.CreatedTo),
                     ("updatedFrom", AdminConsoleCopy.FilterUpdatedFrom, state.UpdatedFrom),
                     ("updatedTo", AdminConsoleCopy.FilterUpdatedTo, state.UpdatedTo)
                 })
        {
            html.Append(Field(key, label,
                $"<input type=\"date\" id=\"{key}\" name=\"{key}\" value=\"{E(DateInputValue(value))}\">"));
        }

        html.Append("</div>");

        html.Append("<div class=\"list-options\">");
        html.Append("<div class=\"check toggle\">");
        html.Append($"<input type=\"checkbox\" id=\"includeDeleted\" name=\"includeDeleted\" value=\"true\"{(state.IncludeDeleted ? " checked" : string.Empty)} data-auto>");
        html.Append($"<label for=\"includeDeleted\">{E(AdminConsoleCopy.ShowDeleted)}</label></div>");
        html.Append("<div class=\"field page-size\">");
        html.Append($"<label for=\"limit\">{E(AdminConsoleCopy.PageSize)}</label><select id=\"limit\" name=\"limit\" data-auto>");
        foreach (var size in AdminListState.PageSizes)
        {
            var selected = size == state.Limit ? " selected" : string.Empty;
            html.Append($"<option value=\"{size}\"{selected}>{size}</option>");
        }

        html.Append("</select></div>");
        if (state.Sort != AdminListState.DefaultSort)
            html.Append($"<input type=\"hidden\" name=\"sort\" value=\"{E(state.Sort)}\">");
        if (!state.Descending)
            html.Append("<input type=\"hidden\" name=\"dir\" value=\"asc\">");
        html.Append($"<button type=\"submit\" id=\"apply-filters\">{E(AdminConsoleCopy.ApplyFilters)}</button>");
        html.Append("</div></form>");

        html.Append(Chips(list, state, names));

        // The table, in a region that scrolls sideways by itself on a narrow screen.
        var total = result?.Total ?? 0;
        html.Append($"<div id=\"list-region\" class=\"table-wrap\" role=\"region\" aria-labelledby=\"list-caption\" tabindex=\"0\"");
        if (result is not null)
            html.Append($" data-announce=\"{E(AdminConsoleCopy.Results(total))}\"");
        html.Append('>');
        html.Append($"<table id=\"list-table\" class=\"list\"><caption id=\"list-caption\">{E(list.Screen)}</caption>");
        html.Append("<thead><tr>");
        foreach (var column in list.Columns)
            html.Append(HeaderCell(list, state, column));
        html.Append("</tr></thead><tbody>");

        var colspan = list.Columns.Count;
        if (result is null)
        {
            html.Append($"<tr><td colspan=\"{colspan}\" class=\"state error\"><p id=\"list-error\">{E(AdminConsoleCopy.CouldNotLoadList(list.TypeLabel))}</p>");
            html.Append($"<a id=\"retry\" class=\"button-link\" href=\"{E(canonical)}\">{E(AdminConsoleCopy.Retry)}</a></td></tr>");
        }
        else if (total == 0 && !state.HasFilters)
        {
            html.Append($"<tr><td colspan=\"{colspan}\" class=\"state\"><p id=\"list-empty\">{E(AdminConsoleCopy.NoneYet(list.TypeLabel))}</p></td></tr>");
        }
        else if (total == 0)
        {
            html.Append($"<tr><td colspan=\"{colspan}\" class=\"state\"><p id=\"list-miss\">{E(AdminConsoleCopy.NoResults)}</p>");
            html.Append($"<a id=\"clear-filters\" class=\"button-link\" href=\"{E(state.Cleared().Url(list))}\" data-focus=\"q\">{E(AdminConsoleCopy.ClearFilters)}</a></td></tr>");
        }
        else
        {
            var detailQuery = state.ToQueryString(list);
            foreach (var row in result.Items)
                html.Append(Row(list, row, detailQuery, state.IncludeDeleted));
        }

        html.Append("</tbody></table></div>");

        if (result is not null && total > 0)
            html.Append(Pager(list, state, total));

        // After a delete the list loads itself anew and says what was deleted.
        html.Append(AdminDeletePage.Dialog(list.Kind, canonical));
        return html.ToString();
    }

    private static string Field(string id, string label, string control)
        => $"<div class=\"field\"><label for=\"{id}\">{E(label)}</label>{control}</div>";

    /// <summary>
    /// A typeahead (TD-ADM-UI-na-B05): the visible box is a combobox whose
    /// options the script fills from one page of the read API's matches; the
    /// chosen value travels in the hidden field. Without JavaScript the
    /// current choice stays and its chip removes it.
    /// </summary>
    private static string Picker(string key, string label, string source, string valueKey, string labelKey, string? value, string? shown)
        => $"<div class=\"field picker\" data-source=\"{source}\" data-value-key=\"{valueKey}\" data-label-key=\"{labelKey}\" data-no-matches=\"{E(AdminConsoleCopy.NoMatches)}\">"
           + $"<label for=\"{key}-picker\">{E(label)}</label>"
           + $"<input type=\"text\" id=\"{key}-picker\" role=\"combobox\" aria-autocomplete=\"list\" aria-expanded=\"false\" aria-controls=\"{key}-options\" autocomplete=\"off\" spellcheck=\"false\" maxlength=\"{AdminListQuery.MaxQueryLength}\" value=\"{E(shown)}\">"
           + $"<input type=\"hidden\" id=\"{key}\" name=\"{key}\" value=\"{E(value)}\">"
           + $"<ul id=\"{key}-options\" class=\"options\" role=\"listbox\" aria-label=\"{E(label)}\" hidden></ul>"
           + "</div>";

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

    /// <summary>One removable chip per active filter (TD-ADM-UI-na-B05), then "Clear all filters".</summary>
    private static string Chips(AdminListDefinition list, AdminListState state, ChipNames names)
    {
        var chips = new List<(string Label, string Value, AdminListState Without, bool Deleted)>();
        var page1 = state with { Offset = 0 };
        if (!string.IsNullOrEmpty(state.Q))
            chips.Add((AdminConsoleCopy.ChipSearch, state.Q, page1 with { Q = null }, false));
        if (state.Statuses.Count > 0)
            chips.Add((AdminConsoleCopy.FilterStatus, string.Join(", ", state.Statuses.Select(StatusLabel)), page1 with { Statuses = [] }, false));
        if (state.Participant is not null && list.ParticipantFilter is not null)
            chips.Add((list.ParticipantFilter, names.Participant ?? state.Participant, page1 with { Participant = null }, names.ParticipantDeleted));
        if (state.Artifact is { } artifact)
            chips.Add((AdminConsoleCopy.FilterArtifact, names.Artifact ?? artifact.ToString("D"), page1 with { Artifact = null }, names.ArtifactDeleted));
        if (state.NegotiationId is { } negotiation)
            chips.Add((AdminConsoleCopy.FilterNegotiationId, negotiation.ToString("D"), page1 with { NegotiationId = null }, false));
        if (state.AmountMin is { } amountMin)
            chips.Add((AdminConsoleCopy.FilterAmountMin, amountMin.ToString(CultureInfo.InvariantCulture), page1 with { AmountMin = null }, false));
        if (state.AmountMax is { } amountMax)
            chips.Add((AdminConsoleCopy.FilterAmountMax, amountMax.ToString(CultureInfo.InvariantCulture), page1 with { AmountMax = null }, false));
        if (state.CreatedFrom is { } createdFrom)
            chips.Add((AdminConsoleCopy.FilterCreatedFrom, DateChip(createdFrom), page1 with { CreatedFrom = null }, false));
        if (state.CreatedTo is { } createdTo)
            chips.Add((AdminConsoleCopy.FilterCreatedTo, DateChip(createdTo), page1 with { CreatedTo = null }, false));
        if (state.UpdatedFrom is { } updatedFrom)
            chips.Add((AdminConsoleCopy.FilterUpdatedFrom, DateChip(updatedFrom), page1 with { UpdatedFrom = null }, false));
        if (state.UpdatedTo is { } updatedTo)
            chips.Add((AdminConsoleCopy.FilterUpdatedTo, DateChip(updatedTo), page1 with { UpdatedTo = null }, false));

        if (chips.Count == 0)
            return string.Empty;

        var html = new StringBuilder("<div id=\"chips\" class=\"chips-bar\"><ul class=\"chips\">");
        foreach (var (label, value, without, deleted) in chips)
        {
            html.Append($"<li class=\"chip\"><span>{E(label)}: {E(value)}</span>");
            if (deleted)
                html.Append(Badge(AdminConsoleCopy.Deleted, "deleted"));
            html.Append($"<a href=\"{E(without.Url(list))}\" aria-label=\"{E(AdminConsoleCopy.RemoveChipName(label))}\" data-focus=\"q\">{E(AdminConsoleCopy.RemoveChip)}</a></li>");
        }

        html.Append("</ul>");
        html.Append($"<a id=\"clear-all\" class=\"button-link secondary\" href=\"{E(state.Cleared().Url(list))}\" data-focus=\"q\">{E(AdminConsoleCopy.ClearAllFilters)}</a>");
        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>
    /// A column heading. A sortable one is a button in a small GET form that
    /// asks the server for the list in the new order (TD-ADM-UI-na-B02): the
    /// sorted column carries aria-sort and says its direction in words.
    /// </summary>
    private static string HeaderCell(AdminListDefinition list, AdminListState state, AdminListColumn column)
    {
        if (column.Sort is null)
            return $"<th scope=\"col\">{E(column.Label)}</th>";

        var sorted = state.Sort == column.Sort;
        // The sorted column turns over; another column starts from the top: newest, highest, last.
        var next = state with { Sort = column.Sort, Descending = !sorted || !state.Descending, Offset = 0 };
        var html = new StringBuilder();
        html.Append("<th scope=\"col\"");
        if (sorted)
            html.Append($" aria-sort=\"{(state.Descending ? "descending" : "ascending")}\"");
        html.Append($"><form class=\"sort-form\" method=\"get\" action=\"{list.Path}\">");
        foreach (var (key, value) in QueryPairs(list, next))
            html.Append($"<input type=\"hidden\" name=\"{key}\" value=\"{E(value)}\">");
        html.Append($"<button type=\"submit\" id=\"sort-{column.Sort}\" class=\"sort-button\">{E(column.Label)}");
        if (sorted)
        {
            html.Append($"<span class=\"sort-mark\" aria-hidden=\"true\">{(state.Descending ? "▼" : "▲")}</span>");
            html.Append($"<span class=\"visually-hidden\">{E(state.Descending ? AdminConsoleCopy.SortedDescending : AdminConsoleCopy.SortedAscending)}</span>");
        }

        html.Append("</button></form></th>");
        return html.ToString();
    }

    private static IEnumerable<(string Key, string Value)> QueryPairs(AdminListDefinition list, AdminListState state)
    {
        var query = QueryHelpers.ParseQuery(state.ToQueryString(list));
        foreach (var key in list.QueryKeys)
        {
            if (query.TryGetValue(key, out var value))
                yield return (key, value.ToString());
        }
    }

    private static string Row(AdminListDefinition list, Dictionary<string, object?> row, string listQuery, bool includeDeleted)
    {

        var id = IdOf(row, "id");
        var detail = $"{list.Path}/{id:D}{listQuery}";
        var toggle = includeDeleted ? "?includeDeleted=true" : string.Empty;
        var html = new StringBuilder("<tr>");
        foreach (var column in list.Columns)
        {
            html.Append("<td>");
            switch (column.Key)
            {
                case "id":
                    html.Append($"<a class=\"id\" href=\"{E(detail)}\">{id:D}</a>");
                    break;
                case "name" when list.Kind == AdminListKind.Participants:
                    html.Append(Text(row, "displayName"));
                    break;
                case "name":
                    html.Append($"<a href=\"{E(detail)}\">{Text(row, "name")}</a>");
                    break;
                case "status":
                    html.Append(StatusCell(list, row));
                    break;
                case "created":
                    html.Append(Time(row, "createdAt"));
                    break;
                case "updated":
                    html.Append(UpdatedTime(row));
                    // The row's Delete, in its last cell: the columns stay the test design's.
                    html.Append(AdminDeletePage.RowButton(list.Kind, row));
                    break;
                case "owner":
                    html.Append(ParticipantText(row, "ownerName", "ownerParticipantId"));
                    break;
                case "artifact":
                    html.Append(ArtifactLink(row, toggle, includeDeleted));
                    break;
                case "participants":
                    html.Append(ParticipantText(row, "partyAName", "partyAParticipantId"));
                    html.Append("<span class=\"sep\" aria-hidden=\"true\"> · </span><span class=\"visually-hidden\">, </span>");
                    html.Append(ParticipantText(row, "partyBName", "partyBParticipantId"));
                    break;
                case "negotiationId":
                    html.Append(NegotiationLink(row, toggle, includeDeleted));
                    break;
                case "offering":
                    html.Append(ParticipantText(row, "fromParticipantName", "fromParticipantId"));
                    break;
                case "amount":
                    html.Append(AmountText(row));
                    break;
            }

            html.Append("</td>");
        }

        html.Append("</tr>");
        return html.ToString();
    }

    private static Guid IdOf(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) && v is Guid g ? g : Guid.Empty;

    private static string Text(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) && v is string s && s.Length > 0 ? E(s) : E(AdminConsoleCopy.NoValue);

    private static bool Flag(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) && v is true;

    /// <summary>
    /// A participant by name; by its Core identifier when it has none to show.
    /// A soft-deleted one, with "Show deleted" off, shows its identifier and
    /// "Deleted" (the read gives no name).
    /// </summary>
    private static string ParticipantText(Dictionary<string, object?> row, string nameKey, string subKey)
    {
        if (row.TryGetValue(nameKey, out var name) && name is string n && n.Length > 0)
            return E(n);
        var text = row.TryGetValue(subKey, out var sub) && sub is string s && s.Length > 0 ? E(s) : E(AdminConsoleCopy.NoValue);
        return Flag(row, AdminReadProjection.DeletedKey(nameKey)) ? text + " " + Badge(AdminConsoleCopy.Deleted, "deleted") : text;
    }

    /// <summary>
    /// The artifact by name, linked. A soft-deleted one, with "Show deleted"
    /// off, is its id and "Deleted", not a link (its page would be S-D3).
    /// </summary>
    private static string ArtifactLink(Dictionary<string, object?> row, string toggle, bool includeDeleted)
    {
        if (!row.TryGetValue("artifactId", out var a) || a is not Guid artifactId)
            return E(AdminConsoleCopy.NoValue);
        if (Flag(row, "artifactDeleted") && !includeDeleted)
            return $"<span class=\"id\">{artifactId:D}</span> {Badge(AdminConsoleCopy.Deleted, "deleted")}";
        var name = row.TryGetValue("artifactName", out var n) && n is string s && s.Length > 0 ? s : artifactId.ToString("D");
        return $"<a href=\"{AdminSignedInPage.Artifacts}/{artifactId:D}{toggle}\">{E(name)}</a>";
    }

    /// <summary>An offer's negotiation, linked; a soft-deleted one, with "Show deleted" off, its id and "Deleted".</summary>
    private static string NegotiationLink(Dictionary<string, object?> row, string toggle, bool includeDeleted)
    {
        if (!row.TryGetValue("negotiationId", out var n) || n is not Guid negotiation)
            return E(AdminConsoleCopy.NoValue);
        if (Flag(row, "negotiationDeleted") && !includeDeleted)
            return $"<span class=\"id\">{negotiation:D}</span> {Badge(AdminConsoleCopy.Deleted, "deleted")}";
        return $"<a class=\"id\" href=\"{AdminSignedInPage.Negotiations}/{negotiation:D}{toggle}\">{negotiation:D}</a>";
    }

    /// <summary>"1,234.50 USD": the amount with its currency, or a dash.</summary>
    private static string AmountText(Dictionary<string, object?> row)
    {
        if (!row.TryGetValue("amount", out var a) || a is not decimal amount)
            return E(AdminConsoleCopy.NoValue);
        var currency = row.TryGetValue("currency", out var c) && c is string s ? " " + s : string.Empty;
        return E(amount.ToString("N2", CultureInfo.GetCultureInfo("en-US")) + currency);
    }

    /// <summary>Superseded reads "Superseded (countered)"; every other status as it is named.</summary>
    public static string StatusLabel(string status)
        => status == "Superseded" ? AdminConsoleCopy.SupersededLabel : status;

    private static string Badge(string text, string kind)
        => $"<span class=\"badge badge-{kind}\">{E(text)}</span>";

    /// <summary>The status badge, and beside it (never in its place) "Deleted" with the deleted time (TD-ADM-UI-na-B08).</summary>
    private static string StatusCell(AdminListDefinition list, Dictionary<string, object?> row)
    {
        var html = new StringBuilder("<span class=\"badges\">");
        if (list.Kind == AdminListKind.Participants)
        {
            if (row.TryGetValue("isActive", out var a) && a is bool active)
                html.Append(Badge(active ? AdminConsoleCopy.StatusActive : AdminConsoleCopy.StatusSuspended, active ? "active" : "suspended"));
        }
        else if (row.TryGetValue("status", out var s) && s is string status)
        {
            html.Append(Badge(StatusLabel(status), status.ToLowerInvariant()));
        }

        if (row.TryGetValue("deletedAt", out var d) && d is DateTimeOffset deletedAt)
        {
            html.Append(Badge(AdminConsoleCopy.Deleted, "deleted"));
            html.Append(TimeHtml(deletedAt));
        }

        html.Append("</span>");
        return html.ToString();
    }

    private static string Time(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) && v is DateTimeOffset at ? TimeHtml(at) : E(AdminConsoleCopy.NoValue);

    /// <summary>Updated, or Created for a row never updated: the time the default order sorts by.</summary>
    private static string UpdatedTime(Dictionary<string, object?> row)
        => row.TryGetValue("updatedAt", out var v) && v is DateTimeOffset at ? TimeHtml(at) : Time(row, "createdAt");

    /// <summary>
    /// A time in ET with its label (TD-ADM-UI-na-B15). The UTC value shows in
    /// a tooltip on hover (title) and on keyboard focus (the stylesheet shows
    /// data-utc), so the element takes the focus.
    /// </summary>
    public static string TimeHtml(DateTimeOffset at)
    {
        var utc = at.UtcDateTime;
        var iso = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var utcText = utc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        return $"<time class=\"ts\" datetime=\"{iso}\" tabindex=\"0\" title=\"{utcText}\" data-utc=\"{utcText}\">{E(EasternTime.Format(at))}</time>";
    }

    /// <summary>"Showing a–b of N", Previous, numbered pages and Next, each a link to its page (TD-ADM-UI-na-B06).</summary>
    private static string Pager(AdminListDefinition list, AdminListState state, int total)
    {
        var pages = (total + state.Limit - 1) / state.Limit;
        var page = state.Offset / state.Limit + 1;
        var first = state.Offset + 1;
        var last = Math.Min(state.Offset + state.Limit, total);
        string Href(int p) => E((state with { Offset = (p - 1) * state.Limit }).Url(list));

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

    // ---- the detail page ----

    private static async Task<IResult> DetailPageAsync(
        HttpContext http,
        Guid id,
        AdminAntiForgeryService antiforgery,
        DealowareDbContext db,
        IFieldPolicy policy,
        AdminListDefinition list,
        CancellationToken ct)
    {
        // The list's state rides along, so Back to list restores it and the toggle decides about deleted records.
        var state = AdminListState.Read(list, http.Request);
        var principal = AdminReadProjection.CoreOwnerFrom(http);
        // The record and its edit token from one read, so the edit mode (T05) saves against what the page shows.
        var detail = list.Kind switch
        {
            AdminListKind.Participants => await AdminReadEndpoints.ReadParticipantAsync(db, policy, principal, id, state.IncludeDeleted, ct),
            AdminListKind.Artifacts => await AdminReadEndpoints.ReadArtifactAsync(db, policy, principal, id, state.IncludeDeleted, ct),
            AdminListKind.Negotiations => await AdminReadEndpoints.ReadNegotiationAsync(db, policy, principal, id, state.IncludeDeleted, ct),
            _ => await AdminReadEndpoints.ReadOfferAsync(db, policy, principal, id, state.IncludeDeleted, ct)
        };
        if (detail is null)
            return AdminConsolePages.NotFound(http, antiforgery);

        var record = detail.Dto;
        var edit = AdminEditPage.For(list.Kind, record, detail.EditToken, policy, principal, DateTimeOffset.UtcNow);
        var deletable = AdminDeletePage.Deletable(record);
        var scripts = new List<string>();
        if (edit is not null || deletable)
            scripts.Add(AdminSignedOutPage.DomScriptPath);
        if (edit is not null)
            scripts.Add(AdminEditPage.ScriptPath);
        if (deletable)
            scripts.Add(AdminDeletePage.ScriptPath);
        return AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(list.DetailScreen),
            Heading = list.DetailScreen,
            Section = list.Section,
            BodyHtml = DetailBody(list, state, record, edit, deletable),
            Scripts = scripts
        });
    }

    /// <summary>
    /// The record as text. With an edit plan (<see cref="AdminEditPage"/>),
    /// the Edit button and the actions come first, the list sits in the edit
    /// form, and each editable row carries its hidden control beside its text.
    /// </summary>
    private static string DetailBody(
        AdminListDefinition list, AdminListState state, Dictionary<string, object?> record, AdminEditPage.Plan? edit, bool deletable)
    {
        var toggle = state.IncludeDeleted ? "?includeDeleted=true" : string.Empty;
        var html = new StringBuilder(4096);
        html.Append($"<p class=\"back\"><a id=\"back-to-list\" href=\"{E(state.Url(list))}\">{E(AdminConsoleCopy.BackToList)}</a></p>");
        if (edit is not null)
            html.Append(AdminEditPage.Start(list.Kind, record, edit, deletable ? AdminDeletePage.Button(list.Kind, record) : null));
        else if (deletable)
            html.Append(AdminDeletePage.ActionsBar(list.Kind, record));
        html.Append("<dl id=\"details\" class=\"details\">");

        void Item(string label, string valueHtml, string? editHtml = null)
            => html.Append(editHtml is null
                ? $"<div class=\"detail\"><dt>{E(label)}</dt><dd>{valueHtml}</dd></div>"
                : $"<div class=\"detail editable\"><dt>{E(label)}</dt><dd><div class=\"view\">{valueHtml}</div>{editHtml}</dd></div>");

        string Participant(string nameKey, string subKey)
        {
            var sub = record.TryGetValue(subKey, out var s) && s is string text && text.Length > 0 ? text : null;
            var name = record.TryGetValue(nameKey, out var n) && n is string nameText && nameText.Length > 0 ? nameText : null;
            if (name is null)
            {
                var shown = sub is null ? E(AdminConsoleCopy.NoValue) : E(sub);
                return Flag(record, AdminReadProjection.DeletedKey(nameKey)) ? shown + " " + Badge(AdminConsoleCopy.Deleted, "deleted") : shown;
            }

            return sub is null ? E(name) : $"{E(name)} <span class=\"sub\">({E(sub)})</span>";
        }

        switch (list.Kind)
        {
            case AdminListKind.Participants:
                Item(AdminConsoleCopy.ColumnName, Text(record, "displayName"), edit is null ? null : AdminEditPage.DisplayName(edit, record));
                Item(AdminConsoleCopy.ColumnId, $"<span class=\"id\">{IdOf(record, "id"):D}</span>");
                Item(AdminConsoleCopy.CoreIdentifier, Text(record, "sub"));
                Item(AdminConsoleCopy.LoginEmail, Text(record, "loginEmail"));
                Item(AdminConsoleCopy.ContactEmail, Text(record, "contactEmail"));
                break;
            case AdminListKind.Artifacts:
                Item(AdminConsoleCopy.ColumnName, Text(record, "name"));
                Item(AdminConsoleCopy.ColumnId, $"<span class=\"id\">{IdOf(record, "id"):D}</span>");
                Item(AdminConsoleCopy.Intent, Text(record, "intent"));
                Item(AdminConsoleCopy.ColumnOwner, Participant("ownerName", "ownerParticipantId"),
                    edit is null ? null : AdminEditPage.Owner(edit, record, OwnerShown(record)));
                Item(AdminConsoleCopy.SubjectEntities, Entities(record), edit is null ? null : AdminEditPage.SubjectEntity(edit, record));
                break;
            case AdminListKind.Negotiations:
                Item(AdminConsoleCopy.ColumnId, $"<span class=\"id\">{IdOf(record, "id"):D}</span>");
                Item(AdminConsoleCopy.ColumnArtifact, ArtifactLink(record, toggle, state.IncludeDeleted));
                Item(AdminConsoleCopy.PartyA, Participant("partyAName", "partyAParticipantId"));
                Item(AdminConsoleCopy.PartyB, Participant("partyBName", "partyBParticipantId"));
                Item(AdminConsoleCopy.EndsAt, Time(record, "endsAt"), edit is null ? null : AdminEditPage.EndsAt(edit, record));
                break;
            default:
                Item(AdminConsoleCopy.ColumnId, $"<span class=\"id\">{IdOf(record, "id"):D}</span>");
                Item(AdminConsoleCopy.ColumnNegotiationId, NegotiationLink(record, toggle, state.IncludeDeleted));
                Item(AdminConsoleCopy.ColumnArtifact, ArtifactLink(record, toggle, state.IncludeDeleted));
                Item(AdminConsoleCopy.ColumnOffering, Participant("fromParticipantName", "fromParticipantId"));
                Item(AdminConsoleCopy.Receiving, Participant("toParticipantName", "toParticipantId"));
                Item(AdminConsoleCopy.ColumnAmount, AmountText(record));
                Item(AdminConsoleCopy.Terms, record.TryGetValue("terms", out var t) && t is string terms && terms.Length > 0
                    ? $"<span class=\"free-text\">{E(terms)}</span>"
                    : E(AdminConsoleCopy.NoValue));
                break;
        }

        // The status badge, with "Deleted" and the deleted time beside it for a soft-deleted record.
        Item(AdminConsoleCopy.ColumnStatus, StatusCell(list, record), edit is null ? null : AdminEditPage.Active(edit, record));
        Item(AdminConsoleCopy.ColumnCreated, Time(record, "createdAt"));
        Item(AdminConsoleCopy.ColumnUpdated, UpdatedTime(record));
        html.Append("</dl>");
        if (edit is not null)
            html.Append(AdminEditPage.End(edit));
        // After a delete the page goes to the list, with its state.
        if (deletable)
            html.Append(AdminDeletePage.Dialog(list.Kind, state.Url(list)));
        return html.ToString();
    }

    /// <summary>The owner as the edit mode's picker shows it: the name, or the Core identifier when there is none.</summary>
    private static string OwnerShown(Dictionary<string, object?> record)
        => record.TryGetValue("ownerName", out var n) && n is string name && name.Length > 0 ? name
            : record.TryGetValue("ownerParticipantId", out var s) && s is string sub ? sub : string.Empty;

    /// <summary>An artifact's subject entities: each name, and its description as text.</summary>
    private static string Entities(Dictionary<string, object?> record)
    {
        if (!record.TryGetValue("entities", out var e) || e is not IEnumerable<Dictionary<string, object?>> entities)
            return E(AdminConsoleCopy.NoValue);
        var html = new StringBuilder("<ul class=\"entities\">");
        foreach (var entity in entities)
        {
            html.Append("<li>");
            if (entity.TryGetValue("name", out var name) && name is string n)
                html.Append($"<span class=\"entity-name\">{E(n)}</span>");
            if (entity.TryGetValue("description", out var description) && description is string text && text.Length > 0)
                html.Append($"<span class=\"visually-hidden\">{E(AdminConsoleCopy.Description)}: </span><span class=\"free-text\">{E(text)}</span>");
            html.Append("</li>");
        }

        html.Append("</ul>");
        return html.ToString();
    }
}

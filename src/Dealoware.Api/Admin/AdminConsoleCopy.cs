namespace Dealoware.Api.Admin;

/// <summary>
/// Copy of the signed-in console shell and of the pages built on it in T04.
/// Titles follow "{Screen} · Dealoware admin" (TD-ADM-UI-na-X03).
/// Nav labels, the skip link, S-D3's sentence and the signed-out sentence are
/// the test design's (TD-ADM-UI-na-X03, TD-ADM-179, TD-ADM-UI-auth-17); the
/// stat labels are Spec §6's counts. The lists' columns, search and filter
/// labels, empty states, paging and chips are the test design's
/// (TD-ADM-UI-na-B01 to B15, X04) or draft #39's; the statuses are the
/// status-list note's. Words written new are marked "New copy".
/// </summary>
public static class AdminConsoleCopy
{
    public const string TitleSuffix = " · Dealoware admin";

    /// <summary>The name above the nav, as draft #39 had it; the titles carry it too.</summary>
    public const string Brand = "Dealoware admin";

    public const string SkipLink = "Skip to main content";

    /// <summary>The nav's accessible name, as draft #39 had it.</summary>
    public const string NavLabel = "Admin";

    public const string NavStats = "Stats";
    public const string NavParticipants = "Participants";
    public const string NavArtifacts = "Artifacts";
    public const string NavNegotiations = "Negotiations";
    public const string NavOffers = "Offers";
    public const string NavSecuritySettings = "Security settings";
    public const string SignOut = "Sign out";

    // ---- S-B1 Stats (Spec §6) ----

    public const string StatsHeading = "Stats";
    public const string StatParticipants = "Participants";
    public const string StatOpenNegotiations = "Open negotiations";
    public const string StatOffers = "Offers";
    public const string StatAccepts = "Accepts";
    public const string StatDeclines = "Declines";

    /// <summary>"As of {time} ET" (TD-ADM-UI-na-B09), the time as in TD-ADM-UI-na-B15.</summary>
    public const string AsOfPrefix = "As of ";

    /// <summary>Each Stats total links to its list (TD-ADM-UI-na-B09); Refresh counts again.</summary>
    public const string Refresh = "Refresh";

    /// <summary>A total that could not be counted (TD-ADM-UI-na-B09), with Retry.</summary>
    public const string CouldNotLoad = "Couldn't load";

    public const string Retry = "Retry";

    // ---- Lists (S-B2 to S-B5) ----

    /// <summary>Column headings (TD-ADM-UI-na-B01).</summary>
    public const string ColumnName = "Name";
    public const string ColumnId = "ID";
    public const string ColumnStatus = "Status";
    public const string ColumnCreated = "Created";
    public const string ColumnUpdated = "Updated";
    public const string ColumnOwner = "Owner participant";
    public const string ColumnArtifact = "Artifact";
    public const string ColumnParticipants = "Participants";
    public const string ColumnNegotiationId = "Negotiation ID";
    public const string ColumnOffering = "Offering participant";
    public const string ColumnAmount = "Amount";

    /// <summary>Search labels, as draft #39 had them (TD-ADM-UI-na-B07 names the Negotiations one).</summary>
    public const string SearchParticipants = "Search by display name or Core identifier";
    public const string SearchArtifacts = "Search by artifact name";
    public const string SearchNegotiations = "Search by artifact or participant name";
    public const string SearchOffers = "Search by artifact, offering participant, or negotiation id";

    /// <summary>Clears the search (TD-ADM-UI-na-B07).</summary>
    public const string ClearSearch = "Clear";

    /// <summary>Filter labels (TD-ADM-UI-na-B05, draft #39).</summary>
    public const string FilterStatus = "Status";
    public const string FilterParticipant = "Participant";
    public const string FilterOwner = "Owner participant";
    public const string FilterArtifact = "Artifact";
    public const string FilterNegotiationId = "Negotiation ID";
    public const string FilterAmountMin = "Amount min";
    public const string FilterAmountMax = "Amount max";
    public const string FilterCreatedFrom = "Created from";
    public const string FilterCreatedTo = "Created to";
    public const string FilterUpdatedFrom = "Updated from";
    public const string FilterUpdatedTo = "Updated to";

    /// <summary>The search, named on its chip, as draft #39 had it.</summary>
    public const string ChipSearch = "Search";

    /// <summary>The soft-deleted toggle (TD-ADM-UI-na-B08).</summary>
    public const string ShowDeleted = "Show deleted";

    /// <summary>Page size select, as draft #39 had it (TD-ADM-UI-na-B06).</summary>
    public const string PageSize = "Page size";

    /// <summary>Sends the filters typed into the form. New copy.</summary>
    public const string ApplyFilters = "Apply filters";

    /// <summary>Removes one chip: "Remove", named "Remove {filter} filter", as draft #39 had it.</summary>
    public const string RemoveChip = "Remove";

    public static string RemoveChipName(string filter) => "Remove " + filter + " filter";

    public const string ClearAllFilters = "Clear all filters";

    /// <summary>The filter-miss button (TD-ADM-UI-na-B14).</summary>
    public const string ClearFilters = "Clear filters";

    /// <summary>A picker found nothing for the typed text. New copy.</summary>
    public const string NoMatches = "No matches";

    /// <summary>Visually hidden after the sorted column's heading, for screen readers. New copy.</summary>
    public const string SortedDescending = " (sorted descending)";
    public const string SortedAscending = " (sorted ascending)";

    /// <summary>Participant statuses, and Superseded as its badge reads (status-list note; TD-ADM-063).</summary>
    public const string StatusActive = "Active";
    public const string StatusSuspended = "Suspended";
    public const string SupersededLabel = "Superseded (countered)";

    /// <summary>The badge beside the status of a soft-deleted row (TD-ADM-UI-na-B08).</summary>
    public const string Deleted = "Deleted";

    /// <summary>Paging (TD-ADM-UI-na-B06).</summary>
    public const string Previous = "Previous";
    public const string Next = "Next";

    public static string PageName(int page) => "Page " + page.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>"Showing 101–200 of 1,234" (TD-ADM-UI-na-B06).</summary>
    public static string Showing(int first, int last, int total)
        => "Showing " + Count(first) + "–" + Count(last) + " of " + Count(total);

    /// <summary>The polite announcement after a search, filter, sort or page change (TD-ADM-UI-na-B07).</summary>
    public static string Results(int total) => Count(total) + " results";

    /// <summary>Empty table (TD-ADM-UI-na-B14): "No {type} yet."</summary>
    public static string NoneYet(string type) => "No " + type + " yet.";

    /// <summary>Filter miss (TD-ADM-UI-na-B14).</summary>
    public const string NoResults = "No results match these filters.";

    /// <summary>A list that could not be read (TD-ADM-UI-na-X04): "We couldn't load {type}."</summary>
    public static string CouldNotLoadList(string type) => "We couldn't load " + type + ".";

    /// <summary>A value the record does not have: a dash. New copy.</summary>
    public const string NoValue = "—";

    // ---- Details (S-B6) ----

    /// <summary>Detail headings. New copy.</summary>
    public const string ParticipantDetails = "Participant details";
    public const string ArtifactDetails = "Artifact details";
    public const string NegotiationDetails = "Negotiation details";
    public const string OfferDetails = "Offer details";

    /// <summary>Back to the list with its state (TD-ADM-UI-na-B06), as draft #39 had it.</summary>
    public const string BackToList = "Back to list";

    /// <summary>
    /// Detail labels that are not already a column heading. New copy, except
    /// Ends at and Terms (TD-ADM-UI-na-B10) and Intent (the artifact's field).
    /// </summary>
    public const string CoreIdentifier = "Core identifier";
    public const string LoginEmail = "Login email";
    public const string ContactEmail = "Contact email";
    public const string Intent = "Intent";
    public const string SubjectEntities = "Subject entities";
    public const string Description = "Description";
    public const string PartyA = "Participant A";
    public const string PartyB = "Participant B";
    public const string EndsAt = "Ends at";
    public const string Receiving = "Receiving participant";
    public const string Terms = "Terms";

    private static string Count(int n) => n.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    // ---- S-D3 ----

    /// <summary>S-D3's title. New copy.</summary>
    public const string NotFoundTitle = "Page not found" + TitleSuffix;

    public const string NotFoundHeading = "We can't find that page.";

    /// <summary>S-D3's link to Stats. New copy.</summary>
    public const string GoToStats = "Go to Stats";

    public static string Title(string screen) => screen + TitleSuffix;

    // ---- S-D1 and S-D2: the audit log (T07) ----

    /// <summary>The nav entry (TD-ADM-UI-na-X03); also S-D1's title, heading and table caption.</summary>
    public const string NavAuditLog = "Audit log";

    /// <summary>Under S-D1's heading, as draft #42 had it.</summary>
    public const string AuditLede = "Read-only record of admin authentication and entity changes. This log cannot be edited or deleted.";

    /// <summary>
    /// Column headings (TD-ADM-UI-na-X02). Action and Entity type also name
    /// their filters and chips; all six label the entry page's fields.
    /// </summary>
    public const string AuditColumnTime = "Time (ET)";
    public const string AuditColumnActor = "Actor";
    public const string AuditColumnAction = "Action";
    public const string AuditColumnEntityType = "Entity type";
    public const string AuditColumnEntityId = "Entity ID";
    public const string AuditColumnOutcome = "Outcome";

    /// <summary>Filter labels, as draft #42 had them: every entity type, and the range of days.</summary>
    public const string AuditAllTypes = "All types";
    public const string AuditFilterFrom = "From";
    public const string AuditFilterTo = "To";

    /// <summary>"No audit entries yet." and "We couldn't load the audit log.", as draft #42 had them.</summary>
    public const string AuditEntriesType = "audit entries";
    public const string AuditLogType = "the audit log";

    /// <summary>The outcome as the Outcome column words it. New copy.</summary>
    public const string AuditOutcomeSuccess = "Success";
    public const string AuditOutcomeFailure = "Failure";
    public const string AuditOutcomeDenied = "Denied";

    /// <summary>S-D2: its title and heading, its way back and its two sections, as draft #42 had them.</summary>
    public const string AuditEntry = "Audit entry";
    public const string BackToAuditLog = "Back to audit log";
    public const string AuditEntryHeading = "Entry";
    public const string AuditBeforeAfter = "Before and after";
    public const string AuditBefore = "Before";
    public const string AuditAfter = "After";

    /// <summary>The marker on a field whose before and after differ (TD-ADM-UI-na-X02).</summary>
    public const string AuditChanged = "Changed";

    /// <summary>A side with no fields to show, as draft #42 had it.</summary>
    public const string AuditNoFields = "No allowed fields.";

    /// <summary>The first characters of the keyed hash of the IP, as draft #42 named it.</summary>
    public const string AuditIpPrefix = "IP HMAC prefix";

    /// <summary>The failure reason class of an entry (Spec §10 Context). New copy.</summary>
    public const string AuditReason = "Reason";

    // ---- Edit mode on the detail pages (T05, A7 Step 9: UXR-B14 to B17) ----
    // From the test design: Save, Cancel, "Saving…", "Changes saved.",
    // "Discard your changes?", Discard, Keep editing, Reload, Retry, "Nothing
    // was changed.", "You can't do that.", the Expire dialog's title, body,
    // buttons, "Expiring…" and "Negotiation {ID} expired.". The field
    // sentences and the 409 are the edit API's own (AdminEditCopy). Words
    // written new are marked "New copy".

    /// <summary>Turns a detail page into its edit mode. New copy.</summary>
    public const string Edit = "Edit";

    public const string Save = "Save";
    public const string Saving = "Saving…";
    public const string CancelEdit = "Cancel";
    public const string ChangesSaved = "Changes saved.";

    public const string DiscardTitle = "Discard your changes?";
    public const string Discard = "Discard";
    public const string KeepEditing = "Keep editing";

    /// <summary>Heads the error summary above the fields. New copy.</summary>
    public const string ErrorSummaryTitle = "Your changes were not saved.";

    /// <summary>A change the server refused with the generic 400: the test design's two sentences.</summary>
    public const string EditRefused = "You can't do that. Nothing was changed.";

    public const string NothingChanged = "Nothing was changed.";
    public const string Reload = "Reload";

    /// <summary>Field labels in edit mode. New copy, except Description and Owner participant (already the page's).</summary>
    public const string DisplayNameLabel = "Display name";
    public const string SubjectEntity = "Subject entity";
    public const string EntityNameLabel = "Name";

    /// <summary>After the label of a field that must have a value (TD-ADM-UI-na-B10: required marked with text). New copy.</summary>
    public const string RequiredSuffix = " (required)";

    /// <summary>Under Ends at: the zone the time is typed in. New copy.</summary>
    public const string EndsAtHint = "Eastern Time (ET).";

    /// <summary>Close (new copy, as the Expire dialog words it) and Expire (UXR-B14).</summary>
    public const string CloseNegotiation = "Close negotiation";
    public const string Closing = "Closing…";
    public const string ExpireNegotiation = "Expire negotiation";
    public const string Expiring = "Expiring…";

    /// <summary>The second button of the Close and Expire dialogs (UXR-B14).</summary>
    public const string DialogCancel = "Cancel";

    public static string CloseNegotiationTitle(string id) => "Close negotiation " + id + "?";
    public static string ExpireNegotiationTitle(string id) => "Expire negotiation " + id + "?";
    public static string NegotiationClosed(string id) => "Negotiation " + id + " closed.";
    public static string NegotiationExpired(string id) => "Negotiation " + id + " expired.";

    /// <summary>
    /// The body of the Close and Expire dialogs, from the server's count
    /// (UXR-B14): "3 open offers will be cancelled.", "No open offers will be
    /// cancelled."; the one-offer form is new copy.
    /// </summary>
    public static string OpenOffersCancelled(int count) => count switch
    {
        0 => "No open offers will be cancelled.",
        1 => "1 open offer will be cancelled.",
        _ => Count(count) + " open offers will be cancelled."
    };

    /// <summary>An offer's one admin change (E12). New copy.</summary>
    public const string CancelOffer = "Cancel offer";
    public const string Cancelling = "Cancelling…";
    public const string KeepOffer = "Keep offer";
    public const string CancelOfferBody = "Its amount, currency and terms stay as sent. Cancelled is final.";

    public static string CancelOfferTitle(string id) => "Cancel offer " + id + "?";
    public static string OfferCancelled(string id) => "Offer " + id + " cancelled.";
}

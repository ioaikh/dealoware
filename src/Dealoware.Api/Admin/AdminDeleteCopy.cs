namespace Dealoware.Api.Admin;

/// <summary>
/// Sentences the admin delete API answers with (A7 Step 10), and the words
/// of the delete dialog on the console's pages (T06b). None repeats a
/// stored value.
/// <para>
/// The exact wording is still open (test design OQ6): no document gives it,
/// so these are new sentences, kept in one place to be replaced.
/// </para>
/// </summary>
public static class AdminDeleteCopy
{
    /// <summary>The <c>error</c> of a 409 whose confirm token cannot be used.</summary>
    public const string TokenInvalidError = "ConfirmTokenInvalid";

    /// <summary>The <c>error</c> of a 409 for an artifact a negotiation still refers to.</summary>
    public const string BlockedError = "Blocked";

    /// <summary>
    /// The confirm token is unknown, used, run out, made in another session or
    /// for another record. One sentence for all of them.
    /// </summary>
    public const string TokenInvalid =
        "This delete confirmation is no longer valid. Nothing was deleted. Open the delete again.";

    /// <summary>
    /// The record, or a row that would go with it, changed after the delete
    /// was opened (a party acted, or another admin write landed).
    /// </summary>
    public const string Changed =
        "This record or something deleted with it changed after you opened the delete. Nothing was deleted. Open the delete again.";

    /// <summary>Spec §11.2: an artifact is not deleted while a negotiation that is not deleted refers to it.</summary>
    public const string ArtifactBlocked =
        "This artifact cannot be deleted while negotiations refer to it. Delete those negotiations first.";

    // ---- The delete dialog (T06b; UXR-C01 to C07, test design TD-ADM-UI-del-01 to del-06) ----
    // From the test design: Delete, the title, the undo statement, the
    // destructive button, Cancel, "Deleting…", the expired sentence, the
    // blocked artifact's title, lede and Close, "Type {value} to confirm",
    // "{Type} {name} deleted.", "Nothing was deleted." and Retry. Words written
    // new are marked "New copy". A template's {name} and {n} are filled in by
    // console-delete.js with the delete-intent's own values.

    /// <summary>Stands for a name or a value in a template the page fills in.</summary>
    public const string NamePlaceholder = "{name}";

    /// <summary>Stands for a count in a template the page fills in.</summary>
    public const string CountPlaceholder = "{n}";

    /// <summary>The control that opens the dialog: beside Edit on a detail page, and on each list row.</summary>
    public const string Delete = "Delete";

    /// <summary>The four kinds, as the dialog names them. "participant" in "Delete participant Alex Rivera?".</summary>
    public const string ParticipantNoun = "participant";
    public const string ArtifactNoun = "artifact";
    public const string NegotiationNoun = "negotiation";
    public const string OfferNoun = "offer";

    /// <summary>UXR-C02: "Delete {type} {display name}?". A negotiation and an offer are named by their ID.</summary>
    public static string DialogTitle(string noun, string name) => "Delete " + noun + " " + name + "?";

    /// <summary>UXR-C02: the destructive button names the action, "Delete negotiation".</summary>
    public static string ConfirmLabel(string noun) => "Delete " + noun;

    public const string Cancel = "Cancel";

    /// <summary>UXR-C04: the destructive button while its one request is out.</summary>
    public const string Deleting = "Deleting…";

    /// <summary>UXR-C01, Product's C01 interim decision.</summary>
    public const string Undo = "This can't be restored from the admin.";

    /// <summary>The record's ID in the dialog. New copy.</summary>
    public static string IdLine(string id) => "ID: " + id;

    /// <summary>Above the counts of what goes with the record (UXR-C02: the cascade as a list). New copy.</summary>
    public const string AlsoDeleted = "Also deleted with it:";

    /// <summary>Nothing goes with the record. New copy.</summary>
    public const string NothingElse = "Nothing else is deleted with it.";

    /// <summary>The counts in the list. New copy.</summary>
    public const string OneNegotiation = "1 negotiation";

    public static string ManyNegotiations(string count) => count + " negotiations";

    public const string OneOffer = "1 offer";

    public static string ManyOffers(string count) => count + " offers";

    /// <summary>
    /// Spec §11.2 and §11.3, UXR-C06: an open negotiation is named as open,
    /// with its offers' count, and the dialog says it leaves the lists. New copy.
    /// </summary>
    public const string OpenNegotiationNoOffers =
        "This negotiation is open. It will no longer show in the lists unless “Show deleted” is on.";

    public const string OpenNegotiationOneOffer =
        "This negotiation is open and has 1 offer. They will no longer show in the lists unless “Show deleted” is on.";

    public static string OpenNegotiationOffers(string count)
        => "This negotiation is open and has " + count + " offers. They will no longer show in the lists unless “Show deleted” is on.";

    /// <summary>A participant's delete takes open negotiations with it (Spec §11.3). New copy.</summary>
    public const string OneOpenNegotiation =
        "1 of these negotiations is open. It and its offers will no longer show in the lists unless “Show deleted” is on.";

    public static string OpenNegotiations(string count)
        => count + " of these negotiations are open. They and their offers will no longer show in the lists unless “Show deleted” is on.";

    /// <summary>UXR-C06: the typed confirmation's label; the value is the participant's name or the negotiation's ID.</summary>
    public static string TypeToConfirm(string value) => "Type " + value + " to confirm";

    /// <summary>UXR-C05: an artifact that negotiations still use.</summary>
    public const string BlockedTitle = "This artifact can't be deleted";

    public const string BlockedLede = "It is used by these negotiations:";

    /// <summary>The server lists at most 100 of them; the rest are counted. New copy.</summary>
    public static string BlockedMore(string count) => "And " + count + " more.";

    public const string Close = "Close";

    /// <summary>
    /// UXR-C04: the confirm token ran out, was used, or another tab asked for
    /// a newer one; the dialog asks again, with fresh counts. The test design
    /// says "This confirmation expired."; that is not true when another tab
    /// replaced the token, so the wording is new copy for the owner to approve.
    /// </summary>
    public const string Expired = "This confirmation is no longer valid. Review the details again.";

    /// <summary>
    /// What went with the record changed after the dialog opened (the
    /// delete API's <c>Conflict</c>); the dialog asks again, with fresh
    /// counts. New copy.
    /// </summary>
    public const string ChangedReview =
        "This record changed after this dialog opened. Nothing was deleted. Review the details again.";

    /// <summary>UXR-C07: a delete the server refused or rolled back; the dialog stays open with Retry.</summary>
    public const string NothingDeleted = "Nothing was deleted.";

    public const string Retry = AdminConsoleCopy.Retry;

    /// <summary>The record is not there any more (deleted already, or unknown). New copy.</summary>
    public const string NotFound = "We can't find this record. It may have been deleted already.";

    /// <summary>No clear answer to the delete-intent, which changes nothing: the console's retry sentence.</summary>
    public const string Unclear = AdminSignInCopy.SubmitRetry;

    /// <summary>
    /// No clear answer to the DELETE itself (a time-out, a dropped
    /// connection, an answer the page does not know): it may have gone
    /// through. New copy.
    /// </summary>
    public const string UnclearDelete = "We couldn't confirm the delete. Reload the page to see whether it went through.";

    /// <summary>UXR-C07: said on the list after the delete, "Participant Alex Rivera deleted."</summary>
    public static string DeletedNote(string noun, string name)
        => char.ToUpperInvariant(noun[0]) + noun[1..] + " " + name + " deleted.";
}

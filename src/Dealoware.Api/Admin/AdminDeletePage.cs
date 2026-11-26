using System.Text;
using System.Text.Json;

namespace Dealoware.Api.Admin;

/// <summary>
/// The delete dialog of the console (T06b; A7 Step 10, UXR-C01 to C07): what
/// the server writes for it on a detail page (S-B6) and on a list (S-B2 to
/// S-B5). The dialog itself is filled in by <see cref="ScriptPath"/> from the
/// delete API's own answers (<see cref="AdminDeleteEndpoints"/>): what goes
/// with the record, the typed confirmation, the blocked artifact and where the
/// page goes afterwards.
/// <para>
/// A record that is soft-deleted has no Delete control. Every control starts
/// hidden; the script shows it, so without JavaScript nothing can be deleted
/// (the delete needs the intent's confirm token, which only the script asks
/// for). The dialog's words travel as one JSON object in an attribute, every
/// one of them from <see cref="AdminDeleteCopy"/>; no stored value is in it.
/// </para>
/// </summary>
public static class AdminDeletePage
{
    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/console-delete.js";

    /// <summary>The delete API's name for a kind: /admin/api/{type}/{id}.</summary>
    public static string ApiType(AdminListKind kind) => kind switch
    {
        AdminListKind.Participants => AdminDeleteEndpoints.Participants,
        AdminListKind.Artifacts => AdminDeleteEndpoints.Artifacts,
        AdminListKind.Negotiations => AdminDeleteEndpoints.Negotiations,
        _ => AdminDeleteEndpoints.Offers
    };

    public static string Noun(AdminListKind kind) => kind switch
    {
        AdminListKind.Participants => AdminDeleteCopy.ParticipantNoun,
        AdminListKind.Artifacts => AdminDeleteCopy.ArtifactNoun,
        AdminListKind.Negotiations => AdminDeleteCopy.NegotiationNoun,
        _ => AdminDeleteCopy.OfferNoun
    };

    private static string E(string? value) => AdminSignedOutPage.Encode(value);

    /// <summary>A record that is not soft-deleted can be deleted; one that is, cannot again.</summary>
    internal static bool Deletable(Dictionary<string, object?> record)
        => !(record.TryGetValue("deletedAt", out var deleted) && deleted is DateTimeOffset)
           && IdOf(record) != Guid.Empty;

    private static Guid IdOf(Dictionary<string, object?> record)
        => record.TryGetValue("id", out var v) && v is Guid g ? g : Guid.Empty;

    /// <summary>
    /// What the control calls the record until the intent answers: a
    /// participant's display name, an artifact's name, otherwise (or when
    /// there is none to show) its ID.
    /// </summary>
    internal static string NameOf(AdminListKind kind, Dictionary<string, object?> record)
    {
        var key = kind switch
        {
            AdminListKind.Participants => "displayName",
            AdminListKind.Artifacts => "name",
            _ => null
        };
        return key is not null && record.TryGetValue(key, out var v) && v is string name && name.Length > 0
            ? name
            : IdOf(record).ToString("D");
    }

    private static string Data(Dictionary<string, object?> record, AdminListKind kind)
        => $" data-delete-id=\"{IdOf(record):D}\" data-delete-name=\"{E(NameOf(kind, record))}\"";

    /// <summary>A detail page's Delete, beside Edit in the actions bar.</summary>
    internal static string Button(AdminListKind kind, Dictionary<string, object?> record)
        => $"<button type=\"button\" id=\"delete-button\" class=\"secondary delete-open\"{Data(record, kind)} hidden>{E(AdminDeleteCopy.Delete)}</button>";

    /// <summary>A detail page with no edit plan: its Delete in an actions bar of its own.</summary>
    internal static string ActionsBar(AdminListKind kind, Dictionary<string, object?> record)
        => $"<div id=\"delete-actions\" class=\"record-actions\" hidden>{Button(kind, record)}</div>";

    /// <summary>
    /// A list row's Delete, at the end of its last cell (no column is added:
    /// the columns are the test design's). Its name says which row: "Delete
    /// Alex Rivera" to a screen reader. None on a soft-deleted row.
    /// </summary>
    internal static string RowButton(AdminListKind kind, Dictionary<string, object?> row)
        => Deletable(row)
            ? $"<span class=\"row-actions\"><button type=\"button\" class=\"secondary delete-open\"{Data(row, kind)} hidden>{E(AdminDeleteCopy.Delete)}"
              + $"<span class=\"visually-hidden\"> {E(NameOf(kind, row))}</span></button></span>"
            : string.Empty;

    /// <summary>
    /// The dialog, empty until the script fills it: its title, its body (the
    /// record and what goes with it, which describes the dialog), the typed
    /// confirmation's place and its buttons. <paramref name="returnTo"/> is
    /// the list, with its state, the page goes to after a delete.
    /// </summary>
    internal static string Dialog(AdminListKind kind, string returnTo)
    {
        var html = new StringBuilder(4096);
        html.Append("<dialog id=\"delete-dialog\" class=\"edit-dialog delete-dialog\" aria-modal=\"true\"");
        html.Append(" aria-labelledby=\"delete-dialog-title\" aria-describedby=\"delete-dialog-content\"");
        html.Append($" data-type=\"{ApiType(kind)}\" data-return=\"{E(returnTo)}\" data-copy=\"{E(CopyJson(kind))}\">");
        html.Append("<div class=\"edit-dialog-inner\"><h2 id=\"delete-dialog-title\"></h2>");
        html.Append("<div id=\"delete-dialog-content\"></div><div id=\"delete-dialog-field\"></div>");
        html.Append("<div id=\"delete-dialog-actions\" class=\"edit-dialog-actions\"></div></div></dialog>");
        return html.ToString();
    }

    /// <summary>Every word the dialog may show for this kind, templates with {name} or {n} included.</summary>
    internal static Dictionary<string, string> Copy(AdminListKind kind)
    {
        const string name = AdminDeleteCopy.NamePlaceholder;
        const string n = AdminDeleteCopy.CountPlaceholder;
        var noun = Noun(kind);
        return new Dictionary<string, string>
        {
            ["title"] = AdminDeleteCopy.DialogTitle(noun, name),
            ["confirm"] = AdminDeleteCopy.ConfirmLabel(noun),
            ["cancel"] = AdminDeleteCopy.Cancel,
            ["close"] = AdminDeleteCopy.Close,
            ["busy"] = AdminDeleteCopy.Deleting,
            ["undo"] = AdminDeleteCopy.Undo,
            ["id"] = AdminDeleteCopy.IdLine(name),
            ["also"] = AdminDeleteCopy.AlsoDeleted,
            ["none"] = AdminDeleteCopy.NothingElse,
            ["negotiationsOne"] = AdminDeleteCopy.OneNegotiation,
            ["negotiationsMany"] = AdminDeleteCopy.ManyNegotiations(n),
            ["offersOne"] = AdminDeleteCopy.OneOffer,
            ["offersMany"] = AdminDeleteCopy.ManyOffers(n),
            ["openNone"] = AdminDeleteCopy.OpenNegotiationNoOffers,
            ["openOne"] = AdminDeleteCopy.OpenNegotiationOneOffer,
            ["openMany"] = AdminDeleteCopy.OpenNegotiationOffers(n),
            ["openAmongOne"] = AdminDeleteCopy.OneOpenNegotiation,
            ["openAmongMany"] = AdminDeleteCopy.OpenNegotiations(n),
            ["typed"] = AdminDeleteCopy.TypeToConfirm(name),
            ["blockedTitle"] = AdminDeleteCopy.BlockedTitle,
            ["blockedLede"] = AdminDeleteCopy.BlockedLede,
            ["blockedMore"] = AdminDeleteCopy.BlockedMore(n),
            ["expired"] = AdminDeleteCopy.Expired,
            ["changed"] = AdminDeleteCopy.ChangedReview,
            ["nothing"] = AdminDeleteCopy.NothingDeleted,
            ["retry"] = AdminDeleteCopy.Retry,
            ["notFound"] = AdminDeleteCopy.NotFound,
            ["unclear"] = AdminDeleteCopy.Unclear,
            ["note"] = AdminDeleteCopy.DeletedNote(noun, name)
        };
    }

    private static string CopyJson(AdminListKind kind) => JsonSerializer.Serialize(Copy(kind));
}

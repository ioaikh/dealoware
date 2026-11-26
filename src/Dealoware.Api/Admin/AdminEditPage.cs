using System.Globalization;
using System.Text;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Admin;

/// <summary>
/// What a detail page (S-B6) lets the Core owner change, and how much of it
/// may be shown: edit mode on the same address (A7 Step 9, UXR-B14 to B17).
/// <para>
/// A control is written only when the field policy lets the owner write that
/// field (the same question the edit API asks, so there is no second list of
/// permissions), the API has a path for it, and the record is in a state
/// that takes the change: a participant's display name and Active or
/// Suspended; an artifact's subject entity name and description and its
/// owner; an open negotiation's end time while it has not lapsed; Close
/// (open, not lapsed) and Expire (open) on a negotiation; Cancel on an open
/// offer. An offer's amount, currency and terms are never an input: on an
/// offer the console can only cancel (owner's decision, 26 Nov 2026).
/// </para>
/// <para>
/// A soft-deleted record, and a read that carries no edit token, gets
/// nothing: the page only reads. Every control starts hidden; the page's
/// script (<see cref="ScriptPath"/>) shows the Edit button and the actions,
/// so without JavaScript the page stays read-only.
/// </para>
/// </summary>
public static class AdminEditPage
{
    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/console-edit.js";

    /// <summary>The four API paths a detail page sends its one PATCH to.</summary>
    public static string ApiPath(AdminListKind kind, Guid id) => kind switch
    {
        AdminListKind.Participants => "/admin/api/participants/",
        AdminListKind.Artifacts => "/admin/api/artifacts/",
        AdminListKind.Negotiations => "/admin/api/negotiations/",
        _ => "/admin/api/offers/"
    } + id.ToString("D");

    /// <summary>What this record's page offers. <see cref="Any"/> false: nothing, the page only reads.</summary>
    internal sealed record Plan(
        string Token,
        bool DisplayName,
        bool Active,
        bool EntityName,
        bool EntityDescription,
        bool Owner,
        bool EndsAt,
        bool Close,
        bool Expire,
        bool CancelOffer)
    {
        public bool Fields => DisplayName || Active || EntityName || EntityDescription || Owner || EndsAt;

        public bool Any => Fields || Close || Expire || CancelOffer;
    }

    private static string E(string? value) => AdminSignedOutPage.Encode(value);

    private static bool Writes(IFieldPolicy policy, FieldPrincipal principal, FieldClass field)
        => policy.Evaluate(principal, field, FieldAction.Write, FieldResourceContext.ForSelfProfile(string.Empty));

    /// <summary>
    /// The plan for one record as the detail read gave it, or null when the
    /// page only reads (a deleted record, a read with no edit token). A plan
    /// with nothing in it (<see cref="Plan.Any"/> false: a closed
    /// negotiation, an offer that is not open) still lets the page announce
    /// what was just done to it.
    /// </summary>
    internal static Plan? For(
        AdminListKind kind,
        Dictionary<string, object?> record,
        string? token,
        IFieldPolicy policy,
        FieldPrincipal principal,
        DateTimeOffset now)
    {
        if (token is null || record.TryGetValue("deletedAt", out var deleted) && deleted is DateTimeOffset)
            return null;

        bool W(FieldClass field) => Writes(policy, principal, field);
        var status = record.TryGetValue("status", out var s) ? s as string : null;
        Plan plan;
        switch (kind)
        {
            case AdminListKind.Participants:
                plan = new Plan(token,
                    DisplayName: record.ContainsKey("displayName") && W(FieldClass.DisplayName),
                    Active: record.ContainsKey("isActive") && W(FieldClass.ParticipantActive),
                    false, false, false, false, false, false, false);
                break;
            case AdminListKind.Artifacts:
                var entities = Entities(record);
                plan = new Plan(token, false, false,
                    EntityName: entities.Count > 0 && entities.All(e => e.Name is not null) && W(FieldClass.ArtifactName),
                    EntityDescription: entities.Count > 0 && entities.All(e => e.HasDescription) && W(FieldClass.ArtifactDescription),
                    Owner: record.ContainsKey("ownerParticipantId") && W(FieldClass.ArtifactOwnerParticipantId),
                    false, false, false, false);
                break;
            case AdminListKind.Negotiations:
                var open = status == "Open";
                var lapsed = record.TryGetValue("endsAt", out var e) && e is DateTimeOffset endsAt && now >= endsAt;
                var statusWrite = record.ContainsKey("openOfferCount") && W(FieldClass.NegotiationStatus)
                                  && (Count(record) == 0 || W(FieldClass.OfferStatus));
                plan = new Plan(token, false, false, false, false, false,
                    EndsAt: open && !lapsed && record.ContainsKey("endsAt") && W(FieldClass.NegotiationEndsAt),
                    Close: open && !lapsed && statusWrite,
                    Expire: open && statusWrite,
                    false);
                break;
            default:
                plan = new Plan(token, false, false, false, false, false, false, false, false,
                    CancelOffer: status == "Open" && W(FieldClass.OfferStatus));
                break;
        }

        return plan;
    }

    private sealed record EntityRow(Guid Id, string? Name, bool HasDescription, string? Description);

    private static List<EntityRow> Entities(Dictionary<string, object?> record)
    {
        if (!record.TryGetValue("entities", out var e) || e is not IEnumerable<Dictionary<string, object?>> entities)
            return [];
        return entities.Select(x => new EntityRow(
            x.TryGetValue("id", out var id) && id is Guid g ? g : Guid.Empty,
            x.TryGetValue("name", out var n) ? n as string : null,
            x.ContainsKey("description"),
            x.TryGetValue("description", out var d) ? d as string : null)).ToList();
    }

    private static int Count(Dictionary<string, object?> record)
        => record.TryGetValue("openOfferCount", out var c) && c is int n ? n : 0;

    private static Guid IdOf(Dictionary<string, object?> record)
        => record.TryGetValue("id", out var v) && v is Guid g ? g : Guid.Empty;

    // ---- markup ----

    /// <summary>
    /// Before the details list: the record's settings for the script (its API
    /// path and edit token, the shared sentences), the Edit button and the
    /// actions, and, when a field can change, the start of the edit form with
    /// its error summary. Everything the script shows starts hidden.
    /// </summary>
    internal static string Start(AdminListKind kind, Dictionary<string, object?> record, Plan plan)
    {
        var id = IdOf(record);
        var html = new StringBuilder(2048);
        html.Append("<div id=\"record\" class=\"record\"");
        html.Append($" data-id=\"{id:D}\" data-api=\"{E(ApiPath(kind, id))}\" data-etag=\"{E(AdminEditToken.ToHeader(plan.Token))}\"");
        html.Append($" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\" data-refused=\"{E(AdminConsoleCopy.EditRefused)}\"");
        html.Append($" data-nothing=\"{E(AdminConsoleCopy.NothingChanged)}\" data-retry-label=\"{E(AdminConsoleCopy.Retry)}\"");
        html.Append($" data-conflict=\"{E(AdminEditCopy.Conflict)}\"");
        html.Append($" data-saving=\"{E(AdminConsoleCopy.Saving)}\" data-save=\"{E(AdminConsoleCopy.Save)}\"");
        // What the page says after it has loaded itself anew over a change it made (#edit-{note}).
        var shortId = id.ToString("D");
        if (kind != AdminListKind.Offers)
            html.Append($" data-note-saved=\"{E(AdminConsoleCopy.ChangesSaved)}\"");
        if (kind == AdminListKind.Negotiations)
        {
            html.Append($" data-note-closed=\"{E(AdminConsoleCopy.NegotiationClosed(shortId))}\"");
            html.Append($" data-note-expired=\"{E(AdminConsoleCopy.NegotiationExpired(shortId))}\"");
        }

        if (kind == AdminListKind.Offers)
            html.Append($" data-note-cancelled=\"{E(AdminConsoleCopy.OfferCancelled(shortId))}\"");
        html.Append('>');

        if (!plan.Any)
            return html.ToString();

        html.Append("<div id=\"record-actions\" class=\"record-actions\" hidden>");
        if (plan.Fields)
            html.Append($"<button type=\"button\" id=\"edit-button\">{E(AdminConsoleCopy.Edit)}</button>");
        if (plan.Close)
        {
            html.Append(ActionButton("close-button", AdminConsoleCopy.CloseNegotiation, "Closed",
                AdminConsoleCopy.CloseNegotiationTitle(shortId), AdminConsoleCopy.OpenOffersCancelled(Count(record)),
                AdminConsoleCopy.CloseNegotiation, AdminConsoleCopy.DialogCancel, AdminConsoleCopy.Closing, "closed"));
        }

        if (plan.Expire)
        {
            html.Append(ActionButton("expire-button", AdminConsoleCopy.ExpireNegotiation, "Expired",
                AdminConsoleCopy.ExpireNegotiationTitle(shortId), AdminConsoleCopy.OpenOffersCancelled(Count(record)),
                AdminConsoleCopy.ExpireNegotiation, AdminConsoleCopy.DialogCancel, AdminConsoleCopy.Expiring, "expired"));
        }

        if (plan.CancelOffer)
        {
            html.Append(ActionButton("cancel-offer-button", AdminConsoleCopy.CancelOffer, "Cancelled",
                AdminConsoleCopy.CancelOfferTitle(shortId), AdminConsoleCopy.CancelOfferBody,
                AdminConsoleCopy.CancelOffer, AdminConsoleCopy.KeepOffer, AdminConsoleCopy.Cancelling, "cancelled"));
        }

        html.Append("</div>");

        if (plan.Fields)
        {
            html.Append("<form id=\"edit-form\" class=\"edit-form\" autocomplete=\"off\" novalidate>");
            html.Append("<div id=\"edit-summary\" class=\"error-summary\" tabindex=\"-1\" hidden>");
            html.Append($"<h2 id=\"edit-summary-title\">{E(AdminConsoleCopy.ErrorSummaryTitle)}</h2>");
            html.Append("<ul id=\"edit-summary-list\"></ul></div>");
        }

        return html.ToString();
    }

    /// <summary>A button that opens the confirm dialog, its words carried for the script.</summary>
    private static string ActionButton(
        string id, string label, string status, string title, string body, string confirm, string cancel, string busy, string note)
        => $"<button type=\"button\" id=\"{id}\" class=\"secondary record-action\" data-status=\"{status}\""
           + $" data-title=\"{E(title)}\" data-body=\"{E(body)}\" data-confirm=\"{E(confirm)}\" data-cancel=\"{E(cancel)}\""
           + $" data-busy=\"{E(busy)}\" data-note=\"{note}\">{E(label)}</button>";

    /// <summary>
    /// After the details list: the conflict notice, Save and Cancel, the end
    /// of the form, and the two dialogs (the action's confirm and "Discard
    /// your changes?").
    /// </summary>
    internal static string End(Plan plan)
    {
        if (!plan.Any)
            return "</div>";
        var html = new StringBuilder(2048);
        if (plan.Fields)
        {
            html.Append("<div id=\"edit-conflict\" class=\"edit-conflict\" tabindex=\"-1\" hidden>");
            html.Append("<p id=\"edit-conflict-text\"></p>");
            html.Append($"<button type=\"button\" id=\"edit-reload\" class=\"secondary\">{E(AdminConsoleCopy.Reload)}</button></div>");
            html.Append("<p id=\"edit-alert\" class=\"edit-alert\" tabindex=\"-1\" hidden></p>");
            html.Append("<div id=\"edit-buttons\" class=\"edit-buttons\" hidden>");
            html.Append($"<button type=\"submit\" id=\"edit-save\" disabled>{E(AdminConsoleCopy.Save)}</button>");
            html.Append($"<button type=\"button\" id=\"edit-cancel\" class=\"secondary\">{E(AdminConsoleCopy.CancelEdit)}</button>");
            html.Append("</div></form>");
        }

        html.Append("</div>");

        if (plan.Close || plan.Expire || plan.CancelOffer)
        {
            html.Append("<dialog id=\"action-dialog\" class=\"edit-dialog\" aria-modal=\"true\" aria-labelledby=\"action-dialog-title\" aria-describedby=\"action-dialog-body\">");
            html.Append("<div class=\"edit-dialog-inner\">");
            html.Append("<h2 id=\"action-dialog-title\"></h2>");
            html.Append("<p id=\"action-dialog-body\"></p>");
            html.Append("<p id=\"action-dialog-alert\" class=\"edit-dialog-alert\" role=\"alert\"></p>");
            html.Append("<div class=\"edit-dialog-actions\">");
            html.Append("<button type=\"button\" id=\"action-dialog-confirm\"></button>");
            html.Append($"<button type=\"button\" id=\"action-dialog-reload\" hidden>{E(AdminConsoleCopy.Reload)}</button>");
            html.Append("<button type=\"button\" id=\"action-dialog-cancel\" class=\"secondary\"></button>");
            html.Append("</div></div></dialog>");
        }

        if (plan.Fields)
        {
            html.Append("<dialog id=\"discard-dialog\" class=\"edit-dialog\" aria-modal=\"true\" aria-labelledby=\"discard-dialog-title\">");
            html.Append("<div class=\"edit-dialog-inner\">");
            html.Append($"<h2 id=\"discard-dialog-title\">{E(AdminConsoleCopy.DiscardTitle)}</h2>");
            html.Append("<div class=\"edit-dialog-actions\">");
            html.Append($"<button type=\"button\" id=\"discard-confirm\">{E(AdminConsoleCopy.Discard)}</button>");
            html.Append($"<button type=\"button\" id=\"discard-keep\" class=\"secondary\">{E(AdminConsoleCopy.KeepEditing)}</button>");
            html.Append("</div></div></dialog>");
        }

        return html.ToString();
    }

    /// <summary>The hidden edit part of a row: a control and the line its error goes into.</summary>
    private static string Edit(string controls) => $"<div class=\"edit\" hidden>{controls}</div>";

    private static string Error(string key) => $"<p id=\"edit-{key}-error\" class=\"field-error\"></p>";

    /// <summary>Participant: the display name (optional, 256 characters).</summary>
    internal static string? DisplayName(Plan plan, Dictionary<string, object?> record)
    {
        if (!plan.DisplayName)
            return null;
        var value = record.TryGetValue("displayName", out var v) ? v as string : null;
        return Edit(
            $"<label for=\"edit-displayName\">{E(AdminConsoleCopy.DisplayNameLabel)}</label>"
            + $"<input type=\"text\" id=\"edit-displayName\" data-key=\"displayName\" maxlength=\"{AdminEditEndpoints.MaxDisplayNameLength}\""
            + $" autocomplete=\"off\" spellcheck=\"false\" aria-describedby=\"edit-displayName-error\" value=\"{E(value)}\">"
            + Error("displayName"));
    }

    /// <summary>Participant: Active or Suspended, as two radio buttons.</summary>
    internal static string? Active(Plan plan, Dictionary<string, object?> record)
    {
        if (!plan.Active)
            return null;
        var active = record.TryGetValue("isActive", out var a) && a is true;
        string Radio(string id, string value, string label, bool on)
            => $"<span class=\"check\"><input type=\"radio\" id=\"{id}\" name=\"isActive\" value=\"{value}\"{(on ? " checked" : string.Empty)}"
               + $" aria-describedby=\"edit-isActive-error\"><label for=\"{id}\">{E(label)}</label></span>";
        return Edit(
            "<fieldset id=\"edit-isActive\" class=\"edit-choice\" data-key=\"isActive\">"
            + $"<legend>{E(AdminConsoleCopy.ColumnStatus)}</legend>"
            + Radio("edit-isActive-active", "true", AdminConsoleCopy.StatusActive, active)
            + Radio("edit-isActive-suspended", "false", AdminConsoleCopy.StatusSuspended, !active)
            + "</fieldset>"
            + Error("isActive"));
    }

    /// <summary>
    /// Artifact: the subject entity's name (required, 4096) and description
    /// (optional, 4096). With several entities, which one is picked first;
    /// picking another loads its stored words.
    /// </summary>
    internal static string? SubjectEntity(Plan plan, Dictionary<string, object?> record)
    {
        if (!plan.EntityName && !plan.EntityDescription)
            return null;
        var entities = Entities(record);
        var first = entities[0];
        var html = new StringBuilder();
        if (entities.Count > 1)
        {
            html.Append("<div class=\"edit-field\">");
            html.Append($"<label for=\"edit-entityId\">{E(AdminConsoleCopy.SubjectEntity)}</label>");
            html.Append("<select id=\"edit-entityId\">");
            foreach (var entity in entities)
            {
                html.Append($"<option value=\"{entity.Id:D}\" data-name=\"{E(entity.Name)}\" data-description=\"{E(entity.Description)}\"");
                html.Append(entity == first ? " selected>" : ">");
                html.Append(E(string.IsNullOrEmpty(entity.Name) ? entity.Id.ToString("D") : entity.Name));
                html.Append("</option>");
            }

            html.Append("</select></div>");
        }
        else
        {
            html.Append($"<input type=\"hidden\" id=\"edit-entityId\" value=\"{first.Id:D}\">");
        }

        if (plan.EntityName)
        {
            html.Append("<div class=\"edit-field\">");
            html.Append($"<label for=\"edit-name\">{E(AdminConsoleCopy.EntityNameLabel + AdminConsoleCopy.RequiredSuffix)}</label>");
            html.Append($"<input type=\"text\" id=\"edit-name\" data-key=\"name\" maxlength=\"{Domain.Artifacts.SubjectEntity.MaxTextLength}\"");
            html.Append($" autocomplete=\"off\" aria-required=\"true\" aria-describedby=\"edit-name-error\" value=\"{E(first.Name)}\">");
            html.Append(Error("name"));
            html.Append("</div>");
        }

        if (plan.EntityDescription)
        {
            html.Append("<div class=\"edit-field\">");
            html.Append($"<label for=\"edit-description\">{E(AdminConsoleCopy.Description)}</label>");
            // The parser drops one line break straight after the start tag: this one, never the text's own.
            html.Append($"<textarea id=\"edit-description\" data-key=\"description\" rows=\"4\" maxlength=\"{Domain.Artifacts.SubjectEntity.MaxTextLength}\"");
            html.Append($" aria-describedby=\"edit-description-error\">\n{E(first.Description)}</textarea>");
            html.Append(Error("description"));
            html.Append("</div>");
        }

        return Edit(html.ToString());
    }

    /// <summary>
    /// Artifact: the owner, as a typeahead over the participants that are
    /// not deleted (one page of matches from the read API). The value sent
    /// is the chosen participant's Core identifier.
    /// </summary>
    internal static string? Owner(Plan plan, Dictionary<string, object?> record, string shown)
    {
        if (!plan.Owner)
            return null;
        var sub = record.TryGetValue("ownerParticipantId", out var o) ? o as string : null;
        return Edit(
            "<div class=\"edit-field\">"
            + $"<label for=\"edit-owner-picker\">{E(AdminConsoleCopy.ColumnOwner)}</label>"
            + $"<div class=\"picker\" id=\"edit-owner\" data-source=\"/admin/api/participants\" data-no-matches=\"{E(AdminConsoleCopy.NoMatches)}\">"
            + "<input type=\"text\" id=\"edit-owner-picker\" role=\"combobox\" aria-autocomplete=\"list\" aria-expanded=\"false\""
            + $" aria-controls=\"edit-owner-options\" autocomplete=\"off\" spellcheck=\"false\" maxlength=\"{AdminListQuery.MaxQueryLength}\""
            + $" aria-describedby=\"edit-ownerParticipantId-error\" value=\"{E(shown)}\">"
            + $"<input type=\"hidden\" id=\"edit-ownerParticipantId\" data-key=\"ownerParticipantId\" value=\"{E(sub)}\" data-initial=\"{E(sub)}\">"
            + $"<ul id=\"edit-owner-options\" class=\"options\" role=\"listbox\" aria-label=\"{E(AdminConsoleCopy.ColumnOwner)}\" hidden></ul>"
            + "</div>"
            + Error("ownerParticipantId")
            + "</div>");
    }

    /// <summary>Negotiation: the end time, typed as an ET date and time; empty for none.</summary>
    internal static string? EndsAt(Plan plan, Dictionary<string, object?> record)
    {
        if (!plan.EndsAt)
            return null;
        var value = record.TryGetValue("endsAt", out var e) && e is DateTimeOffset at
            ? EasternTime.ToEastern(at).ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture)
            : string.Empty;
        return Edit(
            $"<label for=\"edit-endsAt\">{E(AdminConsoleCopy.EndsAt)}</label>"
            + $"<p id=\"edit-endsAt-hint\" class=\"hint\">{E(AdminConsoleCopy.EndsAtHint)}</p>"
            + $"<input type=\"datetime-local\" id=\"edit-endsAt\" data-key=\"endsAt\" aria-describedby=\"edit-endsAt-hint edit-endsAt-error\" value=\"{value}\">"
            + Error("endsAt"));
    }
}

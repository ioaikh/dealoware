using System.Security.Claims;
using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// CoreOwner edit API (PATCH) and edit/Expire UI under /admin/{type}/{id}/edit.
/// If-Match on Version: missing 428, stale 409. One audit row per successful edit.
/// </summary>
public static class AdminEditEndpoints
{
    private static readonly HashSet<string> ParticipantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "displayName", "isActive"
    };

    private static readonly HashSet<string> ArtifactKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "description", "ownerParticipantId"
    };

    private static readonly HashSet<string> NegotiationKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "endsAt"
    };

    private static readonly HashSet<string> OfferKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "amount", "currency", "terms", "status"
    };

    public static void MapAdminEditEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/admin/api")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        api.MapPatch("/participants/{id:guid}", PatchParticipant).WithName("AdminPatchParticipant");
        api.MapPatch("/artifacts/{id:guid}", PatchArtifact).WithName("AdminPatchArtifact");
        api.MapPatch("/negotiations/{id:guid}", PatchNegotiation).WithName("AdminPatchNegotiation");
        api.MapPatch("/offers/{id:guid}", PatchOffer).WithName("AdminPatchOffer");

        // Signed-in shell only (route note r3 b079a814): /admin/ui/* and
        // /admin/{type}/{id}/edit are mapped after AdminSessionMiddleware.
        // Only /admin/auth/ static files may load signed out.
        var ui = app.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        ui.MapGet("/participants/{id:guid}/edit", (Guid id) => AdminEditPage.Page("participants", id))
            .WithName("AdminEditParticipantPage");
        ui.MapGet("/artifacts/{id:guid}/edit", (Guid id) => AdminEditPage.Page("artifacts", id))
            .WithName("AdminEditArtifactPage");
        ui.MapGet("/negotiations/{id:guid}/edit", (Guid id) => AdminEditPage.Page("negotiations", id))
            .WithName("AdminEditNegotiationPage");
        ui.MapGet("/offers/{id:guid}/edit", (Guid id) => AdminEditPage.Page("offers", id))
            .WithName("AdminEditOfferPage");
        ui.MapGet("/ui/edit.css", AdminEditPage.Css).WithName("AdminEditCss");
        ui.MapGet("/ui/edit.js", AdminEditPage.Js).WithName("AdminEditJs");
    }

    private static bool TryCoreOwner(HttpContext context, out IResult? deny)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue(ClaimTypes.Name)
                    ?? string.Empty;
        if (string.IsNullOrEmpty(email) || !context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
        {
            deny = AdminDeny.UnauthorizedResult();
            return false;
        }

        deny = null;
        return true;
    }

    private static bool TryReadIfMatch(HttpContext context, out uint version, out IResult? error)
    {
        version = 0;
        if (!context.Request.Headers.TryGetValue("If-Match", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            error = AdminDeny.PreconditionRequiredResult();
            return false;
        }

        var text = raw.ToString().Trim();
        if (text.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            text = text[2..].Trim();
        text = text.Trim().Trim('"');
        if (!uint.TryParse(text, out version))
        {
            error = AdminDeny.PreconditionRequiredResult();
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryReadObject(
        JsonElement body,
        HashSet<string> allowed,
        out Dictionary<string, JsonElement> fields,
        out IResult? deny)
    {
        fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (body.ValueKind != JsonValueKind.Object)
        {
            deny = AdminDeny.BadRequestResult();
            return false;
        }

        foreach (var property in body.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                deny = AdminDeny.BadRequestResult();
                return false;
            }

            fields[property.Name] = property.Value;
        }

        if (fields.Count == 0)
        {
            deny = AdminDeny.ValidationResult(new Dictionary<string, string>
            {
                ["form"] = "No changes."
            });
            return false;
        }

        deny = null;
        return true;
    }

    private static string ActorEmail(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.Email)
        ?? context.User.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    private static string IpHmac(HttpContext context, IIpHasher hasher)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
            ip = "0.0.0.0";
        return hasher.Hash(ip);
    }

    private static async Task<IResult> SaveEditAsync(
        HttpContext context,
        DealowareDbContext db,
        IAdminAuditRepository audit,
        IIpHasher hasher,
        string entityType,
        Guid entityId,
        string before,
        string after,
        CancellationToken cancellationToken)
    {
        try
        {
            await audit.AddAsync(
                AdminAuditEntry.CreateEntityEvent(
                    "Edit",
                    ActorEmail(context),
                    IpHmac(context, hasher),
                    entityType,
                    entityId,
                    before,
                    after),
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AdminDeny.ConflictResult();
        }

        return null!;
    }

    private static IResult JsonWithEtag(HttpContext context, uint version, object payload)
    {
        context.Response.Headers.ETag = $"\"{version}\"";
        return Results.Json(payload);
    }

    private static async Task<IResult> PatchParticipant(
        HttpContext context,
        Guid id,
        JsonElement body,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditRepository audit,
        IIpHasher hasher,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!TryReadIfMatch(context, out var ifMatch, out var matchError))
            return matchError!;
        if (!TryReadObject(body, ParticipantKeys, out var fields, out deny))
            return deny!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        if (fields.ContainsKey("displayName")
            && !fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("isActive")
            && !fieldPolicy.Evaluate(principal, FieldClass.ParticipantActive, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();

        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (participant is null)
            return AdminDeny.NotFoundResult();
        if (participant.Version != ifMatch)
            return AdminDeny.ConflictResult();

        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (fields.TryGetValue("displayName", out var displayNameEl))
        {
            if (displayNameEl.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                errors["displayName"] = "Enter a display name of at most 256 characters.";
            else
            {
                var value = displayNameEl.ValueKind == JsonValueKind.Null ? null : displayNameEl.GetString();
                if (value is { Length: > 256 })
                    errors["displayName"] = "Enter a display name of at most 256 characters.";
            }
        }

        if (fields.TryGetValue("isActive", out var activeEl)
            && activeEl.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            errors["isActive"] = "Choose Active or Suspended.";

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        var before = AdminEditSnapshot.Participant(participant);

        if (fields.TryGetValue("displayName", out displayNameEl))
        {
            var value = displayNameEl.ValueKind == JsonValueKind.Null ? null : displayNameEl.GetString();
            participant.UpdateDisplayName(string.IsNullOrWhiteSpace(value) ? null : value);
        }

        if (fields.TryGetValue("isActive", out activeEl))
        {
            if (activeEl.GetBoolean())
                participant.Activate();
            else
                participant.Deactivate();
        }

        participant.MarkEdited();
        var after = AdminEditSnapshot.Participant(participant);
        var save = await SaveEditAsync(
            context, db, audit, hasher, "Participant", participant.Id, before, after, cancellationToken);
        if (save is not null)
            return save;

        return JsonWithEtag(
            context,
            participant.Version,
            AdminReadProjection.Participant(participant, fieldPolicy, principal, detail: true));
    }

    private static async Task<IResult> PatchArtifact(
        HttpContext context,
        Guid id,
        JsonElement body,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditRepository audit,
        IIpHasher hasher,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!TryReadIfMatch(context, out var ifMatch, out var matchError))
            return matchError!;
        if (!TryReadObject(body, ArtifactKeys, out var fields, out deny))
            return deny!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        if (fields.ContainsKey("name")
            && !fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("description")
            && !fieldPolicy.Evaluate(principal, FieldClass.ArtifactDescription, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("ownerParticipantId")
            && !fieldPolicy.Evaluate(principal, FieldClass.ArtifactOwnerParticipantId, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();

        var artifact = await db.Artifacts
            .Include(a => a.Entities)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (artifact is null)
            return AdminDeny.NotFoundResult();
        if (artifact.Version != ifMatch)
            return AdminDeny.ConflictResult();

        var subject = artifact.Entities.FirstOrDefault();
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (fields.TryGetValue("name", out var nameEl))
        {
            if (nameEl.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameEl.GetString()))
                errors["name"] = "Name is required.";
            else if (nameEl.GetString()!.Length > 4096)
                errors["name"] = "Enter a name of at most 4096 characters.";
        }

        if (fields.TryGetValue("description", out var descEl))
        {
            if (descEl.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                errors["description"] = "Enter a description of at most 4096 characters.";
            else if ((descEl.GetString() ?? string.Empty).Length > 4096)
                errors["description"] = "Enter a description of at most 4096 characters.";
        }

        Domain.Participants.Participant? newOwner = null;
        if (fields.TryGetValue("ownerParticipantId", out var ownerEl))
        {
            var ownerId = ownerEl.ValueKind == JsonValueKind.String ? ownerEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(ownerId))
                errors["ownerParticipantId"] = "Choose an existing participant.";
            else
            {
                newOwner = await db.Participants.FirstOrDefaultAsync(
                    p => p.Sub == ownerId || p.Id.ToString() == ownerId,
                    cancellationToken);
                if (newOwner is null || newOwner.DeletedAt != null || !newOwner.IsActive)
                    return AdminDeny.BadRequestResult();
            }
        }

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        if ((fields.ContainsKey("name") || fields.ContainsKey("description")) && subject is null)
            return AdminDeny.BadRequestResult();

        var before = AdminEditSnapshot.Artifact(artifact);

        if (fields.TryGetValue("name", out nameEl) && subject is not null)
        {
            if (!subject.UpdateName(nameEl.GetString()!))
                return AdminDeny.ValidationResult(new Dictionary<string, string>
                {
                    ["name"] = "Name is required."
                });
        }

        if (fields.TryGetValue("description", out descEl) && subject is not null)
        {
            if (!subject.UpdateDescription(descEl.ValueKind == JsonValueKind.Null ? string.Empty : descEl.GetString()))
                return AdminDeny.ValidationResult(new Dictionary<string, string>
                {
                    ["description"] = "Enter a description of at most 4096 characters."
                });
        }

        if (newOwner is not null)
            artifact.ReassignOwner(newOwner.Sub);

        artifact.MarkEdited();
        var after = AdminEditSnapshot.Artifact(artifact);
        var save = await SaveEditAsync(
            context, db, audit, hasher, "Artifact", artifact.Id, before, after, cancellationToken);
        if (save is not null)
            return save;

        return JsonWithEtag(
            context,
            artifact.Version,
            AdminReadProjection.Artifact(artifact, fieldPolicy, principal, detail: true));
    }

    private static async Task<IResult> PatchNegotiation(
        HttpContext context,
        Guid id,
        JsonElement body,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditRepository audit,
        IIpHasher hasher,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!TryReadIfMatch(context, out var ifMatch, out var matchError))
            return matchError!;
        if (!TryReadObject(body, NegotiationKeys, out var fields, out deny))
            return deny!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        if (fields.ContainsKey("status")
            && !fieldPolicy.Evaluate(principal, FieldClass.NegotiationStatus, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("endsAt")
            && !fieldPolicy.Evaluate(principal, FieldClass.NegotiationEndsAt, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();

        var negotiation = await db.Negotiations
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (negotiation is null)
            return AdminDeny.NotFoundResult();
        if (negotiation.Version != ifMatch)
            return AdminDeny.ConflictResult();

        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        DateTimeOffset? endsAt = negotiation.EndsAt;
        var endsAtSet = false;
        if (fields.TryGetValue("endsAt", out var endsEl))
        {
            endsAtSet = true;
            if (endsEl.ValueKind == JsonValueKind.Null)
                endsAt = null;
            else if (endsEl.ValueKind == JsonValueKind.String
                     && DateTimeOffset.TryParse(endsEl.GetString(), out var parsed))
                endsAt = parsed.ToUniversalTime();
            else
                errors["endsAt"] = "Enter a valid end time after the start time.";
        }

        string? requestedStatus = null;
        if (fields.TryGetValue("status", out var statusEl))
        {
            if (statusEl.ValueKind != JsonValueKind.String)
                return AdminDeny.BadRequestResult();
            requestedStatus = statusEl.GetString();
        }

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        if (endsAtSet && negotiation.Status != NegotiationStatus.Open)
            return AdminDeny.BadRequestResult();

        if (endsAtSet
            && negotiation.StartsAt.HasValue
            && endsAt.HasValue
            && endsAt.Value <= negotiation.StartsAt.Value)
        {
            return AdminDeny.ValidationResult(new Dictionary<string, string>
            {
                ["endsAt"] = "Ends at must be after Starts at."
            });
        }

        if (requestedStatus is not null)
        {
            var allowed = negotiation.Status == NegotiationStatus.Open
                          && (string.Equals(requestedStatus, "Closed", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(requestedStatus, "Expired", StringComparison.OrdinalIgnoreCase));
            if (!allowed)
                return AdminDeny.BadRequestResult();
        }

        var before = AdminEditSnapshot.Negotiation(negotiation);

        if (endsAtSet && !negotiation.UpdateEndsAt(endsAt))
            return AdminDeny.BadRequestResult();

        if (requestedStatus is not null)
        {
            var openOffers = await db.Offers
                .Where(o => o.NegotiationId == negotiation.Id
                            && o.DeletedAt == null
                            && o.Status == OfferStatus.Open)
                .ToListAsync(cancellationToken);

            var ok = string.Equals(requestedStatus, "Closed", StringComparison.OrdinalIgnoreCase)
                ? negotiation.Close()
                : negotiation.Expire();
            if (!ok)
                return AdminDeny.BadRequestResult();

            foreach (var offer in openOffers)
            {
                offer.Cancel();
                offer.MarkEdited();
            }
        }

        negotiation.MarkEdited();
        var after = AdminEditSnapshot.Negotiation(negotiation);
        var save = await SaveEditAsync(
            context, db, audit, hasher, "Negotiation", negotiation.Id, before, after, cancellationToken);
        if (save is not null)
            return save;

        var openCount = await db.Offers.CountAsync(
            o => o.NegotiationId == negotiation.Id
                 && o.DeletedAt == null
                 && o.Status == OfferStatus.Open,
            cancellationToken);
        return JsonWithEtag(
            context,
            negotiation.Version,
            AdminReadProjection.Negotiation(negotiation, fieldPolicy, principal, detail: true, openCount));
    }

    private static async Task<IResult> PatchOffer(
        HttpContext context,
        Guid id,
        JsonElement body,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditRepository audit,
        IIpHasher hasher,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!TryReadIfMatch(context, out var ifMatch, out var matchError))
            return matchError!;
        if (!TryReadObject(body, OfferKeys, out var fields, out deny))
            return deny!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        if (fields.ContainsKey("amount")
            && !fieldPolicy.Evaluate(principal, FieldClass.OfferAmount, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("currency")
            && !fieldPolicy.Evaluate(principal, FieldClass.OfferCurrency, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("terms")
            && !fieldPolicy.Evaluate(principal, FieldClass.OfferTerms, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();
        if (fields.ContainsKey("status")
            && !fieldPolicy.Evaluate(principal, FieldClass.OfferStatus, FieldAction.Write, ctx))
            return AdminDeny.BadRequestResult();

        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
            return AdminDeny.NotFoundResult();
        if (offer.Version != ifMatch)
            return AdminDeny.ConflictResult();

        string? requestedStatus = null;
        if (fields.TryGetValue("status", out var statusEl))
        {
            if (statusEl.ValueKind != JsonValueKind.String)
                return AdminDeny.BadRequestResult();
            requestedStatus = statusEl.GetString();
            var allowed = offer.Status == OfferStatus.Open
                          && string.Equals(requestedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                return AdminDeny.BadRequestResult();
        }

        var termsTouched = fields.ContainsKey("amount")
                           || fields.ContainsKey("currency")
                           || fields.ContainsKey("terms");
        if (termsTouched && offer.Status != OfferStatus.Open)
            return AdminDeny.BadRequestResult();

        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        decimal? amount = offer.Amount;
        string? currency = offer.Currency;
        string? terms = offer.Terms;

        if (fields.TryGetValue("amount", out var amountEl))
        {
            if (amountEl.ValueKind == JsonValueKind.Null)
                amount = null;
            else if (amountEl.ValueKind == JsonValueKind.Number && amountEl.TryGetDecimal(out var parsed))
            {
                if (parsed < 0)
                    errors["amount"] = "Amount must be 0 or more.";
                else if (decimal.Round(parsed, 2) != parsed)
                    errors["amount"] = "Amount can have at most 2 decimal places.";
                else
                    amount = parsed;
            }
            else
                errors["amount"] = "Amount must be 0 or more.";
        }

        if (fields.TryGetValue("currency", out var currencyEl))
        {
            currency = currencyEl.ValueKind == JsonValueKind.Null ? null : currencyEl.GetString();
            if (amount.HasValue && string.IsNullOrWhiteSpace(currency))
                errors["currency"] = "Currency is required when Amount is set.";
            else if (!string.IsNullOrWhiteSpace(currency) && currency.Trim().Length != 3)
                errors["currency"] = "Currency must be 3 letters.";
        }
        else if (amount.HasValue && string.IsNullOrWhiteSpace(currency))
            errors["currency"] = "Currency is required when Amount is set.";

        if (fields.TryGetValue("terms", out var termsEl))
        {
            terms = termsEl.ValueKind == JsonValueKind.Null ? null : termsEl.GetString();
            if (terms is { Length: > 2000 })
                errors["terms"] = "Terms can be at most 2000 characters.";
        }

        if (termsTouched && !amount.HasValue && string.IsNullOrWhiteSpace(terms))
        {
            errors["amount"] = "Enter an amount or terms.";
            errors["terms"] = "Enter an amount or terms.";
        }

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        var before = AdminEditSnapshot.Offer(offer);

        if (termsTouched && !offer.UpdateTerms(amount, currency, terms))
            return AdminDeny.BadRequestResult();

        if (requestedStatus is not null && !offer.Cancel())
            return AdminDeny.BadRequestResult();

        offer.MarkEdited();
        var after = AdminEditSnapshot.Offer(offer);
        var save = await SaveEditAsync(
            context, db, audit, hasher, "Offer", offer.Id, before, after, cancellationToken);
        if (save is not null)
            return save;

        return JsonWithEtag(
            context,
            offer.Version,
            AdminReadProjection.Offer(offer, fieldPolicy, principal, detail: true));
    }
}

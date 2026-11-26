using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// The Core owner's edit API (A7 Step 9): <c>PATCH /admin/api/{type}/{id}</c>
/// for participants, artifacts, negotiations and offers. Behind the signed-in
/// session, and, as every PATCH under /admin, behind the Origin gate and the
/// anti-forgery token of <see cref="AdminSessionMiddleware"/>. The edit page
/// is a later piece.
/// <para>
/// What can be written is what the field policy lets the Core owner write
/// (no second list of permissions) and this API has a path for: a
/// participant's display name and active flag (E1, E2); an artifact's
/// subject name and description and its owner (E3 to E5); a negotiation's
/// end time, and Open to Closed or Expired (E6 to E8); an offer's Open to
/// Cancelled (E12). An offer's amount, currency and terms are never written:
/// what a party sent stays as sent. Accepted, Declined, Withdrawn and
/// Superseded are never set here.
/// </para>
/// <para>
/// Replies: 200 with the record as the detail read gives it and its new
/// <c>ETag</c>; 428 without <c>If-Match</c>; 409 when the record is not as
/// the <c>If-Match</c> says (<see cref="AdminEditToken"/>); 404 for no record
/// or a soft-deleted one; the generic 400 for everything else that is
/// refused (a field that is not editable, a change that is not allowed, a
/// request that cannot be read, a value of the wrong JSON type), always the
/// same body, with no word about the record's state; the same 400 with a
/// <c>fields</c> map when a value breaks a rule (<see cref="AdminEditCopy"/>).
/// </para>
/// <para>
/// Expired is final even before the status says so: a negotiation whose end
/// time has passed is not given a new end time and is not closed, though its
/// stored status is still Open; it can only be expired.
/// </para>
/// <para>
/// A write never saves over a change it did not see. The engine's party
/// actions (accept, decline, counter, withdraw, close, a profile update)
/// leave <c>Version</c> alone, so <c>Version</c> is not enough. Each write is
/// therefore checked twice against the state it was prepared for: the
/// <c>If-Match</c> token covers that state, and inside the write's
/// transaction every row about to be written is first claimed with an update
/// that matches only if the row is still exactly as it was read
/// (<see cref="ClaimAsync"/>). A party action that lands in between makes
/// the claim match nothing: 409, nothing written, no audit row.
/// </para>
/// <para>
/// Every write moves <c>Version</c> on each row it changes, and is paired
/// with its audit rows in one transaction
/// (<see cref="IAdminAuditUnitOfWork"/>): action <c>entity_edit</c>, before
/// and after of the fields the request named plus the version, through the
/// field policy. Closing or expiring a negotiation cancels its open offers
/// in that transaction (not its soft-deleted ones, which no admin write
/// touches); each cancelled offer has its own audit row, tied to the
/// negotiation's by a correlation id.
/// </para>
/// <para>
/// A PostgreSQL deadlock or serialization failure in the write's transaction
/// is answered like any other collision: 409, nothing written.
/// </para>
/// </summary>
public static partial class AdminEditEndpoints
{
    /// <summary>Largest request body read. The longest legal body is two texts of 4096 characters.</summary>
    public const int MaxBodyBytes = 64 * 1024;

    public const int MaxDisplayNameLength = 256;

    /// <summary>
    /// Names the artifact's subject entity that <c>name</c> and
    /// <c>description</c> are for. Needed when the artifact has more than one.
    /// It addresses a row; it is not a field.
    /// </summary>
    public const string EntityIdKey = "entityId";

    /// <summary>
    /// A body key: the field class the policy is asked about, and whether this
    /// API has a path that writes it. A key outside the table, a key with no
    /// path and a key the policy refuses all get the same reply.
    /// </summary>
    private sealed record Key(FieldClass Field, bool HasEditPath = false);

    private static Dictionary<string, Key> Keys(params (string Name, Key Key)[] own)
    {
        var keys = new Dictionary<string, Key>(StringComparer.Ordinal)
        {
            ["createdAt"] = new(FieldClass.EntityCreatedAt),
            ["updatedAt"] = new(FieldClass.EntityCreatedAt),
            ["deletedAt"] = new(FieldClass.SoftDeletedAt),
            ["version"] = new(FieldClass.EntityVersion)
        };
        foreach (var (name, key) in own)
            keys[name] = key;
        return keys;
    }

    private static readonly Dictionary<string, Key> ParticipantKeys = Keys(
        ("displayName", new(FieldClass.DisplayName, HasEditPath: true)),
        ("isActive", new(FieldClass.ParticipantActive, HasEditPath: true)),
        ("loginEmail", new(FieldClass.LoginEmail)),
        ("contactEmail", new(FieldClass.ContactEmail)));

    private static readonly Dictionary<string, Key> ArtifactKeys = Keys(
        ("name", new(FieldClass.ArtifactName, HasEditPath: true)),
        ("description", new(FieldClass.ArtifactDescription, HasEditPath: true)),
        ("ownerParticipantId", new(FieldClass.ArtifactOwnerParticipantId, HasEditPath: true)));

    private static readonly Dictionary<string, Key> NegotiationKeys = Keys(
        ("status", new(FieldClass.NegotiationStatus, HasEditPath: true)),
        ("endsAt", new(FieldClass.NegotiationEndsAt, HasEditPath: true)),
        ("partyAParticipantId", new(FieldClass.NegotiationPartyA)),
        ("partyBParticipantId", new(FieldClass.NegotiationPartyB)),
        ("artifactId", new(FieldClass.NegotiationArtifactId)));

    private static readonly Dictionary<string, Key> OfferKeys = Keys(
        ("status", new(FieldClass.OfferStatus, HasEditPath: true)),
        ("amount", new(FieldClass.OfferAmount)),
        ("currency", new(FieldClass.OfferCurrency)),
        ("terms", new(FieldClass.OfferTerms)),
        ("negotiationId", new(FieldClass.OfferNegotiationId)));

    public static void MapAdminEditEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/admin/api")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        api.MapPatch("/participants/{id:guid}", PatchParticipant).WithName("AdminPatchParticipant");
        api.MapPatch("/artifacts/{id:guid}", PatchArtifact).WithName("AdminPatchArtifact");
        api.MapPatch("/negotiations/{id:guid}", PatchNegotiation).WithName("AdminPatchNegotiation");
        api.MapPatch("/offers/{id:guid}", PatchOffer).WithName("AdminPatchOffer");
    }

    // ---- Participant: E1 isActive, E2 displayName ---------------------------

    private static async Task<IResult> PatchParticipant(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditUnitOfWork unitOfWork,
        IAdminAuditRecorder recorder,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        var (request, refusal) = await ReadRequestAsync(context, fieldPolicy, ParticipantKeys, cancellationToken);
        if (request is null)
            return refusal!;

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        string? displayName = null;
        if (request.Fields.TryGetValue("displayName", out var displayNameValue))
        {
            if (!TryOptionalText(displayNameValue, out displayName))
                return AdminDeny.BadRequestResult();
            if (displayName is { Length: > MaxDisplayNameLength })
                errors["displayName"] = AdminEditCopy.DisplayNameTooLong;
            else if (string.IsNullOrWhiteSpace(displayName))
                displayName = null;
        }

        bool? isActive = null;
        if (request.Fields.TryGetValue("isActive", out var isActiveValue))
        {
            switch (isActiveValue.ValueKind)
            {
                case JsonValueKind.True or JsonValueKind.False:
                    isActive = isActiveValue.GetBoolean();
                    break;
                case JsonValueKind.Null:
                    errors["isActive"] = AdminEditCopy.ActiveNotChosen;
                    break;
                default:
                    return AdminDeny.BadRequestResult();
            }
        }

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
        if (participant is null)
            return AdminDeny.NotFoundResult();
        if (!AdminEditToken.Matches(request.IfMatch, AdminEditToken.For(participant)))
            return AdminDeny.ConflictResult();

        // The row as this write was prepared against it.
        var version = participant.Version;
        var wasActive = participant.IsActive;
        var hadDisplayName = participant.DisplayName;
        var wasUpdatedAt = participant.UpdatedAt;

        Dictionary<FieldClass, object?> Values() => Named(
            request,
            ("displayName", FieldClass.DisplayName, participant.DisplayName),
            ("isActive", FieldClass.ParticipantActive, participant.IsActive));

        var before = Values();
        if (request.Has("displayName"))
            participant.UpdateDisplayName(displayName);
        if (isActive == true)
            participant.Activate();
        else if (isActive == false)
            participant.Deactivate();

        var after = Values();
        if (Same(before, after))
            return NoChanges(db);

        var now = EditTime(clock);
        participant.MarkEdited(now);
        var audit = Audit(
            context, request, recorder, fieldPolicy, "Participant", participant.Id,
            before, version, after, participant.Version, correlationId: null, now);

        return await SaveAsync(
            context,
            db,
            unitOfWork,
            [audit],
            ct => ClaimAsync(
                db.Participants
                    .Where(p => p.Id == id
                                && p.DeletedAt == null
                                && p.Version == version
                                && p.IsActive == wasActive
                                && p.DisplayName == hadDisplayName
                                && p.UpdatedAt == wasUpdatedAt)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Version, p => p.Version), ct)),
            () => AdminReadEndpoints.ReadParticipantAsync(
                db, fieldPolicy, request.Principal, id, includeDeleted: false, cancellationToken),
            cancellationToken);
    }

    // ---- Artifact: E3 name, E4 description, E5 owner ------------------------

    private static async Task<IResult> PatchArtifact(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditUnitOfWork unitOfWork,
        IAdminAuditRecorder recorder,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        var (request, refusal) = await ReadRequestAsync(
            context, fieldPolicy, ArtifactKeys, cancellationToken, addressKey: EntityIdKey);
        if (request is null)
            return refusal!;

        var editsSubject = request.Has("name") || request.Has("description");
        Guid? entityId = null;
        if (request.Address is { } address)
        {
            // An address with nothing to write there, or one that is not an id.
            if (!editsSubject
                || address.ValueKind != JsonValueKind.String
                || !Guid.TryParse(address.GetString(), out var parsedEntityId))
            {
                return AdminDeny.BadRequestResult();
            }

            entityId = parsedEntityId;
        }

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        string? name = null;
        if (request.Fields.TryGetValue("name", out var nameValue))
        {
            if (!TryOptionalText(nameValue, out name))
                return AdminDeny.BadRequestResult();
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
                errors["name"] = AdminEditCopy.NameRequired;
            else if (name.Length > SubjectEntity.MaxTextLength)
                errors["name"] = AdminEditCopy.NameTooLong;
        }

        string? description = null;
        if (request.Fields.TryGetValue("description", out var descriptionValue))
        {
            if (!TryOptionalText(descriptionValue, out description))
                return AdminDeny.BadRequestResult();
            if (description is { Length: > SubjectEntity.MaxTextLength })
                errors["description"] = AdminEditCopy.DescriptionTooLong;
        }

        string? ownerText = null;
        if (request.Fields.TryGetValue("ownerParticipantId", out var ownerValue))
        {
            if (!TryOptionalText(ownerValue, out ownerText))
                return AdminDeny.BadRequestResult();
            ownerText = ownerText?.Trim();
            if (string.IsNullOrEmpty(ownerText))
                errors["ownerParticipantId"] = AdminEditCopy.OwnerNotChosen;
        }

        if (errors.Count > 0)
            return AdminDeny.ValidationResult(errors);

        var artifact = await db.Artifacts
            .Include(a => a.Entities)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
        if (artifact is null)
            return AdminDeny.NotFoundResult();
        if (!AdminEditToken.Matches(request.IfMatch, AdminEditToken.For(artifact)))
            return AdminDeny.ConflictResult();

        // Which subject entity: the one named, or the only one. Never a guess.
        SubjectEntity? subject = null;
        if (editsSubject)
        {
            subject = entityId is { } wanted
                ? artifact.Entities.FirstOrDefault(e => e.Id == wanted)
                : artifact.Entities.Count == 1 ? artifact.Entities[0] : null;
            if (subject is null)
                return AdminDeny.BadRequestResult();
        }

        // E5: an existing participant that is not soft-deleted, by Sub or by id.
        string? newOwnerSub = null;
        if (ownerText is not null)
        {
            var owners = db.Participants.AsNoTracking().Where(p => p.DeletedAt == null);
            owners = Guid.TryParse(ownerText, out var ownerId)
                ? owners.Where(p => p.Sub == ownerText || p.Id == ownerId)
                : owners.Where(p => p.Sub == ownerText);
            newOwnerSub = await owners.Select(p => p.Sub).FirstOrDefaultAsync(cancellationToken);
            if (newOwnerSub is null)
                return AdminDeny.BadRequestResult();
        }

        var version = artifact.Version;
        var hadOwner = artifact.OwnerParticipantId;
        var wasUpdatedAt = artifact.UpdatedAt;
        var subjectId = subject?.Id;
        var hadName = subject?.Name;
        var hadDescription = subject?.Description;

        Dictionary<FieldClass, object?> Values() => Named(
            request,
            ("name", FieldClass.ArtifactName, subject?.Name),
            ("description", FieldClass.ArtifactDescription, subject?.Description),
            ("ownerParticipantId", FieldClass.ArtifactOwnerParticipantId, artifact.OwnerParticipantId));

        var before = Values();
        if (request.Has("name") && !subject!.UpdateName(name!))
            return Refuse(db);
        if (request.Has("description") && !subject!.UpdateDescription(description))
            return Refuse(db);
        if (newOwnerSub is not null)
            artifact.ReassignOwner(newOwnerSub);

        var after = Values();
        if (Same(before, after))
            return NoChanges(db);

        var now = EditTime(clock);
        artifact.MarkEdited(now);
        var audit = Audit(
            context, request, recorder, fieldPolicy, "Artifact", artifact.Id,
            before, version, after, artifact.Version, correlationId: null, now);

        return await SaveAsync(
            context,
            db,
            unitOfWork,
            [audit],
            async ct =>
            {
                await ClaimAsync(
                    db.Artifacts
                        .Where(a => a.Id == id
                                    && a.DeletedAt == null
                                    && a.Version == version
                                    && a.OwnerParticipantId == hadOwner
                                    && a.UpdatedAt == wasUpdatedAt)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.Version, a => a.Version), ct));
                if (subjectId is { } entity)
                {
                    await ClaimAsync(
                        db.SubjectEntities
                            .Where(e => e.Id == entity && e.Name == hadName && e.Description == hadDescription)
                            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, e => e.Name), ct));
                }

                // The new owner is still there as the write lands.
                if (newOwnerSub is not null
                    && !await db.Participants.AnyAsync(p => p.Sub == newOwnerSub && p.DeletedAt == null, ct))
                {
                    throw new StaleEditException();
                }
            },
            () => AdminReadEndpoints.ReadArtifactAsync(
                db, fieldPolicy, request.Principal, id, includeDeleted: false, cancellationToken),
            cancellationToken);
    }

    // ---- Negotiation: E6 endsAt, E7 Open to Closed, E8 Open to Expired ------

    private static async Task<IResult> PatchNegotiation(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditUnitOfWork unitOfWork,
        IAdminAuditRecorder recorder,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        var (request, refusal) = await ReadRequestAsync(context, fieldPolicy, NegotiationKeys, cancellationToken);
        if (request is null)
            return refusal!;

        // The only statuses the owner sets: Closed and Expired. Asked for
        // anything else (Open again, an offer's status, a made-up one): refused.
        NegotiationStatus? wantedStatus = null;
        if (request.Fields.TryGetValue("status", out var statusValue))
        {
            wantedStatus = Text(statusValue) switch
            {
                nameof(NegotiationStatus.Closed) => NegotiationStatus.Closed,
                nameof(NegotiationStatus.Expired) => NegotiationStatus.Expired,
                _ => null
            };
            if (wantedStatus is null)
                return AdminDeny.BadRequestResult();
        }

        DateTimeOffset? endsAt = null;
        if (request.Fields.TryGetValue("endsAt", out var endsAtValue))
        {
            if (endsAtValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                return AdminDeny.BadRequestResult();
            if (!TryInstant(endsAtValue, out endsAt))
            {
                return AdminDeny.ValidationResult(new Dictionary<string, string>
                {
                    ["endsAt"] = AdminEditCopy.EndsAtInvalid
                });
            }
        }

        // Its open offers come with it: closing or expiring cancels them, and
        // the token covers them. Soft-deleted offers are left out: no admin
        // write touches a soft-deleted row.
        var negotiation = await db.Negotiations
            .Include(n => n.Offers.Where(o => o.Status == OfferStatus.Open && o.DeletedAt == null))
            .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null, cancellationToken);
        if (negotiation is null)
            return AdminDeny.NotFoundResult();

        // By id: one fixed order for claiming the rows, so that two writers
        // after the same offers are less likely to end up waiting on each other.
        var openOffers = negotiation.Offers
            .Where(o => o.Status == OfferStatus.Open && o.DeletedAt == null)
            .OrderBy(o => o.Id)
            .ToList();
        if (!AdminEditToken.Matches(request.IfMatch, AdminEditToken.For(negotiation, openOffers)))
            return AdminDeny.ConflictResult();

        // Its end time has passed, so it has expired, though the stored status
        // still says Open (the engine writes Expired when a party next touches
        // it; this is the engine's clock, as that check uses). Expired is
        // final: no new end time, and not Closed. It can only be expired.
        if (negotiation.HasLapsed(DateTimeOffset.UtcNow)
            && (request.Has("endsAt") || wantedStatus == NegotiationStatus.Closed))
        {
            return Refuse(db);
        }

        var version = negotiation.Version;
        var wasStatus = negotiation.Status;
        var hadEndsAt = negotiation.EndsAt;
        var wasUpdatedAt = negotiation.UpdatedAt;
        var openAsRead = openOffers.Select(o => (o.Id, o.Version, o.UpdatedAt)).ToList();

        Dictionary<FieldClass, object?> Values() => Named(
            request,
            ("status", FieldClass.NegotiationStatus, negotiation.Status.ToString()),
            ("endsAt", FieldClass.NegotiationEndsAt, negotiation.EndsAt));

        var before = Values();
        if (request.Has("endsAt"))
        {
            // Not open: refused like any change that is not allowed, with no word about why.
            if (negotiation.Status != NegotiationStatus.Open)
                return Refuse(db);
            if (endsAt is { } end && negotiation.StartsAt is { } start && end <= start)
            {
                db.ChangeTracker.Clear();
                return AdminDeny.ValidationResult(new Dictionary<string, string>
                {
                    ["endsAt"] = AdminEditCopy.EndsAtNotAfterStart
                });
            }

            if (!negotiation.UpdateEndsAt(endsAt))
                return Refuse(db);
        }

        if (wantedStatus is { } wanted)
        {
            // Closing or expiring writes each open offer's status too, so the
            // policy must allow the owner that write as well.
            if (openOffers.Count > 0
                && !fieldPolicy.Evaluate(
                    request.Principal,
                    FieldClass.OfferStatus,
                    FieldAction.Write,
                    FieldResourceContext.ForNegotiation(negotiation.PartyAParticipantId, negotiation.PartyBParticipantId)))
            {
                return Refuse(db);
            }

            // Both cancel the open offers loaded above. Closed and Expired are final.
            var done = wanted == NegotiationStatus.Closed ? negotiation.Close() : negotiation.Expire();
            if (!done || negotiation.Status != wanted)
                return Refuse(db);
        }

        var after = Values();
        if (Same(before, after))
            return NoChanges(db);

        var now = EditTime(clock);
        negotiation.MarkEdited(now);

        // One audit row for the negotiation and one for each offer the close
        // or expire cancelled, tied together.
        var cancelled = openOffers.Where(o => o.Status == OfferStatus.Cancelled).ToList();
        Guid? correlationId = wantedStatus is null ? null : Guid.NewGuid();
        var audits = new List<AdminAuditEntry>
        {
            Audit(
                context, request, recorder, fieldPolicy, "Negotiation", negotiation.Id,
                before, version, after, negotiation.Version, correlationId, now)
        };
        foreach (var offer in cancelled)
        {
            var offerVersion = offer.Version;
            offer.MarkEdited(now);
            audits.Add(Audit(
                context, request, recorder, fieldPolicy, "Offer", offer.Id,
                OfferStatusValues(OfferStatus.Open), offerVersion,
                OfferStatusValues(offer.Status), offer.Version, correlationId, now));
        }

        return await SaveAsync(
            context,
            db,
            unitOfWork,
            audits,
            async ct =>
            {
                await ClaimAsync(
                    db.Negotiations
                        .Where(n => n.Id == id
                                    && n.DeletedAt == null
                                    && n.Version == version
                                    && n.Status == wasStatus
                                    && n.EndsAt == hadEndsAt
                                    && n.UpdatedAt == wasUpdatedAt)
                        .ExecuteUpdateAsync(s => s.SetProperty(n => n.Version, n => n.Version), ct));
                if (wantedStatus is null)
                    return;

                // Each offer about to be cancelled is still open and untouched,
                // and no other open offer has appeared since they were read.
                foreach (var (offerId, offerVersion, offerUpdatedAt) in openAsRead)
                {
                    await ClaimAsync(
                        db.Offers
                            .Where(o => o.Id == offerId
                                        && o.Version == offerVersion
                                        && o.Status == OfferStatus.Open
                                        && o.UpdatedAt == offerUpdatedAt)
                            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Version, o => o.Version), ct));
                }

                var claimed = openAsRead.Select(o => o.Id).ToList();
                if (await db.Offers.AnyAsync(
                        o => o.NegotiationId == id
                             && o.Status == OfferStatus.Open
                             && o.DeletedAt == null
                             && !claimed.Contains(o.Id),
                        ct))
                {
                    throw new StaleEditException();
                }
            },
            () => AdminReadEndpoints.ReadNegotiationAsync(
                db, fieldPolicy, request.Principal, id, includeDeleted: false, cancellationToken),
            cancellationToken);
    }

    // ---- Offer: E12 Open to Cancelled, and nothing else ---------------------

    private static async Task<IResult> PatchOffer(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditUnitOfWork unitOfWork,
        IAdminAuditRecorder recorder,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        // Amount, currency and terms never get past this: the policy refuses the write.
        var (request, refusal) = await ReadRequestAsync(context, fieldPolicy, OfferKeys, cancellationToken);
        if (request is null)
            return refusal!;

        // The owner's one offer change. Accepted, Declined, Withdrawn and
        // Superseded are the parties' to set, never the owner's.
        if (!request.Fields.TryGetValue("status", out var statusValue)
            || Text(statusValue) != nameof(OfferStatus.Cancelled))
        {
            return AdminDeny.BadRequestResult();
        }

        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
        if (offer is null)
            return AdminDeny.NotFoundResult();
        if (!AdminEditToken.Matches(request.IfMatch, AdminEditToken.For(offer)))
            return AdminDeny.ConflictResult();

        var version = offer.Version;
        var wasStatus = offer.Status;
        var wasUpdatedAt = offer.UpdatedAt;

        // Only from Open. Every other status is final.
        if (!offer.Cancel())
            return Refuse(db);

        var now = EditTime(clock);
        offer.MarkEdited(now);
        var audit = Audit(
            context, request, recorder, fieldPolicy, "Offer", offer.Id,
            OfferStatusValues(wasStatus), version, OfferStatusValues(offer.Status), offer.Version,
            correlationId: null, now);

        return await SaveAsync(
            context,
            db,
            unitOfWork,
            [audit],
            ct => ClaimAsync(
                db.Offers
                    .Where(o => o.Id == id
                                && o.DeletedAt == null
                                && o.Version == version
                                && o.Status == wasStatus
                                && o.UpdatedAt == wasUpdatedAt)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Version, o => o.Version), ct)),
            () => AdminReadEndpoints.ReadOfferAsync(
                db, fieldPolicy, request.Principal, id, includeDeleted: false, cancellationToken),
            cancellationToken);
    }

    // ---- The request ---------------------------------------------------------

    private sealed record EditRequest(
        FieldPrincipal Principal,
        string ActorEmail,
        string IfMatch,
        IReadOnlyDictionary<string, JsonElement> Fields,
        JsonElement? Address)
    {
        public bool Has(string key) => Fields.ContainsKey(key);
    }

    /// <summary>
    /// The checks every edit starts with, in this order: the Core owner; the
    /// <c>If-Match</c>; a JSON object; then each key, which must be in the
    /// table, be writable by the policy, and have an edit path. Nothing is
    /// read from the database before these pass.
    /// </summary>
    private static async Task<(EditRequest? Request, IResult? Refusal)> ReadRequestAsync(
        HttpContext context,
        IFieldPolicy fieldPolicy,
        IReadOnlyDictionary<string, Key> keys,
        CancellationToken cancellationToken,
        string? addressKey = null)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue(ClaimTypes.Name)
                    ?? string.Empty;
        if (string.IsNullOrEmpty(email) || !context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
            return (null, AdminDeny.UnauthorizedResult());

        switch (AdminEditToken.Read(context.Request, out var ifMatch))
        {
            case AdminEditToken.Presented.Missing:
                return (null, AdminDeny.PreconditionRequiredResult());
            case AdminEditToken.Presented.Malformed:
                return (null, AdminDeny.BadRequestResult());
        }

        using var body = await ReadBodyAsync(context.Request, cancellationToken);
        if (body is null || body.RootElement.ValueKind != JsonValueKind.Object)
            return (null, AdminDeny.BadRequestResult());

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var resource = FieldResourceContext.ForSelfProfile(string.Empty);
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        JsonElement? address = null;
        foreach (var property in body.RootElement.EnumerateObject())
        {
            if (addressKey is not null && property.NameEquals(addressKey))
            {
                if (address is not null)
                    return (null, AdminDeny.BadRequestResult());
                address = property.Value.Clone();
                continue;
            }

            // The policy is asked about every key it has a class for, path or no path.
            if (fields.ContainsKey(property.Name)
                || !keys.TryGetValue(property.Name, out var key)
                || !fieldPolicy.Evaluate(principal, key.Field, FieldAction.Write, resource)
                || !key.HasEditPath)
            {
                return (null, AdminDeny.BadRequestResult());
            }

            fields[property.Name] = property.Value.Clone();
        }

        if (fields.Count == 0 && address is null)
        {
            return (null, AdminDeny.ValidationResult(new Dictionary<string, string>
            {
                [AdminEditCopy.FormKey] = AdminEditCopy.NoChanges
            }));
        }

        return (new EditRequest(principal, email, ifMatch, fields, address), null);
    }

    private static async Task<JsonDocument?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaxBodyBytes)
            return null;

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBodyBytes)
                return null;
            buffer.Write(chunk, 0, read);
        }

        try
        {
            return JsonDocument.Parse(
                buffer.GetBuffer().AsMemory(0, (int)buffer.Length),
                new JsonDocumentOptions { MaxDepth = 4 });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A text or null. False for any other kind of value.</summary>
    private static bool TryOptionalText(JsonElement value, out string? text)
    {
        text = null;
        if (value.ValueKind == JsonValueKind.Null)
            return true;
        if (value.ValueKind != JsonValueKind.String)
            return false;
        text = value.GetString();
        return true;
    }

    /// <summary>
    /// A time or null, from a JSON text or null. The time is ISO 8601 with its offset (<c>Z</c> or
    /// <c>+hh:mm</c>), so it is one instant whatever the server's own zone; in
    /// the years 1900 to 2999, as the list filters keep dates; cut to the
    /// millisecond, which every store keeps whole.
    /// </summary>
    private static bool TryInstant(JsonElement value, out DateTimeOffset? at)
    {
        at = null;
        if (value.ValueKind == JsonValueKind.Null)
            return true;
        if (!WithOffset().IsMatch(value.GetString()!)
            || !value.TryGetDateTimeOffset(out var parsed)
            || parsed.Year is < 1900 or > 2999)
        {
            return false;
        }

        at = ToMillisecond(parsed);
        return true;
    }

    [GeneratedRegex("(Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex WithOffset();

    private static DateTimeOffset ToMillisecond(DateTimeOffset at)
    {
        var utc = at.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);
    }

    /// <summary>The time of this edit, for the rows and their audit rows alike.</summary>
    private static DateTimeOffset EditTime(IAdminClock clock) => ToMillisecond(clock.UtcNow);

    // ---- Refusals -------------------------------------------------------------

    /// <summary>
    /// The generic 400 after the rows were touched in memory: they are let go
    /// of first, so nothing later in the request can save them.
    /// </summary>
    private static IResult Refuse(DealowareDbContext db)
    {
        db.ChangeTracker.Clear();
        return AdminDeny.BadRequestResult();
    }

    private static IResult NoChanges(DealowareDbContext db)
    {
        db.ChangeTracker.Clear();
        return AdminDeny.ValidationResult(new Dictionary<string, string>
        {
            [AdminEditCopy.FormKey] = AdminEditCopy.NoChanges
        });
    }

    // ---- Before and after -----------------------------------------------------

    /// <summary>The current values of the fields this request names, by field class.</summary>
    private static Dictionary<FieldClass, object?> Named(
        EditRequest request,
        params (string Key, FieldClass Field, object? Value)[] fields)
    {
        var values = new Dictionary<FieldClass, object?>();
        foreach (var (key, field, value) in fields)
        {
            if (request.Has(key))
                values[field] = value;
        }

        return values;
    }

    private static Dictionary<FieldClass, object?> OfferStatusValues(OfferStatus status)
        => new() { [FieldClass.OfferStatus] = status.ToString() };

    private static bool Same(Dictionary<FieldClass, object?> before, Dictionary<FieldClass, object?> after)
        => before.All(kv => Equals(kv.Value, after[kv.Key]));

    /// <summary>
    /// One <c>entity_edit</c> audit row. Before and after hold the named
    /// fields and the version, and pass through the field policy
    /// (<see cref="AdminAuditSnapshots.FromAllowedFields"/>): a class the
    /// owner may not read is left out. The client address is stored only as
    /// its keyed hash; with no address, the hash is of the same stand-in the
    /// session gate uses (<see cref="AdminAuthService.NormalizeIp"/>).
    /// </summary>
    private static AdminAuditEntry Audit(
        HttpContext context,
        EditRequest request,
        IAdminAuditRecorder recorder,
        IFieldPolicy fieldPolicy,
        string entityType,
        Guid entityId,
        Dictionary<FieldClass, object?> before,
        uint versionBefore,
        Dictionary<FieldClass, object?> after,
        uint versionAfter,
        Guid? correlationId,
        DateTimeOffset now)
    {
        return recorder.Entity(
            AdminAuditActions.EntityEdit,
            request.ActorEmail,
            AdminAuthService.NormalizeIp(context.Connection.RemoteIpAddress?.ToString()),
            entityType,
            entityId,
            Snapshot(fieldPolicy, request.Principal, before, versionBefore),
            Snapshot(fieldPolicy, request.Principal, after, versionAfter),
            correlationId,
            now);
    }

    private static string Snapshot(
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Dictionary<FieldClass, object?> values,
        uint version)
    {
        var fields = values.ToDictionary(kv => kv.Key, kv => kv.Value is string text ? Clip(text) : kv.Value);
        fields[FieldClass.EntityVersion] = version;
        return AdminAuditSnapshots.FromAllowedFields(
            fieldPolicy, principal, fields, FieldResourceContext.ForSelfProfile(string.Empty));
    }

    /// <summary>Room one text gets in a snapshot, counted as it is written in JSON.</summary>
    internal const int SnapshotTextBudget = 1500;

    /// <summary>
    /// A snapshot is kept whole only up to 4096 characters
    /// (<see cref="AdminAuditSnapshots.MaxSnapshotChars"/>), and an artifact's
    /// name and description may each be that long. So a long text is stored
    /// as its beginning, its full length and its SHA-256 (Spec §10, large
    /// snapshots), which keeps the before and after readable JSON.
    /// </summary>
    private static object Clip(string text)
    {
        var used = 0;
        var keep = 0;
        while (keep < text.Length)
        {
            // Plain ASCII is written as itself; anything else may be written as \uXXXX.
            var cost = text[keep] is >= ' ' and <= '~' and not ('"' or '\\' or '<' or '>' or '&' or '\'' or '+' or '`')
                ? 1
                : 6;
            if (used + cost > SnapshotTextBudget)
                break;
            used += cost;
            keep++;
        }

        if (keep == text.Length)
            return text;
        if (keep > 0 && char.IsHighSurrogate(text[keep - 1]))
            keep--;

        return new Dictionary<string, object?>
        {
            ["truncated"] = text[..keep],
            ["length"] = text.Length,
            ["sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))
        };
    }

    // ---- The write --------------------------------------------------------------

    /// <summary>
    /// A row is no longer as it was when this write was prepared: a party
    /// acted on it, or another admin write landed first.
    /// </summary>
    private sealed class StaleEditException : Exception;

    /// <summary>
    /// Claims one row inside the write's transaction: an update that changes
    /// nothing and matches only while the row is exactly as it was read
    /// (version, and what a party action changes). Matching, it holds the
    /// row until the transaction ends, so nothing can change it before the
    /// write itself lands. Not matching, the write stops here.
    /// </summary>
    private static async Task ClaimAsync(Task<int> rowStillAsRead)
    {
        if (await rowStillAsRead != 1)
            throw new StaleEditException();
    }

    /// <summary>
    /// The claims, the rows' changes and the audit rows in one transaction;
    /// then the record as the detail read gives it, with its new edit token.
    /// A stale row is 409 with nothing written. So is a transaction that
    /// PostgreSQL stopped because it collided with another (a deadlock or a
    /// serialization failure): it was rolled back, and the admin reads again.
    /// </summary>
    private static async Task<IResult> SaveAsync(
        HttpContext context,
        DealowareDbContext db,
        IAdminAuditUnitOfWork unitOfWork,
        IReadOnlyList<AdminAuditEntry> audits,
        Func<CancellationToken, Task> claimRowsAsync,
        Func<Task<AdminReadEndpoints.Detail?>> readBackAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.ExecutePairedAsync(claimRowsAsync, audits, cancellationToken);
        }
        catch (Exception ex) when (ex is StaleEditException or DbUpdateConcurrencyException
                                   || DatabaseErrors.IsTransactionCollision(ex))
        {
            db.ChangeTracker.Clear();
            return AdminDeny.ConflictResult();
        }

        return AdminReadEndpoints.DetailResult(context, await readBackAsync());
    }
}

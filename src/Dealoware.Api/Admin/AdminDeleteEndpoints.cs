using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// The Core owner's delete API (A7 Step 10; Spec §9, §10, §11), in two steps:
/// <list type="number">
/// <item><c>POST /admin/api/{type}/{id}/delete-intent</c> names the record,
/// states what goes with it, and hands out a confirm token;</item>
/// <item><c>DELETE /admin/api/{type}/{id}</c> deletes, only with that token
/// in the <see cref="ConfirmTokenHeader"/> header. A DELETE without one is
/// refused (428); a token in the URL is never read (any query string is
/// refused).</item>
/// </list>
/// Both are behind the signed-in Core owner's session and, like every POST
/// and DELETE under /admin, behind the Origin gate and the anti-forgery token
/// of <see cref="AdminSessionMiddleware"/>. No route is exempted.
/// <para>
/// The token is 32 random bytes; only its SHA-256 is stored
/// (<see cref="AdminDeleteConfirmToken"/>). It is good for five minutes,
/// once, in the session that asked for it, for the one record it names, and
/// for the exact cascade set the intent saw: every row the delete will
/// mark, with its id, version, time of last change and the state a party
/// can change (<see cref="Plan.Hash"/>). An unknown, used, run-out or foreign
/// token is a 409 with nothing deleted; so is a cascade set that is no longer
/// what the token was made for.
/// </para>
/// <para>
/// The delete is a soft delete only (<c>DeletedAt</c>); nothing is erased and
/// there is no hard delete. Cascades (Spec §11.2, §11.3):
/// an offer takes only itself; a negotiation takes its offers, open or not;
/// an artifact takes only itself and is refused while a negotiation that is
/// not deleted refers to it (409, with those negotiations' ids); a
/// participant takes the negotiations it is a party to and their offers, and
/// never an artifact. Rows already soft-deleted are left as they are, and a
/// soft-deleted record cannot be deleted again (404). An offer's amount,
/// currency, terms and status are never written.
/// </para>
/// <para>
/// A delete never marks a row a party changed after the intent. Inside the
/// delete's transaction it first uses the token (a conditional update: two
/// confirms of one token delete once), then claims each row it will mark,
/// in a fixed order (the record, then negotiations, then offers, each by
/// id), with an update that matches only while the row is exactly as the
/// token was made for; then it checks that no row has joined the cascade set
/// since. Any of these failing is a 409 with nothing written. A PostgreSQL
/// deadlock or serialization failure is answered the same way.
/// </para>
/// <para>
/// Every marked row moves its <c>Version</c> and has its own
/// <c>entity_delete</c> audit row (before and after through the field
/// policy), all tied by one correlation id and written in the delete's
/// transaction (<see cref="IAdminAuditUnitOfWork"/>). Asking for an intent
/// and not confirming it (cancel) writes no audit row.
/// </para>
/// </summary>
public static partial class AdminDeleteEndpoints
{
    /// <summary>The request header that carries the confirm token. Never a URL.</summary>
    public const string ConfirmTokenHeader = "X-Admin-Confirm-Token";

    public const string Participants = "participants";
    public const string Artifacts = "artifacts";
    public const string Negotiations = "negotiations";
    public const string Offers = "offers";

    /// <summary>At most this many blocking negotiation ids are listed; the count is always whole.</summary>
    public const int MaxListedNegotiations = 100;

    private static readonly string[] Types = [Participants, Artifacts, Negotiations, Offers];

    public static void MapAdminDeleteEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/admin/api")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        foreach (var type in Types)
        {
            var name = char.ToUpperInvariant(type[0]) + type[1..^1];
            api.MapPost(
                    $"/{type}/{{id:guid}}/delete-intent",
                    (HttpContext context, Guid id, DealowareDbContext db, IFieldPolicy fieldPolicy, IAdminClock clock,
                            CancellationToken cancellationToken)
                        => IntentAsync(type, context, id, db, fieldPolicy, clock, cancellationToken))
                .WithName($"AdminDeleteIntent{name}");
            api.MapDelete(
                    $"/{type}/{{id:guid}}",
                    (HttpContext context, Guid id, DealowareDbContext db, IFieldPolicy fieldPolicy,
                            IAdminAuditUnitOfWork unitOfWork, IAdminAuditRecorder recorder, IAdminClock clock,
                            CancellationToken cancellationToken)
                        => DeleteAsync(type, context, id, db, fieldPolicy, unitOfWork, recorder, clock, cancellationToken))
                .WithName($"AdminDelete{name}");
        }
    }

    // ---- Step 1: the intent ------------------------------------------------------

    private static async Task<IResult> IntentAsync(
        string type,
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryOwner(context, out _, out var sessionId))
            return AdminDeny.UnauthorizedResult();
        if (context.Request.QueryString.HasValue)
            return AdminDeny.BadRequestResult();

        var plan = await LoadPlanAsync(db, type, id, tracking: false, cancellationToken);
        if (plan is null)
            return AdminDeny.NotFoundResult();
        if (plan.Blocked)
            return BlockedResult(plan);

        var now = clock.UtcNow;
        var raw = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var token = AdminDeleteConfirmToken.Create(HashToken(raw), sessionId, type, id, plan.Hash(), now);

        // One live token per record per session: a new intent replaces the
        // earlier ones. Tokens that have run out are let go of here too.
        await db.AdminDeleteConfirmTokens
            .Where(t => (t.SessionId == sessionId && t.EntityType == type && t.EntityId == id) || t.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
        db.AdminDeleteConfirmTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(new Dictionary<string, object?>
        {
            ["entityType"] = type,
            ["id"] = id,
            ["identity"] = Identity(plan, fieldPolicy, principal),
            ["cascade"] = Summary(plan),
            ["confirmToken"] = raw,
            ["expiresAt"] = token.ExpiresAt
        });
    }

    // ---- Step 2: the delete ------------------------------------------------------

    private static async Task<IResult> DeleteAsync(
        string type,
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        IAdminAuditUnitOfWork unitOfWork,
        IAdminAuditRecorder recorder,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryOwner(context, out var actorEmail, out var sessionId))
            return AdminDeny.UnauthorizedResult();

        // No hard-delete switch, and no token, in a URL.
        if (context.Request.QueryString.HasValue)
            return AdminDeny.BadRequestResult();

        var presented = context.Request.Headers[ConfirmTokenHeader];
        if (presented.Count == 0 || presented.All(string.IsNullOrWhiteSpace))
            return AdminDeny.PreconditionRequiredResult();
        var raw = presented.Count == 1 ? presented[0]!.Trim() : string.Empty;
        if (!TokenShape().IsMatch(raw))
            return AdminDeny.BadRequestResult();

        var now = clock.UtcNow;
        var tokenHash = HashToken(raw);
        var token = await db.AdminDeleteConfirmTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (token is null || !token.IsUsableBy(sessionId, type, id, now))
            return TokenInvalidResult();

        var plan = await LoadPlanAsync(db, type, id, tracking: true, cancellationToken);
        if (plan is null)
            return AdminDeny.NotFoundResult();
        if (plan.Blocked)
            return Release(db, BlockedResult(plan));
        if (!string.Equals(plan.Hash(), token.CascadeHash, StringComparison.Ordinal))
            return Release(db, ChangedResult());

        var at = ToMillisecond(now);
        var correlationId = Guid.NewGuid();
        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ip = AdminAuthService.NormalizeIp(context.Connection.RemoteIpAddress?.ToString());
        var audits = plan.Rows()
            .Select(row => recorder.Entity(
                AdminAuditActions.EntityDelete,
                actorEmail,
                ip,
                row.AuditType,
                row.Id,
                Snapshot(fieldPolicy, principal, row.Values, row.Version, deletedAt: null),
                Snapshot(fieldPolicy, principal, row.Values, row.Version + 1, deletedAt: at),
                correlationId,
                at))
            .ToList();

        try
        {
            await unitOfWork.ExecutePairedAsync(
                async ct =>
                {
                    await UseTokenAsync(db, token.Id, sessionId, now, ct);
                    await ClaimRowsAsync(db, plan, ct);
                    await CheckNothingJoinedAsync(db, plan, ct);
                    plan.SoftDeleteAll(at);
                },
                audits,
                cancellationToken);
        }
        catch (TokenUsedException)
        {
            db.ChangeTracker.Clear();
            return TokenInvalidResult();
        }
        catch (Exception ex) when (ex is StaleDeleteException or DbUpdateConcurrencyException
                                   || DatabaseErrors.IsTransactionCollision(ex))
        {
            db.ChangeTracker.Clear();
            return ChangedResult();
        }

        return Results.Json(new Dictionary<string, object?>
        {
            ["entityType"] = type,
            ["id"] = id,
            ["correlationId"] = correlationId,
            ["deleted"] = Counts(plan)
        });
    }

    // ---- The caller --------------------------------------------------------------

    /// <summary>
    /// The Core owner and the session the gate let through (its cookie,
    /// already checked by <see cref="AdminSessionMiddleware"/>).
    /// </summary>
    private static bool TryOwner(HttpContext context, out string email, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue(ClaimTypes.Name)
                ?? string.Empty;
        return !string.IsNullOrEmpty(email)
               && context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole)
               && Guid.TryParse(context.Request.Cookies[AdminSessionCookie.Name], out sessionId)
               && sessionId != Guid.Empty;
    }

    // ---- The plan: the record and every row that goes with it ---------------------

    /// <summary>
    /// What one delete marks: the record and, per Spec §11.2, the rows that
    /// go with it, all not yet soft-deleted; or, for an artifact, the
    /// negotiations that block it.
    /// </summary>
    private sealed class Plan
    {
        public required string Type { get; init; }
        public required Guid Id { get; init; }
        public Participant? Participant { get; init; }
        public Artifact? Artifact { get; init; }

        /// <summary>By id: the order the rows are hashed and claimed in.</summary>
        public List<Negotiation> Negotiations { get; init; } = [];

        /// <summary>By id.</summary>
        public List<Offer> Offers { get; init; } = [];

        public List<Guid> BlockingNegotiations { get; init; } = [];

        public bool Blocked => BlockingNegotiations.Count > 0;

        /// <summary>One row to mark, as read, with the values its audit row shows.</summary>
        public sealed record Row(
            string AuditType,
            Guid Id,
            uint Version,
            DateTimeOffset? UpdatedAt,
            IReadOnlyList<string?> State,
            Dictionary<FieldClass, object?> Values);

        /// <summary>The rows in claim order: the participant or artifact, then negotiations, then offers.</summary>
        public IEnumerable<Row> Rows()
        {
            if (Participant is { } p)
            {
                yield return new Row(
                    "Participant", p.Id, p.Version, p.UpdatedAt,
                    [p.IsActive ? "1" : "0", p.DisplayName],
                    new Dictionary<FieldClass, object?>
                    {
                        [FieldClass.DisplayName] = p.DisplayName is null ? null : AdminEditEndpoints.Clip(p.DisplayName),
                        [FieldClass.ParticipantActive] = p.IsActive
                    });
            }

            if (Artifact is { } a)
            {
                var state = new List<string?> { a.OwnerParticipantId };
                foreach (var entity in a.Entities.OrderBy(e => e.Id))
                {
                    state.Add(entity.Id.ToString("N"));
                    state.Add(entity.Name);
                }

                yield return new Row(
                    "Artifact", a.Id, a.Version, a.UpdatedAt, state,
                    new Dictionary<FieldClass, object?>
                    {
                        [FieldClass.ArtifactName] = ArtifactName(a) is { } name ? AdminEditEndpoints.Clip(name) : null,
                        [FieldClass.ArtifactOwnerParticipantId] = a.OwnerParticipantId
                    });
            }

            foreach (var n in Negotiations)
            {
                yield return new Row(
                    "Negotiation", n.Id, n.Version, n.UpdatedAt,
                    [Number((int)n.Status), Instant(n.EndsAt)],
                    new Dictionary<FieldClass, object?>
                    {
                        [FieldClass.NegotiationStatus] = n.Status.ToString(),
                        [FieldClass.NegotiationEndsAt] = n.EndsAt,
                        [FieldClass.NegotiationArtifactId] = n.ArtifactId,
                        [FieldClass.NegotiationPartyA] = n.PartyAParticipantId,
                        [FieldClass.NegotiationPartyB] = n.PartyBParticipantId
                    });
            }

            foreach (var o in Offers)
            {
                yield return new Row(
                    "Offer", o.Id, o.Version, o.UpdatedAt,
                    [Number((int)o.Status)],
                    new Dictionary<FieldClass, object?>
                    {
                        [FieldClass.OfferStatus] = o.Status.ToString(),
                        [FieldClass.OfferAmount] = o.Amount,
                        [FieldClass.OfferCurrency] = o.Currency,
                        [FieldClass.OfferNegotiationId] = o.NegotiationId
                    });
            }
        }

        /// <summary>
        /// SHA-256 of the record and every row the delete marks: kind, id,
        /// version, time of last change and the state a party can change.
        /// Equal hashes mean the same rows in the same state.
        /// </summary>
        public string Hash()
        {
            var text = new StringBuilder();
            Append(text, Type);
            Append(text, Id.ToString("N"));
            foreach (var row in Rows())
            {
                Append(text, row.AuditType);
                Append(text, row.Id.ToString("N"));
                Append(text, Number(row.Version));
                Append(text, Instant(row.UpdatedAt));
                Append(text, Number(row.State.Count));
                foreach (var part in row.State)
                    Append(text, part);
            }

            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
        }

        /// <summary>Marks every row; each must still be live, or the write stops.</summary>
        public void SoftDeleteAll(DateTimeOffset at)
        {
            if (Participant is { } p && !p.SoftDelete(at))
                throw new StaleDeleteException();
            if (Artifact is { } a && !a.SoftDelete(at))
                throw new StaleDeleteException();
            foreach (var n in Negotiations)
            {
                if (!n.SoftDelete(at))
                    throw new StaleDeleteException();
            }

            foreach (var o in Offers)
            {
                if (!o.SoftDelete(at))
                    throw new StaleDeleteException();
            }
        }
    }

    private static async Task<Plan?> LoadPlanAsync(
        DealowareDbContext db,
        string type,
        Guid id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<T> From<T>(DbSet<T> set) where T : class => tracking ? set : set.AsNoTracking();

        async Task<List<Offer>> LiveOffersOfAsync(List<Guid> negotiationIds)
            => negotiationIds.Count == 0
                ? []
                : (await From(db.Offers)
                    .Where(o => o.DeletedAt == null && negotiationIds.Contains(o.NegotiationId))
                    .ToListAsync(cancellationToken))
                .OrderBy(o => o.Id)
                .ToList();

        switch (type)
        {
            case Participants:
            {
                var participant = await From(db.Participants)
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
                if (participant is null)
                    return null;

                // Every negotiation it is a party to (Spec §11.2), and their offers.
                var sub = participant.Sub;
                var negotiations = (await From(db.Negotiations)
                        .Where(n => n.DeletedAt == null
                                    && (n.PartyAParticipantId == sub || n.PartyBParticipantId == sub))
                        .ToListAsync(cancellationToken))
                    .OrderBy(n => n.Id)
                    .ToList();
                return new Plan
                {
                    Type = type,
                    Id = id,
                    Participant = participant,
                    Negotiations = negotiations,
                    Offers = await LiveOffersOfAsync(negotiations.Select(n => n.Id).ToList())
                };
            }

            case Artifacts:
            {
                var artifact = await From(db.Artifacts)
                    .Include(a => a.Entities)
                    .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
                if (artifact is null)
                    return null;

                var blocking = (await db.Negotiations.AsNoTracking()
                        .Where(n => n.DeletedAt == null && n.ArtifactId == id)
                        .Select(n => n.Id)
                        .ToListAsync(cancellationToken))
                    .Order()
                    .ToList();
                return new Plan { Type = type, Id = id, Artifact = artifact, BlockingNegotiations = blocking };
            }

            case Negotiations:
            {
                var negotiation = await From(db.Negotiations)
                    .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt == null, cancellationToken);
                if (negotiation is null)
                    return null;

                return new Plan
                {
                    Type = type,
                    Id = id,
                    Negotiations = [negotiation],
                    Offers = await LiveOffersOfAsync([id])
                };
            }

            case Offers:
            {
                var offer = await From(db.Offers)
                    .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
                return offer is null ? null : new Plan { Type = type, Id = id, Offers = [offer] };
            }

            default:
                return null;
        }
    }

    // ---- Inside the delete's transaction -----------------------------------------

    /// <summary>The token was used meanwhile (a second confirm), or ran out.</summary>
    private sealed class TokenUsedException : Exception;

    /// <summary>
    /// A row is no longer as the token was made for, or a row has joined the
    /// cascade set: a party acted, or another admin write landed first.
    /// </summary>
    private sealed class StaleDeleteException : Exception;

    /// <summary>
    /// Uses the token: a conditional update that matches only while it is
    /// unused, not run out and this session's. Two confirms of one token at
    /// once: the second waits on the row, then matches nothing.
    /// </summary>
    private static async Task UseTokenAsync(
        DealowareDbContext db,
        Guid tokenId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var used = await db.AdminDeleteConfirmTokens
            .Where(t => t.Id == tokenId && t.SessionId == sessionId && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct);
        if (used != 1)
            throw new TokenUsedException();
    }

    /// <summary>
    /// Claims each row about to be marked, in the plan's order: an update
    /// that changes nothing and matches only while the row is exactly as the
    /// token was made for. Matching, it holds the row until the transaction
    /// ends; not matching, the delete stops.
    /// </summary>
    private static async Task ClaimRowsAsync(DealowareDbContext db, Plan plan, CancellationToken ct)
    {
        if (plan.Participant is { } p)
        {
            var (pid, version, updatedAt, active, name) = (p.Id, p.Version, p.UpdatedAt, p.IsActive, p.DisplayName);
            await ClaimAsync(db.Participants
                .Where(x => x.Id == pid && x.DeletedAt == null && x.Version == version && x.UpdatedAt == updatedAt
                            && x.IsActive == active && x.DisplayName == name)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version), ct));
        }

        if (plan.Artifact is { } a)
        {
            var (aid, version, updatedAt, owner) = (a.Id, a.Version, a.UpdatedAt, a.OwnerParticipantId);
            await ClaimAsync(db.Artifacts
                .Where(x => x.Id == aid && x.DeletedAt == null && x.Version == version && x.UpdatedAt == updatedAt
                            && x.OwnerParticipantId == owner)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version), ct));
        }

        foreach (var n in plan.Negotiations)
        {
            var (nid, version, updatedAt, status, endsAt) = (n.Id, n.Version, n.UpdatedAt, n.Status, n.EndsAt);
            await ClaimAsync(db.Negotiations
                .Where(x => x.Id == nid && x.DeletedAt == null && x.Version == version && x.UpdatedAt == updatedAt
                            && x.Status == status && x.EndsAt == endsAt)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version), ct));
        }

        foreach (var o in plan.Offers)
        {
            var (oid, version, updatedAt, status) = (o.Id, o.Version, o.UpdatedAt, o.Status);
            await ClaimAsync(db.Offers
                .Where(x => x.Id == oid && x.DeletedAt == null && x.Version == version && x.UpdatedAt == updatedAt
                            && x.Status == status)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version), ct));
        }
    }

    private static async Task ClaimAsync(Task<int> rowStillAsRead)
    {
        if (await rowStillAsRead != 1)
            throw new StaleDeleteException();
    }

    /// <summary>
    /// No row has joined the cascade set since the intent: no new negotiation
    /// for the participant, no new offer in a negotiation being deleted, no
    /// negotiation now referring to the artifact. Such a row would be left
    /// live under a deleted record, or block the delete.
    /// </summary>
    private static async Task CheckNothingJoinedAsync(DealowareDbContext db, Plan plan, CancellationToken ct)
    {
        var negotiationIds = plan.Negotiations.Select(n => n.Id).ToList();
        var offerIds = plan.Offers.Select(o => o.Id).ToList();

        if (plan.Participant is { } p)
        {
            var sub = p.Sub;
            if (await db.Negotiations.AnyAsync(
                    n => n.DeletedAt == null
                         && (n.PartyAParticipantId == sub || n.PartyBParticipantId == sub)
                         && !negotiationIds.Contains(n.Id),
                    ct))
            {
                throw new StaleDeleteException();
            }
        }

        if (plan.Artifact is { } a)
        {
            var aid = a.Id;
            if (await db.Negotiations.AnyAsync(n => n.DeletedAt == null && n.ArtifactId == aid, ct))
                throw new StaleDeleteException();
        }

        if (negotiationIds.Count > 0
            && await db.Offers.AnyAsync(
                o => o.DeletedAt == null && negotiationIds.Contains(o.NegotiationId) && !offerIds.Contains(o.Id),
                ct))
        {
            throw new StaleDeleteException();
        }
    }

    // ---- Replies -------------------------------------------------------------------

    /// <summary>What the owner is about to delete, through the field policy.</summary>
    private static Dictionary<string, object?> Identity(Plan plan, IFieldPolicy fieldPolicy, FieldPrincipal principal)
    {
        object? Read(FieldClass field, object? value)
            => AdminReadProjection.Can(fieldPolicy, principal, field, FieldAction.Read, FieldResourceContext.ForSelfProfile(string.Empty))
                ? value
                : null;

        return plan.Type switch
        {
            Participants => new Dictionary<string, object?>
            {
                ["displayName"] = Read(FieldClass.DisplayName, plan.Participant!.DisplayName),
                ["isActive"] = Read(FieldClass.ParticipantActive, plan.Participant.IsActive)
            },
            Artifacts => new Dictionary<string, object?>
            {
                ["name"] = Read(FieldClass.ArtifactName, ArtifactName(plan.Artifact!))
            },
            Negotiations => new Dictionary<string, object?>
            {
                ["status"] = Read(FieldClass.NegotiationStatus, plan.Negotiations[0].Status.ToString()),
                ["isOpen"] = plan.Negotiations[0].Status == NegotiationStatus.Open
            },
            _ => new Dictionary<string, object?>
            {
                ["status"] = Read(FieldClass.OfferStatus, plan.Offers[0].Status.ToString()),
                ["amount"] = Read(FieldClass.OfferAmount, plan.Offers[0].Amount),
                ["currency"] = Read(FieldClass.OfferCurrency, plan.Offers[0].Currency),
                ["negotiationId"] = Read(FieldClass.OfferNegotiationId, plan.Offers[0].NegotiationId)
            }
        };
    }

    /// <summary>How many rows of each kind the delete marks.</summary>
    private static Dictionary<string, int> Counts(Plan plan) => new()
    {
        ["participants"] = plan.Participant is null ? 0 : 1,
        ["artifacts"] = plan.Artifact is null ? 0 : 1,
        ["negotiations"] = plan.Negotiations.Count,
        ["offers"] = plan.Offers.Count
    };

    /// <summary>The counts, and the open ones among them (Spec §11.3: the confirm warns of an open negotiation).</summary>
    private static Dictionary<string, int> Summary(Plan plan)
    {
        var summary = Counts(plan);
        summary["openNegotiations"] = plan.Negotiations.Count(n => n.Status == NegotiationStatus.Open);
        summary["openOffers"] = plan.Offers.Count(o => o.Status == OfferStatus.Open);
        return summary;
    }

    private static IResult BlockedResult(Plan plan) =>
        Results.Json(
            new Dictionary<string, object?>
            {
                ["error"] = AdminDeleteCopy.BlockedError,
                ["message"] = AdminDeleteCopy.ArtifactBlocked,
                ["negotiationCount"] = plan.BlockingNegotiations.Count,
                ["negotiationIds"] = plan.BlockingNegotiations.Take(MaxListedNegotiations).ToList()
            },
            statusCode: StatusCodes.Status409Conflict);

    private static IResult TokenInvalidResult() =>
        Results.Json(
            new { error = AdminDeleteCopy.TokenInvalidError, message = AdminDeleteCopy.TokenInvalid },
            statusCode: StatusCodes.Status409Conflict);

    private static IResult ChangedResult() =>
        Results.Json(
            new { error = AdminDeny.GenericConflict, message = AdminDeleteCopy.Changed },
            statusCode: StatusCodes.Status409Conflict);

    /// <summary>A refusal after rows were loaded for writing: they are let go of first.</summary>
    private static IResult Release(DealowareDbContext db, IResult result)
    {
        db.ChangeTracker.Clear();
        return result;
    }

    // ---- Snapshots and hashes --------------------------------------------------------

    /// <summary>
    /// One audit snapshot: the row's fields as the delete found them, its
    /// version and its soft-delete mark, through the field policy (a class
    /// the owner may not read is left out). No credential, email or token.
    /// </summary>
    private static string Snapshot(
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Dictionary<FieldClass, object?> values,
        uint version,
        DateTimeOffset? deletedAt)
    {
        var fields = new Dictionary<FieldClass, object?>(values)
        {
            [FieldClass.EntityVersion] = version,
            [FieldClass.SoftDeletedAt] = deletedAt
        };
        return AdminAuditSnapshots.FromAllowedFields(
            fieldPolicy, principal, fields, FieldResourceContext.ForSelfProfile(string.Empty));
    }

    /// <summary>
    /// The name an artifact is shown by: its subject entity with the lowest id,
    /// the order the edit token and the cascade hash use. Rows come back in no
    /// fixed order, so the first loaded is not used.
    /// </summary>
    private static string? ArtifactName(Artifact artifact)
        => artifact.Entities.OrderBy(e => e.Id).FirstOrDefault()?.Name;

    private static string HashToken(string raw)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(raw)));

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex TokenShape();

    /// <summary>Each part with its length, so two parts never read as one; null is not the empty text.</summary>
    private static void Append(StringBuilder text, string? part)
    {
        if (part is null)
        {
            text.Append("-;");
            return;
        }

        text.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part).Append(';');
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>To the microsecond: what PostgreSQL keeps of a time.</summary>
    private static string? Instant(DateTimeOffset? at)
        => at is null ? null : Number(at.Value.UtcTicks / 10);

    private static DateTimeOffset ToMillisecond(DateTimeOffset at)
    {
        var utc = at.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);
    }
}

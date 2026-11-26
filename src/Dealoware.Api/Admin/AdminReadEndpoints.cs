using System.Security.Claims;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// CoreOwner-only admin read API: lists, detail, name search, paging, stats.
/// No writes. Include-deleted is a read and writes no audit row.
/// <para>
/// Each list and each detail is one query function
/// (<see cref="ListParticipantsAsync"/> and the rest), used by the JSON
/// route here and by the console pages (<see cref="AdminListPages"/>), so a
/// page shows exactly what the API answers. A status list is
/// comma-separated. A detail of a soft-deleted record is answered only with
/// <c>includeDeleted=true</c> (the "Show deleted" toggle); without it, as
/// for a record that is not there, 404. The JSON routes refuse a value they
/// do not take with the generic 400; the pages drop it before asking.
/// </para>
/// <para>
/// A detail's JSON reply carries the row's edit token as <c>ETag</c>
/// (<see cref="AdminEditToken"/>): what a write to that row sends back as
/// <c>If-Match</c> (<see cref="AdminEditEndpoints"/>).
/// </para>
/// </summary>
public static class AdminReadEndpoints
{
    public static void MapAdminReadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin/api")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        group.MapGet("/participants", ListParticipants).WithName("AdminListParticipants");
        group.MapGet("/participants/{id:guid}", GetParticipant).WithName("AdminGetParticipant");
        group.MapGet("/artifacts", ListArtifacts).WithName("AdminListArtifacts");
        group.MapGet("/artifacts/{id:guid}", GetArtifact).WithName("AdminGetArtifact");
        group.MapGet("/negotiations", ListNegotiations).WithName("AdminListNegotiations");
        group.MapGet("/negotiations/{id:guid}", GetNegotiation).WithName("AdminGetNegotiation");
        group.MapGet("/offers", ListOffers).WithName("AdminListOffers");
        group.MapGet("/offers/{id:guid}", GetOffer).WithName("AdminGetOffer");
        group.MapGet("/stats", GetStats).WithName("AdminGetStats");
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

    /// <summary>Participant statuses, as the list filters them (from <c>IsActive</c>).</summary>
    public static readonly IReadOnlyList<string> ParticipantStatuses = ["Active", "Suspended"];

    public static readonly IReadOnlyList<string> NegotiationStatuses = Enum.GetNames<NegotiationStatus>();

    public static readonly IReadOnlyList<string> OfferStatuses = Enum.GetNames<OfferStatus>();

    /// <summary>One page of a list: the filtered total and this page's rows, already projected.</summary>
    internal sealed record ListResult(int Total, IReadOnlyList<Dictionary<string, object?>> Items);

    /// <summary>
    /// One record, projected, with its edit token; the token is null where the
    /// policy does not let the reader see the row's version.
    /// </summary>
    internal sealed record Detail(Dictionary<string, object?> Dto, string? EditToken);

    /// <summary>A detail as JSON, its edit token as <c>ETag</c>; 404 when there is none.</summary>
    internal static IResult DetailResult(HttpContext context, Detail? detail)
    {
        if (detail is null)
            return AdminDeny.NotFoundResult();
        if (detail.EditToken is not null)
            context.Response.Headers.ETag = AdminEditToken.ToHeader(detail.EditToken);
        return Results.Json(detail.Dto);
    }

    private static IResult Page(AdminListQuery query, ListResult result)
        => Results.Json(AdminReadProjection.ListPage(query.Offset, query.Limit, result.Total, result.Items));

    /// <summary>The status filter of a list that has one: names only, comma-separated; anything else is the generic 400.</summary>
    private static bool TryWithStatuses(
        AdminListQuery query,
        IReadOnlyList<string> allowed,
        out AdminListQuery withStatuses,
        out IResult? error)
    {
        withStatuses = query;
        error = null;
        if (!AdminListQuery.TrySplitStatuses(query.Status, allowed, out var statuses))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        withStatuses = query with { Statuses = statuses };
        return true;
    }

    private static async Task<IResult> ListParticipants(
        HttpContext context,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParse(context.Request.Query, AdminListQuery.ParticipantSortKeys, allowAmount: false, out var query, out var error))
            return error!;
        if (!TryWithStatuses(query, ParticipantStatuses, out query, out error))
            return error!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Page(query, await ListParticipantsAsync(db, fieldPolicy, principal, query, cancellationToken));
    }

    internal static async Task<ListResult> ListParticipantsAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        AdminListQuery query,
        CancellationToken cancellationToken)
    {
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);

        var rows = db.Participants.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(p => p.DeletedAt == null);

        // Active, Suspended, or both (which is no constraint).
        var active = query.Statuses.Contains("Active");
        var suspended = query.Statuses.Contains("Suspended");
        if (active && !suspended)
            rows = rows.Where(p => p.IsActive);
        else if (suspended && !active)
            rows = rows.Where(p => !p.IsActive);

        if (!string.IsNullOrEmpty(query.Participant))
        {
            var participant = query.Participant;
            if (Guid.TryParse(participant, out var participantId))
                rows = rows.Where(p => p.Sub == participant || p.Id == participantId);
            else
                rows = rows.Where(p => p.Sub == participant);
        }

        rows = ApplyCreatedUpdated(rows, query, p => p.CreatedAt, p => p.UpdatedAt);

        if (!string.IsNullOrEmpty(query.Q))
        {
            var needle = query.Q.ToLowerInvariant();
            rows = rows.Where(p =>
                (canSearchName && p.DisplayName != null && p.DisplayName.ToLower().Contains(needle))
                || p.Sub.ToLower().Contains(needle));
        }

        var (total, items) = await PageQueryAsync(
            rows, ApplyParticipantSort(rows, query), query, cancellationToken);

        return new ListResult(
            total,
            items.Select(p => AdminReadProjection.Participant(p, fieldPolicy, principal, detail: false)).ToList());
    }

    private static async Task<IResult> GetParticipant(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParseIncludeDeleted(context.Request.Query, out var includeDeleted, out var error))
            return error!;

        return DetailResult(context, await ReadParticipantAsync(
            db, fieldPolicy, AdminReadProjection.CoreOwnerFrom(context), id, includeDeleted, cancellationToken));
    }

    /// <summary>
    /// One participant, or null when there is none, or when it is soft-deleted
    /// and <paramref name="includeDeleted"/> (the "Show deleted" toggle) is off.
    /// </summary>
    internal static async Task<Dictionary<string, object?>?> GetParticipantAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
        => (await ReadParticipantAsync(db, fieldPolicy, principal, id, includeDeleted, cancellationToken))?.Dto;

    /// <summary><see cref="GetParticipantAsync"/>, with the row's edit token.</summary>
    internal static async Task<Detail?> ReadParticipantAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var participant = await db.Participants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && (includeDeleted || p.DeletedAt == null), cancellationToken);
        if (participant is null)
            return null;

        return new Detail(
            AdminReadProjection.Participant(participant, fieldPolicy, principal, detail: true),
            AdminReadProjection.CanReadEditToken(fieldPolicy, principal) ? AdminEditToken.For(participant) : null);
    }

    private static async Task<IResult> ListArtifacts(
        HttpContext context,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParse(context.Request.Query, AdminListQuery.ArtifactSortKeys, allowAmount: false, out var query, out var error))
            return error!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Page(query, await ListArtifactsAsync(db, fieldPolicy, principal, query, cancellationToken));
    }

    internal static async Task<ListResult> ListArtifactsAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        AdminListQuery query,
        CancellationToken cancellationToken)
    {
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchName = fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx);

        var rows = db.Artifacts.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(a => a.DeletedAt == null);

        if (!string.IsNullOrEmpty(query.Participant))
            rows = rows.Where(a => a.OwnerParticipantId == query.Participant);
        if (query.ArtifactId.HasValue)
            rows = rows.Where(a => a.Id == query.ArtifactId.Value);

        rows = ApplyCreatedUpdated(rows, query, a => a.CreatedAt, a => a.UpdatedAt);

        if (!string.IsNullOrEmpty(query.Q) && canSearchName)
        {
            var needle = query.Q.ToLowerInvariant();
            rows = rows.Where(a =>
                a.Intent.ToLower().Contains(needle)
                || a.Entities.Any(e => e.Name.ToLower().Contains(needle)));
        }
        else if (!string.IsNullOrEmpty(query.Q) && !canSearchName)
        {
            rows = rows.Where(a => false);
        }

        var ordered = ApplyArtifactSort(rows, query).Include(a => a.Entities);
        var (total, items) = await PageQueryAsync(rows, ordered, query, cancellationToken);
        var related = await RelatedRowsAsync(db, items.Select(a => a.OwnerParticipantId), [], [], query.IncludeDeleted, cancellationToken);

        return new ListResult(
            total,
            items.Select(a => AdminReadProjection.Artifact(a, fieldPolicy, principal, detail: false, related)).ToList());
    }

    private static async Task<IResult> GetArtifact(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParseIncludeDeleted(context.Request.Query, out var includeDeleted, out var error))
            return error!;

        return DetailResult(context, await ReadArtifactAsync(
            db, fieldPolicy, AdminReadProjection.CoreOwnerFrom(context), id, includeDeleted, cancellationToken));
    }

    /// <summary>One artifact; null when there is none, or it is soft-deleted and the toggle is off.</summary>
    internal static async Task<Dictionary<string, object?>?> GetArtifactAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
        => (await ReadArtifactAsync(db, fieldPolicy, principal, id, includeDeleted, cancellationToken))?.Dto;

    /// <summary><see cref="GetArtifactAsync"/>, with the row's edit token.</summary>
    internal static async Task<Detail?> ReadArtifactAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var artifact = await db.Artifacts.AsNoTracking()
            .Include(a => a.Entities)
            .FirstOrDefaultAsync(a => a.Id == id && (includeDeleted || a.DeletedAt == null), cancellationToken);
        if (artifact is null)
            return null;

        var related = await RelatedRowsAsync(db, [artifact.OwnerParticipantId], [], [], includeDeleted, cancellationToken);
        return new Detail(
            AdminReadProjection.Artifact(artifact, fieldPolicy, principal, detail: true, related),
            AdminReadProjection.CanReadEditToken(fieldPolicy, principal) ? AdminEditToken.For(artifact) : null);
    }

    private static async Task<IResult> ListNegotiations(
        HttpContext context,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParse(context.Request.Query, AdminListQuery.NegotiationSortKeys, allowAmount: false, out var query, out var error))
            return error!;
        if (!TryWithStatuses(query, NegotiationStatuses, out query, out error))
            return error!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Page(query, await ListNegotiationsAsync(db, fieldPolicy, principal, query, cancellationToken));
    }

    internal static async Task<ListResult> ListNegotiationsAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        AdminListQuery query,
        CancellationToken cancellationToken)
    {
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchArtifactName = fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx);
        var canSearchDisplayName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);

        var rows = db.Negotiations.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(n => n.DeletedAt == null);

        if (query.Statuses.Count > 0)
        {
            var statuses = query.Statuses.Select(Enum.Parse<NegotiationStatus>).ToList();
            rows = rows.Where(n => statuses.Contains(n.Status));
        }

        if (!string.IsNullOrEmpty(query.Participant))
        {
            var participant = query.Participant;
            rows = rows.Where(n => n.PartyAParticipantId == participant || n.PartyBParticipantId == participant);
        }

        if (query.ArtifactId.HasValue)
            rows = rows.Where(n => n.ArtifactId == query.ArtifactId.Value);
        if (query.NegotiationId.HasValue)
            rows = rows.Where(n => n.Id == query.NegotiationId.Value);

        rows = ApplyCreatedUpdated(rows, query, n => n.CreatedAt, n => n.UpdatedAt);

        if (!string.IsNullOrEmpty(query.Q))
        {
            var needle = query.Q.ToLowerInvariant();
            var matchingIds = db.Negotiations.AsNoTracking().Select(n => n.Id);
            if (canSearchArtifactName || canSearchDisplayName)
            {
                matchingIds =
                    from n in db.Negotiations.AsNoTracking()
                    join a in db.Artifacts.AsNoTracking() on n.ArtifactId equals a.Id
                    join pa in db.Participants.AsNoTracking() on n.PartyAParticipantId equals pa.Sub into paJoin
                    from pa in paJoin.DefaultIfEmpty()
                    join pb in db.Participants.AsNoTracking() on n.PartyBParticipantId equals pb.Sub into pbJoin
                    from pb in pbJoin.DefaultIfEmpty()
                    where (canSearchArtifactName && (
                            a.Intent.ToLower().Contains(needle)
                            || a.Entities.Any(e => e.Name.ToLower().Contains(needle))))
                        || (canSearchDisplayName && (
                            (pa != null && pa.DisplayName != null && pa.DisplayName.ToLower().Contains(needle))
                            || (pb != null && pb.DisplayName != null && pb.DisplayName.ToLower().Contains(needle))))
                    select n.Id;
            }
            else
            {
                matchingIds = matchingIds.Where(id => false);
            }

            rows = rows.Where(n => matchingIds.Contains(n.Id));
        }

        var (total, items) = await PageQueryAsync(
            rows, ApplyNegotiationSort(rows, query), query, cancellationToken);
        var related = await RelatedRowsAsync(
            db,
            items.SelectMany(n => new[] { n.PartyAParticipantId, n.PartyBParticipantId }),
            items.Select(n => n.ArtifactId),
            [],
            query.IncludeDeleted,
            cancellationToken);

        return new ListResult(
            total,
            items.Select(n => AdminReadProjection.Negotiation(n, fieldPolicy, principal, detail: false, related)).ToList());
    }

    private static async Task<IResult> GetNegotiation(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParseIncludeDeleted(context.Request.Query, out var includeDeleted, out var error))
            return error!;

        return DetailResult(context, await ReadNegotiationAsync(
            db, fieldPolicy, AdminReadProjection.CoreOwnerFrom(context), id, includeDeleted, cancellationToken));
    }

    /// <summary>One negotiation; null when there is none, or it is soft-deleted and the toggle is off.</summary>
    internal static async Task<Dictionary<string, object?>?> GetNegotiationAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
        => (await ReadNegotiationAsync(db, fieldPolicy, principal, id, includeDeleted, cancellationToken))?.Dto;

    /// <summary>
    /// <see cref="GetNegotiationAsync"/>, with the row's edit token. The
    /// detail says how many open offers the negotiation has, and the token
    /// covers them (one read of their ids, versions and times; a party has at
    /// most one open offer in a negotiation). Soft-deleted offers are left out
    /// of both, as closing or expiring leaves them out.
    /// </summary>
    internal static async Task<Detail?> ReadNegotiationAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var negotiation = await db.Negotiations.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && (includeDeleted || n.DeletedAt == null), cancellationToken);
        if (negotiation is null)
            return null;

        var related = await RelatedRowsAsync(
            db,
            [negotiation.PartyAParticipantId, negotiation.PartyBParticipantId],
            [negotiation.ArtifactId],
            [],
            includeDeleted,
            cancellationToken);
        var openOffers = (await db.Offers.AsNoTracking()
                .Where(o => o.NegotiationId == id && o.DeletedAt == null && o.Status == OfferStatus.Open)
                .Select(o => new { o.Id, o.Status, o.Version, o.UpdatedAt })
                .ToListAsync(cancellationToken))
            .Select(o => new AdminEditToken.OpenOffer(o.Id, o.Status, o.Version, o.UpdatedAt))
            .ToList();
        return new Detail(
            AdminReadProjection.Negotiation(negotiation, fieldPolicy, principal, detail: true, related, openOffers.Count),
            AdminReadProjection.CanReadEditToken(fieldPolicy, principal)
                ? AdminEditToken.For(negotiation, openOffers)
                : null);
    }

    private static async Task<IResult> ListOffers(
        HttpContext context,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParse(context.Request.Query, AdminListQuery.OfferSortKeys, allowAmount: true, out var query, out var error))
            return error!;
        if (!TryWithStatuses(query, OfferStatuses, out query, out error))
            return error!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Page(query, await ListOffersAsync(db, fieldPolicy, principal, query, cancellationToken));
    }

    internal static async Task<ListResult> ListOffersAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        AdminListQuery query,
        CancellationToken cancellationToken)
    {
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchArtifactName = fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx);
        var canSearchDisplayName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);
        var canSearchNegotiationId = fieldPolicy.Evaluate(principal, FieldClass.OfferNegotiationId, FieldAction.Read, ctx);

        var rows = db.Offers.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(o => o.DeletedAt == null);

        if (query.Statuses.Count > 0)
        {
            var statuses = query.Statuses.Select(Enum.Parse<OfferStatus>).ToList();
            rows = rows.Where(o => statuses.Contains(o.Status));
        }

        if (!string.IsNullOrEmpty(query.Participant))
        {
            var participant = query.Participant;
            rows = rows.Where(o => o.FromParticipantId == participant || o.ToParticipantId == participant);
        }

        if (query.NegotiationId.HasValue)
            rows = rows.Where(o => o.NegotiationId == query.NegotiationId.Value);

        if (query.AmountMin.HasValue)
            rows = rows.Where(o => o.Amount != null && o.Amount >= query.AmountMin.Value);
        if (query.AmountMax.HasValue)
            rows = rows.Where(o => o.Amount != null && o.Amount <= query.AmountMax.Value);

        if (query.ArtifactId.HasValue)
        {
            var artifactId = query.ArtifactId.Value;
            rows = rows.Where(o => db.Negotiations.Any(n => n.Id == o.NegotiationId && n.ArtifactId == artifactId));
        }

        rows = ApplyCreatedUpdated(rows, query, o => o.CreatedAt, o => o.UpdatedAt);

        if (!string.IsNullOrEmpty(query.Q))
        {
            var needle = query.Q.ToLowerInvariant();
            var parsedNegotiationId = Guid.TryParse(query.Q, out var qGuid) ? qGuid : (Guid?)null;

            var matchingIds =
                from o in db.Offers.AsNoTracking()
                join n in db.Negotiations.AsNoTracking() on o.NegotiationId equals n.Id
                join a in db.Artifacts.AsNoTracking() on n.ArtifactId equals a.Id
                join fp in db.Participants.AsNoTracking() on o.FromParticipantId equals fp.Sub into fpJoin
                from fp in fpJoin.DefaultIfEmpty()
                where (canSearchArtifactName && (
                        a.Intent.ToLower().Contains(needle)
                        || a.Entities.Any(e => e.Name.ToLower().Contains(needle))))
                    || (canSearchDisplayName && fp != null && fp.DisplayName != null && fp.DisplayName.ToLower().Contains(needle))
                    || (canSearchNegotiationId && parsedNegotiationId != null && o.NegotiationId == parsedNegotiationId.Value)
                select o.Id;

            rows = rows.Where(o => matchingIds.Contains(o.Id));
        }

        var (total, items) = await PageQueryAsync(
            rows, ApplyOfferSort(rows, query, db), query, cancellationToken);
        var negotiations = await NegotiationsByIdAsync(db, items.Select(o => o.NegotiationId), cancellationToken);
        var related = await RelatedRowsAsync(
            db,
            items.SelectMany(o => new[] { o.FromParticipantId, o.ToParticipantId }),
            negotiations.Values.Select(n => n.ArtifactId),
            negotiations.Values,
            query.IncludeDeleted,
            cancellationToken);

        return new ListResult(
            total,
            items.Select(o => AdminReadProjection.Offer(o, fieldPolicy, principal, detail: false, related)).ToList());
    }

    private static async Task<IResult> GetOffer(
        HttpContext context,
        Guid id,
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;
        if (!AdminListQuery.TryParseIncludeDeleted(context.Request.Query, out var includeDeleted, out var error))
            return error!;

        return DetailResult(context, await ReadOfferAsync(
            db, fieldPolicy, AdminReadProjection.CoreOwnerFrom(context), id, includeDeleted, cancellationToken));
    }

    /// <summary>One offer; null when there is none, or it is soft-deleted and the toggle is off.</summary>
    internal static async Task<Dictionary<string, object?>?> GetOfferAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
        => (await ReadOfferAsync(db, fieldPolicy, principal, id, includeDeleted, cancellationToken))?.Dto;

    /// <summary><see cref="GetOfferAsync"/>, with the row's edit token.</summary>
    internal static async Task<Detail?> ReadOfferAsync(
        DealowareDbContext db,
        IFieldPolicy fieldPolicy,
        FieldPrincipal principal,
        Guid id,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var offer = await db.Offers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && (includeDeleted || o.DeletedAt == null), cancellationToken);
        if (offer is null)
            return null;

        var negotiations = await NegotiationsByIdAsync(db, [offer.NegotiationId], cancellationToken);
        var related = await RelatedRowsAsync(
            db,
            [offer.FromParticipantId, offer.ToParticipantId],
            negotiations.Values.Select(n => n.ArtifactId),
            negotiations.Values,
            includeDeleted,
            cancellationToken);
        return new Detail(
            AdminReadProjection.Offer(offer, fieldPolicy, principal, detail: true, related),
            AdminReadProjection.CanReadEditToken(fieldPolicy, principal) ? AdminEditToken.For(offer) : null);
    }

    private static async Task<Dictionary<Guid, Negotiation>> NegotiationsByIdAsync(
        DealowareDbContext db,
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        if (wanted.Count == 0)
            return [];
        return await db.Negotiations.AsNoTracking()
            .Where(n => wanted.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, cancellationToken);
    }

    /// <summary>
    /// The participants and artifacts a page's rows name, read by key: never
    /// more rows than the page refers to. Soft-deleted ones too, so that a row
    /// can say what it refers to is deleted; their names go out only with the
    /// read's "Show deleted" toggle (<paramref name="includeDeleted"/>).
    /// </summary>
    private static async Task<AdminRelatedRows> RelatedRowsAsync(
        DealowareDbContext db,
        IEnumerable<string> participantSubs,
        IEnumerable<Guid> artifactIds,
        IEnumerable<Negotiation> negotiations,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var subs = participantSubs.Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.Ordinal).ToList();
        var artifacts = artifactIds.Distinct().ToList();

        var participantsBySub = subs.Count == 0
            ? new Dictionary<string, Domain.Participants.Participant>(StringComparer.Ordinal)
            : (await db.Participants.AsNoTracking().Where(p => subs.Contains(p.Sub)).ToListAsync(cancellationToken))
                .ToDictionary(p => p.Sub, StringComparer.Ordinal);
        var artifactsById = artifacts.Count == 0
            ? new Dictionary<Guid, Domain.Artifacts.Artifact>()
            : await db.Artifacts.AsNoTracking().Include(a => a.Entities)
                .Where(a => artifacts.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

        return new AdminRelatedRows(participantsBySub, artifactsById, negotiations.ToDictionary(n => n.Id), includeDeleted);
    }

    private static async Task<IResult> GetStats(
        HttpContext context,
        DealowareDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;

        var totals = await CountTotalsAsync(db, cancellationToken);
        return Results.Json(new
        {
            participants = totals.Participants,
            openNegotiations = totals.OpenNegotiations,
            offers = totals.Offers,
            accepts = totals.Accepts,
            declines = totals.Declines
        });
    }

    /// <summary>The five Spec §6 counts. All leave soft-deleted rows out.</summary>
    internal sealed record Totals(int Participants, int OpenNegotiations, int Offers, int Accepts, int Declines);

    /// <summary>
    /// The five Spec §6 counts, in the order the Stats page shows them; each
    /// leaves soft-deleted rows out. Offers counts every offer of any status.
    /// </summary>
    internal static readonly IReadOnlyList<Func<DealowareDbContext, CancellationToken, Task<int>>> TotalCounts =
    [
        (db, ct) => db.Participants.CountAsync(p => p.DeletedAt == null, ct),
        (db, ct) => db.Negotiations.CountAsync(n => n.DeletedAt == null && n.Status == NegotiationStatus.Open, ct),
        (db, ct) => db.Offers.CountAsync(o => o.DeletedAt == null, ct),
        (db, ct) => db.Offers.CountAsync(o => o.DeletedAt == null && o.Status == OfferStatus.Accepted, ct),
        (db, ct) => db.Offers.CountAsync(o => o.DeletedAt == null && o.Status == OfferStatus.Declined, ct)
    ];

    /// <summary>The one count of the Spec §6 totals, for the stats API.</summary>
    internal static async Task<Totals> CountTotalsAsync(DealowareDbContext db, CancellationToken cancellationToken)
    {
        var counts = new int[TotalCounts.Count];
        for (var i = 0; i < counts.Length; i++)
            counts[i] = await TotalCounts[i](db, cancellationToken);
        return new Totals(counts[0], counts[1], counts[2], counts[3], counts[4]);
    }

    private static IQueryable<T> ApplyCreatedUpdated<T>(
        IQueryable<T> rows,
        AdminListQuery query,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> createdAt,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset?>> updatedAt)
    {
        if (query.CreatedFrom.HasValue)
            rows = rows.Where(PropertyGte(createdAt, query.CreatedFrom.Value));
        if (query.CreatedTo.HasValue)
            rows = rows.Where(PropertyLte(createdAt, query.CreatedTo.Value));
        if (query.UpdatedFrom.HasValue || query.UpdatedTo.HasValue)
        {
            var from = query.UpdatedFrom;
            var to = query.UpdatedTo;
            rows = rows.Where(EffectiveUpdatedBetween(createdAt, updatedAt, from, to));
        }

        return rows;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> PropertyGte<T>(
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset value)
    {
        var p = selector.Parameters[0];
        var body = System.Linq.Expressions.Expression.GreaterThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(value));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> PropertyLte<T>(
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset value)
    {
        var p = selector.Parameters[0];
        var body = System.Linq.Expressions.Expression.LessThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(value));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> EffectiveUpdatedBetween<T>(
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> createdAt,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset?>> updatedAt,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var p = createdAt.Parameters[0];
        var updatedBody = new ParameterReplace(updatedAt.Parameters[0], p).Visit(updatedAt.Body)!;
        var coalesce = System.Linq.Expressions.Expression.Coalesce(updatedBody, createdAt.Body);
        System.Linq.Expressions.Expression body = System.Linq.Expressions.Expression.Constant(true);
        if (from.HasValue)
            body = System.Linq.Expressions.Expression.AndAlso(
                body,
                System.Linq.Expressions.Expression.GreaterThanOrEqual(coalesce, System.Linq.Expressions.Expression.Constant(from.Value)));
        if (to.HasValue)
            body = System.Linq.Expressions.Expression.AndAlso(
                body,
                System.Linq.Expressions.Expression.LessThanOrEqual(coalesce, System.Linq.Expressions.Expression.Constant(to.Value)));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
    }

    private sealed class ParameterReplace : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.ParameterExpression _from;
        private readonly System.Linq.Expressions.ParameterExpression _to;

        public ParameterReplace(System.Linq.Expressions.ParameterExpression from, System.Linq.Expressions.ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }

    /// <summary>
    /// Count on the filtered query; sort, Skip, and Take stay in SQL so the
    /// data query never reads more than <see cref="AdminListQuery.Limit"/> rows.
    /// </summary>
    private static async Task<(int Total, List<T> Items)> PageQueryAsync<T>(
        IQueryable<T> filtered,
        IQueryable<T> ordered,
        AdminListQuery query,
        CancellationToken cancellationToken)
    {
        var total = await filtered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
        return (total, items);
    }

    private static IQueryable<Domain.Participants.Participant> ApplyParticipantSort(
        IQueryable<Domain.Participants.Participant> rows,
        AdminListQuery query)
    {
        var key = AdminListQuery.NormalizeSort(query.Sort);
        IOrderedQueryable<Domain.Participants.Participant> ordered = key switch
        {
            "updated" => query.Descending
                ? rows.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                : rows.OrderBy(p => p.UpdatedAt ?? p.CreatedAt),
            "name" => query.Descending
                ? rows.OrderByDescending(p => p.DisplayName)
                : rows.OrderBy(p => p.DisplayName),
            _ => query.Descending
                ? rows.OrderByDescending(p => p.CreatedAt)
                : rows.OrderBy(p => p.CreatedAt)
        };
        return query.Descending ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
    }

    private static IQueryable<Domain.Artifacts.Artifact> ApplyArtifactSort(
        IQueryable<Domain.Artifacts.Artifact> rows,
        AdminListQuery query)
    {
        var key = AdminListQuery.NormalizeSort(query.Sort);
        IOrderedQueryable<Domain.Artifacts.Artifact> ordered = key switch
        {
            "updated" => query.Descending
                ? rows.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                : rows.OrderBy(a => a.UpdatedAt ?? a.CreatedAt),
            "name" or "artifact" => query.Descending
                ? rows.OrderByDescending(a => a.Entities.Select(e => e.Name).FirstOrDefault())
                : rows.OrderBy(a => a.Entities.Select(e => e.Name).FirstOrDefault()),
            _ => query.Descending
                ? rows.OrderByDescending(a => a.CreatedAt)
                : rows.OrderBy(a => a.CreatedAt)
        };
        return query.Descending ? ordered.ThenByDescending(a => a.Id) : ordered.ThenBy(a => a.Id);
    }

    private static IQueryable<Negotiation> ApplyNegotiationSort(
        IQueryable<Negotiation> rows,
        AdminListQuery query)
    {
        var key = AdminListQuery.NormalizeSort(query.Sort);
        IOrderedQueryable<Negotiation> ordered = key switch
        {
            "updated" => query.Descending
                ? rows.OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
                : rows.OrderBy(n => n.UpdatedAt ?? n.CreatedAt),
            "status" => query.Descending
                ? rows.OrderByDescending(n => n.Status)
                : rows.OrderBy(n => n.Status),
            "participant" => query.Descending
                ? rows.OrderByDescending(n => n.PartyAParticipantId)
                : rows.OrderBy(n => n.PartyAParticipantId),
            "artifact" => query.Descending
                ? rows.OrderByDescending(n => n.ArtifactId)
                : rows.OrderBy(n => n.ArtifactId),
            "id" or "negotiationid" => query.Descending
                ? rows.OrderByDescending(n => n.Id)
                : rows.OrderBy(n => n.Id),
            _ => query.Descending
                ? rows.OrderByDescending(n => n.CreatedAt)
                : rows.OrderBy(n => n.CreatedAt)
        };
        return query.Descending ? ordered.ThenByDescending(n => n.Id) : ordered.ThenBy(n => n.Id);
    }

    private static IQueryable<Offer> ApplyOfferSort(
        IQueryable<Offer> rows,
        AdminListQuery query,
        DealowareDbContext db)
    {
        var key = AdminListQuery.NormalizeSort(query.Sort);
        IOrderedQueryable<Offer> ordered = key switch
        {
            "updated" => query.Descending
                ? rows.OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                : rows.OrderBy(o => o.UpdatedAt ?? o.CreatedAt),
            "status" => query.Descending
                ? rows.OrderByDescending(o => o.Status)
                : rows.OrderBy(o => o.Status),
            "participant" => query.Descending
                ? rows.OrderByDescending(o => o.FromParticipantId)
                : rows.OrderBy(o => o.FromParticipantId),
            "artifact" => query.Descending
                ? rows.OrderByDescending(o => db.Negotiations
                    .Where(n => n.Id == o.NegotiationId)
                    .Select(n => n.ArtifactId)
                    .FirstOrDefault())
                : rows.OrderBy(o => db.Negotiations
                    .Where(n => n.Id == o.NegotiationId)
                    .Select(n => n.ArtifactId)
                    .FirstOrDefault()),
            "amount" => query.Descending
                ? rows.OrderByDescending(o => o.Amount)
                : rows.OrderBy(o => o.Amount),
            "negotiationid" => query.Descending
                ? rows.OrderByDescending(o => o.NegotiationId)
                : rows.OrderBy(o => o.NegotiationId),
            "id" => query.Descending
                ? rows.OrderByDescending(o => o.Id)
                : rows.OrderBy(o => o.Id),
            _ => query.Descending
                ? rows.OrderByDescending(o => o.CreatedAt)
                : rows.OrderBy(o => o.CreatedAt)
        };
        return query.Descending ? ordered.ThenByDescending(o => o.Id) : ordered.ThenBy(o => o.Id);
    }
}

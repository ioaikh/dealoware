using System.Security.Claims;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// CoreOwner-only admin read API: lists, detail, name search, paging, stats.
/// No writes. Include-deleted is a read and writes no audit row.
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

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);

        var rows = db.Participants.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(p => p.DeletedAt == null);

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

        var (total, items) = await PageFilteredAsync(
            rows, query, p => ParticipantSortKey(p, query), cancellationToken);

        return Results.Json(AdminReadProjection.ListPage(
            query.Offset,
            query.Limit,
            total,
            items.Select(p => AdminReadProjection.Participant(p, fieldPolicy, principal, detail: false))));
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

        var participant = await db.Participants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (participant is null)
            return AdminDeny.NotFoundResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Results.Json(AdminReadProjection.Participant(participant, fieldPolicy, principal, detail: true));
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

        var (total, items) = await PageFilteredAsync(
            rows.Include(a => a.Entities), query, a => ArtifactSortKey(a, query), cancellationToken);

        return Results.Json(AdminReadProjection.ListPage(
            query.Offset,
            query.Limit,
            total,
            items.Select(a => AdminReadProjection.Artifact(a, fieldPolicy, principal, detail: false))));
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

        var artifact = await db.Artifacts.AsNoTracking()
            .Include(a => a.Entities)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (artifact is null)
            return AdminDeny.NotFoundResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Results.Json(AdminReadProjection.Artifact(artifact, fieldPolicy, principal, detail: true));
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

        if (!string.IsNullOrEmpty(query.Status)
            && !Enum.TryParse<NegotiationStatus>(query.Status, ignoreCase: true, out _))
            return AdminDeny.BadRequestResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchArtifactName = fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx);
        var canSearchDisplayName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);

        var rows = db.Negotiations.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(n => n.DeletedAt == null);

        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<NegotiationStatus>(query.Status, ignoreCase: true, out var status))
            rows = rows.Where(n => n.Status == status);

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

        var (total, items) = await PageFilteredAsync(
            rows, query, n => NegotiationSortKey(n, query), cancellationToken);

        return Results.Json(AdminReadProjection.ListPage(
            query.Offset,
            query.Limit,
            total,
            items.Select(n => AdminReadProjection.Negotiation(n, fieldPolicy, principal, detail: false))));
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

        var negotiation = await db.Negotiations.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (negotiation is null)
            return AdminDeny.NotFoundResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Results.Json(AdminReadProjection.Negotiation(negotiation, fieldPolicy, principal, detail: true));
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

        if (!string.IsNullOrEmpty(query.Status)
            && !Enum.TryParse<OfferStatus>(query.Status, ignoreCase: true, out _))
            return AdminDeny.BadRequestResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var ctx = FieldResourceContext.ForSelfProfile(string.Empty);
        var canSearchArtifactName = fieldPolicy.Evaluate(principal, FieldClass.ArtifactName, FieldAction.Read, ctx);
        var canSearchDisplayName = fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, ctx);
        var canSearchNegotiationId = fieldPolicy.Evaluate(principal, FieldClass.OfferNegotiationId, FieldAction.Read, ctx);

        var rows = db.Offers.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
            rows = rows.Where(o => o.DeletedAt == null);

        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<OfferStatus>(query.Status, ignoreCase: true, out var status))
            rows = rows.Where(o => o.Status == status);

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

        var materialized = await rows.ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, Guid>? offerArtifacts = null;
        if (AdminListQuery.NormalizeSort(query.Sort) is "artifact")
        {
            var negotiationIds = materialized.Select(o => o.NegotiationId).Distinct().ToList();
            offerArtifacts = await db.Negotiations.AsNoTracking()
                .Where(n => negotiationIds.Contains(n.Id))
                .Select(n => new { n.Id, n.ArtifactId })
                .ToDictionaryAsync(n => n.Id, n => n.ArtifactId, cancellationToken);
        }

        var (total, items) = PageInMemory(
            materialized, query, o => OfferSortKey(o, query, offerArtifacts));

        return Results.Json(AdminReadProjection.ListPage(
            query.Offset,
            query.Limit,
            total,
            items.Select(o => AdminReadProjection.Offer(o, fieldPolicy, principal, detail: false))));
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

        var offer = await db.Offers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
            return AdminDeny.NotFoundResult();

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        return Results.Json(AdminReadProjection.Offer(offer, fieldPolicy, principal, detail: true));
    }

    private static async Task<IResult> GetStats(
        HttpContext context,
        DealowareDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryCoreOwner(context, out var deny))
            return deny!;

        var participants = await db.Participants.CountAsync(p => p.DeletedAt == null, cancellationToken);
        var openNegotiations = await db.Negotiations.CountAsync(
            n => n.DeletedAt == null && n.Status == NegotiationStatus.Open, cancellationToken);
        var offers = await db.Offers.CountAsync(o => o.DeletedAt == null, cancellationToken);
        var accepts = await db.Offers.CountAsync(
            o => o.DeletedAt == null && o.Status == OfferStatus.Accepted, cancellationToken);
        var declines = await db.Offers.CountAsync(
            o => o.DeletedAt == null && o.Status == OfferStatus.Declined, cancellationToken);

        return Results.Json(new
        {
            participants,
            openNegotiations,
            offers,
            accepts,
            declines
        });
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

    private static async Task<(int Total, List<T> Items)> PageFilteredAsync<T>(
        IQueryable<T> rows,
        AdminListQuery query,
        Func<T, IComparable> key,
        CancellationToken cancellationToken)
    {
        var materialized = await rows.ToListAsync(cancellationToken);
        return PageInMemory(materialized, query, key);
    }

    private static (int Total, List<T> Items) PageInMemory<T>(
        List<T> materialized,
        AdminListQuery query,
        Func<T, IComparable> key)
    {
        var ordered = query.Descending
            ? materialized.OrderByDescending(key)
            : materialized.OrderBy(key);
        var page = ordered.Skip(query.Offset).Take(query.Limit).ToList();
        return (materialized.Count, page);
    }

    private static IComparable ParticipantSortKey(Domain.Participants.Participant p, AdminListQuery query)
        => AdminListQuery.NormalizeSort(query.Sort) switch
        {
            "updated" => p.UpdatedAt ?? p.CreatedAt,
            "name" => p.DisplayName ?? string.Empty,
            _ => p.CreatedAt
        };

    private static IComparable ArtifactSortKey(Domain.Artifacts.Artifact a, AdminListQuery query)
        => AdminListQuery.NormalizeSort(query.Sort) switch
        {
            "updated" => a.UpdatedAt ?? a.CreatedAt,
            "name" or "artifact" => a.Entities.Select(e => e.Name).FirstOrDefault() ?? string.Empty,
            _ => a.CreatedAt
        };

    private static IComparable NegotiationSortKey(Negotiation n, AdminListQuery query)
        => AdminListQuery.NormalizeSort(query.Sort) switch
        {
            "updated" => n.UpdatedAt ?? n.CreatedAt,
            "status" => n.Status,
            "participant" => n.PartyAParticipantId,
            "artifact" => n.ArtifactId,
            "id" or "negotiationid" => n.Id,
            _ => n.CreatedAt
        };

    private static IComparable OfferSortKey(
        Offer o,
        AdminListQuery query,
        IReadOnlyDictionary<Guid, Guid>? offerArtifacts)
        => AdminListQuery.NormalizeSort(query.Sort) switch
        {
            "updated" => o.UpdatedAt ?? o.CreatedAt,
            "status" => o.Status,
            "participant" => o.FromParticipantId,
            "artifact" => offerArtifacts != null && offerArtifacts.TryGetValue(o.NegotiationId, out var artifactId)
                ? artifactId
                : Guid.Empty,
            "amount" => o.Amount ?? decimal.MinValue,
            "negotiationid" => o.NegotiationId,
            "id" => o.Id,
            _ => o.CreatedAt
        };
}

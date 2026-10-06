using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

public sealed class AdminDeleteService
{
    public const string ConfirmTokenHeader = "X-Admin-Confirm-Token";
    public const string DeleteAction = "Delete";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly DealowareDbContext _db;
    private readonly IAdminDeleteConfirmTokenRepository _tokens;
    private readonly IAdminAuditRepository _audit;
    private readonly IAdminClock _clock;
    private readonly IIpHasher _ipHasher;
    private readonly IFieldPolicy _fieldPolicy;
    private readonly AdminDeleteConfirmOptions _options;

    public AdminDeleteService(
        DealowareDbContext db,
        IAdminDeleteConfirmTokenRepository tokens,
        IAdminAuditRepository audit,
        IAdminClock clock,
        IIpHasher ipHasher,
        IFieldPolicy fieldPolicy,
        IOptions<AdminDeleteConfirmOptions> options)
    {
        _db = db;
        _tokens = tokens;
        _audit = audit;
        _clock = clock;
        _ipHasher = ipHasher;
        _fieldPolicy = fieldPolicy;
        _options = options.Value;
    }

    public async Task<AdminDeleteResult> CreateIntentAsync(
        string entityType,
        Guid id,
        string actorEmail,
        Guid actorSessionId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeType(entityType, out var type))
            return AdminDeleteResult.NotFound();

        var plan = await BuildPlanAsync(type, id, cancellationToken);
        if (plan is null)
            return AdminDeleteResult.NotFound();

        if (plan.Blocked)
        {
            return AdminDeleteResult.Ok(new AdminDeleteIntentResponse(
                ConfirmToken: string.Empty,
                EntityType: type,
                Id: id,
                DisplayIdentity: plan.DisplayIdentity,
                Version: plan.RootVersion,
                IsOpen: plan.IsOpen,
                Cascade: plan.Counts,
                ExpiresAt: _clock.UtcNow,
                BlockedByNegotiations: plan.BlockedByNegotiations,
                Blocked: true));
        }

        var now = _clock.UtcNow;
        var lifetime = _options.LifetimeSeconds > 0
            ? TimeSpan.FromSeconds(_options.LifetimeSeconds)
            : TimeSpan.FromMinutes(Math.Max(1, _options.LifetimeMinutes));
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var token = AdminDeleteConfirmToken.Create(
            tokenHash: HashToken(raw),
            actorEmail: actorEmail,
            actorSessionId: actorSessionId,
            entityType: type,
            entityId: id,
            cascadeSetHash: plan.CascadeSetHash,
            createdAt: now,
            expiresAt: now.Add(lifetime));

        await _tokens.AddAsync(token, cancellationToken);
        await _tokens.SaveChangesAsync(cancellationToken);

        return AdminDeleteResult.Ok(new AdminDeleteIntentResponse(
            ConfirmToken: raw,
            EntityType: type,
            Id: id,
            DisplayIdentity: plan.DisplayIdentity,
            Version: plan.RootVersion,
            IsOpen: plan.IsOpen,
            Cascade: plan.Counts,
            ExpiresAt: token.ExpiresAt,
            BlockedByNegotiations: null,
            Blocked: false));
    }

    public async Task<AdminDeleteResult> ConfirmAsync(
        string entityType,
        Guid id,
        string? rawToken,
        string? ifMatch,
        string actorEmail,
        Guid actorSessionId,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeType(entityType, out var type))
            return AdminDeleteResult.NotFound();

        if (string.IsNullOrWhiteSpace(rawToken))
            return AdminDeleteResult.BlindDelete();

        if (!TryParseIfMatch(ifMatch, out var expectedVersion))
            return AdminDeleteResult.PreconditionRequired();

        var stored = await _tokens.FindByHashAsync(HashToken(rawToken.Trim()), cancellationToken);
        var now = _clock.UtcNow;
        if (stored is null
            || stored.IsConsumed
            || stored.IsExpired(now)
            || stored.ActorSessionId != actorSessionId
            || !string.Equals(stored.ActorEmail, actorEmail, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(stored.EntityType, type, StringComparison.Ordinal)
            || stored.EntityId != id)
        {
            return AdminDeleteResult.InvalidToken();
        }

        var plan = await BuildPlanAsync(type, id, cancellationToken);
        if (plan is null)
            return AdminDeleteResult.NotFound();

        if (plan.Blocked)
            return AdminDeleteResult.ArtifactBlocked(plan.BlockedByNegotiations);

        if (!string.Equals(plan.CascadeSetHash, stored.CascadeSetHash, StringComparison.Ordinal))
            return AdminDeleteResult.InvalidToken();

        if (plan.RootVersion != expectedVersion)
            return AdminDeleteResult.StaleVersion();

        stored.MarkConsumed(now);

        var correlationId = Guid.NewGuid();
        var ipHmac = HashIp(remoteIp);
        var coreOwner = FieldPrincipal.CoreOwner(actorEmail);
        var resourceContext = new FieldResourceContext();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var target in plan.Targets)
            {
                var before = Snapshot(coreOwner, resourceContext, target);
                ApplySoftDelete(target, now);
                var after = """{"deleted":true}""";
                var entry = AdminAuditEntry.CreateEntityEvent(
                    DeleteAction,
                    actorEmail,
                    ipHmac,
                    target.EntityType,
                    target.Id,
                    before,
                    after,
                    correlationId,
                    now);
                await _audit.AddAsync(entry, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return AdminDeleteResult.Deleted();
    }

    public async Task<IReadOnlyList<AdminDeleteUiRow>> ListAsync(
        string entityType,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeType(entityType, out var type))
            return [];

        return type switch
        {
            "participants" => await ListParticipantsAsync(includeDeleted, cancellationToken),
            "artifacts" => await ListArtifactsAsync(includeDeleted, cancellationToken),
            "negotiations" => await ListNegotiationsAsync(includeDeleted, cancellationToken),
            "offers" => await ListOffersAsync(includeDeleted, cancellationToken),
            _ => []
        };
    }

    public async Task<AdminDeleteStatsResponse> StatsAsync(CancellationToken cancellationToken)
    {
        var participants = await _db.Participants.CountAsync(p => p.DeletedAt == null, cancellationToken);
        var openNegotiations = await _db.Negotiations.CountAsync(
            n => n.DeletedAt == null && n.Status == NegotiationStatus.Open, cancellationToken);
        var offers = await _db.Offers.CountAsync(o => o.DeletedAt == null, cancellationToken);
        var accepts = await _db.Offers.CountAsync(
            o => o.DeletedAt == null && o.Status == OfferStatus.Accepted, cancellationToken);
        var declines = await _db.Offers.CountAsync(
            o => o.DeletedAt == null && o.Status == OfferStatus.Declined, cancellationToken);
        return new AdminDeleteStatsResponse(participants, openNegotiations, offers, accepts, declines);
    }

    public static bool TryNormalizeType(string? raw, out string type)
    {
        type = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return type is "participants" or "artifacts" or "negotiations" or "offers";
    }

    public static bool TryParseIfMatch(string? raw, out uint version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var value = raw.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..].Trim();
        value = value.Trim('"');
        return uint.TryParse(value, out version);
    }

    private async Task<CascadePlan?> BuildPlanAsync(
        string type,
        Guid id,
        CancellationToken cancellationToken)
    {
        return type switch
        {
            "participants" => await PlanParticipantAsync(id, cancellationToken),
            "artifacts" => await PlanArtifactAsync(id, cancellationToken),
            "negotiations" => await PlanNegotiationAsync(id, cancellationToken),
            "offers" => await PlanOfferAsync(id, cancellationToken),
            _ => null
        };
    }

    private async Task<CascadePlan?> PlanParticipantAsync(Guid id, CancellationToken cancellationToken)
    {
        var participant = await _db.Participants.SingleOrDefaultAsync(
            p => p.Id == id && p.DeletedAt == null, cancellationToken);
        if (participant is null)
            return null;

        var negotiations = await _db.Negotiations
            .Where(n => n.DeletedAt == null
                        && (n.PartyAParticipantId == participant.Sub
                            || n.PartyBParticipantId == participant.Sub))
            .ToListAsync(cancellationToken);
        var negotiationIds = negotiations.Select(n => n.Id).ToList();
        var offers = negotiationIds.Count == 0
            ? []
            : await _db.Offers
                .Where(o => o.DeletedAt == null && negotiationIds.Contains(o.NegotiationId))
                .ToListAsync(cancellationToken);

        var targets = new List<DeleteTarget>
        {
            new("Participant", participant.Id, participant.Version, participant)
        };
        targets.AddRange(negotiations.Select(n => new DeleteTarget("Negotiation", n.Id, n.Version, n)));
        targets.AddRange(offers.Select(o => new DeleteTarget("Offer", o.Id, o.Version, o)));

        return new CascadePlan(
            participant.DisplayName ?? participant.Sub,
            participant.Version,
            IsOpen: negotiations.Any(n => n.Status == NegotiationStatus.Open),
            new AdminDeleteCascadeCounts(negotiations.Count, offers.Count, 0, 1),
            HashCascade(targets),
            targets,
            Blocked: false,
            BlockedByNegotiations: []);
    }

    private async Task<CascadePlan?> PlanArtifactAsync(Guid id, CancellationToken cancellationToken)
    {
        var artifact = await _db.Artifacts
            .Include(a => a.Entities)
            .SingleOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
        if (artifact is null)
            return null;

        var blocking = await _db.Negotiations
            .Where(n => n.DeletedAt == null && n.ArtifactId == id)
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        var name = artifact.Entities.FirstOrDefault()?.Name ?? artifact.Id.ToString("D");
        if (blocking.Count > 0)
        {
            return new CascadePlan(
                name,
                artifact.Version,
                IsOpen: null,
                new AdminDeleteCascadeCounts(0, 0, 1, 0),
                CascadeSetHash: string.Empty,
                Targets: [],
                Blocked: true,
                BlockedByNegotiations: blocking);
        }

        var targets = new List<DeleteTarget>
        {
            new("Artifact", artifact.Id, artifact.Version, artifact)
        };
        return new CascadePlan(
            name,
            artifact.Version,
            IsOpen: null,
            new AdminDeleteCascadeCounts(0, 0, 1, 0),
            HashCascade(targets),
            targets,
            Blocked: false,
            BlockedByNegotiations: []);
    }

    private async Task<CascadePlan?> PlanNegotiationAsync(Guid id, CancellationToken cancellationToken)
    {
        var negotiation = await _db.Negotiations.SingleOrDefaultAsync(
            n => n.Id == id && n.DeletedAt == null, cancellationToken);
        if (negotiation is null)
            return null;

        var offers = await _db.Offers
            .Where(o => o.DeletedAt == null && o.NegotiationId == id)
            .ToListAsync(cancellationToken);

        var targets = new List<DeleteTarget>
        {
            new("Negotiation", negotiation.Id, negotiation.Version, negotiation)
        };
        targets.AddRange(offers.Select(o => new DeleteTarget("Offer", o.Id, o.Version, o)));

        return new CascadePlan(
            negotiation.Id.ToString("D"),
            negotiation.Version,
            IsOpen: negotiation.Status == NegotiationStatus.Open,
            new AdminDeleteCascadeCounts(1, offers.Count, 0, 0),
            HashCascade(targets),
            targets,
            Blocked: false,
            BlockedByNegotiations: []);
    }

    private async Task<CascadePlan?> PlanOfferAsync(Guid id, CancellationToken cancellationToken)
    {
        var offer = await _db.Offers.SingleOrDefaultAsync(
            o => o.Id == id && o.DeletedAt == null, cancellationToken);
        if (offer is null)
            return null;

        var targets = new List<DeleteTarget>
        {
            new("Offer", offer.Id, offer.Version, offer)
        };
        return new CascadePlan(
            offer.Id.ToString("D"),
            offer.Version,
            IsOpen: offer.Status == OfferStatus.Open,
            new AdminDeleteCascadeCounts(0, 1, 0, 0),
            HashCascade(targets),
            targets,
            Blocked: false,
            BlockedByNegotiations: []);
    }

    private async Task<IReadOnlyList<AdminDeleteUiRow>> ListParticipantsAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = _db.Participants.AsQueryable();
        if (!includeDeleted)
            query = query.Where(p => p.DeletedAt == null);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminDeleteUiRow(
                p.Id,
                "participants",
                p.DisplayName ?? p.Sub,
                p.Version,
                p.DeletedAt,
                p.IsActive ? "Active" : "Suspended",
                null))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdminDeleteUiRow>> ListArtifactsAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = _db.Artifacts.Include(a => a.Entities).AsQueryable();
        if (!includeDeleted)
            query = query.Where(a => a.DeletedAt == null);

        var rows = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(a => new AdminDeleteUiRow(
            a.Id,
            "artifacts",
            a.Entities.FirstOrDefault()?.Name ?? a.Id.ToString("D"),
            a.Version,
            a.DeletedAt,
            null,
            null)).ToList();
    }

    private async Task<IReadOnlyList<AdminDeleteUiRow>> ListNegotiationsAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = _db.Negotiations.AsQueryable();
        if (!includeDeleted)
            query = query.Where(n => n.DeletedAt == null);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new AdminDeleteUiRow(
                n.Id,
                "negotiations",
                n.Id.ToString(),
                n.Version,
                n.DeletedAt,
                n.Status.ToString(),
                n.Status == NegotiationStatus.Open))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdminDeleteUiRow>> ListOffersAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = _db.Offers.AsQueryable();
        if (!includeDeleted)
            query = query.Where(o => o.DeletedAt == null);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminDeleteUiRow(
                o.Id,
                "offers",
                o.Id.ToString(),
                o.Version,
                o.DeletedAt,
                o.Status.ToString(),
                o.Status == OfferStatus.Open))
            .ToListAsync(cancellationToken);
    }

    private string Snapshot(FieldPrincipal principal, FieldResourceContext resourceContext, DeleteTarget target)
    {
        object payload = target.Entity switch
        {
            Participant p => Filter(principal, new Dictionary<string, object?>
            {
                ["id"] = p.Id,
                ["displayName"] = Allow(principal, FieldClass.DisplayName, resourceContext) ? p.DisplayName : null,
                ["isActive"] = Allow(principal, FieldClass.ParticipantActive, resourceContext) ? p.IsActive : null,
                ["createdAt"] = Allow(principal, FieldClass.EntityCreatedAt, resourceContext) ? p.CreatedAt : null,
                ["version"] = Allow(principal, FieldClass.EntityVersion, resourceContext) ? p.Version : null,
                ["deletedAt"] = Allow(principal, FieldClass.SoftDeletedAt, resourceContext) ? p.DeletedAt : null
            }),
            Artifact a => Filter(principal, new Dictionary<string, object?>
            {
                ["id"] = a.Id,
                ["name"] = Allow(principal, FieldClass.ArtifactName, resourceContext)
                    ? a.Entities.FirstOrDefault()?.Name
                    : null,
                ["description"] = Allow(principal, FieldClass.ArtifactDescription, resourceContext)
                    ? a.Entities.FirstOrDefault()?.Description
                    : null,
                ["ownerParticipantId"] = Allow(principal, FieldClass.ArtifactOwnerParticipantId, resourceContext)
                    ? a.OwnerParticipantId
                    : null,
                ["createdAt"] = Allow(principal, FieldClass.EntityCreatedAt, resourceContext) ? a.CreatedAt : null,
                ["version"] = Allow(principal, FieldClass.EntityVersion, resourceContext) ? a.Version : null,
                ["deletedAt"] = Allow(principal, FieldClass.SoftDeletedAt, resourceContext) ? a.DeletedAt : null
            }),
            Negotiation n => Filter(principal, new Dictionary<string, object?>
            {
                ["id"] = n.Id,
                ["status"] = Allow(principal, FieldClass.NegotiationStatus, resourceContext) ? n.Status.ToString() : null,
                ["endsAt"] = Allow(principal, FieldClass.NegotiationEndsAt, resourceContext) ? n.EndsAt : null,
                ["artifactId"] = Allow(principal, FieldClass.NegotiationArtifactId, resourceContext) ? n.ArtifactId : null,
                ["partyA"] = Allow(principal, FieldClass.NegotiationPartyA, resourceContext) ? n.PartyAParticipantId : null,
                ["partyB"] = Allow(principal, FieldClass.NegotiationPartyB, resourceContext) ? n.PartyBParticipantId : null,
                ["createdAt"] = Allow(principal, FieldClass.EntityCreatedAt, resourceContext) ? n.CreatedAt : null,
                ["version"] = Allow(principal, FieldClass.EntityVersion, resourceContext) ? n.Version : null,
                ["deletedAt"] = Allow(principal, FieldClass.SoftDeletedAt, resourceContext) ? n.DeletedAt : null
            }),
            Offer o => Filter(principal, new Dictionary<string, object?>
            {
                ["id"] = o.Id,
                ["amount"] = Allow(principal, FieldClass.OfferAmount, resourceContext) ? o.Amount : null,
                ["currency"] = Allow(principal, FieldClass.OfferCurrency, resourceContext) ? o.Currency : null,
                ["terms"] = Allow(principal, FieldClass.OfferTerms, resourceContext) ? o.Terms : null,
                ["status"] = Allow(principal, FieldClass.OfferStatus, resourceContext) ? o.Status.ToString() : null,
                ["negotiationId"] = Allow(principal, FieldClass.OfferNegotiationId, resourceContext) ? o.NegotiationId : null,
                ["createdAt"] = Allow(principal, FieldClass.EntityCreatedAt, resourceContext) ? o.CreatedAt : null,
                ["version"] = Allow(principal, FieldClass.EntityVersion, resourceContext) ? o.Version : null,
                ["deletedAt"] = Allow(principal, FieldClass.SoftDeletedAt, resourceContext) ? o.DeletedAt : null
            }),
            _ => new Dictionary<string, object?> { ["id"] = target.Id }
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private bool Allow(FieldPrincipal principal, FieldClass fieldClass, FieldResourceContext resourceContext)
        => _fieldPolicy.Evaluate(principal, fieldClass, FieldAction.Read, resourceContext);

    private static Dictionary<string, object?> Filter(
        FieldPrincipal _,
        Dictionary<string, object?> fields)
    {
        return fields
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static void ApplySoftDelete(DeleteTarget target, DateTimeOffset now)
    {
        switch (target.Entity)
        {
            case Participant p:
                p.SoftDelete(now);
                break;
            case Artifact a:
                a.SoftDelete(now);
                break;
            case Negotiation n:
                n.SoftDelete(now);
                break;
            case Offer o:
                o.SoftDelete(now);
                break;
        }
    }

    private static string HashCascade(IReadOnlyList<DeleteTarget> targets)
    {
        var payload = string.Join(
            "|",
            targets.OrderBy(t => t.EntityType, StringComparer.Ordinal)
                .ThenBy(t => t.Id)
                .Select(t => $"{t.EntityType}:{t.Id:D}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string HashToken(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private string HashIp(string? remoteIp)
    {
        var ip = string.IsNullOrWhiteSpace(remoteIp) ? "0.0.0.0" : remoteIp;
        return _ipHasher.Hash(ip);
    }

    private sealed record DeleteTarget(string EntityType, Guid Id, uint Version, object Entity);

    private sealed record CascadePlan(
        string DisplayIdentity,
        uint RootVersion,
        bool? IsOpen,
        AdminDeleteCascadeCounts Counts,
        string CascadeSetHash,
        IReadOnlyList<DeleteTarget> Targets,
        bool Blocked,
        IReadOnlyList<Guid> BlockedByNegotiations);
}

public sealed class AdminDeleteResult
{
    public int StatusCode { get; }
    public object? Body { get; }

    private AdminDeleteResult(int statusCode, object? body)
    {
        StatusCode = statusCode;
        Body = body;
    }

    public static AdminDeleteResult Ok(AdminDeleteIntentResponse body)
        => new(StatusCodes.Status200OK, body);

    public static AdminDeleteResult Deleted()
        => new(StatusCodes.Status204NoContent, null);

    public static AdminDeleteResult NotFound()
        => new(StatusCodes.Status404NotFound, new { error = "Not found" });

    public static AdminDeleteResult BlindDelete()
        => new(StatusCodes.Status400BadRequest, new { error = "Bad Request" });

    public static AdminDeleteResult InvalidToken()
        => new(StatusCodes.Status400BadRequest, new { error = "Bad Request" });

    public static AdminDeleteResult PreconditionRequired()
        => new(StatusCodes.Status428PreconditionRequired, new { error = "Precondition Required" });

    public static AdminDeleteResult StaleVersion()
        => new(StatusCodes.Status409Conflict, new { error = "Conflict" });

    public static AdminDeleteResult ArtifactBlocked(IReadOnlyList<Guid> negotiationIds)
        => new(StatusCodes.Status409Conflict, new
        {
            error = "Conflict",
            blockedByNegotiations = negotiationIds
        });
}

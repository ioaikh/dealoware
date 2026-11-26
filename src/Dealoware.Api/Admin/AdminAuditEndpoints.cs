using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Domain.Admin;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Admin;

/// <summary>
/// One audit entry as the owner may read it: the read model of the audit
/// read API and of the audit viewer pages (<see cref="AdminAuditPages"/>).
/// Built only by <see cref="AdminAuditEndpoints.ToView"/>: the IP is the
/// first <see cref="AdminAuditEndpoints.IpHmacPrefixLength"/> characters of
/// its keyed hash, never the hash and never an address; the snapshots have
/// passed the secret-key strip and the field policy; the outcome comes from
/// <see cref="AdminAuditOutcome"/>.
/// </summary>
public sealed record AdminAuditView(
    Guid Id,
    DateTimeOffset Timestamp,
    string Action,
    string ActorEmail,
    string IpHmacPrefix,
    string? EntityType,
    Guid? EntityId,
    string? ReasonClass,
    string Outcome,
    Guid? CorrelationId,
    string? BeforeSnapshot,
    string? AfterSnapshot);

/// <summary>
/// CoreOwner read API for the append-only audit log. GET only — no update or delete path.
/// <list type="bullet">
/// <item>GET /admin/api/audit: one page of entries (50 by default, never
/// more than 200), filtered and sorted in SQL (<see cref="AdminAuditQuery"/>).</item>
/// <item>GET /admin/api/audit/{id}: one entry, or a plain 404.</item>
/// </list>
/// Every other method on either path is a 405.
/// </summary>
public static class AdminAuditEndpoints
{
    public const int IpHmacPrefixLength = 12;

    private const string CollectionPath = "/admin/api/audit";
    private const string EntryPath = CollectionPath + "/{id:guid}";
    private static readonly string[] MutatingMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public static void MapAdminAuditEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/api/audit", ListAudit)
            .WithName("AdminAuditList")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet(EntryPath, GetAuditEntry)
            .WithName("AdminAuditEntry")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        app.MapMethods(
                CollectionPath,
                MutatingMethods,
                () => Results.StatusCode(StatusCodes.Status405MethodNotAllowed))
            .WithName("AdminAuditMutationsDenied")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        app.MapMethods(
                EntryPath,
                MutatingMethods,
                () => Results.StatusCode(StatusCodes.Status405MethodNotAllowed))
            .WithName("AdminAuditEntryMutationsDenied")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);
    }

    private static async Task<IResult> ListAudit(
        HttpContext context,
        IAdminAuditRepository audit,
        IFieldPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
            return AdminDeny.UnauthorizedResult();

        if (!AdminAuditQuery.TryParse(context.Request.Query, out var criteria, out var error))
            return error!;

        var principal = AdminReadProjection.CoreOwnerFrom(context);
        var (items, total) = await ListAsync(audit, policy, principal, criteria, cancellationToken);
        var payload = new
        {
            items = items.Select(ToReadModel).ToArray(),
            total,
            offset = criteria.Offset,
            limit = criteria.Limit
        };
        return Results.Json(payload);
    }

    private static async Task<IResult> GetAuditEntry(
        HttpContext context,
        Guid id,
        IAdminAuditRepository audit,
        IFieldPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
            return AdminDeny.UnauthorizedResult();

        var view = await GetAsync(audit, policy, AdminReadProjection.CoreOwnerFrom(context), id, cancellationToken);
        return view is null ? Results.NotFound() : Results.Json(ToReadModel(view));
    }

    /// <summary>One page of entries as the owner may read them. The API's and the list page's one read.</summary>
    public static async Task<(IReadOnlyList<AdminAuditView> Items, int Total)> ListAsync(
        IAdminAuditRepository audit,
        IFieldPolicy policy,
        FieldPrincipal principal,
        AdminAuditListCriteria criteria,
        CancellationToken cancellationToken)
    {
        var (items, total) = await audit.ListPageAsync(criteria, cancellationToken);
        return (items.Select(entry => ToView(entry, policy, principal)).ToArray(), total);
    }

    /// <summary>One entry by its id as the owner may read it, or null. The API's and the entry page's one read.</summary>
    public static async Task<AdminAuditView?> GetAsync(
        IAdminAuditRepository audit,
        IFieldPolicy policy,
        FieldPrincipal principal,
        Guid id,
        CancellationToken cancellationToken)
    {
        var (items, _) = await audit.ListPageAsync(
            new AdminAuditListCriteria { Id = id, Offset = 0, Limit = 1 },
            cancellationToken);
        return items.Count == 0 ? null : ToView(items[0], policy, principal);
    }

    public static AdminAuditView ToView(AdminAuditEntry entry, IFieldPolicy policy, FieldPrincipal principal)
        => new(
            entry.Id,
            entry.Timestamp,
            entry.Action,
            entry.ActorEmail,
            Prefix(entry.IpHmac),
            entry.EntityType,
            entry.EntityId,
            entry.ReasonClass,
            AdminAuditOutcome.Of(entry.Action, entry.ReasonClass),
            entry.CorrelationId,
            SanitizeStored(entry.BeforeSnapshot, policy, principal),
            SanitizeStored(entry.AfterSnapshot, policy, principal));

    internal static object ToReadModel(AdminAuditView entry) => new
    {
        id = entry.Id,
        timestamp = entry.Timestamp,
        action = entry.Action,
        actorEmail = entry.ActorEmail,
        ipHmacPrefix = entry.IpHmacPrefix,
        entityType = entry.EntityType,
        entityId = entry.EntityId,
        reasonClass = entry.ReasonClass,
        outcome = entry.Outcome,
        correlationId = entry.CorrelationId,
        beforeSnapshot = entry.BeforeSnapshot,
        afterSnapshot = entry.AfterSnapshot
    };

    /// <summary>
    /// A stored snapshot as it may be read: secret-looking keys stripped
    /// again (<see cref="AdminAuditSnapshots.StripForbiddenKeys"/>), then
    /// every key that names a field the policy does not let this reader
    /// read left out (Spec §10 Access: the field policy applies inside
    /// snapshots). What cannot be read as JSON becomes the fixed placeholder.
    /// </summary>
    private static string? SanitizeStored(string? snapshot, IFieldPolicy policy, FieldPrincipal principal)
    {
        if (snapshot is null)
            return null;
        var stripped = AdminAuditSnapshots.StripForbiddenKeys(snapshot)
                       ?? AdminAuditSnapshots.FailClosedPlaceholder;
        try
        {
            var node = JsonNode.Parse(stripped, documentOptions: new JsonDocumentOptions { MaxDepth = 64 });
            return DropDeniedFields(node, policy, principal) ? node!.ToJsonString() : stripped;
        }
        catch (JsonException)
        {
            return AdminAuditSnapshots.FailClosedPlaceholder;
        }
    }

    /// <summary>
    /// Removes, at any depth, each key that is the name of a registered field
    /// class the reader may not read. True when one was removed.
    /// </summary>
    private static bool DropDeniedFields(JsonNode? node, IFieldPolicy policy, FieldPrincipal principal)
    {
        var removed = false;
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (IsDeniedField(key, policy, principal))
                    {
                        obj.Remove(key);
                        removed = true;
                    }
                    else
                    {
                        removed |= DropDeniedFields(obj[key], policy, principal);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                    removed |= DropDeniedFields(item, policy, principal);
                break;
        }

        return removed;
    }

    private static bool IsDeniedField(string key, IFieldPolicy policy, FieldPrincipal principal)
    {
        if (key.Length == 0)
            return false;
        // A snapshot names a field as its class is named, or with a small first letter.
        var named = FieldClass.Custom(char.ToUpperInvariant(key[0]) + key[1..]);
        return policy.IsRegistered(named)
               && !policy.Evaluate(principal, named, FieldAction.Read, new FieldResourceContext());
    }

    private static string Prefix(string ipHmac)
    {
        if (string.IsNullOrEmpty(ipHmac))
            return string.Empty;
        return ipHmac.Length <= IpHmacPrefixLength
            ? ipHmac
            : ipHmac[..IpHmacPrefixLength];
    }
}

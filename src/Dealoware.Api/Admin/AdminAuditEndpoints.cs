using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// CoreOwner read API for the append-only audit log. GET only — no update or delete path.
/// </summary>
public static class AdminAuditEndpoints
{
    public const int IpHmacPrefixLength = 12;

    public static void MapAdminAuditEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/api/audit", ListAudit)
            .WithName("AdminAuditList")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapMethods(
                "/admin/api/audit",
                ["POST", "PUT", "PATCH", "DELETE"],
                () => Results.StatusCode(StatusCodes.Status405MethodNotAllowed))
            .WithName("AdminAuditMutationsDenied")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);
    }

    private static async Task<IResult> ListAudit(
        HttpContext context,
        IAdminAuditRepository audit,
        CancellationToken cancellationToken)
    {
        if (!context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
            return AdminDeny.UnauthorizedResult();

        if (!AdminAuditQuery.TryParse(context.Request.Query, out var criteria, out var error))
            return error!;

        var (items, total) = await audit.ListPageAsync(criteria, cancellationToken);
        var payload = new
        {
            items = items.Select(ToReadModel).ToArray(),
            total,
            offset = criteria.Offset,
            limit = criteria.Limit
        };
        return Results.Json(payload);
    }

    internal static object ToReadModel(AdminAuditEntry entry) => new
    {
        id = entry.Id,
        timestamp = entry.Timestamp,
        action = entry.Action,
        actorEmail = entry.ActorEmail,
        ipHmacPrefix = Prefix(entry.IpHmac),
        entityType = entry.EntityType,
        entityId = entry.EntityId,
        reasonClass = entry.ReasonClass,
        correlationId = entry.CorrelationId,
        beforeSnapshot = entry.BeforeSnapshot,
        afterSnapshot = entry.AfterSnapshot
    };

    private static string Prefix(string ipHmac)
    {
        if (string.IsNullOrEmpty(ipHmac))
            return string.Empty;
        return ipHmac.Length <= IpHmacPrefixLength
            ? ipHmac
            : ipHmac[..IpHmacPrefixLength];
    }
}

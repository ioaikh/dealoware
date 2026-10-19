using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Builds audit rows with keyed HMAC-SHA256 IP. Raw IP is never stored.
/// Later auth / edit / delete PRs call this, then pair via <see cref="IAdminAuditUnitOfWork"/>.
/// </summary>
public interface IAdminAuditRecorder
{
    AdminAuditEntry Auth(
        string action,
        string actorEmail,
        string clientIp,
        string? reasonClass = null,
        DateTimeOffset? timestamp = null);

    AdminAuditEntry Entity(
        string action,
        string actorEmail,
        string clientIp,
        string entityType,
        Guid entityId,
        string? beforeSnapshot = null,
        string? afterSnapshot = null,
        Guid? correlationId = null,
        DateTimeOffset? timestamp = null);
}

public sealed class AdminAuditRecorder : IAdminAuditRecorder
{
    private readonly IIpHasher _ipHasher;

    public AdminAuditRecorder(IIpHasher ipHasher)
    {
        _ipHasher = ipHasher;
    }

    public AdminAuditEntry Auth(
        string action,
        string actorEmail,
        string clientIp,
        string? reasonClass = null,
        DateTimeOffset? timestamp = null)
        => AdminAuditEntry.CreateAuthEvent(
            action,
            actorEmail,
            _ipHasher.Hash(clientIp),
            reasonClass,
            timestamp);

    public AdminAuditEntry Entity(
        string action,
        string actorEmail,
        string clientIp,
        string entityType,
        Guid entityId,
        string? beforeSnapshot = null,
        string? afterSnapshot = null,
        Guid? correlationId = null,
        DateTimeOffset? timestamp = null)
        => AdminAuditEntry.CreateEntityEvent(
            action,
            actorEmail,
            _ipHasher.Hash(clientIp),
            entityType,
            entityId,
            AdminAuditSnapshots.StripForbiddenKeys(beforeSnapshot),
            AdminAuditSnapshots.StripForbiddenKeys(afterSnapshot),
            correlationId,
            timestamp);
}

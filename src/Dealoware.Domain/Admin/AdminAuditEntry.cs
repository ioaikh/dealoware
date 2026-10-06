namespace Dealoware.Domain.Admin;

/// <summary>
/// Append-only audit entry for admin actions.
/// Cannot be edited or deleted from admin UI/API.
/// </summary>
public sealed class AdminAuditEntry
{
    /// <summary>
    /// Unique identifier for the audit entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// When the audit event occurred (UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// The action performed (e.g., login_success, login_failure, entity_edit, entity_delete).
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// The actor email (CoreOwner email or anonymous for unauthenticated attempts).
    /// </summary>
    public string ActorEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Keyed HMAC-SHA256 of the client IP. Raw IP is never stored.
    /// </summary>
    public string IpHmac { get; private set; } = string.Empty;

    /// <summary>
    /// Entity type involved (e.g., Participant, Artifact, Negotiation, Offer).
    /// Null for auth events.
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// Entity ID involved. Null for auth events.
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// Reason class for failures (e.g., bad_password, bad_2fa, locked, rate_limited).
    /// Null for success events.
    /// </summary>
    public string? ReasonClass { get; private set; }

    /// <summary>
    /// Correlation ID for cascade operations.
    /// </summary>
    public Guid? CorrelationId { get; private set; }

    /// <summary>
    /// FieldPolicy-allowed snapshot before change (JSON). For edit/delete only.
    /// Values larger than 4 KiB are truncated.
    /// </summary>
    public string? BeforeSnapshot { get; private set; }

    /// <summary>
    /// FieldPolicy-allowed snapshot after change (JSON). For edit only.
    /// Values larger than 4 KiB are truncated.
    /// </summary>
    public string? AfterSnapshot { get; private set; }

    private AdminAuditEntry() { }

    /// <summary>
    /// Creates an audit entry for an authentication event.
    /// </summary>
    public static AdminAuditEntry CreateAuthEvent(
        string action,
        string actorEmail,
        string ipHmac,
        string? reasonClass = null,
        DateTimeOffset? timestamp = null)
    {
        return new AdminAuditEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Action = action,
            ActorEmail = actorEmail,
            IpHmac = ipHmac,
            ReasonClass = reasonClass
        };
    }

    /// <summary>
    /// Creates an audit entry for an entity operation.
    /// </summary>
    public static AdminAuditEntry CreateEntityEvent(
        string action,
        string actorEmail,
        string ipHmac,
        string entityType,
        Guid entityId,
        string? beforeSnapshot = null,
        string? afterSnapshot = null,
        Guid? correlationId = null,
        DateTimeOffset? timestamp = null)
    {
        return new AdminAuditEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Action = action,
            ActorEmail = actorEmail,
            IpHmac = ipHmac,
            EntityType = entityType,
            EntityId = entityId,
            BeforeSnapshot = TruncateSnapshot(beforeSnapshot),
            AfterSnapshot = TruncateSnapshot(afterSnapshot),
            CorrelationId = correlationId
        };
    }

    private static string? TruncateSnapshot(string? snapshot)
    {
        if (snapshot is null) return null;
        var truncated = AdminAuditSnapshots.Truncate(snapshot);
        return truncated.Length == 0 ? null : truncated;
    }
}

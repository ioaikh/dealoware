namespace Dealoware.Domain.Admin;

/// <summary>
/// Single-use delete confirm token. The raw token is never stored — only a hash.
/// Bound to actor session, entity, and the intended cascade set.
/// </summary>
public sealed class AdminDeleteConfirmToken
{
    public Guid Id { get; private set; }

    /// <summary>SHA-256 hex of the raw confirm token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>CoreOwner email that requested the intent.</summary>
    public string ActorEmail { get; private set; } = string.Empty;

    /// <summary>Admin session id the token is bound to.</summary>
    public Guid ActorSessionId { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    /// <summary>SHA-256 hex of the intended cascade set (sorted type:id rows).</summary>
    public string CascadeSetHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private AdminDeleteConfirmToken() { }

    public static AdminDeleteConfirmToken Create(
        string tokenHash,
        string actorEmail,
        Guid actorSessionId,
        string entityType,
        Guid entityId,
        string cascadeSetHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new AdminDeleteConfirmToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            ActorEmail = actorEmail,
            ActorSessionId = actorSessionId,
            EntityType = entityType,
            EntityId = entityId,
            CascadeSetHash = cascadeSetHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsConsumed => ConsumedAt is not null;

    public void MarkConsumed(DateTimeOffset now)
    {
        ConsumedAt = now;
    }
}

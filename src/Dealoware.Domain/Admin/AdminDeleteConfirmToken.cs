namespace Dealoware.Domain.Admin;

/// <summary>
/// The confirm token of one admin delete (Spec §9, SA §3.4): handed out by
/// <c>POST …/delete-intent</c> and presented, once, by the <c>DELETE</c>.
/// <para>
/// Only the token's SHA-256 is stored, never the token. A row is bound to:
/// the signed-in session that asked for it (and so to the Core owner); the
/// one record to delete (type and id); and the exact set of rows the delete
/// will mark, each with its id, version and state (<see cref="CascadeHash"/>).
/// It runs out five minutes after it was made, and is used at most once:
/// the delete sets <see cref="ConsumedAt"/> with a conditional update inside
/// its own transaction, so two confirms of one token delete once.
/// </para>
/// <para>
/// Deleted with its session (database cascade). A new intent for the same
/// record in the same session replaces the earlier tokens for it.
/// </para>
/// </summary>
public sealed class AdminDeleteConfirmToken
{
    /// <summary>Spec §9: a confirm token is good for five minutes.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public Guid Id { get; private set; }

    /// <summary>SHA-256 of the token, as 64 lowercase hex digits.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>The admin session the token was made for.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>The record's kind as the route names it: participants, artifacts, negotiations or offers.</summary>
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    /// <summary>SHA-256 of the cascade set as the intent saw it (every row, its version and state).</summary>
    public string CascadeHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When the delete used it. Null while unused.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    private AdminDeleteConfirmToken() { }

    public static AdminDeleteConfirmToken Create(
        string tokenHash,
        Guid sessionId,
        string entityType,
        Guid entityId,
        string cascadeHash,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64)
            throw new ArgumentException("The token is stored as its SHA-256 only.", nameof(tokenHash));
        if (sessionId == Guid.Empty)
            throw new ArgumentException("A confirm token belongs to a session.", nameof(sessionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        if (entityId == Guid.Empty)
            throw new ArgumentException("A confirm token names one record.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(cascadeHash) || cascadeHash.Length != 64)
            throw new ArgumentException("A confirm token names its cascade set.", nameof(cascadeHash));

        return new AdminDeleteConfirmToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            SessionId = sessionId,
            EntityType = entityType,
            EntityId = entityId,
            CascadeHash = cascadeHash,
            CreatedAt = now,
            ExpiresAt = now + Lifetime
        };
    }

    /// <summary>
    /// Unused, not run out, and made for this session and this record. The
    /// delete checks this first and then claims the row inside its
    /// transaction, so a second use is refused even when both arrive at once.
    /// </summary>
    public bool IsUsableBy(Guid sessionId, string entityType, Guid entityId, DateTimeOffset now)
        => ConsumedAt is null
           && now < ExpiresAt
           && SessionId == sessionId
           && string.Equals(EntityType, entityType, StringComparison.Ordinal)
           && EntityId == entityId;
}

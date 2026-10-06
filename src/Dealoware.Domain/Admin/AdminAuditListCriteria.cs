namespace Dealoware.Domain.Admin;

/// <summary>
/// Server-side filter/sort/page for GET /admin/api/audit.
/// Filtering, sorting, Skip/Take and the count all run in SQL.
/// </summary>
public sealed class AdminAuditListCriteria
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
    public const int MaxQLength = 100;
    public const int MaxFilterLength = 200;
    public const int MaxActionLength = 64;

    public int Offset { get; init; }
    public int Limit { get; init; } = DefaultLimit;
    public string Sort { get; init; } = "-timestamp";
    public string? Action { get; init; }
    public string? ActorEmail { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? ReasonClass { get; init; }
    public Guid? CorrelationId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Q { get; init; }
}

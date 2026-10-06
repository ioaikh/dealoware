namespace Dealoware.Api.Admin;

public sealed record AdminDeleteCascadeCounts(
    int Negotiations,
    int Offers,
    int Artifacts,
    int Participants);

public sealed record AdminDeleteIntentResponse(
    string ConfirmToken,
    string EntityType,
    Guid Id,
    string DisplayIdentity,
    uint Version,
    bool? IsOpen,
    AdminDeleteCascadeCounts Cascade,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<Guid>? BlockedByNegotiations,
    bool Blocked);

public sealed record AdminDeleteUiRow(
    Guid Id,
    string EntityType,
    string DisplayIdentity,
    uint Version,
    DateTimeOffset? DeletedAt,
    string? Status,
    bool? IsOpen);

public sealed record AdminDeleteStatsResponse(
    int Participants,
    int OpenNegotiations,
    int Offers,
    int Accepts,
    int Declines);

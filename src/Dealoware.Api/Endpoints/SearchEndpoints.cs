using Dealoware.Api.Auth;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Artifacts.Mapping;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Discovery search API endpoints (MVP Stage B #40).
/// 
/// SECURITY ARCHITECTURE:
/// This is a DISCOVERY surface, SEPARATE from owner inventory (GET /artifacts).
/// 
/// 1. Authentication REQUIRED (#5 principal): Unauthenticated → 401
/// 2. Discovery returns only DISCOVERABLE fields (D1-D5 Artifact fields already on path)
/// 3. OMITTED from search payloads per #31 IFieldPolicy + #40 AC:
///    - OwnerParticipantId (prevents inventory enumeration)
///    - LoginEmail (FieldClass: never discoverable)
///    - ContactEmail (FieldClass: no share-after-Accept yet - #42 HOLD)
///    - StrategyBody (Strategy ACL - #41 HOLD)
///    - Auth secrets (never in any DTO)
///    - Private account lists/inventory
/// 4. Query-plane filter: search predicate applied at DB level, not fetch-all-then-filter
/// 5. Uniform deny: errors do not leak private fields
/// 
/// See: Issue #40, #31 IFieldPolicy, #18 Option A Stage B
/// </summary>
public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/search");

        group.MapGet("/artifacts", SearchArtifacts)
            .WithName("SearchArtifacts")
            .Produces<SearchArtifactsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Instant search over discoverable Artifact fields.
    /// 
    /// Authentication: REQUIRED (401 if missing/invalid)
    /// Search fields: Subject entities (name/description), Intent, Locations
    /// Omitted: OwnerParticipantId, LoginEmail, ContactEmail, StrategyBody, auth secrets
    /// </summary>
    private static async Task<IResult> SearchArtifacts(
        HttpContext context,
        string? q,
        int? limit,
        IArtifactRepository artifactRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(sub))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new { error = "Query parameter 'q' is required" });
        }

        var effectiveLimit = Math.Clamp(limit ?? 50, 1, 100);

        var artifacts = await artifactRepository.SearchDiscoverableAsync(
            q, sub, effectiveLimit, cancellationToken);

        var response = new SearchArtifactsResponse
        {
            Query = q,
            Results = ArtifactMapper.ToDiscoverableResponseList(artifacts),
            TotalCount = artifacts.Count
        };

        return Results.Ok(response);
    }
}

/// <summary>
/// Response wrapper for artifact search results.
/// </summary>
public sealed record SearchArtifactsResponse
{
    /// <summary>
    /// The search query that was executed.
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Discoverable artifact results (no private fields).
    /// </summary>
    public List<DiscoverableArtifactResponse> Results { get; init; } = new();

    /// <summary>
    /// Number of results returned.
    /// </summary>
    public int TotalCount { get; init; }
}

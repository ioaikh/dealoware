using Dealoware.Api.Auth;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Artifacts.Mapping;
using Dealoware.Application.Artifacts.Validation;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Artifact API endpoints with fail-closed authentication.
/// 
/// Authentication is REQUIRED for all artifact operations:
/// - POST /artifacts - Create artifact (requires valid credential)
/// - GET /artifacts/{id} - Get artifact (requires valid credential + owner-scope)
/// - GET /artifacts - List own artifacts (requires valid credential)
/// 
/// All endpoints use the Authorization header only (no query string or body tokens).
/// The authenticated principal's sub claim maps to Artifact.OwnerParticipantId.
/// 
/// Authorization formats:
/// - Bearer {jwt_token}  - JWT Bearer token
/// - ApiKey {api_key}    - API key authentication
/// - Bearer {api_key}    - API key as bearer (also supported)
/// </summary>
public static class ArtifactEndpoints
{
    public static void MapArtifactEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/artifacts");

        group.MapPost("/", CreateArtifact)
            .WithName("CreateArtifact")
            .Produces<ArtifactResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetArtifact)
            .WithName("GetArtifact")
            .Produces<ArtifactResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", ListOwnArtifacts)
            .WithName("ListOwnArtifacts")
            .Produces<List<ArtifactResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CreateArtifact(
        HttpContext context,
        CreateArtifactRequest request,
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

        var validation = ArtifactValidator.Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", validation.Errors.ToArray() }
                });
        }

        var artifact = ArtifactMapper.ToDomain(request, sub);
        await artifactRepository.AddAsync(artifact, cancellationToken);
        await artifactRepository.SaveChangesAsync(cancellationToken);

        var response = ArtifactMapper.ToResponse(artifact);
        return Results.Created($"/artifacts/{artifact.Id}", response);
    }

    private static async Task<IResult> GetArtifact(
        HttpContext context,
        Guid id,
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

        var artifact = await artifactRepository.GetByIdAsync(id, cancellationToken);
        
        if (artifact is null || artifact.OwnerParticipantId != sub)
        {
            return Results.NotFound();
        }

        var response = ArtifactMapper.ToResponse(artifact);
        return Results.Ok(response);
    }

    private static async Task<IResult> ListOwnArtifacts(
        HttpContext context,
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

        var artifacts = await artifactRepository.GetByOwnerAsync(sub, cancellationToken);
        var response = ArtifactMapper.ToResponseList(artifacts);
        return Results.Ok(response);
    }
}

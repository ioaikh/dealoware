using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Artifacts.Mapping;
using Dealoware.Application.Artifacts.Validation;
using Dealoware.Domain.Artifacts;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

public static class ArtifactEndpoints
{
    /// <summary>
    /// Header for interim PoC principal identification.
    /// Future: When #5 auth is implemented, prefer JWT sub claim if available.
    /// The consume path will check for a validated JWT first, then fall back to this header.
    /// </summary>
    public const string OwnerIdHeader = "X-PoC-Owner-Id";

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
        IArtifactRepository repository,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(context);
        if (string.IsNullOrWhiteSpace(ownerId))
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

        var artifact = ArtifactMapper.ToDomain(request, ownerId);
        await repository.AddAsync(artifact, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var response = ArtifactMapper.ToResponse(artifact);
        return Results.Created($"/artifacts/{artifact.Id}", response);
    }

    private static async Task<IResult> GetArtifact(
        HttpContext context,
        Guid id,
        IArtifactRepository repository,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(context);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var artifact = await repository.GetByIdAsync(id, cancellationToken);
        
        if (artifact is null || artifact.OwnerParticipantId != ownerId)
        {
            return Results.NotFound();
        }

        var response = ArtifactMapper.ToResponse(artifact);
        return Results.Ok(response);
    }

    private static async Task<IResult> ListOwnArtifacts(
        HttpContext context,
        IArtifactRepository repository,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(context);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        var artifacts = await repository.GetByOwnerAsync(ownerId, cancellationToken);
        var response = ArtifactMapper.ToResponseList(artifacts);
        return Results.Ok(response);
    }

    /// <summary>
    /// Get owner ID from request headers.
    /// Future: When #5 auth is implemented, check for validated JWT sub claim first,
    /// then fall back to X-PoC-Owner-Id header. For now, only the header is used.
    /// </summary>
    private static string? GetOwnerId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(OwnerIdHeader, out var ownerIdValues))
        {
            return ownerIdValues.FirstOrDefault();
        }

        return null;
    }
}

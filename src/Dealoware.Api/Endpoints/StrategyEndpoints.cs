using Dealoware.Api.Auth;
using Dealoware.Application.Strategies.Dtos;
using Dealoware.Application.Strategies.Mapping;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Strategy CRUD endpoints with owner-scoped authorization.
/// MVP Stage B minimal CRUD (P3 partial).
/// 
/// All endpoints require valid Authorization header (Bearer JWT or ApiKey).
/// Owner-only access: OwnerParticipantId == principal sub (query-plane enforced).
/// Non-owner requests return 404 (no information leak).
/// 
/// StrategyBody FieldClass ACL:
/// - User R/W
/// - OwnAgent R/W (for owner)
/// - Counterparty Deny
/// - Stranger/Unauth Deny
/// 
/// OUT: Free-form condition engine (V1); Strategy sandbox A5 (V4);
///      Assistant runtime (Stage C/X1).
/// </summary>
public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/strategies");

        group.MapGet("/", ListStrategies)
            .WithName("ListStrategies")
            .Produces<List<StrategyResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateStrategy)
            .WithName("CreateStrategy")
            .Produces<StrategyResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetStrategy)
            .WithName("GetStrategy")
            .Produces<StrategyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateStrategy)
            .WithName("UpdateStrategy")
            .Produces<StrategyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", PatchStrategy)
            .WithName("PatchStrategy")
            .Produces<StrategyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteStrategy)
            .WithName("DeleteStrategy")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Lists all strategies owned by the authenticated participant.
    /// Owner-scoped at query plane.
    /// </summary>
    private static async Task<IResult> ListStrategies(
        HttpContext context,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var strategies = await strategyRepository.GetByOwnerAsync(sub, cancellationToken);
        var principal = FieldPrincipal.User(sub);

        var response = strategies
            .Select(s => StrategyMapper.ToResponse(s, principal, fieldPolicy))
            .ToList();

        return Results.Ok(response);
    }

    /// <summary>
    /// Creates a new strategy for the authenticated participant.
    /// </summary>
    private static async Task<IResult> CreateStrategy(
        HttpContext context,
        CreateStrategyRequest request,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var (strategy, errors) = Strategy.Create(sub, request.Name, request.StrategyBody);

        if (errors.Count > 0)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", errors.ToArray() }
                });

        await strategyRepository.AddAsync(strategy!, cancellationToken);
        await strategyRepository.SaveChangesAsync(cancellationToken);

        var principal = FieldPrincipal.User(sub);
        var response = StrategyMapper.ToResponse(strategy!, principal, fieldPolicy);

        return Results.Created($"/strategies/{strategy!.Id}", response);
    }

    /// <summary>
    /// Gets a strategy by ID if the authenticated participant is the owner.
    /// Returns 404 if not found OR if the caller is not the owner (no info leak).
    /// </summary>
    private static async Task<IResult> GetStrategy(
        HttpContext context,
        Guid id,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var strategy = await strategyRepository.GetByIdForOwnerAsync(id, sub, cancellationToken);
        if (strategy is null)
            return Results.NotFound();

        var principal = FieldPrincipal.User(sub);
        var response = StrategyMapper.ToResponse(strategy, principal, fieldPolicy);

        return Results.Ok(response);
    }

    /// <summary>
    /// Updates a strategy (full update).
    /// Only the owner can update their strategy.
    /// </summary>
    private static async Task<IResult> UpdateStrategy(
        HttpContext context,
        Guid id,
        UpdateStrategyRequest request,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var strategy = await strategyRepository.GetByIdForOwnerAsync(id, sub, cancellationToken);
        if (strategy is null)
            return Results.NotFound();

        strategy.UpdateName(request.Name);
        strategy.UpdateStrategyBody(request.StrategyBody);

        await strategyRepository.SaveChangesAsync(cancellationToken);

        var principal = FieldPrincipal.User(sub);
        var response = StrategyMapper.ToResponse(strategy, principal, fieldPolicy);

        return Results.Ok(response);
    }

    /// <summary>
    /// Patches a strategy (partial update).
    /// Only the owner can update their strategy.
    /// Only non-null fields in the request are updated.
    /// </summary>
    private static async Task<IResult> PatchStrategy(
        HttpContext context,
        Guid id,
        UpdateStrategyRequest request,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var strategy = await strategyRepository.GetByIdForOwnerAsync(id, sub, cancellationToken);
        if (strategy is null)
            return Results.NotFound();

        if (request.Name is not null)
            strategy.UpdateName(request.Name);

        if (request.StrategyBody is not null)
            strategy.UpdateStrategyBody(request.StrategyBody);

        await strategyRepository.SaveChangesAsync(cancellationToken);

        var principal = FieldPrincipal.User(sub);
        var response = StrategyMapper.ToResponse(strategy, principal, fieldPolicy);

        return Results.Ok(response);
    }

    /// <summary>
    /// Deletes (deactivates) a strategy.
    /// Only the owner can delete their strategy.
    /// </summary>
    private static async Task<IResult> DeleteStrategy(
        HttpContext context,
        Guid id,
        IStrategyRepository strategyRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var strategy = await strategyRepository.GetByIdForOwnerAsync(id, sub, cancellationToken);
        if (strategy is null)
            return Results.NotFound();

        strategy.Deactivate();
        await strategyRepository.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

using Dealoware.Api.Auth;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Negotiations.Mapping;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Negotiation API endpoints with fail-closed authentication.
/// 
/// All endpoints require valid Authorization header (Bearer JWT or ApiKey).
/// Party-only access: non-party requests return 404 (no information leak).
/// 
/// D7-D10 Implementation:
/// - D7: 1:1 Negotiation creation with complementary intents
/// - D8: Place offers (via /negotiations/{id}/offers)
/// - D9: Close negotiation (cancels all open offers)
/// - D10: Expiration check on mutating operations
/// </summary>
public static class NegotiationEndpoints
{
    public static void MapNegotiationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/negotiations");

        group.MapGet("/", ListNegotiations)
            .WithName("ListNegotiations")
            .Produces<List<NegotiationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateNegotiation)
            .WithName("CreateNegotiation")
            .Produces<NegotiationResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetNegotiation)
            .WithName("GetNegotiation")
            .Produces<NegotiationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/offers", PlaceOffer)
            .WithName("PlaceOffer")
            .Produces<OfferResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/close", CloseNegotiation)
            .WithName("CloseNegotiation")
            .Produces<NegotiationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListNegotiations(
        HttpContext context,
        INegotiationRepository negotiationRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var negotiations = await negotiationRepository.GetByParticipantAsync(sub, cancellationToken);
        
        foreach (var negotiation in negotiations)
        {
            negotiation.CheckAndApplyExpiration();
        }
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var response = negotiations.Select(n => NegotiationMapper.ToResponse(n, includeOffers: false)).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateNegotiation(
        HttpContext context,
        CreateNegotiationRequest request,
        INegotiationRepository negotiationRepository,
        IArtifactRepository artifactRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var artifact = await artifactRepository.GetByIdAsync(request.ArtifactId, cancellationToken);
        if (artifact is null)
            return Results.NotFound(new { error = "Artifact not found" });

        if (request.CounterpartyParticipantId == sub)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "CounterpartyParticipantId", new[] { "Cannot negotiate with yourself" } }
                });

        var counterparty = await participantRepository.GetBySubAsync(request.CounterpartyParticipantId, cancellationToken);
        if (counterparty is null || !counterparty.IsActive)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "CounterpartyParticipantId", new[] { "Counterparty participant not found or inactive" } }
                });

        var (negotiation, errors) = Negotiation.Create(
            request.ArtifactId,
            sub,
            request.CounterpartyParticipantId,
            request.CallerIntent,
            request.CounterpartyIntent,
            request.StartsAt,
            request.EndsAt);

        if (errors.Count > 0)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", errors.ToArray() }
                });

        await negotiationRepository.AddAsync(negotiation!, cancellationToken);
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToResponse(negotiation!, includeOffers: false);
        return Results.Created($"/negotiations/{negotiation!.Id}", response);
    }

    private static async Task<IResult> GetNegotiation(
        HttpContext context,
        Guid id,
        INegotiationRepository negotiationRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        negotiation.CheckAndApplyExpiration();
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToResponse(negotiation, includeOffers: true);
        return Results.Ok(response);
    }

    private static async Task<IResult> PlaceOffer(
        HttpContext context,
        Guid id,
        CreateOfferRequest request,
        INegotiationRepository negotiationRepository,
        IOfferRepository offerRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        if (negotiation.CheckAndApplyExpiration())
        {
            await negotiationRepository.SaveChangesAsync(cancellationToken);
            return Results.Conflict(new { error = "Negotiation has expired" });
        }

        if (negotiation.Status != NegotiationStatus.Open)
            return Results.Conflict(new { error = $"Negotiation is {negotiation.Status}, cannot place offer" });

        if (negotiation.HasOpenOfferFrom(sub))
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", new[] { "You already have an open offer in this negotiation" } }
                });

        var toParticipantId = negotiation.GetOtherParty(sub)!;

        var (offer, errors) = Offer.Create(
            negotiation.Id,
            sub,
            toParticipantId,
            request.Amount,
            request.Currency,
            request.Terms);

        if (errors.Count > 0)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", errors.ToArray() }
                });

        await offerRepository.AddAsync(offer!, cancellationToken);
        await offerRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToOfferResponse(offer!);
        return Results.Created($"/offers/{offer!.Id}", response);
    }

    private static async Task<IResult> CloseNegotiation(
        HttpContext context,
        Guid id,
        INegotiationRepository negotiationRepository,
        IOfferRepository offerRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        if (negotiation.CheckAndApplyExpiration())
        {
            await negotiationRepository.SaveChangesAsync(cancellationToken);
            return Results.Conflict(new { error = "Negotiation has expired" });
        }

        if (!negotiation.Close())
            return Results.Conflict(new { error = $"Cannot close negotiation in status {negotiation.Status}" });

        var openOffers = await offerRepository.GetOpenByNegotiationAsync(negotiation.Id, cancellationToken);
        foreach (var offer in openOffers)
        {
            offer.Cancel();
        }
        offerRepository.UpdateRange(openOffers);

        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToResponse(negotiation, includeOffers: true);
        return Results.Ok(response);
    }
}

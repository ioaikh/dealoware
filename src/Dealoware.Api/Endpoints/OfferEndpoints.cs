using Dealoware.Api.Auth;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Negotiations.Mapping;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Offer API endpoints with fail-closed authentication.
/// 
/// All endpoints require valid Authorization header (Bearer JWT or ApiKey).
/// Party-only access: non-party requests return 404 (no information leak).
/// 
/// D8 Implementation:
/// - Accept: Offer → Accepted, other Open offers → Cancelled
/// - Decline: Offer → Declined
/// - Counter: Prior → Superseded, new Offer created from counterparty
/// 
/// Caller must be the Offer's ToParticipantId (recipient) for accept/decline/counter.
/// NO contact/PII in any response.
/// </summary>
public static class OfferEndpoints
{
    public static void MapOfferEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/offers");

        group.MapGet("/", ListOffers)
            .WithName("ListOffers")
            .Produces<List<OfferResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetOffer)
            .WithName("GetOffer")
            .Produces<OfferResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/accept", AcceptOffer)
            .WithName("AcceptOffer")
            .Produces<AcceptOfferResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/decline", DeclineOffer)
            .WithName("DeclineOffer")
            .Produces<OfferResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/counter", CounterOffer)
            .WithName("CounterOffer")
            .Produces<OfferResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListOffers(
        HttpContext context,
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

        var offers = await offerRepository.GetByParticipantAsync(sub, cancellationToken);
        var response = offers.Select(NegotiationMapper.ToOfferResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetOffer(
        HttpContext context,
        Guid id,
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

        var offer = await offerRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (offer is null)
            return Results.NotFound();

        var response = NegotiationMapper.ToOfferResponse(offer);
        return Results.Ok(response);
    }

    /// <summary>
    /// Accepts an offer and creates Accept grants for contact sharing.
    /// Stage B (#42): Contact on accept - counterparty ContactEmail revealed.
    /// </summary>
    private static async Task<IResult> AcceptOffer(
        HttpContext context,
        Guid id,
        IOfferRepository offerRepository,
        INegotiationRepository negotiationRepository,
        IAcceptGrantRepository acceptGrantRepository,
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

        var offer = await offerRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (offer is null)
            return Results.NotFound();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(offer.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        if (offer.ToParticipantId != sub)
            return Results.NotFound();

        if (negotiation.CheckAndApplyExpiration())
        {
            await negotiationRepository.SaveChangesAsync(cancellationToken);
            return Results.Conflict(new { error = "Negotiation has expired" });
        }

        if (negotiation.Status != NegotiationStatus.Open)
            return Results.Conflict(new { error = $"Negotiation is {negotiation.Status}, cannot accept offer" });

        if (offer.Status != OfferStatus.Open)
            return Results.Conflict(new { error = $"Offer is {offer.Status}, cannot accept" });

        if (!offer.Accept(sub))
            return Results.Conflict(new { error = "Failed to accept offer" });

        var openOffers = await offerRepository.GetOpenByNegotiationAsync(negotiation.Id, cancellationToken);
        foreach (var otherOffer in openOffers.Where(o => o.Id != offer.Id))
        {
            otherOffer.Cancel();
        }
        offerRepository.UpdateRange(openOffers);
        offerRepository.Update(offer);

        var (acceptorToOfferor, offerorToAcceptor) = AcceptGrant.CreatePair(
            offer.Id,
            negotiation.Id,
            sub,
            offer.FromParticipantId);
        
        await acceptGrantRepository.AddRangeAsync(new[] { acceptorToOfferor, offerorToAcceptor }, cancellationToken);

        await offerRepository.SaveChangesAsync(cancellationToken);

        var counterparty = await participantRepository.GetBySubAsync(offer.FromParticipantId, cancellationToken);
        if (counterparty is null)
        {
            var fallbackResponse = NegotiationMapper.ToOfferResponse(offer);
            return Results.Ok(fallbackResponse);
        }

        var response = NegotiationMapper.ToAcceptOfferResponse(
            offer, 
            counterparty, 
            fieldPolicy, 
            hasAcceptGrant: true);
        
        return Results.Ok(response);
    }

    private static async Task<IResult> DeclineOffer(
        HttpContext context,
        Guid id,
        IOfferRepository offerRepository,
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

        var offer = await offerRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (offer is null)
            return Results.NotFound();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(offer.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        if (offer.ToParticipantId != sub)
            return Results.NotFound();

        if (negotiation.CheckAndApplyExpiration())
        {
            await negotiationRepository.SaveChangesAsync(cancellationToken);
            return Results.Conflict(new { error = "Negotiation has expired" });
        }

        if (negotiation.Status != NegotiationStatus.Open)
            return Results.Conflict(new { error = $"Negotiation is {negotiation.Status}, cannot decline offer" });

        if (offer.Status != OfferStatus.Open)
            return Results.Conflict(new { error = $"Offer is {offer.Status}, cannot decline" });

        if (!offer.Decline(sub))
            return Results.Conflict(new { error = "Failed to decline offer" });

        offerRepository.Update(offer);
        await offerRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToOfferResponse(offer);
        return Results.Ok(response);
    }

    private static async Task<IResult> CounterOffer(
        HttpContext context,
        Guid id,
        CounterOfferRequest request,
        IOfferRepository offerRepository,
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

        var offer = await offerRepository.GetByIdForPartyAsync(id, sub, cancellationToken);
        if (offer is null)
            return Results.NotFound();

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(offer.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound();

        if (offer.ToParticipantId != sub)
            return Results.NotFound();

        if (negotiation.CheckAndApplyExpiration())
        {
            await negotiationRepository.SaveChangesAsync(cancellationToken);
            return Results.Conflict(new { error = "Negotiation has expired" });
        }

        if (negotiation.Status != NegotiationStatus.Open)
            return Results.Conflict(new { error = $"Negotiation is {negotiation.Status}, cannot counter offer" });

        if (offer.Status != OfferStatus.Open)
            return Results.Conflict(new { error = $"Offer is {offer.Status}, cannot counter" });

        var existingOpenOffer = (await offerRepository.GetOpenByNegotiationAsync(negotiation.Id, cancellationToken))
            .FirstOrDefault(o => o.FromParticipantId == sub && o.Id != id);
        if (existingOpenOffer is not null)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", new[] { "You already have an open offer. Cancel or have it declined/countered first." } }
                });

        var (counterOffer, errors) = Offer.Create(
            negotiation.Id,
            sub,
            offer.FromParticipantId,
            request.Amount,
            request.Currency,
            request.Terms);

        if (errors.Count > 0)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "errors", errors.ToArray() }
                });

        if (!offer.Supersede())
            return Results.Conflict(new { error = "Failed to supersede original offer" });

        offerRepository.Update(offer);
        await offerRepository.AddAsync(counterOffer!, cancellationToken);
        await offerRepository.SaveChangesAsync(cancellationToken);

        var response = NegotiationMapper.ToOfferResponse(counterOffer!);
        return Results.Created($"/offers/{counterOffer!.Id}", response);
    }
}

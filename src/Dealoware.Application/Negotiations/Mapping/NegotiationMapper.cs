using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Domain.Negotiations;

namespace Dealoware.Application.Negotiations.Mapping;

/// <summary>
/// Maps between Negotiation domain entities and DTOs.
/// </summary>
public static class NegotiationMapper
{
    public static NegotiationResponse ToResponse(Negotiation negotiation, bool includeOffers = true)
    {
        var response = new NegotiationResponse
        {
            Id = negotiation.Id,
            ArtifactId = negotiation.ArtifactId,
            PartyAParticipantId = negotiation.PartyAParticipantId,
            PartyBParticipantId = negotiation.PartyBParticipantId,
            PartyAIntent = negotiation.PartyAIntent,
            PartyBIntent = negotiation.PartyBIntent,
            Status = negotiation.Status.ToString(),
            StartsAt = negotiation.StartsAt,
            EndsAt = negotiation.EndsAt,
            CreatedAt = negotiation.CreatedAt
        };

        if (includeOffers && negotiation.Offers.Count > 0)
        {
            response.Offers = negotiation.Offers
                .OrderByDescending(o => o.CreatedAt)
                .Select(ToOfferResponse)
                .ToList();
        }

        return response;
    }

    public static OfferResponse ToOfferResponse(Offer offer)
    {
        return new OfferResponse
        {
            Id = offer.Id,
            NegotiationId = offer.NegotiationId,
            FromParticipantId = offer.FromParticipantId,
            ToParticipantId = offer.ToParticipantId,
            Status = offer.Status.ToString(),
            Amount = offer.Amount,
            Currency = offer.Currency,
            Terms = offer.Terms,
            CreatedAt = offer.CreatedAt
        };
    }
}

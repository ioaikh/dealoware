using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Application.Negotiations.Mapping;

/// <summary>
/// Maps between Negotiation domain entities and DTOs.
/// Stage B (#42): Includes AcceptOfferResponse with counterparty contact.
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
            CreatedAt = negotiation.CreatedAt,
            IdentitySealed = true
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
            CreatedAt = offer.CreatedAt,
            IdentitySealed = true
        };
    }
    
    /// <summary>
    /// Maps an accepted Offer to AcceptOfferResponse with counterparty contact.
    /// Stage B (#42): ShareOutbound(ContactEmail) enabled by Accept grant.
    /// LoginEmail is NEVER included.
    /// </summary>
    /// <param name="offer">The accepted offer</param>
    /// <param name="counterparty">The counterparty participant (offer sender)</param>
    /// <param name="fieldPolicy">Field policy for ACL checks</param>
    /// <param name="hasAcceptGrant">Whether Accept grant is active</param>
    public static AcceptOfferResponse ToAcceptOfferResponse(
        Offer offer,
        Participant counterparty,
        IFieldPolicy fieldPolicy,
        bool hasAcceptGrant)
    {
        var resourceContext = hasAcceptGrant
            ? FieldResourceContext.ForAcceptedNegotiation(counterparty.Sub, offer.ToParticipantId)
            : FieldResourceContext.ForNegotiation(counterparty.Sub, offer.ToParticipantId);
        
        var principal = FieldPrincipal.User(offer.ToParticipantId);
        
        var canShareContactEmail = fieldPolicy.Evaluate(
            principal,
            FieldClass.ContactEmail,
            FieldAction.ShareOutbound,
            resourceContext);
        
        var canReadDisplayName = fieldPolicy.Evaluate(
            principal,
            FieldClass.DisplayName,
            FieldAction.Read,
            resourceContext);

        return new AcceptOfferResponse
        {
            Id = offer.Id,
            NegotiationId = offer.NegotiationId,
            FromParticipantId = offer.FromParticipantId,
            ToParticipantId = offer.ToParticipantId,
            Status = offer.Status.ToString(),
            Amount = offer.Amount,
            Currency = offer.Currency,
            Terms = offer.Terms,
            CreatedAt = offer.CreatedAt,
            IdentitySealed = !canShareContactEmail,
            CounterpartyContactEmail = canShareContactEmail ? counterparty.ContactEmail : null,
            CounterpartyDisplayName = canReadDisplayName ? counterparty.DisplayName : null,
            IncludesContactEmail = canShareContactEmail
        };
    }
}

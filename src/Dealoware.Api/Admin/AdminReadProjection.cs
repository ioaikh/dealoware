using System.Security.Claims;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Admin;

/// <summary>
/// FieldPolicy-only projections for CoreOwner admin reads.
/// Denied fields are omitted (not nulled-with-name).
/// </summary>
public static class AdminReadProjection
{
    public static FieldPrincipal CoreOwnerFrom(HttpContext context)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue(ClaimTypes.Name)
                    ?? string.Empty;
        return FieldPrincipal.CoreOwner(email);
    }

    public static bool Can(
        IFieldPolicy policy,
        FieldPrincipal principal,
        FieldClass fieldClass,
        FieldAction action,
        FieldResourceContext ctx)
        => policy.Evaluate(principal, fieldClass, action, ctx);

    public static Dictionary<string, object?> Participant(
        Participant participant,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail)
    {
        var ctx = FieldResourceContext.ForSelfProfile(participant.Sub);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = participant.Id,
            ["sub"] = participant.Sub
        };

        if (Can(policy, principal, FieldClass.DisplayName, action, ctx))
            dto["displayName"] = participant.DisplayName;
        if (Can(policy, principal, FieldClass.LoginEmail, action, ctx))
            dto["loginEmail"] = participant.LoginEmail;
        if (Can(policy, principal, FieldClass.ContactEmail, action, ctx))
            dto["contactEmail"] = participant.ContactEmail;
        if (Can(policy, principal, FieldClass.ParticipantActive, action, ctx))
            dto["isActive"] = participant.IsActive;
        if (Can(policy, principal, FieldClass.EntityCreatedAt, action, ctx))
        {
            dto["createdAt"] = participant.CreatedAt;
            dto["updatedAt"] = participant.UpdatedAt;
        }
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = participant.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = participant.Version;

        return dto;
    }

    public static Dictionary<string, object?> Artifact(
        Artifact artifact,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail)
    {
        var ctx = FieldResourceContext.ForSelfProfile(artifact.OwnerParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = artifact.Id
        };

        if (Can(policy, principal, FieldClass.ArtifactOwnerParticipantId, action, ctx))
            dto["ownerParticipantId"] = artifact.OwnerParticipantId;
        if (Can(policy, principal, FieldClass.ArtifactName, action, ctx))
        {
            dto["name"] = artifact.Entities.FirstOrDefault()?.Name;
            dto["intent"] = artifact.Intent;
            if (detail)
            {
                dto["entities"] = artifact.Entities.Select(e =>
                {
                    var entity = new Dictionary<string, object?> { ["name"] = e.Name };
                    if (Can(policy, principal, FieldClass.ArtifactDescription, FieldAction.Read, ctx))
                        entity["description"] = e.Description;
                    return entity;
                }).ToList();
            }
        }
        else if (detail && Can(policy, principal, FieldClass.ArtifactDescription, FieldAction.Read, ctx))
        {
            dto["entities"] = artifact.Entities.Select(e => new Dictionary<string, object?>
            {
                ["description"] = e.Description
            }).ToList();
        }

        if (Can(policy, principal, FieldClass.EntityCreatedAt, action, ctx))
        {
            dto["createdAt"] = artifact.CreatedAt;
            dto["updatedAt"] = artifact.UpdatedAt;
        }
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = artifact.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = artifact.Version;

        return dto;
    }

    public static Dictionary<string, object?> Negotiation(
        Negotiation negotiation,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail)
    {
        var ctx = FieldResourceContext.ForNegotiation(
            negotiation.PartyAParticipantId,
            negotiation.PartyBParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = negotiation.Id
        };

        if (Can(policy, principal, FieldClass.NegotiationArtifactId, action, ctx))
            dto["artifactId"] = negotiation.ArtifactId;
        if (Can(policy, principal, FieldClass.NegotiationPartyA, action, ctx))
            dto["partyAParticipantId"] = negotiation.PartyAParticipantId;
        if (Can(policy, principal, FieldClass.NegotiationPartyB, action, ctx))
            dto["partyBParticipantId"] = negotiation.PartyBParticipantId;
        if (Can(policy, principal, FieldClass.NegotiationStatus, action, ctx))
            dto["status"] = negotiation.Status.ToString();
        if (Can(policy, principal, FieldClass.NegotiationEndsAt, action, ctx))
            dto["endsAt"] = negotiation.EndsAt;
        if (Can(policy, principal, FieldClass.EntityCreatedAt, action, ctx))
        {
            dto["createdAt"] = negotiation.CreatedAt;
            dto["updatedAt"] = negotiation.UpdatedAt;
        }
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = negotiation.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = negotiation.Version;

        return dto;
    }

    public static Dictionary<string, object?> Offer(
        Offer offer,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail)
    {
        var ctx = FieldResourceContext.ForNegotiation(offer.FromParticipantId, offer.ToParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = offer.Id,
            ["fromParticipantId"] = offer.FromParticipantId,
            ["toParticipantId"] = offer.ToParticipantId
        };

        if (Can(policy, principal, FieldClass.OfferNegotiationId, action, ctx))
            dto["negotiationId"] = offer.NegotiationId;
        if (Can(policy, principal, FieldClass.OfferStatus, action, ctx))
            dto["status"] = offer.Status.ToString();
        if (Can(policy, principal, FieldClass.OfferAmount, action, ctx))
            dto["amount"] = offer.Amount;
        if (Can(policy, principal, FieldClass.OfferCurrency, action, ctx))
            dto["currency"] = offer.Currency;
        if (Can(policy, principal, FieldClass.OfferTerms, action, ctx))
            dto["terms"] = offer.Terms;
        if (Can(policy, principal, FieldClass.EntityCreatedAt, action, ctx))
        {
            dto["createdAt"] = offer.CreatedAt;
            dto["updatedAt"] = offer.UpdatedAt;
        }
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = offer.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = offer.Version;

        return dto;
    }

    public static object ListPage(int offset, int limit, int total, IEnumerable<object> items)
        => new { offset, limit, total, items };
}

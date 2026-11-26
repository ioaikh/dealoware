using System.Security.Claims;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Admin;

/// <summary>
/// The rows a page of negotiations, offers or artifacts refers to, read once
/// for the page (never more than the page's own rows name), so that a row can
/// carry names, not bare ids. Each name still passes the field policy in the
/// projection that uses it. <see cref="IncludeDeleted"/> is the read's "Show
/// deleted" toggle: without it, a soft-deleted participant or artifact lends
/// no name, only the mark that it is deleted.
/// </summary>
public sealed record AdminRelatedRows(
    IReadOnlyDictionary<string, Participant> ParticipantsBySub,
    IReadOnlyDictionary<Guid, Artifact> ArtifactsById,
    IReadOnlyDictionary<Guid, Negotiation> NegotiationsById,
    bool IncludeDeleted = false)
{
    public static readonly AdminRelatedRows None = new(
        new Dictionary<string, Participant>(StringComparer.Ordinal),
        new Dictionary<Guid, Artifact>(),
        new Dictionary<Guid, Negotiation>());

    public Participant? Participant(string sub)
        => ParticipantsBySub.TryGetValue(sub, out var p) ? p : null;

    public Artifact? Artifact(Guid id)
        => ArtifactsById.TryGetValue(id, out var a) ? a : null;

    public Negotiation? Negotiation(Guid id)
        => NegotiationsById.TryGetValue(id, out var n) ? n : null;
}

/// <summary>
/// FieldPolicy-only projections for CoreOwner admin reads.
/// Denied fields are omitted (not nulled-with-name).
/// <para>
/// T04: rows carry <c>updatedAt</c> (under the same field class as
/// <c>createdAt</c>: the policy has one class for an entity's timestamps),
/// and the names of what they refer to: an artifact's owner, a negotiation's
/// artifact and two parties, an offer's artifact (and its id) and two
/// participants. A name is added only when the policy allows both the
/// reference and the name. A participant row in a list carries neither email
/// address; the detail does (as the policy allows).
/// </para>
/// <para>
/// A referenced participant, artifact or negotiation that is soft-deleted is
/// marked (<c>ownerDeleted</c>, <c>partyADeleted</c>, <c>partyBDeleted</c>,
/// <c>fromParticipantDeleted</c>, <c>toParticipantDeleted</c>,
/// <c>artifactDeleted</c>, <c>negotiationDeleted</c>: true, as the policy
/// allows the soft-delete marker). Its name is left out unless the read has
/// the "Show deleted" toggle on: a deleted record's name shows only where the
/// deleted record itself would.
/// </para>
/// <para>
/// T05 (the edit page needs them; detail only): a negotiation's
/// <c>startsAt</c>, under the same field class as <c>endsAt</c> (the policy
/// has one class for the negotiation's time window), and its
/// <c>openOfferCount</c>, under the offer status class; the <c>id</c> of each
/// of an artifact's subject entities, so a write can say which one it renames.
/// </para>
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

    /// <summary>An artifact's name: the name of its first subject entity.</summary>
    public static string? ArtifactNameOf(Artifact artifact) => artifact.Entities.FirstOrDefault()?.Name;

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
        // The two email addresses are on the detail only, never in a list row.
        if (detail && Can(policy, principal, FieldClass.LoginEmail, action, ctx))
            dto["loginEmail"] = participant.LoginEmail;
        if (detail && Can(policy, principal, FieldClass.ContactEmail, action, ctx))
            dto["contactEmail"] = participant.ContactEmail;
        if (Can(policy, principal, FieldClass.ParticipantActive, action, ctx))
            dto["isActive"] = participant.IsActive;
        AddTimes(dto, participant.CreatedAt, participant.UpdatedAt, policy, principal, action, ctx);
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
        bool detail,
        AdminRelatedRows? related = null)
    {
        related ??= AdminRelatedRows.None;
        var ctx = FieldResourceContext.ForSelfProfile(artifact.OwnerParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = artifact.Id
        };

        if (Can(policy, principal, FieldClass.ArtifactOwnerParticipantId, action, ctx))
        {
            dto["ownerParticipantId"] = artifact.OwnerParticipantId;
            AddParticipantName(dto, "ownerName", artifact.OwnerParticipantId, related, policy, principal, action);
        }

        if (Can(policy, principal, FieldClass.ArtifactName, action, ctx))
        {
            dto["name"] = ArtifactNameOf(artifact);
            dto["intent"] = artifact.Intent;
            if (detail)
            {
                dto["entities"] = artifact.Entities.Select(e =>
                {
                    var entity = new Dictionary<string, object?> { ["id"] = e.Id, ["name"] = e.Name };
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
                ["id"] = e.Id,
                ["description"] = e.Description
            }).ToList();
        }

        AddTimes(dto, artifact.CreatedAt, artifact.UpdatedAt, policy, principal, action, ctx);
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = artifact.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = artifact.Version;

        return dto;
    }

    /// <param name="openOfferCount">
    /// Detail only: how many of the negotiation's offers are open and not
    /// soft-deleted.
    /// </param>
    public static Dictionary<string, object?> Negotiation(
        Negotiation negotiation,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail,
        AdminRelatedRows? related = null,
        int? openOfferCount = null)
    {
        related ??= AdminRelatedRows.None;
        var ctx = FieldResourceContext.ForNegotiation(
            negotiation.PartyAParticipantId,
            negotiation.PartyBParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = negotiation.Id
        };

        if (Can(policy, principal, FieldClass.NegotiationArtifactId, action, ctx))
        {
            dto["artifactId"] = negotiation.ArtifactId;
            AddArtifactName(dto, negotiation.ArtifactId, related, policy, principal, action);
        }

        if (Can(policy, principal, FieldClass.NegotiationPartyA, action, ctx))
        {
            dto["partyAParticipantId"] = negotiation.PartyAParticipantId;
            AddParticipantName(dto, "partyAName", negotiation.PartyAParticipantId, related, policy, principal, action);
        }

        if (Can(policy, principal, FieldClass.NegotiationPartyB, action, ctx))
        {
            dto["partyBParticipantId"] = negotiation.PartyBParticipantId;
            AddParticipantName(dto, "partyBName", negotiation.PartyBParticipantId, related, policy, principal, action);
        }

        if (Can(policy, principal, FieldClass.NegotiationStatus, action, ctx))
            dto["status"] = negotiation.Status.ToString();
        if (Can(policy, principal, FieldClass.NegotiationEndsAt, action, ctx))
        {
            dto["endsAt"] = negotiation.EndsAt;
            if (detail)
                dto["startsAt"] = negotiation.StartsAt;
        }

        AddTimes(dto, negotiation.CreatedAt, negotiation.UpdatedAt, policy, principal, action, ctx);
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = negotiation.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = negotiation.Version;
        if (detail && openOfferCount.HasValue && Can(policy, principal, FieldClass.OfferStatus, FieldAction.Read, ctx))
            dto["openOfferCount"] = openOfferCount.Value;

        return dto;
    }

    public static Dictionary<string, object?> Offer(
        Offer offer,
        IFieldPolicy policy,
        FieldPrincipal principal,
        bool detail,
        AdminRelatedRows? related = null)
    {
        related ??= AdminRelatedRows.None;
        var ctx = FieldResourceContext.ForNegotiation(offer.FromParticipantId, offer.ToParticipantId);
        var action = detail ? FieldAction.Read : FieldAction.List;
        var dto = new Dictionary<string, object?>
        {
            ["id"] = offer.Id,
            ["fromParticipantId"] = offer.FromParticipantId,
            ["toParticipantId"] = offer.ToParticipantId
        };

        AddParticipantName(dto, "fromParticipantName", offer.FromParticipantId, related, policy, principal, action);
        AddParticipantName(dto, "toParticipantName", offer.ToParticipantId, related, policy, principal, action);

        if (Can(policy, principal, FieldClass.OfferNegotiationId, action, ctx))
        {
            dto["negotiationId"] = offer.NegotiationId;
            if (related.Negotiation(offer.NegotiationId) is { } negotiation)
            {
                var negotiationCtx = FieldResourceContext.ForNegotiation(negotiation.PartyAParticipantId, negotiation.PartyBParticipantId);
                if (negotiation.DeletedAt is not null
                    && Can(policy, principal, FieldClass.SoftDeletedAt, action, negotiationCtx))
                {
                    dto["negotiationDeleted"] = true;
                }

                // The artifact is the negotiation's: both references must be allowed.
                if (Can(policy, principal, FieldClass.NegotiationArtifactId, action, negotiationCtx))
                {
                    dto["artifactId"] = negotiation.ArtifactId;
                    AddArtifactName(dto, negotiation.ArtifactId, related, policy, principal, action);
                }
            }
        }

        if (Can(policy, principal, FieldClass.OfferStatus, action, ctx))
            dto["status"] = offer.Status.ToString();
        if (Can(policy, principal, FieldClass.OfferAmount, action, ctx))
            dto["amount"] = offer.Amount;
        if (Can(policy, principal, FieldClass.OfferCurrency, action, ctx))
            dto["currency"] = offer.Currency;
        if (Can(policy, principal, FieldClass.OfferTerms, action, ctx))
            dto["terms"] = offer.Terms;
        AddTimes(dto, offer.CreatedAt, offer.UpdatedAt, policy, principal, action, ctx);
        if (Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["deletedAt"] = offer.DeletedAt;
        if (detail && Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, ctx))
            dto["version"] = offer.Version;

        return dto;
    }

    public static object ListPage(int offset, int limit, int total, IEnumerable<object> items)
        => new { offset, limit, total, items };

    /// <summary>
    /// Whether a detail read may carry the row's edit token
    /// (<see cref="AdminEditToken"/>, sent as <c>ETag</c>): only where the
    /// policy lets the reader see the row's version, which the token includes.
    /// </summary>
    public static bool CanReadEditToken(IFieldPolicy policy, FieldPrincipal principal)
        => Can(policy, principal, FieldClass.EntityVersion, FieldAction.Read, FieldResourceContext.ForSelfProfile(string.Empty));

    private static void AddTimes(
        Dictionary<string, object?> dto,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        IFieldPolicy policy,
        FieldPrincipal principal,
        FieldAction action,
        FieldResourceContext ctx)
    {
        if (!Can(policy, principal, FieldClass.EntityCreatedAt, action, ctx))
            return;
        dto["createdAt"] = createdAt;
        dto["updatedAt"] = updatedAt;
    }

    /// <summary>
    /// A participant's display name under <paramref name="key"/> ("…Name"), if
    /// the policy allows it; null when the row has none. A soft-deleted one is
    /// marked under "…Deleted", and gives its name only with the toggle on.
    /// </summary>
    private static void AddParticipantName(
        Dictionary<string, object?> dto,
        string key,
        string sub,
        AdminRelatedRows related,
        IFieldPolicy policy,
        FieldPrincipal principal,
        FieldAction action)
    {
        var ctx = FieldResourceContext.ForSelfProfile(sub);
        var participant = related.Participant(sub);
        var deleted = participant?.DeletedAt is not null;
        if (deleted && Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto[DeletedKey(key)] = true;
        if (deleted && !related.IncludeDeleted)
            return;
        if (Can(policy, principal, FieldClass.DisplayName, action, ctx))
            dto[key] = participant?.DisplayName;
    }

    /// <summary>"ownerName" → "ownerDeleted", "artifactName" → "artifactDeleted", and so on.</summary>
    public static string DeletedKey(string nameKey) => nameKey[..^"Name".Length] + "Deleted";

    /// <summary>The artifact's name as <c>artifactName</c>, if the policy allows it; null when the row is not there.</summary>
    private static void AddArtifactName(
        Dictionary<string, object?> dto,
        Guid artifactId,
        AdminRelatedRows related,
        IFieldPolicy policy,
        FieldPrincipal principal,
        FieldAction action)
    {
        var artifact = related.Artifact(artifactId);
        var ctx = FieldResourceContext.ForSelfProfile(artifact?.OwnerParticipantId ?? string.Empty);
        var deleted = artifact?.DeletedAt is not null;
        if (deleted && Can(policy, principal, FieldClass.SoftDeletedAt, action, ctx))
            dto["artifactDeleted"] = true;
        if (deleted && !related.IncludeDeleted)
            return;
        if (Can(policy, principal, FieldClass.ArtifactName, action, ctx))
            dto["artifactName"] = artifact is null ? null : ArtifactNameOf(artifact);
    }
}

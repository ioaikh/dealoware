using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Tests;

/// <summary>
/// Domain mutators for Step 9 edit paths E1–E12 (no migration).
/// </summary>
public class AdminEditDomainTests
{
    [Fact]
    public void TD_ADM_178_E1_Activate_SuspendedToActive()
    {
        var participant = Participant.Create("Ada");
        participant.Deactivate();
        Assert.False(participant.IsActive);
        participant.Activate();
        Assert.True(participant.IsActive);
    }

    [Fact]
    public void TD_ADM_178_E3_E4_SubjectEntity_NameAndDescription()
    {
        var entity = SubjectEntity.Create("Old", "desc");
        Assert.True(entity.UpdateName("New"));
        Assert.Equal("New", entity.Name);
        Assert.False(entity.UpdateName(""));
        Assert.False(entity.UpdateName(new string('n', 4097)));
        Assert.True(entity.UpdateDescription(""));
        Assert.Equal("", entity.Description);
        Assert.False(entity.UpdateDescription(new string('d', 4097)));
    }

    [Fact]
    public void TD_ADM_178_E5_ReassignOwner()
    {
        var artifact = Artifact.Create("participant:a", [SubjectEntity.Create("N", "D")], "sell");
        artifact.ReassignOwner("participant:b");
        Assert.Equal("participant:b", artifact.OwnerParticipantId);
    }

    [Fact]
    public void TD_ADM_178_E6_UpdateEndsAt_OnlyWhileOpen()
    {
        var artifactId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow;
        var (n, errors) = Negotiation.Create(artifactId, "a", "b", "sell", "buy", start, start.AddHours(2));
        Assert.Empty(errors);
        Assert.True(n!.UpdateEndsAt(start.AddHours(3)));
        Assert.False(n.UpdateEndsAt(start.AddMinutes(-1)));
        n.Close();
        Assert.False(n.UpdateEndsAt(start.AddHours(4)));
    }

    [Fact]
    public void TD_ADM_178_E8_Expire_CancelsOpenOffers_NoReopen()
    {
        var artifactId = Guid.NewGuid();
        var (n, errors) = Negotiation.Create(artifactId, "a", "b", "sell", "buy");
        Assert.Empty(errors);
        Assert.True(n!.Expire());
        Assert.Equal(NegotiationStatus.Expired, n.Status);
        Assert.False(n.Expire());
        Assert.False(n.Close());
    }

    [Fact]
    public void TD_ADM_178_E9_E11_UpdateTerms_OpenOnly()
    {
        var (offer, errors) = Offer.Create(Guid.NewGuid(), "a", "b", 1m, "USD", "t");
        Assert.Empty(errors);
        Assert.True(offer!.UpdateTerms(2.50m, "eur", "new"));
        Assert.Equal(2.50m, offer.Amount);
        Assert.Equal("EUR", offer.Currency);
        Assert.Equal("new", offer.Terms);
        Assert.False(offer.UpdateTerms(-1m, "USD", "x"));
        Assert.False(offer.UpdateTerms(1.111m, "USD", "x"));
        Assert.False(offer.UpdateTerms(1m, "US", "x"));
        Assert.False(offer.UpdateTerms(null, null, null));
        Assert.False(offer.UpdateTerms(1m, "USD", new string('t', 2001)));
        offer.Cancel();
        Assert.False(offer.UpdateTerms(3m, "USD", "later"));
    }

    [Fact]
    public void TD_ADM_178_MarkEdited_SetsUpdatedAtAndVersion()
    {
        var participant = Participant.Create("Ada");
        Assert.Equal(0u, participant.Version);
        Assert.Null(participant.UpdatedAt);
        participant.MarkEdited();
        Assert.Equal(1u, participant.Version);
        Assert.NotNull(participant.UpdatedAt);
    }
}

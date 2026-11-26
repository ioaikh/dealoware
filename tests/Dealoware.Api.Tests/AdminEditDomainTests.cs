using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Tests;

/// <summary>
/// The domain mutators behind the admin edit paths (A7 Step 9, TD-ADM-178).
/// E9, E10 and E11 (an offer's amount, currency and terms) have none, by the
/// owner's decision "Cancel only": what a party sent is never rewritten.
/// </summary>
public class AdminEditDomainTests
{
    private static Negotiation NewNegotiation(DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null)
    {
        var (negotiation, errors) = Negotiation.Create(Guid.NewGuid(), "participant:a", "participant:b", "sell", "buy", startsAt, endsAt);
        Assert.Empty(errors);
        return negotiation!;
    }

    private static Offer NewOffer()
    {
        var (offer, errors) = Offer.Create(Guid.NewGuid(), "participant:a", "participant:b", 12.50m, "USD", "as sent");
        Assert.Empty(errors);
        return offer!;
    }

    // E1
    [Fact]
    public void TD_ADM_178_E1_Activate_SuspendedToActive_AndBack()
    {
        var participant = Participant.Create("Ada");
        participant.Deactivate();
        Assert.False(participant.IsActive);

        participant.Activate();
        Assert.True(participant.IsActive);

        participant.Activate();
        Assert.True(participant.IsActive);
    }

    // E3
    [Fact]
    public void TD_ADM_178_E3_UpdateName_RequiredAndAtMost4096()
    {
        var entity = SubjectEntity.Create("Old", "desc");

        Assert.True(entity.UpdateName("  New  "));
        Assert.Equal("New", entity.Name);

        Assert.True(entity.UpdateName(new string('n', SubjectEntity.MaxTextLength)));
        Assert.Equal(SubjectEntity.MaxTextLength, entity.Name.Length);

        var kept = entity.Name;
        Assert.False(entity.UpdateName(""));
        Assert.False(entity.UpdateName("   "));
        Assert.False(entity.UpdateName(new string('n', SubjectEntity.MaxTextLength + 1)));
        Assert.Equal(kept, entity.Name);
    }

    // E4
    [Fact]
    public void TD_ADM_178_E4_UpdateDescription_MayBeEmpty_AtMost4096()
    {
        var entity = SubjectEntity.Create("Name", "desc");

        Assert.True(entity.UpdateDescription(""));
        Assert.Equal("", entity.Description);

        Assert.True(entity.UpdateDescription(null));
        Assert.Equal("", entity.Description);

        Assert.True(entity.UpdateDescription(new string('d', SubjectEntity.MaxTextLength)));
        Assert.False(entity.UpdateDescription(new string('d', SubjectEntity.MaxTextLength + 1)));
        Assert.Equal(SubjectEntity.MaxTextLength, entity.Description.Length);
    }

    // E5
    [Fact]
    public void TD_ADM_178_E5_ReassignOwner_SetsTheOwner_RefusesNone()
    {
        var artifact = Artifact.Create("participant:a", [SubjectEntity.Create("N", "D")], "sell");

        artifact.ReassignOwner("participant:b");
        Assert.Equal("participant:b", artifact.OwnerParticipantId);

        Assert.ThrowsAny<ArgumentException>(() => artifact.ReassignOwner(" "));
        Assert.Equal("participant:b", artifact.OwnerParticipantId);
    }

    // E6
    [Fact]
    public void TD_ADM_178_E6_UpdateEndsAt_WhileOpen_AfterTheStart()
    {
        var start = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var negotiation = NewNegotiation(start, start.AddHours(2));

        Assert.True(negotiation.UpdateEndsAt(start.AddHours(3)));
        Assert.Equal(start.AddHours(3), negotiation.EndsAt);

        // Stored as UTC whatever offset it came with.
        Assert.True(negotiation.UpdateEndsAt(new DateTimeOffset(2026, 11, 1, 15, 0, 0, TimeSpan.FromHours(2))));
        Assert.Equal(TimeSpan.Zero, negotiation.EndsAt!.Value.Offset);
        Assert.Equal(start.AddHours(4), negotiation.EndsAt);

        Assert.False(negotiation.UpdateEndsAt(start));
        Assert.False(negotiation.UpdateEndsAt(start.AddMinutes(-1)));
        Assert.Equal(start.AddHours(4), negotiation.EndsAt);

        Assert.True(negotiation.UpdateEndsAt(null));
        Assert.Null(negotiation.EndsAt);
    }

    // E6
    [Fact]
    public void TD_ADM_178_E6_UpdateEndsAt_RefusedOnceClosedOrExpired()
    {
        var closed = NewNegotiation();
        Assert.True(closed.Close());
        Assert.False(closed.UpdateEndsAt(DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Null(closed.EndsAt);

        var expired = NewNegotiation();
        Assert.True(expired.Expire());
        Assert.False(expired.UpdateEndsAt(DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Null(expired.EndsAt);
    }

    // E7, E8
    [Fact]
    public void TD_ADM_178_E7_E8_ClosedAndExpiredAreFinal()
    {
        var expired = NewNegotiation(endsAt: DateTimeOffset.UtcNow.AddDays(7));
        Assert.True(expired.Expire());
        Assert.Equal(NegotiationStatus.Expired, expired.Status);
        Assert.False(expired.Expire());
        Assert.False(expired.Close());
        Assert.Equal(NegotiationStatus.Expired, expired.Status);

        var closed = NewNegotiation();
        Assert.True(closed.Close());
        Assert.False(closed.Expire());
        Assert.False(closed.Close());
        Assert.Equal(NegotiationStatus.Closed, closed.Status);
    }

    // E12
    [Fact]
    public void TD_ADM_178_E12_Cancel_OnlyFromOpen_LeavesWhatWasSent()
    {
        var offer = NewOffer();
        Assert.True(offer.Cancel());
        Assert.Equal(OfferStatus.Cancelled, offer.Status);
        Assert.False(offer.Cancel());

        Assert.Equal(12.50m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal("as sent", offer.Terms);

        var accepted = NewOffer();
        Assert.True(accepted.Accept("participant:b"));
        Assert.False(accepted.Cancel());
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
    }

    [Fact]
    public void TD_ADM_081_MarkEdited_NextVersionAndTheGivenTimeInUtc()
    {
        var at = new DateTimeOffset(2026, 11, 26, 14, 30, 0, TimeSpan.FromHours(2));

        var participant = Participant.Create("Ada");
        Assert.Equal(0u, participant.Version);
        Assert.Null(participant.UpdatedAt);
        participant.MarkEdited(at);
        participant.MarkEdited(at);
        Assert.Equal(2u, participant.Version);
        Assert.Equal(at, participant.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, participant.UpdatedAt!.Value.Offset);

        var artifact = Artifact.Create("participant:a", [SubjectEntity.Create("N", "D")], "sell");
        artifact.MarkEdited(at);
        Assert.Equal(1u, artifact.Version);
        Assert.Equal(at, artifact.UpdatedAt);

        var negotiation = NewNegotiation();
        negotiation.MarkEdited(at);
        Assert.Equal(1u, negotiation.Version);
        Assert.Equal(at, negotiation.UpdatedAt);

        var offer = NewOffer();
        offer.MarkEdited(at);
        Assert.Equal(1u, offer.Version);
        Assert.Equal(at, offer.UpdatedAt);
    }

    /// <summary>
    /// The engine's party paths are as they were: none of them moves
    /// <c>Version</c>. That is why an admin write is checked against the
    /// status and time it was prepared against, not against Version alone.
    /// </summary>
    [Fact]
    public void PartyActions_DoNotMoveVersion()
    {
        var accepted = NewOffer();
        accepted.Accept("participant:b");
        var declined = NewOffer();
        declined.Decline("participant:b");
        var withdrawn = NewOffer();
        withdrawn.Withdraw("participant:a");
        var superseded = NewOffer();
        superseded.Supersede();
        var cancelled = NewOffer();
        cancelled.Cancel();
        Assert.All(new[] { accepted, declined, withdrawn, superseded, cancelled }, o => Assert.Equal(0u, o.Version));
        Assert.Null(cancelled.UpdatedAt);

        var negotiation = NewNegotiation();
        negotiation.Close();
        Assert.Equal(0u, negotiation.Version);

        var participant = Participant.Create("Ada");
        participant.UpdateDisplayName("Ada L.");
        Assert.Equal(0u, participant.Version);
    }
}

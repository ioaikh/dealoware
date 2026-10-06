using Dealoware.Domain.Negotiations;

namespace Dealoware.Api.Tests;

/// <summary>
/// D1.1: Withdrawn is offer-only; maker-only domain Withdraw() is the only setter.
/// </summary>
public class OfferWithdrawTests
{
    [Fact]
    public void TD_ADM_062_OfferStatus_Withdrawn_IsFive_AndDoesNotRenumber()
    {
        Assert.Equal(0, (int)OfferStatus.Open);
        Assert.Equal(1, (int)OfferStatus.Accepted);
        Assert.Equal(2, (int)OfferStatus.Declined);
        Assert.Equal(3, (int)OfferStatus.Superseded);
        Assert.Equal(4, (int)OfferStatus.Cancelled);
        Assert.Equal(5, (int)OfferStatus.Withdrawn);
        Assert.False(Enum.IsDefined(typeof(NegotiationStatus), 5));
        Assert.False(Enum.TryParse<NegotiationStatus>("Withdrawn", ignoreCase: true, out _));
    }

    [Fact]
    public void TD_ADM_063_Withdraw_MakerOnly_SetsWithdrawn()
    {
        var negotiationId = Guid.NewGuid();
        var (offer, errors) = Offer.Create(negotiationId, "maker-sub", "counter-sub", 10m, "USD", null);
        Assert.Empty(errors);
        Assert.NotNull(offer);

        Assert.False(offer!.Withdraw("counter-sub"));
        Assert.Equal(OfferStatus.Open, offer.Status);

        Assert.True(offer.Withdraw("maker-sub"));
        Assert.Equal(OfferStatus.Withdrawn, offer.Status);
        Assert.NotNull(offer.UpdatedAt);
    }

    [Fact]
    public void TD_ADM_063_Withdraw_RejectedWhenNotOpen()
    {
        var (offer, _) = Offer.Create(Guid.NewGuid(), "maker-sub", "counter-sub", 10m, "USD", null);
        Assert.True(offer!.Accept("counter-sub"));
        Assert.False(offer.Withdraw("maker-sub"));
        Assert.Equal(OfferStatus.Accepted, offer.Status);
    }
}

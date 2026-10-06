using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-002 / TD-ADM-005: CoreOwner FieldPolicy dual wall. No parallel ACL.
/// </summary>
public class CoreOwnerFieldPolicyTests
{
    private readonly FieldPolicy _policy = new();
    private readonly FieldPrincipal _owner = FieldPrincipal.CoreOwner("io@aiknowhow.com");
    private readonly FieldResourceContext _ctx = FieldResourceContext.ForSelfProfile("participant:someone");

    [Fact]
    public void TdAdm002_CoreOwnerPrincipal_IsNotRemappedByResourceOwnership()
    {
        Assert.Equal(PrincipalType.CoreOwner, _owner.Type);
        Assert.Equal(PrincipalType.CoreOwner, _owner.GetTypeForContext(_ctx));
        Assert.NotEqual(PrincipalType.User, _owner.GetTypeForContext(_ctx));
        Assert.NotEqual(PrincipalType.Stranger, _owner.GetTypeForContext(_ctx));
    }

    [Fact]
    public void TdAdm005_UnregisteredFieldClass_DeniedForCoreOwner()
    {
        var unknown = FieldClass.Custom("PasswordHash");
        Assert.False(_policy.IsRegistered(unknown));
        Assert.False(_policy.Evaluate(_owner, unknown, FieldAction.Read, _ctx));
        Assert.False(_policy.Evaluate(_owner, unknown, FieldAction.Write, _ctx));
    }

    [Fact]
    public void TdAdm005_StrategyBody_DeniedForCoreOwner()
    {
        Assert.False(_policy.Evaluate(_owner, FieldClass.StrategyBody, FieldAction.Read, _ctx));
        Assert.False(_policy.Evaluate(_owner, FieldClass.StrategyBody, FieldAction.Write, _ctx));
    }

    [Fact]
    public void TdAdm005_LoginEmail_ReadOnly_NoGenericWrite()
    {
        Assert.True(_policy.Evaluate(_owner, FieldClass.LoginEmail, FieldAction.Read, _ctx));
        Assert.False(_policy.Evaluate(_owner, FieldClass.LoginEmail, FieldAction.Write, _ctx));
    }

    [Theory]
    [InlineData("DisplayName", true, true)]
    [InlineData("ParticipantActive", true, true)]
    [InlineData("ArtifactName", true, true)]
    [InlineData("ArtifactDescription", true, true)]
    [InlineData("ArtifactOwnerParticipantId", true, true)]
    [InlineData("NegotiationStatus", true, true)]
    [InlineData("NegotiationEndsAt", true, true)]
    [InlineData("NegotiationPartyA", true, false)]
    [InlineData("NegotiationPartyB", true, false)]
    [InlineData("NegotiationArtifactId", true, false)]
    [InlineData("OfferAmount", true, true)]
    [InlineData("OfferCurrency", true, true)]
    [InlineData("OfferTerms", true, true)]
    [InlineData("OfferStatus", true, true)]
    [InlineData("OfferNegotiationId", true, false)]
    [InlineData("SoftDeletedAt", true, false)]
    [InlineData("EntityVersion", true, false)]
    [InlineData("EntityCreatedAt", true, false)]
    public void TdAdm005_CoreOwner_RegisteredEntityFields(string name, bool canRead, bool canWrite)
    {
        var field = FieldClass.Custom(name);
        Assert.True(_policy.IsRegistered(field), $"{name} should be registered");
        Assert.Equal(canRead, _policy.Evaluate(_owner, field, FieldAction.Read, _ctx));
        Assert.Equal(canWrite, _policy.Evaluate(_owner, field, FieldAction.Write, _ctx));
        Assert.False(_policy.Evaluate(_owner, field, FieldAction.ShareOutbound, _ctx));
    }

    [Fact]
    public void TdAdm005_ParticipantUser_DeniedOnAdminEntityFieldClasses()
    {
        var user = FieldPrincipal.User("participant:owner");
        var ownerCtx = FieldResourceContext.ForSelfProfile("participant:owner");
        Assert.False(_policy.Evaluate(user, FieldClass.ArtifactName, FieldAction.Read, ownerCtx));
        Assert.False(_policy.Evaluate(user, FieldClass.NegotiationStatus, FieldAction.Write, ownerCtx));
        Assert.False(_policy.Evaluate(user, FieldClass.OfferAmount, FieldAction.Read, ownerCtx));
        Assert.False(_policy.Evaluate(user, FieldClass.ParticipantActive, FieldAction.Write, ownerCtx));
    }

    [Fact]
    public void TdAdm007_NoParallelAcl_UserEmailIsNotCoreOwner()
    {
        var lookalike = FieldPrincipal.User("io@aiknowhow.com");
        var ctx = FieldResourceContext.ForSelfProfile("io@aiknowhow.com");
        Assert.Equal(PrincipalType.User, lookalike.GetTypeForContext(ctx));
        Assert.False(_policy.Evaluate(lookalike, FieldClass.ArtifactName, FieldAction.Read, ctx));
    }
}

using System.Globalization;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

public class AdminPasswordRulesTests
{
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void TD_ADM_030_ContextWord_v12_RequiredExamples_AreCultureIndependent(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var ci = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            Assert.False(AdminPasswordRules.IsCommonOrContext(
                AdminPasswordRules.Normalize("radio station"),
                CoreOwnerEmail));

            Assert.True(AdminPasswordRules.IsCommonOrContext(
                AdminPasswordRules.Normalize("io2026!!!"),
                CoreOwnerEmail));
            Assert.True(AdminPasswordRules.IsCommonOrContext(
                AdminPasswordRules.Normalize("io$$$2026$$$"),
                CoreOwnerEmail));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void TD_ADM_030_ContextWord_EqualsContainsAndStrip()
    {
        Assert.True(AdminPasswordRules.MatchesContextWord(
            AdminPasswordRules.Fold("DEALOWARE"),
            AdminPasswordRules.Fold("dealoware")));
        Assert.True(AdminPasswordRules.MatchesContextWord(
            AdminPasswordRules.Fold("xxadminxx"),
            AdminPasswordRules.Fold("admin")));
        Assert.False(AdminPasswordRules.MatchesContextWord(
            AdminPasswordRules.Fold("xxioxx"),
            AdminPasswordRules.Fold("io")));
    }

    [Fact]
    public void TD_ADM_030_Length_CountsCodePointsAfterNfkc()
    {
        var fourteen = new string('a', 14);
        var fifteen = new string('a', 15);
        var max = new string('a', 128);
        var over = new string('a', 129);

        Assert.Equal(AdminPasswordRuleReason.TooShort,
            AdminPasswordRules.Evaluate(fourteen, CoreOwnerEmail, false).Reason);
        Assert.Equal(AdminPasswordRuleReason.Accepted,
            AdminPasswordRules.Evaluate(fifteen, CoreOwnerEmail, false).Reason);
        Assert.Equal(AdminPasswordRuleReason.Accepted,
            AdminPasswordRules.Evaluate(max, CoreOwnerEmail, false).Reason);
        Assert.Equal(AdminPasswordRuleReason.TooLong,
            AdminPasswordRules.Evaluate(over, CoreOwnerEmail, false).Reason);
    }

    [Fact]
    public void TD_ADM_030_BundledBlocklist_NamedFixture()
    {
        var result = AdminPasswordRules.Evaluate(
            AdminPasswordRules.BundledBlocklistFixtureValue,
            CoreOwnerEmail,
            matchesCurrentPassword: false);
        Assert.Equal(AdminPasswordRuleReason.CommonOrContext, result.Reason);
        Assert.Equal(AdminPasswordRules.CommonOrContextMessage, result.Message);
        Assert.Equal("commonpasswordxxx", AdminPasswordRules.BundledBlocklistFixtureValue);
    }
}

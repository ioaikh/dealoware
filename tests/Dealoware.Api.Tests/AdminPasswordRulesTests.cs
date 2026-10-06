using System.Globalization;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// Password-rules note v2.3 (TD-ADM-011 / TD-ADM-UI-auth-10 server side).
/// </summary>
public class AdminPasswordRulesTests
{
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void TD_ADM_011_ContextWord_V12_IsCultureIndependent(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var ci = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            Assert.False(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("radio station")));
            Assert.True(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("io2026!!!")));
            Assert.True(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("io$$$2026$$$")));
            Assert.True(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("dealoware")));
            Assert.True(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("xxadminxx")));
            Assert.False(AdminPasswordRules.MatchesContextWord(AdminPasswordRules.Fold("radio")));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void TD_ADM_011_Length_UsesNfkcCodePoints()
    {
        var fourteen = new string('a', 14);
        var fifteen = new string('a', 15);
        var oneTwentyEight = new string('a', 128);
        var oneTwentyNine = new string('a', 129);

        Assert.Equal(AdminAuthMessages.PasswordTooShort, AdminPasswordRules.Validate(fourteen, null, null).Error);
        Assert.True(AdminPasswordRules.Validate(fifteen, null, null).Succeeded);
        Assert.True(AdminPasswordRules.Validate(oneTwentyEight, null, null).Succeeded);
        Assert.Equal(AdminAuthMessages.PasswordTooLong, AdminPasswordRules.Validate(oneTwentyNine, null, null).Error);
    }

    [Fact]
    public void TD_ADM_011_BundledBlocklist_RejectsFixtureValue()
    {
        Assert.Equal(AdminPasswordRules.BundledBlocklistFixtureName, "bundled-common-v1");
        var result = AdminPasswordRules.Validate("password1234567", null, null);
        Assert.False(result.Succeeded);
        Assert.Equal(AdminAuthMessages.PasswordCommonOrContext, result.Error);
    }

    [Fact]
    public void TD_ADM_011_Hasher_UsesPbkdf2Sha512Floor()
    {
        var password = "radio station " + Guid.NewGuid().ToString("N")[..4];
        var encoded = AdminPasswordHasher.Hash(password);
        Assert.StartsWith("pbkdf2-sha512$220000$", encoded);
        Assert.True(AdminPasswordHasher.Verify(password, encoded));
        Assert.False(AdminPasswordHasher.Verify(password + "x", encoded));
        Assert.DoesNotContain(password, encoded);
    }
}

using System.Globalization;
using System.Text;

namespace Dealoware.Domain.Admin;

/// <summary>
/// Password-rules note v2.3 server checks (length after NFKC, bundled
/// blocklist, context-word v1.2). Local only — no outbound API.
/// </summary>
public static class AdminPasswordRules
{
    public const int MinCodePoints = 15;
    public const int MaxCodePoints = 128;

    /// <summary>
    /// Context words: email local part plus product/host words of any length.
    /// Containment only applies to words of four or more code points.
    /// </summary>
    public static readonly IReadOnlyList<string> ContextWords =
    [
        "io",
        "admin",
        "core",
        "dealoware",
        "aiknowhow"
    ];

    /// <summary>
    /// Bundled common-password list. Referenced by fixture name in tests.
    /// Values are not a CoreOwner password.
    /// </summary>
    public static readonly IReadOnlySet<string> BundledBlocklist = new HashSet<string>(StringComparer.Ordinal)
    {
        NormalizeForCompare("password1234567"),
        NormalizeForCompare("qwertyuiopasdfg"),
        NormalizeForCompare("123456789012345")
    };

    public const string BundledBlocklistFixtureName = "bundled-common-v1";

    public static AdminPasswordRuleResult Validate(string? password, string? currentPasswordHash, Func<string, string, bool>? verifyCurrent)
    {
        if (password is null)
        {
            return AdminPasswordRuleResult.Fail(AdminAuthMessages.PasswordTooShort);
        }

        var nfkc = password.Normalize(NormalizationForm.FormKC);
        var codePoints = nfkc.EnumerateRunes().Count();
        if (codePoints < MinCodePoints)
        {
            return AdminPasswordRuleResult.Fail(AdminAuthMessages.PasswordTooShort);
        }

        if (codePoints > MaxCodePoints)
        {
            return AdminPasswordRuleResult.Fail(AdminAuthMessages.PasswordTooLong);
        }

        var folded = Fold(nfkc);
        if (BundledBlocklist.Contains(folded) || MatchesContextWord(folded))
        {
            return AdminPasswordRuleResult.Fail(AdminAuthMessages.PasswordCommonOrContext);
        }

        if (!string.IsNullOrEmpty(currentPasswordHash)
            && verifyCurrent is not null
            && verifyCurrent(password, currentPasswordHash))
        {
            return AdminPasswordRuleResult.Fail(AdminAuthMessages.PasswordSameAsCurrent);
        }

        return AdminPasswordRuleResult.Ok();
    }

    public static bool MatchesContextWord(string foldedPassword)
    {
        foreach (var word in ContextWords)
        {
            var foldedWord = Fold(word);
            if (foldedPassword == foldedWord)
            {
                return true;
            }

            if (foldedWord.EnumerateRunes().Count() >= 4
                && foldedPassword.Contains(foldedWord, StringComparison.Ordinal))
            {
                return true;
            }

            var stripped = StripNpsz(foldedPassword);
            if (stripped == foldedWord)
            {
                return true;
            }
        }

        return false;
    }

    public static string Fold(string value)
        => value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    public static string NormalizeForCompare(string value) => Fold(value);

    public static string StripNpsz(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var rune in value.EnumerateRunes())
        {
            if (!IsNpsz(rune))
            {
                builder.Append(rune);
            }
        }

        return builder.ToString();
    }

    private static bool IsNpsz(Rune rune)
    {
        if (!rune.IsBmp)
        {
            return false;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory(rune.Value);
        return category
            is UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.OtherNumber
            or UnicodeCategory.SpaceSeparator
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator
            or UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            or UnicodeCategory.OpenPunctuation
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.InitialQuotePunctuation
            or UnicodeCategory.FinalQuotePunctuation
            or UnicodeCategory.OtherPunctuation
            or UnicodeCategory.MathSymbol
            or UnicodeCategory.CurrencySymbol
            or UnicodeCategory.ModifierSymbol
            or UnicodeCategory.OtherSymbol;
    }
}

public readonly record struct AdminPasswordRuleResult(bool Succeeded, string? Error)
{
    public static AdminPasswordRuleResult Ok() => new(true, null);

    public static AdminPasswordRuleResult Fail(string error) => new(false, error);
}

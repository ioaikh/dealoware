using System.Globalization;
using System.Text;

namespace Dealoware.Domain.Admin;

public enum AdminPasswordRuleReason
{
    Accepted,
    TooShort,
    TooLong,
    CommonOrContext,
    SameAsCurrent
}

public readonly record struct AdminPasswordRuleResult(
    AdminPasswordRuleReason Reason,
    string Message)
{
    public bool Accepted => Reason == AdminPasswordRuleReason.Accepted;
}

/// <summary>
/// Password-rules note v2.3 rules 1–5 and 7–9 (API). Length is code points
/// after NFKC. Context-word v1.2: NFKC → Unicode default full case folding;
/// equals / contains ≥4 / equals after stripping N, P, S, Z. Local only.
/// </summary>
public static class AdminPasswordRules
{
    public const int MinCodePoints = 15;
    public const int MaxCodePoints = 128;

    public const string TooShortMessage = "Use at least 15 characters.";
    public const string TooLongMessage = "Use at most 128 characters.";
    public const string CommonOrContextMessage =
        "Choose a different password. That one is too common or matches this account.";
    public const string SameAsCurrentMessage =
        "Choose a password that is different from your current password.";

    /// <summary>Fixture name for the bundled blocklist entry used in tests.</summary>
    public const string BundledBlocklistFixtureName = "bundled-common-password";

    /// <summary>
    /// Bundled common-password list. Values are already NFKC + case-folded.
    /// The named fixture is long enough to pass the length rule so tests can
    /// hit this reason on its own.
    /// </summary>
    public static readonly IReadOnlySet<string> BundledBlocklist =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Fold("commonpasswordxxx"),
            Fold("passwordpassword"),
            Fold("123456789012345")
        };

    public static readonly string BundledBlocklistFixtureValue = "commonpasswordxxx";

    public static AdminPasswordRuleResult Evaluate(
        string? password,
        string email,
        bool matchesCurrentPassword)
    {
        var normalized = Normalize(password ?? string.Empty);
        var codePoints = CountCodePoints(normalized);

        if (codePoints < MinCodePoints)
        {
            return new AdminPasswordRuleResult(AdminPasswordRuleReason.TooShort, TooShortMessage);
        }

        if (codePoints > MaxCodePoints)
        {
            return new AdminPasswordRuleResult(AdminPasswordRuleReason.TooLong, TooLongMessage);
        }

        if (IsCommonOrContext(normalized, email))
        {
            return new AdminPasswordRuleResult(
                AdminPasswordRuleReason.CommonOrContext,
                CommonOrContextMessage);
        }

        if (matchesCurrentPassword)
        {
            return new AdminPasswordRuleResult(
                AdminPasswordRuleReason.SameAsCurrent,
                SameAsCurrentMessage);
        }

        return new AdminPasswordRuleResult(AdminPasswordRuleReason.Accepted, string.Empty);
    }

    public static string Normalize(string password)
        => password.Normalize(NormalizationForm.FormKC);

    public static int CountCodePoints(string value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    public static bool IsCommonOrContext(string nfkcPassword, string email)
    {
        var folded = Fold(nfkcPassword);
        if (BundledBlocklist.Contains(folded))
        {
            return true;
        }

        foreach (var word in ContextWords(email))
        {
            if (MatchesContextWord(folded, word))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Context-word v1.2 on an already-NFKC input (unit-test entry).
    /// </summary>
    public static bool MatchesContextWord(string foldedPassword, string foldedContextWord)
    {
        if (string.IsNullOrEmpty(foldedContextWord))
        {
            return false;
        }

        if (string.Equals(foldedPassword, foldedContextWord, StringComparison.Ordinal))
        {
            return true;
        }

        if (foldedContextWord.Length >= 4
            && foldedPassword.Contains(foldedContextWord, StringComparison.Ordinal))
        {
            return true;
        }

        var stripped = StripNpsz(foldedPassword);
        return string.Equals(stripped, foldedContextWord, StringComparison.Ordinal);
    }

    public static IEnumerable<string> ContextWords(string email)
    {
        yield return Fold("dealoware");
        yield return Fold("admin");

        var at = email.IndexOf('@');
        var local = at > 0 ? email[..at] : email;
        yield return Fold(Normalize(local.Trim()));
    }

    public static string Fold(string value)
    {
        var nfkc = value.Normalize(NormalizationForm.FormKC);
        return nfkc.ToLowerInvariant();
    }

    public static string StripNpsz(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var rune in value.EnumerateRunes())
        {
            if (IsNpsz(CharUnicodeInfo.GetUnicodeCategory(rune.Value)))
            {
                continue;
            }

            builder.Append(rune);
        }

        return builder.ToString();
    }

    private static bool IsNpsz(UnicodeCategory category)
        => category is UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.OtherNumber
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
            or UnicodeCategory.OtherSymbol
            or UnicodeCategory.SpaceSeparator
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator;
}

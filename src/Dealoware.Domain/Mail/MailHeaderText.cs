using System.Globalization;

namespace Dealoware.Domain.Mail;

/// <summary>
/// Header-safe mail text: no CR/LF or other Unicode Cc/Cf controls.
/// Addresses are exactly one mailbox, not a list or group.
/// </summary>
public static class MailHeaderText
{
    public static string NormalizeRequired(string value, int maxLength, string paramName)
    {
        if (value is null)
            throw new ArgumentException($"{paramName} is required and must be {maxLength} characters or fewer.", paramName);

        EnsureNoControls(value, paramName);

        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{paramName} is required and must be {maxLength} characters or fewer.", paramName);

        return value.Trim();
    }

    public static string NormalizeSingleAddress(string value, int maxLength, string paramName)
    {
        var trimmed = NormalizeRequired(value, maxLength, paramName);
        if (trimmed.Contains(',')
            || trimmed.Contains(';')
            || trimmed.Contains(':')
            || trimmed.Contains('<')
            || trimmed.Contains('>')
            || trimmed.Contains('"')
            || trimmed.Contains('\\')
            || trimmed.Contains(' ')
            || CountAt(trimmed) != 1)
        {
            throw new ArgumentException($"{paramName} must be exactly one address.", paramName);
        }

        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
            throw new ArgumentException($"{paramName} must be exactly one address.", paramName);

        return trimmed;
    }

    public static void EnsureNoControls(string value, string paramName)
    {
        foreach (var c in value)
        {
            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                throw new ArgumentException($"{paramName} must not contain control or format characters.", paramName);
        }
    }

    private static int CountAt(string value)
    {
        var count = 0;
        foreach (var c in value)
        {
            if (c == '@')
                count++;
        }

        return count;
    }
}

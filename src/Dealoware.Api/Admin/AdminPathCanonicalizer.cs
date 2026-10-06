using System.Globalization;
using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>r5 §10 C1 (sha c5e9d232): canonicalise before any session-gate exemption compare.</summary>
public static class AdminPathCanonicalizer
{
    public static bool TryCanonicalize(string? rawPath, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrEmpty(rawPath) || rawPath[0] != '/')
        {
            return false;
        }

        if (ContainsForbiddenRawChars(rawPath))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(rawPath);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (ContainsResidualEncoding(decoded) || ContainsForbiddenRawChars(decoded))
        {
            return false;
        }

        if (decoded.Contains("//", StringComparison.Ordinal)
            || decoded.Contains('\\', StringComparison.Ordinal)
            || decoded.Contains("..", StringComparison.Ordinal)
            || decoded.Contains(';', StringComparison.Ordinal))
        {
            return false;
        }

        if (decoded.Length > 1 && decoded.EndsWith('/') && !string.Equals(decoded, "/admin/", StringComparison.Ordinal))
        {
            return false;
        }

        canonical = decoded;
        return true;
    }

    private static bool ContainsResidualEncoding(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower.Contains("%2f", StringComparison.Ordinal)
               || lower.Contains("%5c", StringComparison.Ordinal)
               || lower.Contains("%2e", StringComparison.Ordinal)
               || lower.Contains("%25", StringComparison.Ordinal);
    }

    private static bool ContainsForbiddenRawChars(string value)
    {
        foreach (var c in value)
        {
            if (c == '\\')
            {
                return true;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }
}

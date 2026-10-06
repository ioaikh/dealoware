using System.Globalization;
using Microsoft.AspNetCore.Http.Features;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C1: canonicalise the raw path before any exemption compare.
/// </summary>
public static class AdminPathCanonicalizer
{
    public static string RawPath(HttpContext context)
    {
        var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
        if (!string.IsNullOrEmpty(rawTarget))
        {
            var q = rawTarget.IndexOf('?', StringComparison.Ordinal);
            return q >= 0 ? rawTarget[..q] : rawTarget;
        }

        return context.Request.Path.Value ?? string.Empty;
    }

    public static bool TryCanonicalize(string rawPath, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrEmpty(rawPath))
        {
            return false;
        }

        if (ContainsForbiddenChars(rawPath))
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

        if (ContainsResidualEncodedDanger(decoded) || ContainsForbiddenChars(decoded))
        {
            return false;
        }

        if (decoded.Contains("//", StringComparison.Ordinal)
            || decoded.Contains(';', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var segment in decoded.Split('/'))
        {
            if (segment == "..")
            {
                return false;
            }
        }

        canonical = decoded;
        return true;
    }

    private static bool ContainsResidualEncodedDanger(string value)
        => value.Contains("%2f", StringComparison.OrdinalIgnoreCase)
           || value.Contains("%5c", StringComparison.OrdinalIgnoreCase)
           || value.Contains("%2e", StringComparison.OrdinalIgnoreCase)
           || value.Contains("%25", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsForbiddenChars(string value)
    {
        foreach (var ch in value)
        {
            if (ch == '\\')
            {
                return true;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }
}

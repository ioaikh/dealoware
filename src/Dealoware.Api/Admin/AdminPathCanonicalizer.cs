using System.Globalization;
using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// r5 sha c5e9d232 C1: canonicalise the request path before any exemption compare.
/// </summary>
public static class AdminPathCanonicalizer
{
    public static bool TryCanonicalize(string? rawPath, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrEmpty(rawPath))
        {
            return false;
        }

        var pathOnly = rawPath.Split('?', 2)[0];
        if (pathOnly.Contains('\\') || ContainsCcOrCf(pathOnly))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(pathOnly);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (decoded.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%2e", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%25", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains('\\')
            || ContainsCcOrCf(decoded)
            || decoded.Contains("//", StringComparison.Ordinal)
            || decoded.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = decoded.Split('/');
        foreach (var segment in segments)
        {
            if (segment.Contains(';') || segment == "..")
            {
                return false;
            }
        }

        if (decoded.Length > 1 && decoded.EndsWith('/') && !string.Equals(decoded, "/admin/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        canonical = decoded;
        return true;
    }

    public static bool TryCanonicalize(HttpContext context, out string canonical)
    {
        var rawTarget = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>()?.RawTarget;
        var rawPath = string.IsNullOrEmpty(rawTarget)
            ? context.Request.Path.Value
            : rawTarget.Split('?', 2)[0];
        return TryCanonicalize(rawPath, out canonical);
    }

    private static bool ContainsCcOrCf(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (!rune.IsBmp)
            {
                continue;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(rune.Value);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }
}

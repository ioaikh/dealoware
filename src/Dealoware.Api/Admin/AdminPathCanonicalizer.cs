using System.Globalization;
using Microsoft.AspNetCore.Http.Features;

namespace Dealoware.Api.Admin;

/// <summary>
/// Route-prefix note r3 §10 C1: canonicalise before any session-gate exemption compare.
/// </summary>
public static class AdminPathCanonicalizer
{
    public static bool TryGetComparablePath(HttpContext context, out string canonical)
    {
        canonical = string.Empty;
        var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
        if (!string.IsNullOrEmpty(rawTarget))
        {
            if (!TryExtractPath(rawTarget, out var rawPath) || !IsSafeRawPath(rawPath))
                return false;
        }

        return TryCanonicalize(context.Request.Path.Value, out canonical);
    }

    public static bool TryCanonicalize(string? path, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrEmpty(path))
            return false;

        if (!IsSafeRawPath(path))
            return false;

        if (!TryDecodeOnce(path, out var decoded) || !IsSafeDecodedPath(decoded))
            return false;

        if (decoded.Length > 1
            && decoded.EndsWith('/')
            && !string.Equals(decoded, "/admin/", StringComparison.Ordinal))
        {
            return false;
        }

        canonical = decoded;
        return true;
    }

    internal static bool TryExtractPath(string rawTarget, out string path)
    {
        path = rawTarget;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
                return false;
            path = uri.PathAndQuery;
        }

        var cut = path.IndexOfAny(['?', '#']);
        if (cut >= 0)
            path = path[..cut];

        return path.Length > 0;
    }

    internal static bool IsSafeRawPath(string path)
        => !ContainsForbiddenCharacters(path)
           && TryDecodeOnce(path, out var decoded)
           && !ContainsRemainingEncodedSeparator(decoded)
           && IsSafeDecodedPath(decoded);

    private static bool IsSafeDecodedPath(string path)
    {
        if (ContainsForbiddenCharacters(path) || path.Contains("//", StringComparison.Ordinal))
            return false;

        var segments = path.Split('/');
        foreach (var segment in segments)
        {
            if (segment == "..")
                return false;
            if (segment.Contains(';', StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool ContainsForbiddenCharacters(string value)
    {
        foreach (var ch in value)
        {
            if (ch == '\\')
                return true;
            var category = char.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                return true;
        }

        return false;
    }

    private static bool ContainsRemainingEncodedSeparator(string value)
    {
        return value.Contains("%2f", StringComparison.OrdinalIgnoreCase)
               || value.Contains("%5c", StringComparison.OrdinalIgnoreCase)
               || value.Contains("%2e", StringComparison.OrdinalIgnoreCase)
               || value.Contains("%25", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecodeOnce(string value, out string decoded)
    {
        try
        {
            decoded = Uri.UnescapeDataString(value.Replace("+", "%2B", StringComparison.Ordinal));
            return true;
        }
        catch (UriFormatException)
        {
            decoded = string.Empty;
            return false;
        }
        catch (ArgumentException)
        {
            decoded = string.Empty;
            return false;
        }
    }
}

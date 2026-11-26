using System.Globalization;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Admin;

/// <summary>
/// Route-prefix note r3 §5 / §10 C6 return-path allowlist. Failures become /admin/.
/// <para>
/// TD-ADM-177: the path is decoded once and must then be one of the
/// allowlisted page paths exactly. A list path may carry its query: only the
/// known list keys come back, each checked again and re-encoded by
/// <see cref="AdminReturnQuery"/>; unknown keys and bad values are dropped.
/// </para>
/// </summary>
public static class AdminReturnPath
{
    public const string Stats = "/admin/";
    public const int MaxLength = 2048;
    public const string FixedOrigin = "https://admin.core.dealoware.com";

    private static readonly HashSet<string> ListPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin/participants",
        "/admin/artifacts",
        "/admin/negotiations",
        "/admin/offers",
        "/admin/audit"
    };

    private static readonly HashSet<string> EntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "participants",
        "artifacts",
        "negotiations",
        "offers"
    };

    private static readonly Regex GuidSegment = new(
        "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Resolve(string? candidate)
        => TryValidate(candidate, out var path) ? path : Stats;

    public static bool TryValidate(string? candidate, out string path)
    {
        path = Stats;
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        if (candidate.Length > MaxLength)
            return false;

        // The query is split off before anything is decoded, so an encoded "?"
        // stays part of the path (and fails the allowlist there).
        var queryIndex = candidate.IndexOf('?', StringComparison.Ordinal);
        var rawPath = queryIndex >= 0 ? candidate[..queryIndex] : candidate;
        var rawQuery = queryIndex >= 0 ? candidate[(queryIndex + 1)..] : string.Empty;
        if (rawQuery.Contains('#', StringComparison.Ordinal))
            return false;

        string pathPart;
        try
        {
            pathPart = Uri.UnescapeDataString(rawPath.Replace("+", "%2B", StringComparison.Ordinal));
        }
        catch (UriFormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (pathPart.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || pathPart.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || pathPart.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase)
            || pathPart.Contains("%25", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (pathPart.Length == 0 || pathPart[0] != '/')
            return false;
        if (pathPart.Length > 1 && (pathPart[1] is '/' or '\\'))
            return false;

        if (ContainsForbiddenReturnCharacters(pathPart))
            return false;

        if (string.Equals(pathPart, "/admin", StringComparison.OrdinalIgnoreCase))
            pathPart = Stats;

        if (!IsAllowed(pathPart))
            return false;

        var kept = pathPart + AdminReturnQuery.Filter(pathPart, rawQuery);
        if (kept.Length > MaxLength)
            return false;

        path = kept;
        return true;
    }

    public static string ToFixedOriginLocation(string validatedPath)
        => FixedOrigin + validatedPath;

    private static bool IsAllowed(string path)
    {
        if (string.Equals(path, Stats, StringComparison.OrdinalIgnoreCase))
            return true;
        if (ListPaths.Contains(path))
            return true;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 3
            && string.Equals(segments[0], "admin", StringComparison.OrdinalIgnoreCase)
            && EntityTypes.Contains(segments[1])
            && GuidSegment.IsMatch(segments[2]))
        {
            return true;
        }

        if (segments.Length == 3
            && string.Equals(segments[0], "admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[1], "audit", StringComparison.OrdinalIgnoreCase)
            && GuidSegment.IsMatch(segments[2]))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsForbiddenReturnCharacters(string path)
    {
        if (path.Contains('\\') || path.Contains("//", StringComparison.Ordinal))
            return true;

        var colon = path.IndexOf(':');
        var firstSlash = path.IndexOf('/');
        if (colon >= 0 && (firstSlash < 0 || colon < firstSlash))
            return true;

        var segments = path.Split('/');
        foreach (var segment in segments)
        {
            if (segment == "..")
                return true;
        }

        foreach (var ch in path)
        {
            var category = char.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                return true;
        }

        return false;
    }
}

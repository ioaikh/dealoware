using System.Globalization;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Admin;

/// <summary>
/// Route-prefix note r3 §5 / §10 C6 return-path allowlist. Failures become /admin/.
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

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(candidate.Replace("+", "%2B", StringComparison.Ordinal));
        }
        catch (UriFormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (decoded.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase)
            || decoded.Contains("%25", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var queryIndex = decoded.IndexOf('?', StringComparison.Ordinal);
        var pathPart = queryIndex >= 0 ? decoded[..queryIndex] : decoded;
        var queryPart = queryIndex >= 0 ? decoded[queryIndex..] : string.Empty;

        // Spec has not locked query keys. Unknown keys fail closed to Stats.
        if (queryPart.Length > 0)
            return false;

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

        path = pathPart;
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

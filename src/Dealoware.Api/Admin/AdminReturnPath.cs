using System.Globalization;
using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §5 / §10 C6 return-path allowlist. Failures resolve to /admin/.
/// </summary>
public static class AdminReturnPath
{
    public const string CanonicalStats = "/admin/";
    public const string FixedOrigin = "https://admin.core.dealoware.com";

    private static readonly string[] ListPaths =
    [
        "/admin/participants",
        "/admin/artifacts",
        "/admin/negotiations",
        "/admin/offers"
    ];

    public static string Resolve(string? raw)
        => TryValidate(raw, out var path) ? path : CanonicalStats;

    public static bool TryValidate(string? raw, out string validated)
    {
        validated = CanonicalStats;
        if (string.IsNullOrEmpty(raw) || raw.Length > 2048)
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(raw);
        }
        catch (UriFormatException)
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

        if (decoded.Length == 0 || decoded[0] != '/')
        {
            return false;
        }

        if (decoded.Length > 1 && (decoded[1] is '/' or '\\'))
        {
            return false;
        }

        if (decoded.Contains('\\') || ContainsCcOrCf(decoded) || decoded.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var colon = decoded.IndexOf(':');
        var firstSlash = decoded.IndexOf('/');
        if (colon >= 0 && colon < firstSlash)
        {
            return false;
        }

        var pathAndQuery = decoded.Split('?', 2);
        var path = pathAndQuery[0];
        if (string.Equals(path, "/admin", StringComparison.OrdinalIgnoreCase))
        {
            path = CanonicalStats;
        }

        if (!IsAllowed(path))
        {
            return false;
        }

        validated = path;
        return true;
    }

    private static bool IsAllowed(string path)
    {
        if (string.Equals(path, CanonicalStats, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/admin/audit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var list in ListPaths)
        {
            if (string.Equals(path, list, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var list in ListPaths)
        {
            if (path.StartsWith(list + "/", StringComparison.OrdinalIgnoreCase))
            {
                var id = path[(list.Length + 1)..];
                return Guid.TryParse(id, out _);
            }
        }

        const string auditPrefix = "/admin/audit/";
        if (path.StartsWith(auditPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(path[auditPrefix.Length..], out _);
        }

        return false;
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

using System.Globalization;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Admin;

/// <summary>r5 §5 / §10 C6 (sha c5e9d232) return-path allowlist. Failures become /admin/.</summary>
public static class AdminReturnPath
{
    public const string CanonicalHome = "/admin/";
    public const string FixedOrigin = "https://admin.core.dealoware.com";

    private static readonly HashSet<string> ExactAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin/",
        "/admin/participants",
        "/admin/artifacts",
        "/admin/negotiations",
        "/admin/offers",
        "/admin/audit"
    };

    private static readonly Regex EntityId = new(
        "^/admin/(participants|artifacts|negotiations|offers)/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AuditId = new(
        "^/admin/audit/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Resolve(string? candidate)
        => TryValidate(candidate, out var path) ? path : CanonicalHome;

    public static string ToLocation(string? candidate)
        => FixedOrigin + Resolve(candidate);

    public static bool TryValidate(string? candidate, out string path)
    {
        path = CanonicalHome;
        if (string.IsNullOrEmpty(candidate) || candidate.Length > 2048)
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(candidate);
        }
        catch (UriFormatException)
        {
            return false;
        }

        var lower = decoded.ToLowerInvariant();
        if (lower.Contains("%2f") || lower.Contains("%5c") || lower.Contains("%2e%2e") || lower.Contains("%25"))
        {
            return false;
        }

        var pathPart = decoded;
        var query = string.Empty;
        var q = decoded.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            pathPart = decoded[..q];
            query = decoded[q..];
        }

        if (string.Equals(pathPart, "/admin", StringComparison.OrdinalIgnoreCase))
        {
            pathPart = CanonicalHome;
        }

        if (pathPart.Length == 0 || pathPart[0] != '/' || pathPart.StartsWith("//", StringComparison.Ordinal)
            || pathPart.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (pathPart.Contains('\\') || pathPart.Contains("..", StringComparison.Ordinal)
            || pathPart.Contains(':') && pathPart.IndexOf(':') < pathPart.IndexOf('/'))
        {
            return false;
        }

        foreach (var c in decoded)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return false;
            }
        }

        if (!IsAllowed(pathPart))
        {
            return false;
        }

        path = pathPart + query;
        return true;
    }

    private static bool IsAllowed(string pathPart)
        => ExactAllowed.Contains(pathPart) || EntityId.IsMatch(pathPart) || AuditId.IsMatch(pathPart);
}

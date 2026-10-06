namespace Dealoware.Api.Admin;

/// <summary>
/// Admin page inventory and return-path allowlist (route note r3 b079a814).
/// Every page is under /admin/.... Cookie Path stays /admin. No top-level pages.
/// </summary>
public static class AdminPageRoutes
{
    public const string Prefix = "/admin";

    public const string Stats = "/admin";
    public const string Participants = "/admin/participants";
    public const string Artifacts = "/admin/artifacts";
    public const string Negotiations = "/admin/negotiations";
    public const string Offers = "/admin/offers";

    public static readonly IReadOnlyList<string> ReturnPathAllowlist =
    [
        Stats,
        Participants,
        Artifacts,
        Negotiations,
        Offers
    ];

    public static readonly IReadOnlyList<string> EntityTypes =
    [
        "participants",
        "artifacts",
        "negotiations",
        "offers"
    ];

    public static string List(string entityType) => $"{Prefix}/{entityType}";

    public static string Detail(string entityType, Guid id) => $"{Prefix}/{entityType}/{id:D}";

    /// <summary>
    /// Same-origin return path check. Allowlist is /admin and /admin/{type} plus
    /// /admin/{type}/{id} for the four entity types only.
    /// </summary>
    public static bool IsAllowedReturnPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var path = raw.Trim();
        if (path.Length > 2048)
            return false;
        if (!path.StartsWith('/') || path.StartsWith("//") || path.StartsWith("/\\"))
            return false;
        if (path.Contains('\\') || path.Contains("..") || path.Contains(':'))
            return false;
        if (path.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%25", StringComparison.OrdinalIgnoreCase))
            return false;

        var trimmed = path.TrimEnd('/');
        if (trimmed.Length == 0)
            trimmed = "/";

        if (ReturnPathAllowlist.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            return true;

        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;
        if (!string.Equals(parts[0], "admin", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!EntityTypes.Contains(parts[1], StringComparer.OrdinalIgnoreCase))
            return false;
        return Guid.TryParse(parts[2], out _);
    }
}

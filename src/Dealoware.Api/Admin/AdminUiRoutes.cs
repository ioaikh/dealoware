namespace Dealoware.Api.Admin;

/// <summary>
/// Locked admin page routes (Chief Architect route-prefix note r3, sha b079a814).
/// Pages live under /admin/...; the read API stays under /admin/api/...;
/// the cookie Path is /admin; return-path allowlist uses these page paths.
/// Canonical stats is /admin/; /admin redirects to it. No top-level admin pages.
/// </summary>
public static class AdminUiRoutes
{
    public const string Prefix = "/admin";
    public const string ApiPrefix = "/admin/api";
    public const string SignedInAssetsPrefix = "/admin/ui";
    public const string SignedOutAssetsPrefix = "/admin/auth";
    public const string TestHarnessPrefix = "/admin/ui-test";
    public const string TestHarnessFlag = "DEALOWARE_ADMIN_UI_TEST";
    public const string TestHarnessSessionFile = "DEALOWARE_ADMIN_UI_TEST_SESSION_FILE";

    public const string Stats = "/admin/";
    public const string Participants = "/admin/participants";
    public const string Artifacts = "/admin/artifacts";
    public const string Negotiations = "/admin/negotiations";
    public const string Offers = "/admin/offers";
    public const string SignOut = "/admin/sign-out";
    public const string Audit = "/admin/audit";
    public const string SecuritySettings = "/admin/settings/security";

    /// <summary>
    /// Return-path allowlist for later sign-in restore (Step 14). Exact paths only.
    /// </summary>
    public static readonly string[] ReturnPathAllowlist =
    [
        Stats,
        Participants,
        Artifacts,
        Negotiations,
        Offers,
        Audit
    ];

    public static string Detail(string type, Guid id) => $"{Prefix}/{type}/{id:D}";

    public static bool IsPagePath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return value.Equals(Prefix, StringComparison.OrdinalIgnoreCase)
               || value.Equals(Stats, StringComparison.OrdinalIgnoreCase)
               || value.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase);
    }
}

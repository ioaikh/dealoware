namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Configuration options for admin host gating.
/// </summary>
public sealed class AdminHostOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "AdminHost";

    /// <summary>
    /// Production admin host. Outside Development the allowlist must be exactly this value.
    /// </summary>
    public const string ProductionAdminHost = "admin.core.dealoware.com";

    /// <summary>
    /// Allowed host names for admin routes.
    /// Default: admin.core.dealoware.com per Spec §3.
    /// For development/test, additional hosts can be configured.
    /// </summary>
    public List<string> AllowedHosts { get; set; } = new() { ProductionAdminHost };

    /// <summary>
    /// Whether to enforce host validation. Set to false only in Development.
    /// </summary>
    public bool EnforceHostValidation { get; set; } = true;

    /// <summary>
    /// Exact, case-insensitive host match against <see cref="AllowedHosts"/>.
    /// </summary>
    public bool IsAllowedHost(string? hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName) || AllowedHosts is null || AllowedHosts.Count == 0)
            return false;

        foreach (var allowed in AllowedHosts)
        {
            if (string.Equals(allowed, hostName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

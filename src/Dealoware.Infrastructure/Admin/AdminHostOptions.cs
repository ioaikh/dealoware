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
    /// Allowed host names for admin routes.
    /// Default: admin.core.dealoware.com per Spec §3.
    /// For development/test, additional hosts can be configured.
    /// </summary>
    public List<string> AllowedHosts { get; set; } = new() { "admin.core.dealoware.com" };

    /// <summary>
    /// Whether to enforce host validation. Set to false only in Development.
    /// </summary>
    public bool EnforceHostValidation { get; set; } = true;
}

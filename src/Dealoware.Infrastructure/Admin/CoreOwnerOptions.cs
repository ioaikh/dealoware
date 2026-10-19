namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Configuration options for the CoreOwner principal.
/// </summary>
public sealed class CoreOwnerOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "CoreOwner";

    /// <summary>
    /// The email address of the single system superadmin.
    /// Default: io@aiknowhow.com per Spec §8.1.
    /// </summary>
    public string Email { get; set; } = "io@aiknowhow.com";
}

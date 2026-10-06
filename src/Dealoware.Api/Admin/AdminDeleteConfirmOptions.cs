namespace Dealoware.Api.Admin;

public sealed class AdminDeleteConfirmOptions
{
    public const string SectionName = "Admin:DeleteConfirm";

    /// <summary>Single-use token lifetime. Spec §9 / SA §3.4: 5 minutes.</summary>
    public int LifetimeMinutes { get; set; } = 5;

    /// <summary>Optional seconds override for local UI tests. 0 means use LifetimeMinutes.</summary>
    public int LifetimeSeconds { get; set; }
}

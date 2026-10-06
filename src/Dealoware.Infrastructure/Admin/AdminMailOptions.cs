namespace Dealoware.Infrastructure.Admin;

public sealed class AdminMailOptions
{
    public const string SectionName = "AdminMail";

    /// <summary>
    /// Public origin for bootstrap page links. Pages live under /admin/... .
    /// Env name only: DEALOWARE_ADMIN_PUBLIC_ORIGIN.
    /// </summary>
    public string PublicOrigin { get; set; } = "https://admin.core.dealoware.com";
}

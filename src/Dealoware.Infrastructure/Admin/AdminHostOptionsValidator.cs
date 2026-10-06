namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Startup fail-closed checks for the admin host allowlist (A11 items 3 and 4).
/// Outside Development, host validation must stay on and the allowlist must be
/// exactly <see cref="AdminHostOptions.ProductionAdminHost"/> so an env var such as
/// <c>AdminHost__AllowedHosts__1</c> cannot extend it.
/// </summary>
public static class AdminHostOptionsValidator
{
    public const string EnforceHostValidationMessage =
        "AdminHost:EnforceHostValidation is false. This is not allowed outside Development. Refusing to start.";

    public const string AllowedHostsMessage =
        "AdminHost:AllowedHosts must be exactly admin.core.dealoware.com outside Development. " +
        "Extra or replacement hosts are refused. Refusing to start.";

    /// <summary>
    /// Validates bound admin-host options. Development is unrestricted.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown outside Development when enforcement is off or the allowlist is not exactly
    /// <see cref="AdminHostOptions.ProductionAdminHost"/>.
    /// </exception>
    public static void Validate(AdminHostOptions? options, bool isDevelopment)
    {
        if (isDevelopment)
            return;

        options ??= new AdminHostOptions();

        if (!options.EnforceHostValidation)
            throw new InvalidOperationException(EnforceHostValidationMessage);

        if (!IsExactlyProductionAdminHost(options.AllowedHosts))
            throw new InvalidOperationException(AllowedHostsMessage);
    }

    private static bool IsExactlyProductionAdminHost(IReadOnlyList<string>? hosts)
    {
        // Binders may append the JSON array onto a property default, so compare
        // the distinct effective allowlist rather than raw slot count.
        if (hosts is null || hosts.Count == 0)
            return false;

        string? only = null;
        foreach (var host in hosts)
        {
            if (string.IsNullOrWhiteSpace(host))
                continue;

            if (only is null)
            {
                only = host;
                continue;
            }

            if (!string.Equals(only, host, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return only is not null
            && string.Equals(
                only,
                AdminHostOptions.ProductionAdminHost,
                StringComparison.OrdinalIgnoreCase);
    }
}

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

    /// <summary>Said once at start-up, outside Development, when no owner address is configured.</summary>
    public const string MissingEmailWarningText =
        "CoreOwner__Email is not set: the admin console has no owner, so nobody can sign in and bootstrap-link issues nothing.";

    /// <summary>
    /// The email address of the single system superadmin (the owner address). It is not kept in
    /// the code or the shipped settings: the environment supplies it (<c>CoreOwner__Email</c>,
    /// which the stack binds from a secret). When it is missing, nobody is the owner: sign-in is
    /// refused and bootstrap-link issues no link.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The start-up warning for a missing owner address, or null. Never names an address.</summary>
    public static string? MissingEmailWarning(string? email, string? environmentName)
    {
        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return null;

        return string.IsNullOrWhiteSpace(email) ? MissingEmailWarningText : null;
    }
}

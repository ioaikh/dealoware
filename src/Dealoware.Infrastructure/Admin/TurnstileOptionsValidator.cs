namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Start-up checks for Turnstile. Outside Development the siteverify URL must
/// be Cloudflare's, so a stand-in verifier (as the local browser tests use)
/// can never answer for a deployed admin console; that one refuses to start.
/// A missing public site key does not: the pages fail closed on their own,
/// and start-up says so once in the log.
/// </summary>
public static class TurnstileOptionsValidator
{
    public const string VerifyUrlMessage =
        "Admin:Turnstile:VerifyUrl must be the Cloudflare siteverify URL outside Development. " +
        "A replacement verifier is refused. Refusing to start.";

    /// <summary>Names the setting to fix. Holds no value from configuration, so no secret.</summary>
    public const string SiteKeyMissingWarning =
        "Admin:Turnstile:SiteKey is empty. The admin setup and sign-in pages cannot be submitted " +
        "until Admin__Turnstile__SiteKey is set to the public Turnstile site key.";

    /// <exception cref="InvalidOperationException">
    /// Thrown outside Development when <see cref="TurnstileOptions.VerifyUrl"/>
    /// is not exactly <see cref="TurnstileOptions.DefaultVerifyUrl"/>.
    /// </exception>
    public static void Validate(TurnstileOptions? options, string? environmentName)
    {
        if (IsDevelopment(environmentName))
            return;

        options ??= new TurnstileOptions();
        if (!string.Equals(options.VerifyUrl, TurnstileOptions.DefaultVerifyUrl, StringComparison.Ordinal))
            throw new InvalidOperationException(VerifyUrlMessage);
    }

    /// <summary>
    /// The warning to log once at start-up, or null when there is nothing to
    /// say: the site key is set, or this is Development, where an empty key
    /// is the normal local state.
    /// </summary>
    public static string? SiteKeyWarning(TurnstileOptions? options, string? environmentName)
    {
        if (IsDevelopment(environmentName))
            return null;

        return (options ?? new TurnstileOptions()).ConfiguredSiteKey() is null
            ? SiteKeyMissingWarning
            : null;
    }

    private static bool IsDevelopment(string? environmentName)
        => string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
}

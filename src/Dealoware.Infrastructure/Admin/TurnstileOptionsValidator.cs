namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Startup fail-closed check for Turnstile verification. Outside Development
/// the siteverify URL must be Cloudflare's, so a stand-in verifier (as the
/// local browser tests use) can never answer for a deployed admin console.
/// </summary>
public static class TurnstileOptionsValidator
{
    public const string VerifyUrlMessage =
        "Admin:Turnstile:VerifyUrl must be the Cloudflare siteverify URL outside Development. " +
        "A replacement verifier is refused. Refusing to start.";

    /// <exception cref="InvalidOperationException">
    /// Thrown outside Development when <see cref="TurnstileOptions.VerifyUrl"/>
    /// is not exactly <see cref="TurnstileOptions.DefaultVerifyUrl"/>.
    /// </exception>
    public static void Validate(TurnstileOptions? options, string? environmentName)
    {
        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return;

        options ??= new TurnstileOptions();
        if (!string.Equals(options.VerifyUrl, TurnstileOptions.DefaultVerifyUrl, StringComparison.Ordinal))
            throw new InvalidOperationException(VerifyUrlMessage);
    }
}

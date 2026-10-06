namespace Dealoware.Infrastructure.Admin;

public sealed class TurnstileOptions
{
    public const string SectionName = "Admin:Turnstile";
    public const string SecretEnvironmentVariable = "DEALOWARE_TURNSTILE_SECRET";
    public const string DefaultVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>Cloudflare siteverify URL. Override in tests; never a secret.</summary>
    public string VerifyUrl { get; set; } = DefaultVerifyUrl;
}

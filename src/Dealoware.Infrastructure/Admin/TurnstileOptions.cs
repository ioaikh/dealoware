namespace Dealoware.Infrastructure.Admin;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>
    /// Env name for the Turnstile secret. Never a site key or secret value.
    /// </summary>
    public const string SecretEnvironmentVariable = "DEALOWARE_TURNSTILE_SECRET";

    public string? Secret { get; set; }
}

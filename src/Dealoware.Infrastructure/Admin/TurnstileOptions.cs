namespace Dealoware.Infrastructure.Admin;

public sealed class TurnstileOptions
{
    public const string SectionName = "Admin:Turnstile";
    public const string SecretEnvironmentVariable = "DEALOWARE_TURNSTILE_SECRET";
    public const string DefaultVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>
    /// Cloudflare siteverify URL. Never a secret. Only Development may point
    /// it elsewhere (see <see cref="TurnstileOptionsValidator"/>).
    /// </summary>
    public string VerifyUrl { get; set; } = DefaultVerifyUrl;

    /// <summary>Required siteverify hostname. Defaults to the production admin host.</summary>
    public string ExpectedHostname { get; set; } = AdminHostOptions.ProductionAdminHost;

    /// <summary>
    /// Optional widget action. When set, a successful siteverify must return
    /// the same action; when empty, action is not checked.
    /// </summary>
    public string? ExpectedAction { get; set; }

    /// <summary>
    /// PUBLIC widget site key (<c>Admin:Turnstile:SiteKey</c>, environment
    /// variable <c>Admin__Turnstile__SiteKey</c>). It is rendered into the
    /// signed-out pages that carry the widget and is not a secret. The secret
    /// stays in <see cref="SecretEnvironmentVariable"/> and is never rendered.
    /// When this is empty those pages fail closed: the form cannot be submitted.
    /// </summary>
    public string? SiteKey { get; set; }

    /// <summary>The site key to render, or null when none is configured.</summary>
    public string? ConfiguredSiteKey()
        => string.IsNullOrWhiteSpace(SiteKey) ? null : SiteKey.Trim();

    /// <summary>
    /// The widget action to render, or null when none is configured. Returned
    /// exactly as configured, because siteverify compares it exactly.
    /// </summary>
    public string? ConfiguredAction()
        => string.IsNullOrWhiteSpace(ExpectedAction) ? null : ExpectedAction;
}

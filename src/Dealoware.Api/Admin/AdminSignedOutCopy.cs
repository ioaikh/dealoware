namespace Dealoware.Api.Admin;

/// <summary>
/// Copy shared by the signed-out admin pages: the security check (Turnstile)
/// states, the password helper and the show toggle. No duration, attempt
/// count or lock wording. Rule messages themselves come from the server
/// (<see cref="Dealoware.Domain.Admin.AdminPasswordRules"/>).
/// </summary>
public static class AdminSignedOutCopy
{
    public const string TurnstileLoading = "Security check is loading.";

    public const string TurnstileFailed = "The security check failed.";

    public const string TurnstileExpired = "The security check expired.";

    public const string TurnstileUnavailable =
        "The security check is unavailable. Check your network or disable extensions that block scripts, then retry.";

    /// <summary>No site key is configured: the form stays shut and nothing can be retried.</summary>
    public const string TurnstileNotConfigured = "The security check is unavailable.";

    public const string TurnstileRetry = "Retry";

    /// <summary>States the length rule of AdminPasswordRules (MinCodePoints to MaxCodePoints).</summary>
    public const string PasswordHelper =
        "Use 15 to 128 characters. Avoid common or account-related words.";

    public const string PasswordMismatch = "These passwords don't match.";

    /// <summary>
    /// The toggle's one label. It does not change when pressed: the button is
    /// named after its field ("Show New password") and aria-pressed says
    /// whether the password is shown.
    /// </summary>
    public const string ShowPassword = "Show";

    /// <summary>"Try again" while a link check is running again (the reset page's wording).</summary>
    public const string Checking = "Checking…";
}

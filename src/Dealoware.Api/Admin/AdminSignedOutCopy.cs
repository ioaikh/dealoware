namespace Dealoware.Api.Admin;

/// <summary>
/// Copy shared by the signed-out admin pages: the security check (Turnstile)
/// states, the password helper and the Show/Hide toggle. No duration, attempt
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

    public const string ShowPassword = "Show";

    public const string HidePassword = "Hide";
}

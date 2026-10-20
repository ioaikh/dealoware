namespace Dealoware.Api.Admin;

/// <summary>
/// Neutral copy for password-reset API replies. Known, unknown, and locked
/// accounts share the request body. Replay, expired, and random tokens share
/// the invalid-link body.
/// </summary>
public static class AdminPasswordResetCopy
{
    public const string RequestAccepted = "If that account exists, we've sent instructions.";

    public const string InvalidLink = "This link is no longer valid. Request a new one.";

    public const string SignInGeneric =
        "We couldn't sign you in. Check your details and try again later. You can also reset your password.";

    public const string CompleteFailed = "We couldn't complete this reset. Check your details and try again.";

    public const string CompleteSucceeded =
        "Your password has been changed. Sign in with your new password and authenticator code.";

    public const string ConfirmPageTitle = "Set a new password · Dealoware admin";

    public const string ConfirmHeading = "Set a new password";

    public const string BootstrapConfirmHeading = "Set your admin password";

    public const string BootstrapConfirmPageTitle = "Set your admin password · Dealoware admin";

    public const string SetupConfirmScriptPath = "/admin/auth/setup-confirm.js";

    public const string LinkExpiredPageTitle = "This link no longer works · Dealoware admin";

    public const string LinkExpiredHeading = "This link no longer works";

    public const string LinkExpiredResetBody =
        "Reset links work once and expire after 1 hour.";

    public const string LinkExpiredResetAction = "Request a new link";

    public const string LinkExpiredSetupBody =
        "Ask the operator for a new bootstrap link. This page cannot send another one.";

    public const string ConfirmNoscript = "Turn on JavaScript to set your password.";

    public const string CheckingLink = "Checking your link…";

    public const string CheckRetry =
        "We couldn't check your link. Wait a moment and try again.";

    public const string TryAgain = "Try again";

    public const string ConfirmScriptPath = "/admin/auth/reset-confirm.js";
}

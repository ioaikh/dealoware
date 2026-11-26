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

    // ---- S-A8: ask for a reset link ----

    public const string RequestPageTitle = "Reset password · Dealoware admin";

    public const string RequestHeading = "Reset password";

    public const string RequestLede = "Request an email link to set a new password.";

    public const string RequestSubmit = "Send reset link";

    // ---- S-A9: the link is on its way, if there is an account ----

    public const string SentPageTitle = "Check your email · Dealoware admin";

    public const string SentHeading = "Check your email";

    /// <summary>States the reset link lifetime (Spec §8.4: one hour).</summary>
    public const string SentExpiry = "The link expires in 1 hour.";

    // ---- S-A10: set the new password ----

    public const string ConfirmPageTitle = "Set a new password · Dealoware admin";

    public const string ConfirmLede =
        "Choose a new password and confirm with an authenticator or recovery code.";

    public const string ConfirmHeading = "Set a new password";

    public const string ConfirmSubmit = "Change password";

    /// <summary>
    /// Shown when no usable answer came back and the link is still usable,
    /// so nothing was changed: the first-time setup page's own sentence.
    /// </summary>
    public const string ConfirmRetry = AdminBootstrapCopy.SubmitRetry;

    public const string LinkExpiredPageTitle = "This link no longer works · Dealoware admin";

    public const string LinkExpiredHeading = "This link no longer works";

    public const string LinkExpiredResetBody =
        "Reset links work once and expire after 1 hour.";

    public const string LinkExpiredResetAction = "Request a new link";

    public const string LinkExpiredSetupBody =
        "Setup links work once and expire after 24 hours. Ask for a new setup link through the same channel you got this one.";

    public const string ConfirmNoscript = "Turn on JavaScript to set your password.";

    public const string RequestNoscript = "Turn on JavaScript to request a reset link.";

    public const string CheckingLink = "Checking your link…";

    public const string CheckRetry =
        "We couldn't check your link. Wait a moment and try again.";

    public const string TryAgain = "Try again";

    public const string ConfirmScriptPath = "/admin/auth/reset-confirm.js";
}

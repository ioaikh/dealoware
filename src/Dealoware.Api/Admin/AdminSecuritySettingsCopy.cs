namespace Dealoware.Api.Admin;

/// <summary>
/// Sentences the S-A12 security settings API answers with (T04). The page
/// (a later piece) shows them; the machine-readable <c>reason</c> next to each
/// is what it decides on.
/// </summary>
public static class AdminSecuritySettingsCopy
{
    /// <summary>S-A12 password-change note v1.4: the one generic step-up failure. Never names the factor.</summary>
    public const string StepUpFailed = "We couldn't confirm it's you. Check your current password and code.";

    /// <summary>S-A12 password-change note v1.4: the success copy shown on S-A1.</summary>
    public const string PasswordChanged =
        "Your password has been changed. Sign in with your new password and authenticator code.";

    /// <summary>
    /// New sentence (no document gives one): the step-up is missing or older
    /// than five minutes, so the page asks for the current password and a code again.
    /// </summary>
    public const string StepUpRequired = "Confirm it's you to continue.";

    /// <summary>
    /// New sentence (no document gives one): the code from the new
    /// authenticator key did not match while re-enrolling.
    /// </summary>
    public const string NewCodeFailed = "That code didn't work. Enter the current code from your authenticator app.";

    public const string ReasonStepUpFailed = "step_up_failed";
    public const string ReasonStepUpRequired = "step_up_required";
    public const string ReasonSignedOut = "signed_out";
    public const string ReasonCodeFailed = "code_failed";
}

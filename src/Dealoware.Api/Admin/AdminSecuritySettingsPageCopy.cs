using System.Globalization;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Copy of the S-A12 security settings page and of the UXR-A40 recovery-code
/// banner (T04). Where a document gives the words they are cited; the
/// sentences the server actions answer with are in
/// <see cref="AdminSecuritySettingsCopy"/> and the reused sign-in, set-up and
/// password words in their own classes. Every sentence written new for this
/// piece is marked "New copy".
/// </summary>
public static class AdminSecuritySettingsPageCopy
{
    // ---- Page ----

    /// <summary>New copy: what a browser without JavaScript sees under the heading.</summary>
    public const string Noscript = "Turn on JavaScript to change your security settings.";

    // ---- Password (S-A12 note v1.4, UXR-A42) ----

    /// <summary>Note v1.4: section "Password".</summary>
    public const string PasswordHeading = "Password";

    /// <summary>Note v1.4: field (1).</summary>
    public const string CurrentPasswordLabel = "Current password";

    /// <summary>Note v1.4: field (2), the first-setup page's label.</summary>
    public const string NewPasswordLabel = AdminBootstrapCopy.NewPasswordLabel;

    /// <summary>Note v1.4: field (3).</summary>
    public const string ConfirmNewPasswordLabel = "Confirm new password";

    /// <summary>Note v1.4: control "Change password".</summary>
    public const string ChangePassword = "Change password";

    // ---- Recovery codes (TD-ADM-UI-auth-18, TD-ADM-171) ----

    /// <summary>S-A7's heading.</summary>
    public const string RecoveryCodesHeading = AdminEnrollmentCopy.RecoveryCodesHeading;

    /// <summary>TD-ADM-UI-auth-18: "'N codes left' matches the store". {0} is the server's count.</summary>
    public const string CodesLeftMany = "{0} codes left";

    /// <summary>The same, for one code.</summary>
    public const string CodesLeftOne = "1 code left";

    /// <summary>New copy: what "Create new codes" does to the codes the owner has now (TD-ADM-171).</summary>
    public const string NewCodesReplace = "New codes replace all of your current codes. Your old codes stop working.";

    /// <summary>TD-ADM-UI-auth-18 step 2.</summary>
    public const string CreateNewCodes = "Create new codes";

    // ---- Authenticator (TD-ADM-UI-auth-18) ----

    /// <summary>New copy: the section heading.</summary>
    public const string AuthenticatorHeading = "Authenticator";

    /// <summary>New copy: what "Set up again" is for, and that nothing changes before the confirm.</summary>
    public const string AuthenticatorLede =
        "Set up your authenticator app again, for example on a new phone. Your current app keeps working until you confirm a code from the new one.";

    /// <summary>TD-ADM-UI-auth-18 step 1.</summary>
    public const string SetUpAgain = "Set up again";

    /// <summary>New copy: shown with the new codes once a re-enrolment is confirmed.</summary>
    public const string AuthenticatorChanged = "Your authenticator app has been changed. Your old recovery codes no longer work.";

    /// <summary>New copy: leaves a re-enrolment that was started and not confirmed.</summary>
    public const string Cancel = "Cancel";

    // ---- Step-up (TD-ADM-174) ----

    /// <summary>New copy, from the server's "Confirm it's you to continue.": the step-up dialog's heading.</summary>
    public const string StepUpHeading = "Confirm it's you";

    /// <summary>New copy: what the step-up asks for.</summary>
    public const string StepUpLede = "Enter your current password and a code from your authenticator app.";

    /// <summary>S-A1's primary button word.</summary>
    public const string StepUpSubmit = AdminSignInCopy.SignInSubmit;

    // ---- Recent sign-in activity (TD-ADM-UI-auth-19, UXR-A43, UXR-A44) ----

    /// <summary>TD-ADM-UI-auth-19.</summary>
    public const string ActivityHeading = "Recent sign-in activity";

    /// <summary>TD-ADM-UI-auth-19: the empty state.</summary>
    public const string ActivityEmpty = "No sign-in activity yet.";

    /// <summary>New copy: the table caption (last 20, times in ET).</summary>
    public const string ActivityCaption = "Your last 20 sign-in events. Times are Eastern Time (ET).";

    /// <summary>New copy: column headers.</summary>
    public const string ActivityTime = "Time";

    public const string ActivityEvent = "Event";

    /// <summary>New copy: the Network column holds a short prefix of the keyed hash, never an address.</summary>
    public const string ActivityNetwork = "Network (hashed)";

    /// <summary>New copy: the lock notice's text label next to its icon.</summary>
    public const string LockNoticeLabel = "Lock notice:";

    /// <summary>New copy: an active lock. {start} and {end} are the server's times, in ET.</summary>
    public const string LockNoticeActive = "Sign-in to this account has been locked since {start}. It opens again at {end}.";

    /// <summary>New copy: a lock that ended less than 24 hours ago.</summary>
    public const string LockNoticeEnded = "Sign-in to this account was locked from {start} to {end}.";

    /// <summary>
    /// The events the panel lists (<see cref="AdminSecuritySettingsService.ActivityActions"/>) and
    /// their labels. "Code failed after correct password" is the lockout note's, as TD-ADM-UI-auth-19
    /// quotes it; every other label is new copy.
    /// </summary>
    public static readonly IReadOnlyList<(string Event, string Label)> ActivityLabels =
    [
        (AdminAuthEvents.LoginSuccess, "Signed in"),
        (AdminAuthEvents.LoginFailure, "Sign-in failed"),
        (AdminAuthEvents.SecondFactorFailed, "Code failed after correct password"),
        (AdminAuthAction.LockStart, "Account locked"),
        (AdminAuthEvents.RecoveryCodeUse, "Recovery code used"),
        (AdminAuditActions.ResetComplete, "Password reset"),
        (AdminAuditActions.TotpEnroll, "Authenticator set up"),
        (AdminAuditActions.TotpChange, "Authenticator changed"),
        (AdminAuditActions.TotpChangeFailed, "Authenticator change failed"),
        (AdminAuditActions.PasswordChanged, "Password changed"),
        (AdminAuditActions.PasswordChangeFailed, "Password change failed"),
        (AdminAuditActions.StepUpFailed, "Identity check failed"),
        (AdminAuditActions.RecoveryCodesRegenerated, "New recovery codes created")
    ];

    // ---- UXR-A40 recovery-code banner (TD-ADM-UI-auth-16) ----

    /// <summary>New copy: the banner, with the server's count of unused codes.</summary>
    public static string RecoveryNotice(int left)
        => left == 1
            ? "You signed in with a recovery code. You have 1 recovery code left."
            : string.Format(
                CultureInfo.InvariantCulture,
                "You signed in with a recovery code. You have {0} recovery codes left.",
                left);

    /// <summary>UXR-A40's link to S-A12.</summary>
    public const string ManageRecoveryCodes = "Manage recovery codes";

    /// <summary>New copy: the banner's dismiss button.</summary>
    public const string Dismiss = "Dismiss";
}

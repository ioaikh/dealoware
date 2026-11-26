namespace Dealoware.Domain.Admin;

/// <summary>
/// Locked action and reason-class names for the append-only admin audit log
/// (Spec §8.8, §10; Dev Plan Step 8).
/// </summary>
public static class AdminAuditActions
{
    public const string LoginSuccess = "login_success";
    public const string LoginFailure = "login_failure";
    public const string ResetRequest = "reset_request";
    public const string ResetComplete = "reset_complete";
    public const string TotpEnroll = "totp_enroll";
    public const string TotpChange = "totp_change";
    public const string RecoveryCodeUse = "recovery_code_use";
    public const string SecondFactorFailed = "signin.second_factor_failed";

    /// <summary>The owner signed out of a signed-in session (TD-ADM-175). No reason class.</summary>
    public const string SignOut = "sign_out";

    public const string EntityEdit = "entity_edit";
    public const string EntityDelete = "entity_delete";

    public const string ReasonBadPassword = "bad_password";
    public const string ReasonBad2Fa = "bad_2fa";
    public const string ReasonLocked = "locked";
    public const string ReasonRateLimited = "rate-limited";
    public const string ReasonCaptchaFailed = "captcha-failed";

    // S-A12 security settings (T04). Names from the S-A12 password-change
    // note v1.4 (account.password_changed / account.password_change_failed);
    // re-enrolment success keeps the Spec §8.8 name TotpChange above.
    public const string PasswordChanged = "account.password_changed";
    public const string PasswordChangeFailed = "account.password_change_failed";
    public const string StepUpFailed = "account.step_up_failed";
    public const string RecoveryCodesRegenerated = "account.recovery_codes_regenerated";
    public const string TotpChangeFailed = "account.totp_change_failed";
}

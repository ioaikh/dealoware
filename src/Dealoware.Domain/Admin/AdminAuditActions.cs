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

    public const string EntityEdit = "entity_edit";
    public const string EntityDelete = "entity_delete";

    public const string ReasonBadPassword = "bad_password";
    public const string ReasonBad2Fa = "bad_2fa";
    public const string ReasonLocked = "locked";
    public const string ReasonRateLimited = "rate-limited";
    public const string ReasonCaptchaFailed = "captcha-failed";
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Audit reason classes for admin auth. Never include secrets or raw IP.
/// </summary>
public static class AdminAuthReason
{
    public const string CaptchaFailed = "captcha-failed";
    public const string BadPassword = "bad_password";
    public const string Bad2Fa = "bad_2fa";
    public const string Locked = "locked";
    public const string RateLimited = "rate-limited";
    public const string InvalidLink = "invalid_link";
}

/// <summary>
/// Audit action names for admin auth events.
/// </summary>
public static class AdminAuthAction
{
    public const string LoginSuccess = "login_success";
    public const string LoginFailure = "login_failure";
    public const string LockStart = "lock_start";
    public const string ResetRequest = "reset_request";
    public const string ResetComplete = "reset_complete";
    public const string BootstrapComplete = "bootstrap_complete";
    public const string LinkRejected = "auth.link_rejected";
    public const string SecondFactorFailed = "signin.second_factor_failed";
}

namespace Dealoware.Domain.Admin;

/// <summary>Audit action and reason-class names for admin auth events (Spec §8.8).</summary>
public static class AdminAuthEvents
{
    public const string LoginSuccess = "login_success";
    public const string LoginFailure = "login_failure";
    public const string TotpEnroll = "totp_enroll";
    public const string RecoveryCodeUse = "recovery_code_use";
    public const string SecondFactorFailed = "signin.second_factor_failed";

    public const string ReasonBadPassword = "bad_password";
    public const string ReasonBad2Fa = "bad_2fa";
}

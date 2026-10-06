namespace Dealoware.Domain.Admin;

/// <summary>
/// Generic auth reply copy. Invalid, expired, and already-used bootstrap
/// tokens share one body so the reply does not reveal which case applied.
/// </summary>
public static class AdminAuthMessages
{
    public const string InvalidOrExpiredLink = "This link is no longer valid.";

    public const string TurnstileFailed = "Verification failed, please try again.";

    public const string PasswordTooShort = "too short";

    public const string PasswordTooLong = "over 128";

    public const string PasswordCommonOrContext = "common or context word";

    public const string PasswordSameAsCurrent = "same as current";

    public const string BootstrapPasswordSetAction = "bootstrap_password_set";

    public const string BootstrapLinkSentAction = "bootstrap_link_sent";

    public const string CaptchaFailedReason = "captcha-failed";

    public const string InvalidLinkReason = "invalid_link";
}

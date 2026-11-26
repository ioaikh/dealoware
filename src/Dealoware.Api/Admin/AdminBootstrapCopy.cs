namespace Dealoware.Api.Admin;

/// <summary>
/// Copy for the first-time setup page (S-A4, /admin/bootstrap). Replies to a
/// submitted form are shown as the server words them; this is only the text
/// the page itself carries.
/// </summary>
public static class AdminBootstrapCopy
{
    public const string PageTitle = "Set password · Dealoware admin";

    public const string Heading = "Set password";

    public const string Lede = "Set the first password for Dealoware admin.";

    public const string NewPasswordLabel = "New password";

    public const string ConfirmPasswordLabel = "Confirm password";

    public const string Submit = "Set password";

    public const string Noscript = AdminPasswordResetCopy.ConfirmNoscript;

    /// <summary>Shown when no usable answer came back (network failure, time-out, unexpected reply).</summary>
    public const string SubmitRetry = "We couldn't set your password. Wait a moment and try again.";

    public const string ScriptPath = "/admin/auth/bootstrap.js";
}

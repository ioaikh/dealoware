namespace Dealoware.Api.Admin;

/// <summary>
/// Copy for the three sign-in pages: S-A1 /admin/sign-in, S-A2
/// /admin/sign-in/code and S-A3 /admin/sign-in/recovery. A refused sign-in is
/// shown as the server words it (<see cref="Dealoware.Domain.Admin.AdminAuthCopy"/>);
/// this is only the text the pages themselves carry. No duration, attempt
/// count or lock wording, and nothing that names which factor failed.
/// </summary>
public static class AdminSignInCopy
{
    // ---- S-A1: email and password ----

    public const string SignInPageTitle = "Sign in · Dealoware admin";

    public const string SignInHeading = "Sign in";

    public const string SignInLede = "Sign in to Dealoware admin.";

    public const string EmailLabel = "Email";

    public const string PasswordLabel = "Password";

    public const string EmailRequired = "Enter an email address.";

    public const string EmailInvalid = "Enter a well-formed email address.";

    public const string PasswordRequired = "Enter a password.";

    /// <summary>
    /// The step 1 button. Step 1 signs nobody in yet, so it is "Continue",
    /// not "Sign in" (Chief UI/UX decision on the S-A1 label, UXR-A08).
    /// </summary>
    public const string SignInSubmit = "Continue";

    public const string ForgotPassword = "Forgot your password?";

    // ---- S-A2: authenticator code ----

    public const string CodePageTitle = "Enter code · Dealoware admin";

    public const string CodeHeading = "Enter code";

    public const string CodeLede = "Enter the 6-digit code from your authenticator app.";

    public const string CodeLabel = "Authenticator code";

    public const string CodeRequired = "Enter the 6-digit code.";

    public const string UseRecoveryCode = "Use a recovery code instead";

    // ---- S-A3: recovery code ----

    public const string RecoveryPageTitle = "Recovery code · Dealoware admin";

    public const string RecoveryHeading = "Recovery code";

    public const string RecoveryLede = "Enter one unused recovery code.";

    public const string RecoveryLabel = "Recovery code";

    public const string RecoveryRequired = "Enter a recovery code.";

    public const string UseAuthenticatorCode = "Use an authenticator code instead";

    // ---- Step 2, both pages ----

    public const string SecondStepSubmit = "Sign in";

    public const string BackToSignIn = "Back to sign in";

    // ---- All three pages ----

    /// <summary>The one sentence shown when JavaScript is off.</summary>
    public const string Noscript = "Turn on JavaScript to sign in.";

    /// <summary>
    /// Shown when no usable answer came back (network failure, time-out, a
    /// server error, an unexpected reply). It must not read like the server's
    /// "we couldn't sign you in": nothing here says the details were wrong.
    /// </summary>
    public const string SubmitRetry = "That didn't go through. Wait a moment and try again.";
}

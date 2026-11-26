namespace Dealoware.Api.Admin;

/// <summary>
/// Copy for the two set-up pages that follow a first sign-in: S-A6
/// /admin/setup/authenticator and S-A7 /admin/setup/recovery-codes. A refused
/// code is shown as the server words it
/// (<see cref="Dealoware.Domain.Admin.AdminAuthCopy"/>); this is only the text
/// the pages themselves carry. No duration, attempt count or lock wording.
/// </summary>
public static class AdminEnrollmentCopy
{
    // ---- S-A6: set up the authenticator ----

    public const string AuthenticatorPageTitle = "Authenticator setup · Dealoware admin";

    public const string AuthenticatorHeading = "Authenticator setup";

    public const string AuthenticatorLede =
        "Scan the QR code with your authenticator app, or enter the key by hand. Then enter a 6-digit code to confirm.";

    /// <summary>The name of the QR code, for a reader that cannot show the picture.</summary>
    public const string QrLabel = "Authenticator QR code for Dealoware admin. Scan with your authenticator app.";

    /// <summary>
    /// The QR code's description, and a line every reader sees: the key under
    /// it carries what the picture carries.
    /// </summary>
    public const string KeyHelp = "If you can't scan the code, enter this key in your app instead.";

    public const string KeyLabel = "Manual key";

    /// <summary>The name the authenticator app lists the key under, and asks for when the key is typed in.</summary>
    public const string AccountNameLabel = "Account name";

    public const string CopyKey = "Copy key";

    public const string CodeLabel = AdminSignInCopy.CodeLabel;

    public const string CodeRequired = AdminSignInCopy.CodeRequired;

    public const string AuthenticatorSubmit = "Confirm";

    public const string AuthenticatorNoscript = "Turn on JavaScript to set up your authenticator.";

    // ---- S-A7: save the recovery codes ----

    public const string RecoveryCodesPageTitle = "Recovery codes · Dealoware admin";

    public const string RecoveryCodesHeading = "Recovery codes";

    public const string RecoveryCodesLede = "Save these codes now. They are shown once.";

    public const string RecoveryCodesPurpose = "Each code signs you in once if you can't use your authenticator app.";

    public const string CopyAllCodes = "Copy all";

    public const string CodesSavedLabel = "I have saved these recovery codes.";

    public const string RecoveryCodesContinue = "Continue";

    public const string RecoveryCodesNoscript = "Turn on JavaScript to see your recovery codes.";

    /// <summary>S-A7 opened again once the codes can no longer be shown.</summary>
    public const string RecoveryCodesAlreadyShown = "Your recovery codes have already been shown. They are shown once.";

    public const string BackToSignIn = AdminSignInCopy.BackToSignIn;

    // ---- Both pages ----

    /// <summary>Said in the live region after a copy button put the text on the clipboard.</summary>
    public const string Copied = "Copied.";

    /// <summary>
    /// Said when the browser would not copy. The text has been selected
    /// instead, so the reader's own copy command works on it.
    /// </summary>
    public const string CopyFailed = "Couldn't copy. The text is selected so you can copy it yourself.";

    /// <summary>Shown when no usable answer came back. The sign-in pages' sentence.</summary>
    public const string SubmitRetry = AdminSignInCopy.SubmitRetry;
}

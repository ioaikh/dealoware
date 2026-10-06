namespace Dealoware.Domain.Admin;

/// <summary>
/// Generic client-facing auth copy (UX1-A03). No duration, attempt count, or "locked".
/// </summary>
public static class AdminAuthCopy
{
    public const string SignInFailure =
        "We couldn't sign you in. Check your details and try again later. You can also reset your password.";

    public const string VerificationFailed = "Verification failed, please try again.";

    public const string ResetExists = "If that account exists, we've sent instructions";

    public const string InvalidLink = "This link is invalid or has expired.";
}

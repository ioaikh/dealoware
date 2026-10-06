namespace Dealoware.Api.Admin;

/// <summary>
/// Neutral copy for password-reset API replies. Known, unknown, and locked
/// accounts share the request body. Replay, expired, and random tokens share
/// the invalid-link body.
/// </summary>
public static class AdminPasswordResetCopy
{
    public const string RequestAccepted = "If that account exists, we've sent instructions";

    public const string InvalidLink = "This link is no longer valid. Request a new one.";

    public const string CompleteFailed = "We couldn't complete this reset. Check your details and try again.";

    public const string CompleteSucceeded =
        "Your password has been updated. Sign in with your authenticator to continue.";
}

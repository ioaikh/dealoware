/**
 * Cited copy. UX1-A03 r2, password-rules note v2.3 named reasons,
 * UXR-A23 / A25 / A34 / A35 / A37 / A39 / A40. UI/UX review still required.
 */
(function (global) {
  "use strict";

  global.AdminCopy = {
    signInFailure:
      "We couldn't sign you in. Check your details and try again later. You can also reset your password.",
    resetLinkText: "reset your password",
    emailRequired: "Enter an email address.",
    emailInvalid: "Enter a well-formed email address.",
    passwordRequired: "Enter a password.",
    codeRequired: "Enter the 6-digit code.",
    recoveryRequired: "Enter a recovery code.",
    passwordMismatch: "These passwords don't match.",
    passwordHelper:
      "Use 15 to 128 characters. Avoid common or account-related words.",
    passwordTooShort: "Use at least 15 characters.",
    passwordTooLong: "Use at most 128 characters.",
    passwordCommon: "This password is too common. Choose another.",
    passwordSameAsCurrent: "Choose a password that is different from your current one.",
    stepUpFailure: "We couldn't confirm it's you. Check your current password and code.",
    passwordChangedStatus:
      "Your password has been changed. Sign in with your new password and authenticator code.",
    resetNeutral:
      "If an account exists for that email, we sent a reset link. The link expires in 1 hour.",
    linkExpired: "This link no longer works.",
    linkExpiredResetAction: "Request a new link",
    linkExpiredBootstrap:
      "Ask the operator for a new bootstrap link. This page cannot send another one.",
    sessionEnded: "Your session has ended. Sign in again to continue.",
    passwordSetStatus: "Your password has been set. Sign in to continue.",
    recoveryBannerPrefix: "You signed in with a recovery code. You have ",
    recoveryBannerSuffix: " recovery codes left.",
    totpWrong: "That code did not work. Try again.",
    turnstileLoading: "Security check is loading.",
    turnstileFailed: "The security check failed.",
    turnstileExpired: "The security check expired.",
    turnstileUnavailable:
      "The security check is unavailable. Check your network or disable extensions that block scripts, then retry.",
    codesSavedLabel: "I have saved these recovery codes.",
    copied: "Copied.",
    downloaded: "Download started."
  };
})(window);

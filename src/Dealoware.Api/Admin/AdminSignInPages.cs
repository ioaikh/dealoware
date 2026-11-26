using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// The three sign-in pages, built on the shared signed-out shell
/// (<see cref="AdminSignedOutPage"/>): S-A1 at /admin/sign-in, S-A2 at
/// /admin/sign-in/code and S-A3 at /admin/sign-in/recovery. They change
/// nothing by themselves: each page's script posts to the JSON routes in
/// <see cref="AdminAuthEndpoints"/> and follows only what the reply says.
/// <para>
/// Everything but the heading is hidden until the page script runs, so with
/// JavaScript off only the heading and one sentence show.
/// </para>
/// <para>
/// Only S-A1 carries the security check, so only its policy names Cloudflare.
/// The session gate serves S-A2 and S-A3 only while a pending sign-in exists;
/// without one it sends the browser to <see cref="SentBackLocation"/> before
/// these handlers run.
/// </para>
/// </summary>
public static class AdminSignInPages
{
    /// <summary>
    /// The fixed flag that follows the sign-in address when the browser is
    /// sent back to step 1 because no pending sign-in was left. It says
    /// nothing about the visitor. The page's first script takes it out of the
    /// address bar, and S-A1 then opens with the server's sign-in sentence.
    /// </summary>
    public const string SentBackFlag = "again";

    /// <summary>Step 1, with the note that it has to be done again. A constant: nothing of the request is in it.</summary>
    public const string SentBackLocation = AdminSignedOutExemptions.SignIn + "#" + SentBackFlag;

    public const string SignInScriptPath = "/admin/auth/sign-in.js";

    /// <summary>One script for both second-step pages; the form says which step it is.</summary>
    public const string SecondStepScriptPath = "/admin/auth/sign-in-code.js";

    /// <summary>The form's <c>data-step</c> value on S-A2 and on S-A3.</summary>
    public const string CodeStep = "code";
    public const string RecoveryStep = "recovery";

    /// <summary>
    /// S-A1. The submit button is disabled in the markup and only a fresh
    /// security-check token enables it; with no site key configured nothing can.
    /// </summary>
    public static IResult SignIn(HttpContext http, AdminAntiForgeryService antiforgery, TurnstileOptions turnstile)
    {
        ArgumentNullException.ThrowIfNull(turnstile);

        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminSignInCopy.SignInPageTitle,
            Heading = AdminSignInCopy.SignInHeading,
            Noscript = AdminSignInCopy.Noscript,
            BodyHtml = SignInBody(turnstile),
            Scripts =
            [
                // First, so a flag leaves the address bar even if a later script fails to load.
                AdminSignedOutPage.ArrivalScriptPath,
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.PasswordScriptPath,
                AdminSignedOutPage.TurnstileScriptPath,
                AdminSignedOutPage.RequestScriptPath,
                SignInScriptPath
            ],
            IssueAntiForgery = true,
            AllowTurnstile = turnstile.ConfiguredSiteKey() is not null
        });
    }

    /// <summary>S-A2: the 6-digit authenticator code. No security check on this step.</summary>
    public static IResult Code(HttpContext http, AdminAntiForgeryService antiforgery)
        => SecondStep(http, antiforgery, new SecondStepPage(
            Title: AdminSignInCopy.CodePageTitle,
            Heading: AdminSignInCopy.CodeHeading,
            Lede: AdminSignInCopy.CodeLede,
            Step: CodeStep,
            PagePath: AdminSignedOutExemptions.SignInCode,
            FieldId: "code",
            FieldName: "code",
            // Numeric keypad; the value stays text so a leading zero is kept.
            FieldAttributes: " inputmode=\"numeric\"",
            Label: AdminSignInCopy.CodeLabel,
            Required: AdminSignInCopy.CodeRequired,
            OtherStepPath: AdminSignedOutExemptions.SignInRecovery,
            OtherStepText: AdminSignInCopy.UseRecoveryCode));

    /// <summary>S-A3: one unused recovery code. Letters and digits, so no input mode is set.</summary>
    public static IResult Recovery(HttpContext http, AdminAntiForgeryService antiforgery)
        => SecondStep(http, antiforgery, new SecondStepPage(
            Title: AdminSignInCopy.RecoveryPageTitle,
            Heading: AdminSignInCopy.RecoveryHeading,
            Lede: AdminSignInCopy.RecoveryLede,
            Step: RecoveryStep,
            PagePath: AdminSignedOutExemptions.SignInRecovery,
            FieldId: "recovery-code",
            FieldName: "recoveryCode",
            FieldAttributes: string.Empty,
            Label: AdminSignInCopy.RecoveryLabel,
            Required: AdminSignInCopy.RecoveryRequired,
            OtherStepPath: AdminSignedOutExemptions.SignInCode,
            OtherStepText: AdminSignInCopy.UseAuthenticatorCode));

    private sealed record SecondStepPage(
        string Title,
        string Heading,
        string Lede,
        string Step,
        string PagePath,
        string FieldId,
        string FieldName,
        string FieldAttributes,
        string Label,
        string Required,
        string OtherStepPath,
        string OtherStepText);

    private static IResult SecondStep(HttpContext http, AdminAntiForgeryService antiforgery, SecondStepPage page)
        => AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = page.Title,
            Heading = page.Heading,
            Noscript = AdminSignInCopy.Noscript,
            BodyHtml = SecondStepBody(page),
            Scripts =
            [
                AdminSignedOutPage.ArrivalScriptPath,
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.RequestScriptPath,
                SecondStepScriptPath
            ],
            IssueAntiForgery = true
        });

    /// <summary>
    /// Email keeps what was typed after a refusal; the password is cleared by
    /// the script. The two "enter…" messages are the browser's own check
    /// (filled, and an email that looks like one); everything else is the
    /// server's answer, shown in the one alert above the form.
    /// <c>data-sent-back</c> is the server's own sign-in sentence, for the
    /// one case where no reply carries it: the browser was sent back to this
    /// page because no pending sign-in was left.
    /// </summary>
    private static string SignInBody(TurnstileOptions turnstile)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            "<div id=\"sign-in-panel\" hidden>" +
            $"<p class=\"lede\">{E(AdminSignInCopy.SignInLede)}</p>" +
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>" +
            $"<form id=\"sign-in-form\" method=\"post\" action=\"{AdminSignedOutExemptions.SignIn}\" novalidate" +
            $" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\"" +
            $" data-sent-back=\"{E(AdminAuthCopy.SignInFailure)}\">" +
            "<div class=\"field\">" +
            $"<label for=\"email\">{E(AdminSignInCopy.EmailLabel)}</label>" +
            "<input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"username\" inputmode=\"email\"" +
            " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"email-error\"" +
            $" data-required=\"{E(AdminSignInCopy.EmailRequired)}\"" +
            $" data-invalid=\"{E(AdminSignInCopy.EmailInvalid)}\">" +
            "<span id=\"email-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            "<div class=\"field\">" +
            $"<label id=\"password-label\" for=\"password\">{E(AdminSignInCopy.PasswordLabel)}</label>" +
            "<div class=\"field-row\">" +
            "<input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required" +
            " aria-describedby=\"password-error\"" +
            $" data-required=\"{E(AdminSignInCopy.PasswordRequired)}\">" +
            AdminSignedOutPage.PasswordToggle("password") +
            "</div>" +
            "<span id=\"password-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            AdminSignedOutPage.TurnstileBlock(turnstile) +
            "<button type=\"submit\" id=\"sign-in-submit\" disabled aria-describedby=\"security-check-status\">" +
            $"{E(AdminSignInCopy.SignInSubmit)}</button>" +
            "<div class=\"links\">" +
            $"<a id=\"reset-link\" href=\"{AdminSignedOutExemptions.Reset}\">{E(AdminSignInCopy.ForgotPassword)}</a>" +
            "</div>" +
            "</form></div>";
    }

    /// <summary>
    /// <c>data-no-sign-in</c> is the session gate's word for "no pending
    /// sign-in" (<see cref="AdminDeny.GenericError"/>). A 401 with that word
    /// means the first step has to be done again; a 401 with any other text
    /// is the server's own sentence and is shown as sent.
    /// "Back to sign in" is a real link, so it works with JavaScript off.
    /// With it on, the page script first asks the server to expire the
    /// pending sign-in's cookie, then puts step 1 in this page's place.
    /// </summary>
    private static string SecondStepBody(SecondStepPage page)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            "<div id=\"second-step-panel\" hidden>" +
            $"<p class=\"lede\">{E(page.Lede)}</p>" +
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>" +
            $"<form id=\"second-step-form\" method=\"post\" action=\"{page.PagePath}\" novalidate" +
            $" data-step=\"{page.Step}\"" +
            $" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\"" +
            $" data-no-sign-in=\"{E(AdminDeny.GenericError)}\">" +
            "<div class=\"field\">" +
            $"<label for=\"{page.FieldId}\">{E(page.Label)}</label>" +
            $"<input id=\"{page.FieldId}\" name=\"{page.FieldName}\" type=\"text\"{page.FieldAttributes}" +
            " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required" +
            $" aria-describedby=\"{page.FieldId}-error\"" +
            $" data-required=\"{E(page.Required)}\">" +
            $"<span id=\"{page.FieldId}-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            $"<button type=\"submit\" id=\"second-step-submit\">{E(AdminSignInCopy.SecondStepSubmit)}</button>" +
            "<div class=\"links\">" +
            $"<a id=\"other-step-link\" href=\"{page.OtherStepPath}\">{E(page.OtherStepText)}</a>" +
            $"<a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">{E(AdminSignInCopy.BackToSignIn)}</a>" +
            "</div>" +
            "</form></div>";
    }
}

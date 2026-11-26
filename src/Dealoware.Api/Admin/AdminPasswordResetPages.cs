using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// The two pages a reset starts with, built on the shared signed-out shell
/// (<see cref="AdminSignedOutPage"/>): S-A8 at /admin/reset (ask for a link)
/// and S-A9 at /admin/reset/sent (what to do next). They change nothing by
/// themselves: the S-A8 script posts to the JSON route in
/// <see cref="AdminPasswordResetEndpoints"/> and follows only what the reply
/// says. S-A10 and S-A5 are built in that class.
/// <para>
/// Neither page knows who is asking. S-A9 is the same for every visitor and
/// every address typed on S-A8, so it cannot tell whether an account exists.
/// </para>
/// </summary>
public static class AdminPasswordResetPages
{
    public const string RequestScriptPath = "/admin/auth/reset.js";

    /// <summary>
    /// For a page that is only a message: puts the keyboard focus on the page
    /// heading, where the old link-expired page had it.
    /// </summary>
    public const string HeadingScriptPath = "/admin/auth/admin-heading.js";

    /// <summary>
    /// S-A8. Everything but the heading is hidden until the page script runs.
    /// The submit button is disabled in the markup and only a fresh
    /// security-check token enables it; with no site key configured nothing can.
    /// </summary>
    public static IResult Request(HttpContext http, AdminAntiForgeryService antiforgery, TurnstileOptions turnstile)
    {
        ArgumentNullException.ThrowIfNull(turnstile);

        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminPasswordResetCopy.RequestPageTitle,
            Heading = AdminPasswordResetCopy.RequestHeading,
            Noscript = AdminPasswordResetCopy.RequestNoscript,
            BodyHtml = RequestBody(turnstile),
            Scripts =
            [
                // First, so the reload flag leaves the address bar even if a later script fails to load.
                AdminSignedOutPage.ArrivalScriptPath,
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.TurnstileScriptPath,
                AdminSignedOutPage.RequestScriptPath,
                RequestScriptPath
            ],
            IssueAntiForgery = true,
            AllowTurnstile = turnstile.ConfiguredSiteKey() is not null
        });
    }

    /// <summary>
    /// S-A9: the server's one neutral sentence, how long the link lasts and
    /// the way back. No form, no resend, no cookie, and nothing of the request
    /// that led here.
    /// </summary>
    public static IResult Sent(HttpContext http, AdminAntiForgeryService antiforgery)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminPasswordResetCopy.SentPageTitle,
            Heading = AdminPasswordResetCopy.SentHeading,
            BodyHtml =
                $"<p id=\"reset-sent-message\" class=\"lede\">{E(AdminPasswordResetCopy.RequestAccepted)}</p>" +
                $"<p id=\"reset-sent-expiry\">{E(AdminPasswordResetCopy.SentExpiry)}</p>" +
                BackToSignIn(),
            Scripts = [HeadingScriptPath]
        });
    }

    /// <summary>The way back to S-A1: a plain link, so it works with JavaScript off.</summary>
    private static string BackToSignIn()
        => "<div class=\"links\">" +
           $"<a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">" +
           $"{AdminSignedOutPage.Encode(AdminSignInCopy.BackToSignIn)}</a>" +
           "</div>";

    /// <summary>
    /// The two "enter…" messages are the browser's own check (filled, and an
    /// email that looks like one); everything else is the server's answer,
    /// shown in the one alert above the form. <c>data-accepted</c> is the
    /// server's neutral sentence: the script moves on to S-A9 only for a 200
    /// that carries exactly it.
    /// </summary>
    private static string RequestBody(TurnstileOptions turnstile)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            "<div id=\"reset-panel\" hidden>" +
            $"<p class=\"lede\">{E(AdminPasswordResetCopy.RequestLede)}</p>" +
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>" +
            $"<form id=\"reset-request-form\" method=\"post\" action=\"{AdminSignedOutExemptions.Reset}\" novalidate" +
            $" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\"" +
            $" data-accepted=\"{E(AdminPasswordResetCopy.RequestAccepted)}\">" +
            "<div class=\"field\">" +
            $"<label for=\"email\">{E(AdminSignInCopy.EmailLabel)}</label>" +
            "<input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"username\" inputmode=\"email\"" +
            " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"email-error\"" +
            $" data-required=\"{E(AdminSignInCopy.EmailRequired)}\"" +
            $" data-invalid=\"{E(AdminSignInCopy.EmailInvalid)}\">" +
            "<span id=\"email-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            AdminSignedOutPage.TurnstileBlock(turnstile) +
            "<button type=\"submit\" id=\"reset-submit\" disabled aria-describedby=\"security-check-status\">" +
            $"{E(AdminPasswordResetCopy.RequestSubmit)}</button>" +
            "</form></div>" +
            // Outside the hidden panel, so it is there with JavaScript off too.
            BackToSignIn();
    }
}

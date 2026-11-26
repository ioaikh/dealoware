using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// The two pages a first sign-in leads through, built on the shared
/// signed-out shell (<see cref="AdminSignedOutPage"/>): S-A6 at
/// /admin/setup/authenticator and S-A7 at /admin/setup/recovery-codes.
/// <para>
/// The authenticator key, its QR code and the recovery codes are written into
/// the page by the server. No script asks for them, and there is no address
/// that hands them out as data: they exist in one no-store page, shown while
/// the pending sign-in from step 1 lasts.
/// </para>
/// <para>
/// As on the sign-in pages, everything but the heading is hidden until the
/// page script runs, so with JavaScript off only the heading and one sentence
/// show. Neither page carries the security check, so neither policy names
/// Cloudflare.
/// </para>
/// </summary>
public static class AdminEnrollmentPages
{
    public const string AuthenticatorScriptPath = "/admin/auth/authenticator-setup.js";

    public const string RecoveryCodesScriptPath = "/admin/auth/recovery-codes.js";

    /// <summary>The key is shown in groups of this many characters, so it can be read across to a phone.</summary>
    public const int KeyGroupLength = 4;

    private const string OtpAuthLabelPrefix = "otpauth://totp/";

    /// <summary>
    /// Step 1 has to be done again: the same constant answer the session gate
    /// gives a browser that asks for one of these pages with no pending
    /// sign-in. No cookie is set or cleared.
    /// </summary>
    public static IResult SentBack()
        => Results.Redirect(AdminSignInPages.SentBackLocation);

    /// <summary>The authenticator is set up already, so its page has nothing to show: on to the next one.</summary>
    public static IResult ToRecoveryCodes()
        => Results.Redirect(AdminSignedOutExemptions.SetupRecoveryCodes);

    /// <summary>
    /// S-A6. <paramref name="otpAuthUrl"/> is what the QR code holds; it is
    /// never written out as text or as a link. The key is shown as text for
    /// typing in, and is the QR code's alternative for a reader who cannot
    /// scan it.
    /// </summary>
    public static IResult Authenticator(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        string base32Key,
        string otpAuthUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(base32Key);
        ArgumentException.ThrowIfNullOrEmpty(otpAuthUrl);

        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminEnrollmentCopy.AuthenticatorPageTitle,
            Heading = AdminEnrollmentCopy.AuthenticatorHeading,
            Noscript = AdminEnrollmentCopy.AuthenticatorNoscript,
            BodyHtml = AuthenticatorBody(base32Key, otpAuthUrl),
            Scripts =
            [
                // First, so a flag leaves the address bar even if a later script fails to load.
                AdminSignedOutPage.ArrivalScriptPath,
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.RequestScriptPath,
                AdminSignedOutPage.ClipboardScriptPath,
                AuthenticatorScriptPath
            ],
            IssueAntiForgery = true
        });
    }

    /// <summary>
    /// S-A7 with the codes. The anti-forgery pair is for the one request the
    /// page makes: giving the pending sign-in up when the owner continues.
    /// </summary>
    public static IResult RecoveryCodes(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentOutOfRangeException.ThrowIfZero(codes.Count);

        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminEnrollmentCopy.RecoveryCodesPageTitle,
            Heading = AdminEnrollmentCopy.RecoveryCodesHeading,
            Noscript = AdminEnrollmentCopy.RecoveryCodesNoscript,
            BodyHtml = RecoveryCodesBody(codes),
            Scripts =
            [
                AdminSignedOutPage.ArrivalScriptPath,
                AdminSignedOutPage.RequestScriptPath,
                AdminSignedOutPage.ClipboardScriptPath,
                RecoveryCodesScriptPath
            ],
            IssueAntiForgery = true
        });
    }

    /// <summary>
    /// S-A7 once the codes can no longer be shown: one sentence that says so
    /// and the way back to sign-in. Plain markup, nothing hidden, nothing to
    /// send, so it needs no page script and no anti-forgery pair.
    /// </summary>
    public static IResult RecoveryCodesAlreadyShown(HttpContext http, AdminAntiForgeryService antiforgery)
        => AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminEnrollmentCopy.RecoveryCodesPageTitle,
            Heading = AdminEnrollmentCopy.RecoveryCodesHeading,
            BodyHtml =
                "<div id=\"recovery-codes-gone\">" +
                $"<p class=\"lede\">{AdminSignedOutPage.Encode(AdminEnrollmentCopy.RecoveryCodesAlreadyShown)}</p>" +
                "<div class=\"links\">" +
                $"<a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">" +
                $"{AdminSignedOutPage.Encode(AdminEnrollmentCopy.BackToSignIn)}</a>" +
                "</div></div>",
            // Only to take a flag out of the address bar, when the browser was sent on to here.
            Scripts = [AdminSignedOutPage.ArrivalScriptPath]
        });

    /// <summary>
    /// The name an authenticator app lists the key under: the label of the
    /// otpauth address, which is the part between "totp/" and "?". Read from
    /// the address itself, so the page cannot say one name and the QR code
    /// another.
    /// </summary>
    public static string AccountName(string otpAuthUrl)
    {
        ArgumentNullException.ThrowIfNull(otpAuthUrl);

        var end = otpAuthUrl.IndexOf('?', StringComparison.Ordinal);
        if (!otpAuthUrl.StartsWith(OtpAuthLabelPrefix, StringComparison.Ordinal) || end < OtpAuthLabelPrefix.Length)
        {
            return string.Empty;
        }

        return Uri.UnescapeDataString(otpAuthUrl[OtpAuthLabelPrefix.Length..end]);
    }

    /// <summary>
    /// The QR code as one inline SVG image: a light square with the quiet
    /// zone in it, and the dark modules as one path. Black on white whatever
    /// the page's colours are, because a camera has to read it. Its
    /// description is the line that offers the key instead, so a reader who
    /// cannot scan it is pointed at the same information as text.
    /// </summary>
    public static string QrSvg(AdminQrCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var side = code.SvgSize;
        return
            "<svg id=\"authenticator-qr\" class=\"qr\" role=\"img\"" +
            $" aria-label=\"{AdminSignedOutPage.Encode(AdminEnrollmentCopy.QrLabel)}\"" +
            " aria-describedby=\"manual-key-help\"" +
            $" viewBox=\"0 0 {side} {side}\" shape-rendering=\"crispEdges\">" +
            $"<rect width=\"{side}\" height=\"{side}\" fill=\"#fff\"/>" +
            $"<path fill=\"#000\" d=\"{code.SvgPathData()}\"/>" +
            "</svg>";
    }

    /// <summary>
    /// The key in groups of four. The groups are set apart by the stylesheet,
    /// not by spaces, so the text selected and copied by hand is the key
    /// itself; the empty &lt;wbr&gt; between two groups lets a narrow screen
    /// break the line there.
    /// </summary>
    public static string GroupedKey(string base32Key)
    {
        ArgumentNullException.ThrowIfNull(base32Key);

        var html = new StringBuilder(base32Key.Length * 4);
        for (var at = 0; at < base32Key.Length; at += KeyGroupLength)
        {
            if (at > 0)
            {
                html.Append("<wbr>");
            }

            var group = base32Key.Substring(at, Math.Min(KeyGroupLength, base32Key.Length - at));
            html.Append("<span>").Append(AdminSignedOutPage.Encode(group)).Append("</span>");
        }

        return html.ToString();
    }

    /// <summary>
    /// The key and its QR code come first, then the one alert, then the form:
    /// a refusal is shown next to the field it is about. Both parts stay
    /// hidden until the page script has found its helpers.
    /// <c>data-no-sign-in</c> is the session gate's word for "no pending
    /// sign-in" (<see cref="AdminDeny.GenericError"/>): a 401 with that word
    /// means step 1 has to be done again; a 401 with any other text is the
    /// server's own sentence and is shown as sent.
    /// </summary>
    private static string AuthenticatorBody(string base32Key, string otpAuthUrl)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            "<div id=\"authenticator-panel\" hidden>" +
            "<div id=\"authenticator-key\">" +
            $"<p class=\"lede\">{E(AdminEnrollmentCopy.AuthenticatorLede)}</p>" +
            QrSvg(AdminQrCode.Encode(otpAuthUrl)) +
            $"<p id=\"manual-key-help\" class=\"helper\">{E(AdminEnrollmentCopy.KeyHelp)}</p>" +
            "<dl class=\"details\">" +
            $"<dt>{E(AdminEnrollmentCopy.KeyLabel)}</dt>" +
            $"<dd><span id=\"manual-key\" class=\"key\" translate=\"no\">{GroupedKey(base32Key)}</span></dd>" +
            $"<dt>{E(AdminEnrollmentCopy.AccountNameLabel)}</dt>" +
            $"<dd id=\"account-name\" translate=\"no\">{E(AccountName(otpAuthUrl))}</dd>" +
            "</dl>" +
            "<div class=\"actions\">" +
            "<button type=\"button\" id=\"copy-key\" class=\"secondary\"" +
            $" data-copied=\"{E(AdminEnrollmentCopy.Copied)}\"" +
            $" data-copy-failed=\"{E(AdminEnrollmentCopy.CopyFailed)}\">{E(AdminEnrollmentCopy.CopyKey)}</button>" +
            "</div>" +
            "<p id=\"copy-status\" class=\"live\" role=\"status\"></p>" +
            "</div>" +
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>" +
            $"<form id=\"authenticator-form\" method=\"post\" action=\"{AdminSignedOutExemptions.SetupAuthenticator}\" novalidate" +
            $" data-retry=\"{E(AdminEnrollmentCopy.SubmitRetry)}\"" +
            $" data-no-sign-in=\"{E(AdminDeny.GenericError)}\">" +
            "<div class=\"field\">" +
            $"<label for=\"code\">{E(AdminEnrollmentCopy.CodeLabel)}</label>" +
            // Numeric keypad; the value stays text so a leading zero is kept.
            "<input id=\"code\" name=\"code\" type=\"text\" inputmode=\"numeric\"" +
            " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required" +
            " aria-describedby=\"code-error\"" +
            $" data-required=\"{E(AdminEnrollmentCopy.CodeRequired)}\">" +
            "<span id=\"code-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            $"<button type=\"submit\" id=\"authenticator-submit\">{E(AdminEnrollmentCopy.AuthenticatorSubmit)}</button>" +
            "</form></div>";
    }

    /// <summary>
    /// The codes are a real list, named by the page heading. "Continue" is
    /// disabled in the markup; only ticking the box enables it.
    /// </summary>
    private static string RecoveryCodesBody(IReadOnlyList<string> codes)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        var html = new StringBuilder(1024);
        html.Append("<div id=\"recovery-codes-panel\" hidden>");
        html.Append($"<p class=\"lede\">{E(AdminEnrollmentCopy.RecoveryCodesLede)}</p>");
        html.Append($"<p>{E(AdminEnrollmentCopy.RecoveryCodesPurpose)}</p>");
        // role="list": a list drawn without markers is still announced as one.
        html.Append("<ul id=\"recovery-codes-list\" class=\"code-list\" role=\"list\" aria-labelledby=\"page-heading\" translate=\"no\">");
        foreach (var code in codes)
        {
            html.Append("<li>").Append(E(code)).Append("</li>");
        }

        html.Append("</ul>");
        html.Append("<div class=\"actions\">");
        html.Append("<button type=\"button\" id=\"copy-codes\" class=\"secondary\"");
        html.Append($" data-copied=\"{E(AdminEnrollmentCopy.Copied)}\"");
        html.Append($" data-copy-failed=\"{E(AdminEnrollmentCopy.CopyFailed)}\">{E(AdminEnrollmentCopy.CopyAllCodes)}</button>");
        html.Append("</div>");
        html.Append("<p id=\"copy-status\" class=\"live\" role=\"status\"></p>");
        html.Append("<div class=\"check\">");
        html.Append("<input id=\"codes-saved\" type=\"checkbox\" autocomplete=\"off\">");
        html.Append($"<label for=\"codes-saved\">{E(AdminEnrollmentCopy.CodesSavedLabel)}</label>");
        html.Append("</div>");
        html.Append($"<button type=\"button\" id=\"codes-continue\" disabled>{E(AdminEnrollmentCopy.RecoveryCodesContinue)}</button>");
        html.Append("</div>");
        return html.ToString();
    }
}

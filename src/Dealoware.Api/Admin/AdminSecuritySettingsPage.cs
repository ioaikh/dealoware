using System.Text;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// S-A12 Security settings at /admin/settings/security (T04), one call to the
/// signed-in shell (<see cref="AdminSignedInPage"/>). The server actions it
/// drives are <see cref="AdminSecuritySettingsEndpoints"/>; the page script is
/// <see cref="ScriptPath"/>.
/// <para>
/// Four parts, each a section with its own heading:
/// <list type="bullet">
/// <item>Password: S-A12 note v1.4. Current password, New password and Confirm
/// new password (the last two the first-setup page's fields, with the same
/// helper, show toggles and length data), then one second-factor field at a
/// time with "Use a recovery code instead", and "Change password".</item>
/// <item>Recovery codes: the count of unused codes, "Create new codes", and a
/// panel for the new set, shown once (as on S-A7).</item>
/// <item>Authenticator: "Set up again", a panel for the new key (QR code, key,
/// account name, Copy key, the code from the new key), and a panel for the
/// codes the confirm issues.</item>
/// <item>Recent sign-in activity: the 24-hour lock notice and the last 20 events.</item>
/// </list>
/// Plus one step-up dialog (current password and a code) that "Create new
/// codes" and "Set up again" open when the server asks for it.
/// </para>
/// <para>
/// The page carries no secret and nothing about the owner: every panel that
/// will hold a code, a key or a count is empty in the markup and is filled by
/// the script from a server reply, with textContent only. The sentences the
/// script needs are data attributes, written here from constants. Everything
/// but the noscript sentence is hidden until the script runs. Every form posts
/// to this page's own address, so a form sent without the script (it cannot be
/// seen without it) gets the route's 405 and nothing typed reaches an address.
/// </para>
/// </summary>
public static class AdminSecuritySettingsPage
{
    public const string ScriptPath = AdminSignedInPage.AssetPrefix + "/security-settings.js";

    public const string StylesheetPath = AdminSignedInPage.AssetPrefix + "/security-settings.css";

    public static IResult Render(HttpContext http, AdminAntiForgeryService antiforgery)
        => AdminSignedInPage.Render(http, antiforgery, new AdminSignedInPageContent
        {
            Title = AdminConsoleCopy.Title(AdminConsoleCopy.NavSecuritySettings),
            Heading = AdminConsoleCopy.NavSecuritySettings,
            Section = AdminConsoleSection.SecuritySettings,
            BodyHtml = Body(),
            // The signed-out pages' own helpers, served once from /admin/auth/ as the shell's
            // request helper is: the same files, not copies (see the page script).
            Scripts =
            [
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.PasswordScriptPath,
                AdminSignedOutPage.ClipboardScriptPath,
                ScriptPath
            ],
            Stylesheets = [StylesheetPath]
        });

    private static string E(string value) => AdminSignedOutPage.Encode(value);

    private static string Body()
    {
        var html = new StringBuilder(16384);
        html.Append($"<noscript><p>{E(AdminSecuritySettingsPageCopy.Noscript)}</p></noscript>");
        html.Append("<div id=\"security-settings\" class=\"settings\" hidden");
        html.Append($" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\">");
        html.Append(PasswordSection());
        html.Append(RecoveryCodesSection());
        html.Append(AuthenticatorSection());
        html.Append(ActivitySection());
        html.Append(StepUpDialog());
        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>The polite live region a form's outcome is announced in, tied to the form by aria-describedby (note v1.4 (b)).</summary>
    private static string FormAlert(string id)
        => $"<p id=\"{id}\" class=\"form-alert\" role=\"status\" aria-live=\"polite\"></p>";

    /// <summary>A password field with its show toggle, as on the signed-out pages.</summary>
    private static string PasswordField(string id, string label, string autocomplete, string required)
        => "<div class=\"field\">" +
           $"<label id=\"{id}-label\" for=\"{id}\">{E(label)}</label>" +
           "<div class=\"field-row\">" +
           $"<input id=\"{id}\" type=\"password\" autocomplete=\"{autocomplete}\" required" +
           $" aria-describedby=\"{id}-error\" data-required=\"{E(required)}\">" +
           AdminSignedOutPage.PasswordToggle(id) +
           "</div>" +
           $"<span id=\"{id}-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
           "</div>";

    /// <summary>One second-factor field, as on the sign-in pages (S-A2, S-A3) and S-A10.</summary>
    private static string FactorField(string blockId, string inputId, string inputAttributes, string label, string required, bool hidden)
        => $"<div class=\"field\" id=\"{blockId}\"{(hidden ? " hidden" : string.Empty)}>" +
           $"<label for=\"{inputId}\">{E(label)}</label>" +
           $"<input id=\"{inputId}\" type=\"text\"{inputAttributes}" +
           " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required" +
           $" aria-describedby=\"{inputId}-error\"" +
           $" data-required=\"{E(required)}\">" +
           $"<span id=\"{inputId}-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
           "</div>";

    /// <summary>The authenticator code, then the recovery code (hidden), then the switch between them.</summary>
    private static string SecondFactor(string prefix)
        => FactorField($"{prefix}-code-field", $"{prefix}-code", " inputmode=\"numeric\"",
               AdminSignInCopy.CodeLabel, AdminSignInCopy.CodeRequired, hidden: false) +
           FactorField($"{prefix}-recovery-code-field", $"{prefix}-recovery-code", string.Empty,
               AdminSignInCopy.RecoveryLabel, AdminSignInCopy.RecoveryRequired, hidden: true) +
           "<div class=\"switch\">" +
           $"<button type=\"button\" id=\"{prefix}-factor-switch\" class=\"secondary\"" +
           $" aria-controls=\"{prefix}-code-field {prefix}-recovery-code-field\"" +
           $" data-to-recovery=\"{E(AdminSignInCopy.UseRecoveryCode)}\"" +
           $" data-to-code=\"{E(AdminSignInCopy.UseAuthenticatorCode)}\">" +
           $"{E(AdminSignInCopy.UseRecoveryCode)}</button>" +
           "</div>";

    /// <summary>
    /// Note v1.4: field order current, new, confirm, code with its switch.
    /// New password and Confirm new password are the first-setup page's
    /// fields (S-A4, reused by S-A10): same helper, same length data, same
    /// messages. Confirm has no name and is never sent.
    /// <c>data-changed</c> is the server's success sentence: the page leaves
    /// for sign-in only on a reply that carries exactly it.
    /// </summary>
    private static string PasswordSection()
        => "<section class=\"settings-section\" aria-labelledby=\"password-heading\">" +
           $"<h2 id=\"password-heading\">{E(AdminSecuritySettingsPageCopy.PasswordHeading)}</h2>" +
           $"<form id=\"password-form\" method=\"post\" action=\"{AdminSignedInPage.SecuritySettings}\" novalidate" +
           " aria-describedby=\"password-alert\"" +
           $" data-changed=\"{E(AdminSecuritySettingsCopy.PasswordChanged)}\">" +
           FormAlert("password-alert") +
           PasswordField("current-password", AdminSecuritySettingsPageCopy.CurrentPasswordLabel, "current-password",
               AdminSignInCopy.PasswordRequired) +
           "<div class=\"field\">" +
           $"<label id=\"new-password-label\" for=\"new-password\">{E(AdminSecuritySettingsPageCopy.NewPasswordLabel)}</label>" +
           "<div class=\"field-row\">" +
           "<input id=\"new-password\" name=\"newPassword\" type=\"password\" autocomplete=\"new-password\" required" +
           " aria-describedby=\"password-helper new-password-error\"" +
           $" data-min-length=\"{AdminPasswordRules.MinCodePoints}\"" +
           $" data-max-length=\"{AdminPasswordRules.MaxCodePoints}\"" +
           $" data-too-short=\"{E(AdminPasswordRules.TooShortMessage)}\"" +
           $" data-too-long=\"{E(AdminPasswordRules.TooLongMessage)}\">" +
           AdminSignedOutPage.PasswordToggle("new-password") +
           "</div>" +
           $"<span id=\"password-helper\" class=\"helper\">{E(AdminSignedOutCopy.PasswordHelper)}</span>" +
           "<span id=\"new-password-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
           "</div>" +
           "<div class=\"field\">" +
           $"<label id=\"confirm-password-label\" for=\"confirm-password\">{E(AdminSecuritySettingsPageCopy.ConfirmNewPasswordLabel)}</label>" +
           "<div class=\"field-row\">" +
           "<input id=\"confirm-password\" type=\"password\" autocomplete=\"new-password\" required" +
           " aria-describedby=\"confirm-password-error\"" +
           $" data-mismatch=\"{E(AdminSignedOutCopy.PasswordMismatch)}\">" +
           AdminSignedOutPage.PasswordToggle("confirm-password") +
           "</div>" +
           "<span id=\"confirm-password-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
           "</div>" +
           SecondFactor("password") +
           $"<button type=\"submit\" id=\"password-submit\">{E(AdminSecuritySettingsPageCopy.ChangePassword)}</button>" +
           "</form></section>";

    /// <summary>
    /// The panel a new set of codes is shown in, once, as on S-A7: the list,
    /// "Copy all", the box "I have saved these recovery codes." and
    /// "Continue", which empties the list. Empty in the markup.
    /// </summary>
    private static string CodesPanel(string prefix)
        => $"<div id=\"{prefix}\" class=\"new-codes\" hidden>" +
           $"<p id=\"{prefix}-lede\" class=\"lede\" tabindex=\"-1\">{E(AdminEnrollmentCopy.RecoveryCodesLede)}</p>" +
           // role="list": a list drawn without markers is still announced as one.
           $"<ul id=\"{prefix}-list\" class=\"code-list\" role=\"list\" aria-labelledby=\"{prefix}-lede\" translate=\"no\"></ul>" +
           "<div class=\"actions\">" +
           $"<button type=\"button\" id=\"{prefix}-copy\" class=\"secondary\"" +
           $" data-copied=\"{E(AdminEnrollmentCopy.Copied)}\"" +
           $" data-copy-failed=\"{E(AdminEnrollmentCopy.CopyFailed)}\">{E(AdminEnrollmentCopy.CopyAllCodes)}</button>" +
           "</div>" +
           $"<p id=\"{prefix}-copy-status\" class=\"copy-status\" role=\"status\"></p>" +
           "<div class=\"check\">" +
           $"<input id=\"{prefix}-saved\" type=\"checkbox\" autocomplete=\"off\">" +
           $"<label for=\"{prefix}-saved\">{E(AdminEnrollmentCopy.CodesSavedLabel)}</label>" +
           "</div>" +
           $"<button type=\"button\" id=\"{prefix}-done\" disabled>{E(AdminEnrollmentCopy.RecoveryCodesContinue)}</button>" +
           "</div>";

    /// <summary>TD-ADM-UI-auth-18: "N codes left" from the server, and "Create new codes" behind the step-up.</summary>
    private static string RecoveryCodesSection()
        => "<section class=\"settings-section\" aria-labelledby=\"codes-heading\">" +
           $"<h2 id=\"codes-heading\">{E(AdminSecuritySettingsPageCopy.RecoveryCodesHeading)}</h2>" +
           "<p id=\"codes-left\" class=\"codes-left\"" +
           $" data-one=\"{E(AdminSecuritySettingsPageCopy.CodesLeftOne)}\"" +
           $" data-many=\"{E(AdminSecuritySettingsPageCopy.CodesLeftMany)}\"></p>" +
           $"<p>{E(AdminEnrollmentCopy.RecoveryCodesPurpose)} {E(AdminSecuritySettingsPageCopy.NewCodesReplace)}</p>" +
           FormAlert("codes-alert") +
           $"<button type=\"button\" id=\"codes-create\">{E(AdminSecuritySettingsPageCopy.CreateNewCodes)}</button>" +
           CodesPanel("codes-new") +
           "</section>";

    /// <summary>
    /// TD-ADM-UI-auth-18: "Set up again" behind the step-up, then the new key
    /// as on S-A6 (the QR code is drawn by the script from the reply's path
    /// data, with S-A6's name and description), the code from the new key,
    /// and the codes the confirm issues.
    /// </summary>
    private static string AuthenticatorSection()
        => "<section class=\"settings-section\" aria-labelledby=\"authenticator-heading\">" +
           $"<h2 id=\"authenticator-heading\">{E(AdminSecuritySettingsPageCopy.AuthenticatorHeading)}</h2>" +
           $"<p>{E(AdminSecuritySettingsPageCopy.AuthenticatorLede)}</p>" +
           "<p id=\"authenticator-alert\" class=\"form-alert\" role=\"status\" aria-live=\"polite\"" +
           $" data-changed=\"{E(AdminSecuritySettingsPageCopy.AuthenticatorChanged)}\"></p>" +
           $"<button type=\"button\" id=\"authenticator-start\">{E(AdminSecuritySettingsPageCopy.SetUpAgain)}</button>" +
           "<div id=\"new-key\" class=\"new-key\" hidden>" +
           $"<p id=\"new-key-lede\" class=\"lede\" tabindex=\"-1\">{E(AdminEnrollmentCopy.AuthenticatorLede)}</p>" +
           $"<div id=\"new-key-qr\" class=\"qr-slot\" data-label=\"{E(AdminEnrollmentCopy.QrLabel)}\"></div>" +
           $"<p id=\"manual-key-help\" class=\"helper\">{E(AdminEnrollmentCopy.KeyHelp)}</p>" +
           "<dl class=\"details\">" +
           $"<dt>{E(AdminEnrollmentCopy.KeyLabel)}</dt>" +
           "<dd><span id=\"manual-key\" class=\"key\" translate=\"no\"></span></dd>" +
           $"<dt>{E(AdminEnrollmentCopy.AccountNameLabel)}</dt>" +
           "<dd id=\"account-name\" translate=\"no\"></dd>" +
           "</dl>" +
           "<div class=\"actions\">" +
           "<button type=\"button\" id=\"copy-key\" class=\"secondary\"" +
           $" data-copied=\"{E(AdminEnrollmentCopy.Copied)}\"" +
           $" data-copy-failed=\"{E(AdminEnrollmentCopy.CopyFailed)}\">{E(AdminEnrollmentCopy.CopyKey)}</button>" +
           "</div>" +
           "<p id=\"copy-key-status\" class=\"copy-status\" role=\"status\"></p>" +
           $"<form id=\"new-key-form\" method=\"post\" action=\"{AdminSignedInPage.SecuritySettings}\" novalidate" +
           " aria-describedby=\"new-key-alert\">" +
           FormAlert("new-key-alert") +
           FactorField("new-key-code-field", "new-key-code", " inputmode=\"numeric\"",
               AdminEnrollmentCopy.CodeLabel, AdminEnrollmentCopy.CodeRequired, hidden: false) +
           "<div class=\"actions\">" +
           $"<button type=\"submit\" id=\"new-key-submit\">{E(AdminEnrollmentCopy.AuthenticatorSubmit)}</button>" +
           $"<button type=\"button\" id=\"new-key-cancel\" class=\"secondary\">{E(AdminSecuritySettingsPageCopy.Cancel)}</button>" +
           "</div>" +
           "</form>" +
           "</div>" +
           CodesPanel("reenrol-codes") +
           "</section>";

    /// <summary>
    /// TD-ADM-UI-auth-19: the lock notice (one line at the top, a text label
    /// next to its icon, not dismissible), the empty state, and the table with
    /// a caption and column headers. The labels of the event names are a
    /// hidden list the script reads; an event not on it is shown by its name.
    /// </summary>
    private static string ActivitySection()
    {
        var html = new StringBuilder(2048);
        html.Append("<section class=\"settings-section\" aria-labelledby=\"activity-heading\">");
        html.Append($"<h2 id=\"activity-heading\">{E(AdminSecuritySettingsPageCopy.ActivityHeading)}</h2>");
        html.Append("<p id=\"lock-notice\" class=\"lock-notice\" hidden");
        html.Append($" data-active=\"{E(AdminSecuritySettingsPageCopy.LockNoticeActive)}\"");
        html.Append($" data-ended=\"{E(AdminSecuritySettingsPageCopy.LockNoticeEnded)}\">");
        html.Append("<span class=\"lock-icon\" aria-hidden=\"true\">!</span>");
        html.Append($"<strong>{E(AdminSecuritySettingsPageCopy.LockNoticeLabel)}</strong> ");
        html.Append("<span id=\"lock-notice-text\"></span></p>");
        html.Append(FormAlert("activity-alert"));
        html.Append($"<p id=\"activity-empty\" hidden>{E(AdminSecuritySettingsPageCopy.ActivityEmpty)}</p>");
        // A narrow screen scrolls the table, not the page; the region takes the focus for the keyboard.
        html.Append("<div id=\"activity-wrap\" class=\"table-wrap\" role=\"region\" aria-labelledby=\"activity-caption\" tabindex=\"0\" hidden>");
        html.Append("<table id=\"activity-table\">");
        html.Append($"<caption id=\"activity-caption\">{E(AdminSecuritySettingsPageCopy.ActivityCaption)}</caption>");
        html.Append("<thead><tr>");
        html.Append($"<th scope=\"col\">{E(AdminSecuritySettingsPageCopy.ActivityTime)}</th>");
        html.Append($"<th scope=\"col\">{E(AdminSecuritySettingsPageCopy.ActivityEvent)}</th>");
        html.Append($"<th scope=\"col\">{E(AdminSecuritySettingsPageCopy.ActivityNetwork)}</th>");
        html.Append("</tr></thead><tbody id=\"activity-rows\"></tbody></table></div>");
        html.Append("<ul id=\"activity-labels\" hidden>");
        foreach (var (name, label) in AdminSecuritySettingsPageCopy.ActivityLabels)
        {
            html.Append($"<li data-event=\"{E(name)}\">{E(label)}</li>");
        }

        html.Append("</ul></section>");
        return html.ToString();
    }

    /// <summary>
    /// TD-ADM-174: the current password and one code, asked for when the
    /// server says a step-up is needed. A modal dialog: the rest of the page
    /// is inert while it is open, Escape and Cancel close it and clear it.
    /// </summary>
    private static string StepUpDialog()
        => "<dialog id=\"step-up-dialog\" class=\"step-up\" aria-labelledby=\"step-up-heading\" aria-describedby=\"step-up-lede\">" +
           $"<form id=\"step-up-form\" method=\"post\" action=\"{AdminSignedInPage.SecuritySettings}\" novalidate" +
           " aria-describedby=\"step-up-alert\">" +
           $"<h2 id=\"step-up-heading\">{E(AdminSecuritySettingsPageCopy.StepUpHeading)}</h2>" +
           $"<p id=\"step-up-lede\">{E(AdminSecuritySettingsPageCopy.StepUpLede)}</p>" +
           FormAlert("step-up-alert") +
           PasswordField("step-up-password", AdminSecuritySettingsPageCopy.CurrentPasswordLabel, "current-password",
               AdminSignInCopy.PasswordRequired) +
           SecondFactor("step-up") +
           "<div class=\"actions\">" +
           $"<button type=\"submit\" id=\"step-up-submit\">{E(AdminSecuritySettingsPageCopy.StepUpSubmit)}</button>" +
           $"<button type=\"button\" id=\"step-up-cancel\" class=\"secondary\">{E(AdminSecuritySettingsPageCopy.Cancel)}</button>" +
           "</div>" +
           "</form></dialog>";
}

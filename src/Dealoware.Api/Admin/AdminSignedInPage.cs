using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>The nav entry a signed-in page is, for aria-current. None for a page that is not in the nav.</summary>
public enum AdminConsoleSection
{
    None,
    Stats,
    Participants,
    Artifacts,
    Negotiations,
    Offers,
    AuditLog,
    SecuritySettings
}

/// <summary>What one signed-in admin page puts into the shared console shell.</summary>
public sealed record AdminSignedInPageContent
{
    /// <summary>Text of &lt;title&gt;, "{Screen} · Dealoware admin". Encoded by the shell.</summary>
    public required string Title { get; init; }

    /// <summary>Text of the page's one &lt;h1&gt;. Encoded by the shell.</summary>
    public required string Heading { get; init; }

    /// <summary>Which nav entry is this page (aria-current="page").</summary>
    public AdminConsoleSection Section { get; init; }

    /// <summary>
    /// Markup placed after the heading and the live region. The shell does
    /// NOT encode it: build it from constants and pass every other value
    /// (entity text above all) through <see cref="AdminSignedOutPage.Encode"/>.
    /// </summary>
    public string BodyHtml { get; init; } = string.Empty;

    /// <summary>
    /// The page's own scripts, after the shell's. Same-origin paths only,
    /// under <see cref="AdminSignedInPage.AssetPrefix"/> (served only with a
    /// session) or the signed-out helpers under /admin/auth/.
    /// </summary>
    public IReadOnlyList<string> Scripts { get; init; } = [];

    /// <summary>The page's own stylesheets, after the shell's. Same-origin paths under <see cref="AdminSignedInPage.AssetPrefix"/>.</summary>
    public IReadOnlyList<string> Stylesheets { get; init; } = [];

    /// <summary>HTTP status of the page. 200 except for S-D3.</summary>
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
}

/// <summary>
/// The one place that builds a signed-in admin page (T04): the same document
/// rules as the signed-out shell (<see cref="AdminSignedOutPage"/>: doctype,
/// lang, a fresh nonce and the policy that names it, no-store and
/// no-referrer, the dw-csrf meta, one h1, static scripts only), plus the
/// console frame every signed-in page shares:
/// <list type="bullet">
/// <item>"Skip to main content", the first thing that takes the focus;</item>
/// <item>the nav: Stats, Participants, Artifacts, Negotiations, Offers,
/// Audit log, Security settings, and Sign out (a POST form, driven by
/// <see cref="ShellScriptPath"/>; no inline handler);</item>
/// <item>main, holding the h1 and the page's one polite live region.</item>
/// </list>
/// A page is one call: <see cref="Render"/> with a title, a heading, a body
/// and its scripts. The handlers that call it run only behind the session
/// gate, so nothing here is ever sent to a browser without a session.
/// </summary>
public static class AdminSignedInPage
{
    /// <summary>
    /// Where the signed-in pages' own files are served from: after the
    /// session gate, out of <see cref="AssetFolder"/> (outside wwwroot), so a
    /// request without a session gets no file.
    /// </summary>
    public const string AssetPrefix = "/admin/assets";

    /// <summary>The folder under the content root that holds those files.</summary>
    public const string AssetFolder = "AdminAssets";

    public const string StylesheetPath = AssetPrefix + "/console.css";

    /// <summary>Sign out, and a page brought back by Back/Forward is asked for anew.</summary>
    public const string ShellScriptPath = AssetPrefix + "/console.js";

    public const string LiveRegionId = "console-status";

    public const string Stats = AdminReturnPath.Stats;
    public const string Participants = "/admin/participants";
    public const string Artifacts = "/admin/artifacts";
    public const string Negotiations = "/admin/negotiations";
    public const string Offers = "/admin/offers";
    public const string AuditLog = "/admin/audit";
    public const string SecuritySettings = "/admin/settings/security";

    /// <summary>The nav, in order: path, label, the section it marks as current.</summary>
    public static readonly IReadOnlyList<(string Path, string Label, AdminConsoleSection Section)> Nav =
    [
        (Stats, AdminConsoleCopy.NavStats, AdminConsoleSection.Stats),
        (Participants, AdminConsoleCopy.NavParticipants, AdminConsoleSection.Participants),
        (Artifacts, AdminConsoleCopy.NavArtifacts, AdminConsoleSection.Artifacts),
        (Negotiations, AdminConsoleCopy.NavNegotiations, AdminConsoleSection.Negotiations),
        (Offers, AdminConsoleCopy.NavOffers, AdminConsoleSection.Offers),
        (AuditLog, AdminConsoleCopy.NavAuditLog, AdminConsoleSection.AuditLog),
        (SecuritySettings, AdminConsoleCopy.NavSecuritySettings, AdminConsoleSection.SecuritySettings)
    ];

    /// <summary>The scripts every signed-in page loads, before its own.</summary>
    public static readonly IReadOnlyList<string> ShellScripts =
    [
        AdminSignedOutPage.RequestScriptPath,
        ShellScriptPath,
        // The idle and 8-hour warnings, and "Stay signed in" (AdminSessionEndpoints).
        AssetPrefix + "/session-timer.js"
    ];

    public static IResult Render(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        AdminSignedInPageContent page)
    {
        ArgumentNullException.ThrowIfNull(page);

        AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
        var nonce = AdminSignedOutPage.CreateNonce();
        http.Response.Headers.ContentSecurityPolicy = AdminSignedOutPage.ContentSecurityPolicy(nonce);
        var token = antiforgery.IssueOrReuse(http);

        static string E(string? value) => AdminSignedOutPage.Encode(value);

        var html = new StringBuilder(4096);
        html.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.Append($"<meta name=\"{AdminSignedOutPage.CsrfMetaName}\" content=\"{E(token)}\">");
        html.Append($"<title>{E(page.Title)}</title>");
        html.Append($"<link rel=\"stylesheet\" href=\"{StylesheetPath}\" nonce=\"{nonce}\">");
        foreach (var stylesheet in page.Stylesheets)
        {
            html.Append($"<link rel=\"stylesheet\" href=\"{E(stylesheet)}\" nonce=\"{nonce}\">");
        }

        html.Append("</head><body>");
        html.Append($"<a class=\"skip-link\" href=\"#main\">{E(AdminConsoleCopy.SkipLink)}</a>");
        html.Append("<header class=\"console-header\">");
        html.Append($"<p class=\"brand\">{E(AdminConsoleCopy.Brand)}</p>");
        html.Append($"<nav class=\"console-nav\" aria-label=\"{E(AdminConsoleCopy.NavLabel)}\"><ul>");
        foreach (var (path, label, section) in Nav)
        {
            var current = section == page.Section ? " aria-current=\"page\"" : string.Empty;
            html.Append($"<li><a href=\"{path}\"{current}>{E(label)}</a></li>");
        }

        html.Append("<li class=\"sign-out\">");
        html.Append($"<form id=\"sign-out-form\" method=\"post\" action=\"{AdminSignedOutExemptions.SignOut}\"");
        html.Append($" data-retry=\"{E(AdminSignInCopy.SubmitRetry)}\">");
        // For the form sent without JavaScript: the same token, read by POST /admin/sign-out alone.
        html.Append($"<input type=\"hidden\" name=\"{AdminAntiForgeryService.SignOutFormField}\" value=\"{E(token)}\">");
        html.Append($"<button type=\"submit\" id=\"sign-out\" class=\"secondary\">{E(AdminConsoleCopy.SignOut)}</button>");
        html.Append("</form></li>");
        html.Append("</ul></nav></header>");
        html.Append("<main id=\"main\" class=\"console-main\" tabindex=\"-1\">");
        html.Append($"<h1 id=\"page-heading\">{E(page.Heading)}</h1>");
        html.Append($"<p id=\"{LiveRegionId}\" class=\"live\" role=\"status\" aria-live=\"polite\"></p>");
        // UXR-A40: the recovery-code banner, while this session's is still to be shown.
        html.Append(AdminRecoveryNotice.Markup(http));
        html.Append(page.BodyHtml);
        html.Append("</main>");
        foreach (var script in ShellScripts.Concat(AdminRecoveryNotice.Scripts(http)).Concat(page.Scripts))
        {
            html.Append($"<script src=\"{E(script)}\" nonce=\"{nonce}\"></script>");
        }

        html.Append("</body></html>");
        return Results.Content(html.ToString(), AdminSignedOutPage.ContentType, Encoding.UTF8, page.StatusCode);
    }
}

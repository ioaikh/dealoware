using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>What one signed-out admin page puts into the shared document shell.</summary>
public sealed record AdminSignedOutPageContent
{
    /// <summary>Text of &lt;title&gt;. Encoded by the shell.</summary>
    public required string Title { get; init; }

    /// <summary>Text of the page's one &lt;h1&gt;. Encoded by the shell.</summary>
    public required string Heading { get; init; }

    /// <summary>The one sentence shown when JavaScript is off. Encoded by the shell.</summary>
    public string? Noscript { get; init; }

    /// <summary>
    /// Markup placed after the heading. The shell does NOT encode it: build it
    /// from constants and pass every other value through
    /// <see cref="AdminSignedOutPage.Encode"/>.
    /// </summary>
    public string BodyHtml { get; init; } = string.Empty;

    /// <summary>Same-origin script paths under /admin/auth/, loaded in order with the page nonce.</summary>
    public IReadOnlyList<string> Scripts { get; init; } = [];

    /// <summary>Issue the anti-forgery cookie and render its token as the dw-csrf meta.</summary>
    public bool IssueAntiForgery { get; init; }

    /// <summary>
    /// The page renders the Turnstile widget, so its policy also allows
    /// Cloudflare's script and frame. Leave false on every other page.
    /// </summary>
    public bool AllowTurnstile { get; init; }
}

/// <summary>
/// The one place that builds a signed-out admin page: doctype, lang, title,
/// the dw-csrf meta, a fresh nonce and the Content-Security-Policy that names
/// it, no-store and no-referrer, the shared stylesheet, the single h1, the
/// noscript sentence and the script tags.
/// </summary>
public static class AdminSignedOutPage
{
    public const string ContentType = "text/html; charset=utf-8";
    public const string CsrfMetaName = "dw-csrf";
    public const string StylesheetPath = "/admin/auth/signed-out.css";

    /// <summary>
    /// For a page opened from an emailed link. List it FIRST: it takes the
    /// token out of the address bar before any other script can fail to load.
    /// </summary>
    public const string LinkScriptPath = "/admin/auth/admin-link.js";

    /// <summary>
    /// For the sign-in pages. List it FIRST: it reads the fixed flag that may
    /// follow the address and takes it out of the address bar.
    /// </summary>
    public const string ArrivalScriptPath = "/admin/auth/admin-arrival.js";
    public const string DomScriptPath = "/admin/auth/admin-dom.js";
    public const string PasswordScriptPath = "/admin/auth/admin-password.js";
    public const string TurnstileScriptPath = "/admin/auth/admin-turnstile.js";

    /// <summary>
    /// How a page's script talks to the server: a JSON POST carrying the
    /// page's anti-forgery token, and a plain GET. Same origin only.
    /// </summary>
    public const string RequestScriptPath = "/admin/auth/admin-request.js";

    /// <summary>
    /// For a page with a copy button: puts a text on the clipboard, or
    /// selects it when the browser will not. Nothing is stored.
    /// </summary>
    public const string ClipboardScriptPath = "/admin/auth/admin-clipboard.js";

    /// <summary>The only third-party origin a signed-out page may name, and only for the widget.</summary>
    public const string TurnstileOrigin = "https://challenges.cloudflare.com";

    // Keeps "·" and "…" readable in the markup; still encodes < > & " '.
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    /// <summary>HTML-encodes a value for element text or a double-quoted attribute.</summary>
    public static string Encode(string? value)
        => Encoder.Encode(value ?? string.Empty);

    public static string CreateNonce()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    /// <summary>
    /// The per-page policy. Without Turnstile it is the policy the reset pages
    /// already send. With Turnstile it adds Cloudflare to script-src and a
    /// frame-src for it, and nothing else. The stylesheet is a nonce'd
    /// &lt;link&gt;, so style-src needs no 'self'.
    /// </summary>
    public static string ContentSecurityPolicy(string nonce, bool allowTurnstile = false)
        => "default-src 'none'; "
           + $"script-src 'nonce-{nonce}'{(allowTurnstile ? " " + TurnstileOrigin : string.Empty)}; "
           + $"style-src 'nonce-{nonce}'; "
           + "connect-src 'self'; "
           + (allowTurnstile ? $"frame-src {TurnstileOrigin}; " : string.Empty)
           + "frame-ancestors 'none'; "
           + "base-uri 'none'; "
           + "form-action 'self'; "
           + "object-src 'none'";

    public static IResult Render(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        AdminSignedOutPageContent page)
    {
        ArgumentNullException.ThrowIfNull(page);

        AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
        var nonce = CreateNonce();
        http.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy(nonce, page.AllowTurnstile);

        var html = new StringBuilder(2048);
        html.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        if (page.IssueAntiForgery)
        {
            html.Append($"<meta name=\"{CsrfMetaName}\" content=\"{Encode(antiforgery.Issue(http))}\">");
        }

        html.Append($"<title>{Encode(page.Title)}</title>");
        html.Append($"<link rel=\"stylesheet\" href=\"{StylesheetPath}\" nonce=\"{nonce}\">");
        html.Append("</head><body><main class=\"auth-shell\"><div class=\"auth-card\">");
        html.Append($"<h1 id=\"page-heading\">{Encode(page.Heading)}</h1>");
        if (!string.IsNullOrEmpty(page.Noscript))
        {
            html.Append($"<noscript><p>{Encode(page.Noscript)}</p></noscript>");
        }

        html.Append(page.BodyHtml);
        html.Append("</div></main>");
        foreach (var script in page.Scripts)
        {
            html.Append($"<script src=\"{Encode(script)}\" nonce=\"{nonce}\"></script>");
        }

        html.Append("</body></html>");
        return Results.Content(html.ToString(), ContentType);
    }

    /// <summary>
    /// The security-check block: status line, widget mount and Retry. The
    /// public site key and the state copy travel as data attributes; the
    /// secret is never read here. With no site key the block says the check
    /// is unavailable and carries no key, so the script can never enable the
    /// form's submit button.
    /// The ids start with "security-check" on purpose. An element id becomes a
    /// property of <c>window</c>, and <c>id="turnstile"</c> would stand where
    /// Cloudflare's script installs <c>window.turnstile</c>: its "already
    /// loaded" guard could then skip the install and the form could never be
    /// submitted. No id or control name on a signed-out page may be the name
    /// of a global its scripts read.
    /// </summary>
    public static string TurnstileBlock(TurnstileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var siteKey = options.ConfiguredSiteKey();
        var action = options.ConfiguredAction();
        var html = new StringBuilder(512);
        html.Append("<div class=\"security-check\" id=\"security-check\"");
        if (siteKey is not null)
        {
            html.Append($" data-sitekey=\"{Encode(siteKey)}\"");
            if (action is not null)
            {
                html.Append($" data-action=\"{Encode(action)}\"");
            }
        }

        html.Append($" data-loading=\"{Encode(AdminSignedOutCopy.TurnstileLoading)}\"");
        html.Append($" data-failed=\"{Encode(AdminSignedOutCopy.TurnstileFailed)}\"");
        html.Append($" data-expired=\"{Encode(AdminSignedOutCopy.TurnstileExpired)}\"");
        html.Append($" data-unavailable=\"{Encode(AdminSignedOutCopy.TurnstileUnavailable)}\"");
        html.Append($" data-unconfigured=\"{Encode(AdminSignedOutCopy.TurnstileNotConfigured)}\">");
        html.Append("<p id=\"security-check-status\" class=\"security-check-status\" role=\"status\" tabindex=\"-1\">");
        html.Append(siteKey is null ? Encode(AdminSignedOutCopy.TurnstileNotConfigured) : string.Empty);
        html.Append("</p>");
        html.Append("<div id=\"security-check-widget\"></div>");
        html.Append(
            $"<button type=\"button\" id=\"security-check-retry\" class=\"secondary\" hidden>{Encode(AdminSignedOutCopy.TurnstileRetry)}</button>");
        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>
    /// The show toggle for one password field. Its label never changes: it is
    /// "Show", named after the field ("Show New password"), and
    /// <c>aria-pressed</c> alone says whether the password is shown. The
    /// field's label must carry the id <c>{inputId}-label</c>.
    /// </summary>
    public static string PasswordToggle(string inputId)
        => $"<button type=\"button\" id=\"{inputId}-toggle\" class=\"secondary toggle\" aria-pressed=\"false\"" +
           $" aria-controls=\"{inputId}\" aria-labelledby=\"{inputId}-toggle {inputId}-label\">" +
           $"{Encode(AdminSignedOutCopy.ShowPassword)}</button>";
}

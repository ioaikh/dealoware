using System.Net;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// The password-reset pages on the shared signed-out shell: S-A8
/// (GET /admin/reset), S-A9 (GET /admin/reset/sent), S-A10
/// (GET /admin/reset/confirm) and S-A5 (GET /admin/link-expired), and the two
/// sentences S-A1 can open with (S-A11 and "password changed").
/// The default host has no Turnstile site key, which is the fail-closed case;
/// <see cref="TurnstileSiteKeyWebApplicationFactory"/> is the configured case.
/// What the reset routes answer is covered by <see cref="AdminPasswordResetTests"/>;
/// what the pages do in a browser, by e2e/reset; what the S-A10 script does with
/// each reply, also by tests/Dealoware.Api.Tests/reset-confirm-harness.js.
/// </summary>
[Collection("WebAppTests")]
public class AdminPasswordResetPageTests : IClassFixture<TurnstileSiteKeyWebApplicationFactory>
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string Cloudflare = "challenges.cloudflare.com";

    private const string Request = AdminSignedOutExemptions.Reset;
    private const string Sent = AdminSignedOutExemptions.ResetSent;
    private const string Confirm = AdminSignedOutExemptions.ResetConfirm;
    private const string LinkExpired = AdminSignedOutExemptions.LinkExpired;

    private readonly IsolatedWebApplicationFactory _unconfigured;
    private readonly TurnstileSiteKeyWebApplicationFactory _configured;

    public AdminPasswordResetPageTests(
        IsolatedWebApplicationFactory unconfigured,
        TurnstileSiteKeyWebApplicationFactory configured)
    {
        _unconfigured = unconfigured;
        _configured = configured;
    }

    public static TheoryData<string, string, string> Pages => new()
    {
        { Request, AdminPasswordResetCopy.RequestPageTitle, AdminPasswordResetCopy.RequestHeading },
        { Sent, AdminPasswordResetCopy.SentPageTitle, AdminPasswordResetCopy.SentHeading },
        { Confirm, AdminPasswordResetCopy.ConfirmPageTitle, AdminPasswordResetCopy.ConfirmHeading },
        { LinkExpired, AdminPasswordResetCopy.LinkExpiredPageTitle, AdminPasswordResetCopy.LinkExpiredHeading }
    };

    /// <summary>The scripts of each page, in the order they load. All are files under /admin/auth/.</summary>
    private static string[] ScriptsOf(string path) => path switch
    {
        // First: it takes the reload flag out of the address bar before anything else can fail.
        Request =>
        [
            "/admin/auth/admin-arrival.js",
            "/admin/auth/admin-dom.js",
            "/admin/auth/admin-turnstile.js",
            "/admin/auth/admin-request.js",
            "/admin/auth/reset.js"
        ],
        // First: it takes the link token out of the address bar before anything else can fail.
        Confirm =>
        [
            "/admin/auth/admin-link.js",
            "/admin/auth/admin-dom.js",
            "/admin/auth/admin-password.js",
            "/admin/auth/reset-confirm.js"
        ],
        _ => ["/admin/auth/admin-heading.js"]
    };

    /// <summary>Only the two pages with a form to post issue the anti-forgery pair.</summary>
    private static bool PostsAForm(string path) => path is Request or Confirm;

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_ReturnsTheHtmlPage_InTheSharedShell_WithItsHeadersAndPolicy(string path, string title, string heading)
    {
        foreach (var factory in new[] { _configured, _unconfigured })
        {
            using var response = await GetAsync(factory, path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
            var html = await response.Content.ReadAsStringAsync();
            var nonce = SingleNonce(html);

            Assert.StartsWith("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">", html, StringComparison.Ordinal);
            Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">", html, StringComparison.Ordinal);
            Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
            Assert.EndsWith(" · Dealoware admin", title, StringComparison.Ordinal);
            Assert.Single(Regex.Matches(html, "<h1[ >]"));
            Assert.Contains($"<main class=\"auth-shell\"><div class=\"auth-card\"><h1 id=\"page-heading\">{heading}</h1>", html, StringComparison.Ordinal);
            Assert.Contains(
                $"<link rel=\"stylesheet\" href=\"{AdminSignedOutPage.StylesheetPath}\" nonce=\"{nonce}\">",
                html,
                StringComparison.Ordinal);

            // Every script is a same-origin file under /admin/auth/ carrying the nonce, in this order.
            var scripts = Regex.Matches(html, "<script src=\"([^\"]+)\" nonce=\"([^\"]+)\"></script>");
            Assert.Equal(ScriptsOf(path), scripts.Select(m => m.Groups[1].Value).ToArray());
            Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
            Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
            Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" style=", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" onclick=", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" onsubmit=", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<nav", html, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);

            // The placeholder answer is gone: no JSON, and no token outside the page.
            Assert.DoesNotContain("csrfToken", html, StringComparison.Ordinal);
            Assert.DoesNotContain("\"screen\"", html, StringComparison.Ordinal);
            Assert.False(response.Headers.Contains(AdminAntiForgeryService.HeaderName));

            Assert.Equal("no-store", Header(response, "Cache-Control"));
            Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
            Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
            Assert.Equal(AdminSecurityHeadersMiddleware.FrameOptions, Header(response, "X-Frame-Options"));

            // One policy, with the page nonce. Only S-A8 has the security check, so only it may
            // name Cloudflare, and only when a site key is configured.
            var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
            var withWidget = path == Request && ReferenceEquals(factory, _configured);
            Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce, withWidget), csp);
            Assert.Equal(withWidget ? 2 : 0, Regex.Matches(csp, Regex.Escape(Cloudflare)).Count);
            Assert.Equal(withWidget, csp.Contains("frame-src", StringComparison.Ordinal));
            foreach (var loose in new[] { "unsafe-inline", "unsafe-eval", "strict-dynamic", "*", "data:", "script-src 'self'", "style-src 'self'" })
            {
                Assert.DoesNotContain(loose, csp, StringComparison.Ordinal);
            }

            Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
            if (path != Request)
            {
                Assert.DoesNotContain("security-check", html, StringComparison.Ordinal);
                Assert.DoesNotContain("data-sitekey", html, StringComparison.Ordinal);
            }

            // A page with a form to post sets one cookie, the anti-forgery one, and carries its
            // token. A page that is only a message sets none and carries none.
            var cookies = SetCookies(response);
            if (PostsAForm(path))
            {
                var setCookie = Assert.Single(cookies);
                Assert.StartsWith(AdminCookieNames.AntiForgery + "=", setCookie, StringComparison.Ordinal);
                var cookieValue = setCookie.Split(';', 2)[0].Split('=', 2)[1];
                Assert.Equal(
                    factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(cookieValue),
                    AdminPageAntiForgery.ReadToken(html));
                Assert.DoesNotContain(cookieValue, html, StringComparison.Ordinal);
            }
            else
            {
                Assert.Empty(cookies);
                Assert.DoesNotContain(AdminSignedOutPage.CsrfMetaName, html, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Copy_IsTheDraftAndTheServerWording()
    {
        Assert.Equal("Reset password · Dealoware admin", AdminPasswordResetCopy.RequestPageTitle);
        Assert.Equal("Reset password", AdminPasswordResetCopy.RequestHeading);
        Assert.Equal("Request an email link to set a new password.", AdminPasswordResetCopy.RequestLede);
        Assert.Equal("Send reset link", AdminPasswordResetCopy.RequestSubmit);
        Assert.Equal("Check your email · Dealoware admin", AdminPasswordResetCopy.SentPageTitle);
        Assert.Equal("Check your email", AdminPasswordResetCopy.SentHeading);
        Assert.Equal("The link expires in 1 hour.", AdminPasswordResetCopy.SentExpiry);
        Assert.Equal(
            "Choose a new password and confirm with an authenticator or recovery code.",
            AdminPasswordResetCopy.ConfirmLede);
        Assert.Equal("Your session has ended. Sign in again to continue.", AdminSignInCopy.SessionEnded);
    }

    [Fact]
    public async Task RequestPage_HasOneEmailField_TheSecurityCheck_ADisabledSubmit_AndTheWayBack()
    {
        using var response = await GetAsync(_configured, Request);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // With JavaScript off: the heading, one sentence, and a panel only the script can show.
        Assert.Contains(
            $"<h1 id=\"page-heading\">{AdminPasswordResetCopy.RequestHeading}</h1>"
            + $"<noscript><p>{AdminPasswordResetCopy.RequestNoscript}</p></noscript>"
            + "<div id=\"reset-panel\" hidden>"
            + $"<p class=\"lede\">{AdminPasswordResetCopy.RequestLede}</p>"
            + "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>"
            + $"<form id=\"reset-request-form\" method=\"post\" action=\"{Request}\" novalidate"
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\" data-accepted=\"{AdminPasswordResetCopy.RequestAccepted}\">"
            + "<div class=\"field\">"
            + $"<label for=\"email\">{AdminSignInCopy.EmailLabel}</label>"
            + "<input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"username\" inputmode=\"email\""
            + " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"email-error\""
            + $" data-required=\"{AdminSignInCopy.EmailRequired}\" data-invalid=\"{AdminSignInCopy.EmailInvalid}\">"
            + "<span id=\"email-error\" class=\"field-error\" aria-live=\"polite\"></span>"
            + "</div>"
            + $"<div class=\"security-check\" id=\"security-check\" data-sitekey=\"{TurnstileSiteKeyWebApplicationFactory.SiteKey}\"",
            html,
            StringComparison.Ordinal);

        // The widget sits directly above the button, which only a fresh token can enable; then the way
        // back, outside the hidden panel so that it is there with JavaScript off too.
        Assert.Contains(
            "</button></div>"
            + "<button type=\"submit\" id=\"reset-submit\" disabled aria-describedby=\"security-check-status\">"
            + $"{AdminPasswordResetCopy.RequestSubmit}</button>"
            + "</form></div>"
            + $"<div class=\"links\"><a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">{AdminSignInCopy.BackToSignIn}</a></div>"
            + "</div></main>",
            html,
            StringComparison.Ordinal);

        Assert.Single(Regex.Matches(html, "<form "));
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.Single(Regex.Matches(html, "role=\"alert\""));
        Assert.DoesNotContain("type=\"password\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" value=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);

        // The neutral sentence is in the page once, as data: nothing shows it here.
        Assert.Single(Regex.Matches(html, Regex.Escape(AdminPasswordResetCopy.RequestAccepted)));
        Assert.DoesNotContain(">" + AdminPasswordResetCopy.RequestAccepted, html, StringComparison.Ordinal);
        foreach (var banned in new[] { "locked", "lockout", "minute", "hour", "attempt", "seconds", "remaining" })
        {
            Assert.DoesNotContain(banned, html, StringComparison.OrdinalIgnoreCase);
        }

        // No element id or control name is a global the page scripts read.
        var names = Regex.Matches(html, " (?:id|name)=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
        foreach (var global in new[] { "turnstile", "AdminArrival", "AdminDom", "AdminTurnstile", "AdminRequest", "location", "history", "status", "name" })
        {
            Assert.DoesNotContain(global, names);
        }
    }

    [Fact]
    public async Task RequestPage_WithoutASiteKey_FailsClosed_AndSaysTheCheckIsUnavailable()
    {
        using var response = await GetAsync(_unconfigured, Request);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("data-sitekey", html, StringComparison.Ordinal);
        Assert.Contains(
            "<p id=\"security-check-status\" class=\"security-check-status\" role=\"status\" tabindex=\"-1\">"
            + $"{AdminSignedOutCopy.TurnstileNotConfigured}</p>",
            html,
            StringComparison.Ordinal);
        Assert.Contains("<button type=\"submit\" id=\"reset-submit\" disabled ", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Pages_AreTheSameForEveryVisitor_AndEchoNothingFromTheAddress(string path, string title, string heading)
    {
        var probe = "probe" + Guid.NewGuid().ToString("N");
        using var plain = await GetAsync(_configured, path);
        using var asked = await GetAsync(
            _configured,
            $"{path}?email={probe}%40example.test&token={probe}&status={probe}&next=%2F{probe}");

        Assert.Equal(HttpStatusCode.OK, asked.StatusCode);
        var html = await asked.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{title}</title>", html, StringComparison.Ordinal);
        Assert.Contains(heading, html, StringComparison.Ordinal);
        Assert.DoesNotContain(probe, html, StringComparison.Ordinal);
        Assert.DoesNotContain("@", html, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, html, StringComparison.Ordinal);
        Assert.DoesNotContain("DEALOWARE_", html, StringComparison.Ordinal);
        Assert.DoesNotContain("siteverify", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WithoutRandomValues(await plain.Content.ReadAsStringAsync()), WithoutRandomValues(html));
    }

    [Fact]
    public async Task SentPage_ShowsTheServerSentence_TheLinkLifetime_AndTheWayBack_AndNothingToSend()
    {
        using var response = await GetAsync(_configured, Sent);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains(
            $"<h1 id=\"page-heading\">{AdminPasswordResetCopy.SentHeading}</h1>"
            + $"<p id=\"reset-sent-message\" class=\"lede\">{AdminPasswordResetCopy.RequestAccepted}</p>"
            + $"<p id=\"reset-sent-expiry\">{AdminPasswordResetCopy.SentExpiry}</p>"
            + $"<div class=\"links\"><a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">{AdminSignInCopy.BackToSignIn}</a></div>"
            + "</div></main>",
            html,
            StringComparison.Ordinal);

        // No form, no resend, no other link, and no sentence for "JavaScript is off": the page needs none.
        Assert.Single(Regex.Matches(html, "<a "));
        foreach (var absent in new[] { "<form", "<input", "<button", "<noscript", "resend", "send again", "role=\"alert\"" })
        {
            Assert.DoesNotContain(absent, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("GET", "/admin/reset/sent/", HttpStatusCode.Unauthorized)]
    [InlineData("GET", "/admin/reset/sent/again", HttpStatusCode.Found)]
    [InlineData("GET", "/admin/reset%2fsent", HttpStatusCode.Unauthorized)]
    [InlineData("GET", "/admin/reset/sent;x=1", HttpStatusCode.Unauthorized)]
    [InlineData("POST", "/admin/reset/sent", HttpStatusCode.Forbidden)]
    [InlineData("POST", "/admin/link-expired", HttpStatusCode.Forbidden)]
    public async Task OnlyTheExactPathAndMethod_ServesAResetPage_TheGateAnswersEverythingElse(
        string method,
        string path,
        HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Host = AdminHost;
        using var response = await Client(_configured).SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("page-heading", body, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPasswordResetCopy.RequestAccepted, WebUtility.HtmlDecode(body), StringComparison.Ordinal);
        if (expected == HttpStatusCode.Found)
        {
            // An extra segment is a signed-in page path: without a session it goes to sign-in,
            // and as no sign-in returns there, the only cookie touched is the kept page, removed.
            Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
            Assert.All(SetCookies(response), c => Assert.StartsWith(AdminReturnCookie.Name + "=;", c, StringComparison.Ordinal));
            return;
        }

        Assert.Empty(SetCookies(response));
    }

    [Theory]
    [InlineData("reset.js")]
    [InlineData("reset-confirm.js")]
    [InlineData("admin-heading.js")]
    public async Task PageScripts_AreServedSignedOut_AndKeepToTheRules(string file)
    {
        using var response = await GetAsync(_configured, "/admin/auth/" + file);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        var served = await response.Content.ReadAsStringAsync();
        var script = File.ReadAllText(AssetPath(file));
        Assert.Equal(script, served);

        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "eval(", "localStorage",
                     "sessionStorage", "indexedDB", "document.cookie", "mode:", "window.open", "http://", "https://"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RequestScript_SendsOnlyTheContractFields_AndMovesOnOnlyForTheExactReply()
    {
        var script = File.ReadAllText(AssetPath("reset.js")).Replace("\r\n", "\n");

        Assert.Equal("/admin/auth/reset.js", AdminPasswordResetPages.RequestScriptPath);
        Assert.Contains($"var API = \"{AdminSignedOutExemptions.ApiReset}\";", script, StringComparison.Ordinal);
        Assert.Contains($"var SENT_PAGE = \"{AdminSignedOutExemptions.ResetSent}\";", script, StringComparison.Ordinal);

        // One request, through the shared helper (JSON, the anti-forgery token, this site only), with two fields.
        Assert.Single(Regex.Matches(script, @"AdminRequest\.post\("));
        Assert.DoesNotContain("fetch(", script, StringComparison.Ordinal);
        Assert.Contains(
            "AdminRequest.post(API, {\n      email: email.value.trim(),\n      turnstileToken: turnstileToken\n    })",
            script,
            StringComparison.Ordinal);

        // It leaves the page in one place: a 200 that carries exactly the server's sentence.
        Assert.Single(Regex.Matches(script, @"location\.(replace|assign|href)"));
        Assert.Contains(
            "if (reply.status === 200) {\n        if (message && message === form.getAttribute(\"data-accepted\")) location.replace(SENT_PAGE);\n        else unclear();\n        return;\n      }",
            script,
            StringComparison.Ordinal);

        // The server's words as sent: the refused security check (401) and the throttle (429).
        Assert.Contains("if (reply.status === 401 && error) stay(error);", script, StringComparison.Ordinal);
        Assert.Contains("else if (reply.status === 429 && message) stay(message);", script, StringComparison.Ordinal);

        // No request without a fresh security-check token, and a spent one is replaced after every reply.
        Assert.Contains(
            "if (AdminTurnstile.state() !== \"ready\" || !turnstileToken) return;\n    send(turnstileToken);",
            script,
            StringComparison.Ordinal);
        Assert.Contains("submit.disabled = busy || !ready;", script, StringComparison.Ordinal);
        Assert.Contains("AdminTurnstile.retry();", script, StringComparison.Ordinal);

        // A 403 means a stale anti-forgery pair: load the page anew, once.
        Assert.Contains("var mayReload = AdminArrival.flag() !== \"fresh\";", script, StringComparison.Ordinal);
        Assert.Contains("if (reply.status === 403 && mayReload) {", script, StringComparison.Ordinal);
        Assert.Contains("if (reply.status !== 403) mayReload = true;", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"AdminArrival\.reload\(\)"));

        // Without its helpers the page says so and shows no form. The check comes before any helper is used.
        var guardAt = script.IndexOf("if (typeof AdminArrival === \"undefined\"", StringComparison.Ordinal);
        Assert.True(guardAt > 0);
        Assert.True(guardAt < script.IndexOf("AdminDom.", StringComparison.Ordinal));
        Assert.True(guardAt < script.IndexOf("AdminArrival.flag()", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmPage_HasTheSetupPagesPasswordFields_OneSecondFactorAtATime_AndTheSuccessSentence()
    {
        using var response = await GetAsync(_configured, Confirm);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // The success sentence the page waits for is the server's own, carried on the form.
        Assert.Contains(
            "<form method=\"post\" action=\"/admin/reset/confirm\" id=\"reset-form\" hidden novalidate"
            + $" data-retry=\"{AdminBootstrapCopy.SubmitRetry}\""
            + $" data-changed=\"{AdminPasswordResetCopy.CompleteSucceeded}\">"
            + $"<p class=\"lede\">{AdminPasswordResetCopy.ConfirmLede}</p>"
            + "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>",
            html,
            StringComparison.Ordinal);
        Assert.Equal(AdminBootstrapCopy.SubmitRetry, AdminPasswordResetCopy.ConfirmRetry);

        // New password and Confirm password are the first-time setup page's, byte for byte:
        // labels, helper, the server's length limits and messages, the show toggles, the mismatch text.
        using var setup = await GetAsync(_configured, AdminSignedOutExemptions.Bootstrap);
        var setupHtml = WebUtility.HtmlDecode(await setup.Content.ReadAsStringAsync());
        var setupFields = Regex.Match(setupHtml, "<div class=\"field\"><label id=\"new-password-label\"[\\s\\S]*?confirm-password-error\" class=\"field-error\" aria-live=\"polite\"></span></div>");
        Assert.True(setupFields.Success);
        Assert.Contains(setupFields.Value, html, StringComparison.Ordinal);
        Assert.Contains($"<span id=\"password-helper\" class=\"helper\">{AdminSignedOutCopy.PasswordHelper}</span>", html, StringComparison.Ordinal);
        Assert.Contains($" data-too-short=\"{AdminPasswordRules.TooShortMessage}\"", html, StringComparison.Ordinal);
        Assert.Contains($" data-too-long=\"{AdminPasswordRules.TooLongMessage}\"", html, StringComparison.Ordinal);
        Assert.Contains($" data-mismatch=\"{AdminSignedOutCopy.PasswordMismatch}\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"new-password\" aria-labelledby=\"new-password-toggle new-password-label\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"confirm-password\" aria-labelledby=\"confirm-password-toggle confirm-password-label\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"confirm", html, StringComparison.Ordinal);

        // The second factor: the authenticator code on screen, the recovery code hidden, both with the
        // sign-in pages' labels and messages (UXR-A04: one-time-code; numeric keypad for the code only).
        Assert.Contains(
            "<div class=\"field\" id=\"code-field\">"
            + $"<label for=\"code\">{AdminSignInCopy.CodeLabel}</label>"
            + "<input id=\"code\" name=\"totp\" type=\"text\" inputmode=\"numeric\" autocomplete=\"one-time-code\""
            + " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"code-error\""
            + $" data-required=\"{AdminSignInCopy.CodeRequired}\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<div class=\"field\" id=\"recovery-code-field\" hidden>"
            + $"<label for=\"recovery-code\">{AdminSignInCopy.RecoveryLabel}</label>"
            + "<input id=\"recovery-code\" name=\"recoveryCode\" type=\"text\" autocomplete=\"one-time-code\""
            + " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"recovery-code-error\""
            + $" data-required=\"{AdminSignInCopy.RecoveryRequired}\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<button type=\"submit\" id=\"reset-submit\">{AdminPasswordResetCopy.ConfirmSubmit}</button>"
            + "<div class=\"links\"><button type=\"button\" id=\"factor-switch\" class=\"secondary\" aria-controls=\"code-field recovery-code-field\""
            + $" data-to-recovery=\"{AdminSignInCopy.UseRecoveryCode}\" data-to-code=\"{AdminSignInCopy.UseAuthenticatorCode}\">"
            + $"{AdminSignInCopy.UseRecoveryCode}</button></div></form></div>",
            html,
            StringComparison.Ordinal);

        // No security check on this page (decided: the link and the second factor are what it asks for),
        // and the token is never in the page.
        Assert.DoesNotContain("security-check", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"token\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmScript_MovesOnOnlyForTheServersExactSuccessReply()
    {
        // The server's success reply: 200 with exactly this sentence as "message".
        var endpoints = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminPasswordResetEndpoints.cs"));
        Assert.Contains("return Results.Json(new { message = AdminPasswordResetCopy.CompleteSucceeded });", endpoints, StringComparison.Ordinal);

        var script = File.ReadAllText(AssetPath("reset-confirm.js")).Replace("\r\n", "\n");
        Assert.Single(Regex.Matches(script, @"location\.replace\(SIGN_IN\)"));
        Assert.Contains(
            "if (r.status === 200) {\n        var message = text(data.message);\n"
            + "        if (message && message === form.getAttribute(\"data-changed\")) location.replace(SIGN_IN);\n"
            + "        else afterUnclearReply();",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain("r.ok", script, StringComparison.Ordinal);
        // One script, one way of asking: the token from admin-link.js, the anti-forgery token as X-CSRF-TOKEN.
        Assert.Contains("var token = AdminLink.takeToken();", script, StringComparison.Ordinal);
        Assert.Contains("\"X-CSRF-TOKEN\": csrf", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"\bfetch\("));
    }

    [Fact]
    public async Task SignIn_HoldsTheThreeArrivalSentences_AndShowsOneOnlyForItsFlag()
    {
        // S-A11, "password changed" and "signed out": one fixed word after the sign-in address,
        // nothing about the visitor.
        Assert.Equal("/admin/sign-in#ended", AdminSignInPages.SessionEndedLocation);
        Assert.Equal("/admin/sign-in#changed", AdminSignInPages.PasswordChangedLocation);
        Assert.Equal("/admin/sign-in#signed-out", AdminSignInPages.SignedOutLocation);

        using var response = await GetAsync(_configured, AdminSignedOutExemptions.SignIn);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // A status line, not the alert: hidden and empty until the script fills it, above the alert and the form.
        Assert.Contains(
            $"<p class=\"lede\">{AdminSignInCopy.SignInLede}</p>"
            + "<p id=\"form-status\" class=\"status\" role=\"status\" hidden"
            + $" data-ended=\"{AdminSignInCopy.SessionEnded}\""
            + $" data-changed=\"{AdminPasswordResetCopy.CompleteSucceeded}\""
            + $" data-signed-out=\"{AdminSignInCopy.SignedOut}\"></p>"
            + "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, Regex.Escape(AdminSignInCopy.SessionEnded)));
        Assert.Single(Regex.Matches(html, Regex.Escape(AdminPasswordResetCopy.CompleteSucceeded)));
        Assert.Single(Regex.Matches(html, Regex.Escape(AdminSignInCopy.SignedOut)));
        Assert.Equal("You've been signed out.", AdminSignInCopy.SignedOut);

        // None of the sentences says or implies that a lock ended, or gives a time.
        foreach (var sentence in new[] { AdminSignInCopy.SessionEnded, AdminPasswordResetCopy.CompleteSucceeded, AdminSignInCopy.SignedOut })
        {
            foreach (var banned in new[] { "lock", "minute", "hour", "attempt", "expired after" })
            {
                Assert.DoesNotContain(banned, sentence, StringComparison.OrdinalIgnoreCase);
            }
        }

        var page = File.ReadAllText(AssetPath("sign-in.js"));
        Assert.Contains(
            "if (arrival === \"ended\" || arrival === \"changed\" || arrival === \"signed-out\") {",
            page,
            StringComparison.Ordinal);
        Assert.Contains("showStatus(statusEl && statusEl.getAttribute(\"data-\" + arrival));", page, StringComparison.Ordinal);
        // A new try clears it, so it never stands next to the answer to that try.
        Assert.Matches(@"hideAlert\(\);\s+hideStatus\(\);\s+var emailMessage = checkEmail\(\);", page);

        // S-A10 goes there once the server has changed the password, and nowhere else on success.
        var confirm = File.ReadAllText(AssetPath("reset-confirm.js"));
        Assert.Contains(
            $"var SIGN_IN = \"{AdminSignInPages.PasswordChangedLocation}\";",
            confirm,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(confirm, @"location\.replace\(SIGN_IN\)"));
    }

    /// <summary>The page as text, minus the nonce and the anti-forgery token (random, not copy).</summary>
    private static string WithoutRandomValues(string html)
        => Regex.Replace(html, "(nonce|content)=\"[^\"]*\"", string.Empty);

    /// <summary>Every nonce attribute in the page carries the same value; returns it.</summary>
    private static string SingleNonce(string html)
    {
        var nonces = Regex.Matches(html, "nonce=\"([^\"]+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();
        return Assert.Single(nonces);
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return string.Join(",", values);
        }

        return response.Content.Headers.TryGetValues(name, out var contentValues)
            ? string.Join(",", contentValues)
            : string.Empty;
    }

    private static IReadOnlyList<string> SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    private static async Task<HttpResponseMessage> GetAsync(IsolatedWebApplicationFactory factory, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        return await Client(factory).SendAsync(request);
    }

    private static HttpClient Client(IsolatedWebApplicationFactory factory)
        => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static string AssetPath(string file)
        => Path.Combine(RepoRoot(), "src", "Dealoware.Api", "wwwroot", "admin", "auth", file);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}

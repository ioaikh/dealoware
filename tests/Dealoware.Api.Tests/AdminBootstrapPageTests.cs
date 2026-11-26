using System.Net;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Host with a public Turnstile site key, a widget action and a secret
/// configured, so the page tests can show the key is rendered and the secret
/// never is. All three values are generated or fixed test values, not real keys.
/// </summary>
public sealed class TurnstileSiteKeyWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string SiteKey = "test-site-key_0x4AAAAAAA-example";
    public const string Action = "admin-form";

    public string Secret { get; } = "test-secret-" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Admin:Turnstile:SiteKey", "  " + SiteKey + " ");
        builder.UseSetting("Admin:Turnstile:ExpectedAction", Action);
        builder.UseSetting(TurnstileOptions.SecretEnvironmentVariable, Secret);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// The S-A4 page (GET /admin/bootstrap) and the shared signed-out page shell.
/// The default host has no Turnstile site key, which is the fail-closed case;
/// <see cref="TurnstileSiteKeyWebApplicationFactory"/> is the configured case.
/// What the page does in a browser is covered by e2e/bootstrap.
/// </summary>
[Collection("WebAppTests")]
public class AdminBootstrapPageTests : IClassFixture<TurnstileSiteKeyWebApplicationFactory>
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string Cloudflare = "challenges.cloudflare.com";

    private readonly IsolatedWebApplicationFactory _unconfigured;
    private readonly TurnstileSiteKeyWebApplicationFactory _configured;

    public AdminBootstrapPageTests(
        IsolatedWebApplicationFactory unconfigured,
        TurnstileSiteKeyWebApplicationFactory configured)
    {
        _unconfigured = unconfigured;
        _configured = configured;
    }

    [Fact]
    public async Task Get_ReturnsTheHtmlPage_InTheSharedShell()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        var html = await response.Content.ReadAsStringAsync();
        var nonce = SingleNonce(html);

        Assert.StartsWith("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">", html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">", html, StringComparison.Ordinal);
        Assert.Contains($"<title>{AdminBootstrapCopy.PageTitle}</title>", html, StringComparison.Ordinal);
        Assert.Equal("Set password · Dealoware admin", AdminBootstrapCopy.PageTitle);
        Assert.Single(Regex.Matches(html, "<h1[ >]"));
        Assert.Contains($"<h1 id=\"page-heading\">{AdminBootstrapCopy.Heading}</h1>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<link rel=\"stylesheet\" href=\"{AdminSignedOutPage.StylesheetPath}\" nonce=\"{nonce}\">",
            html,
            StringComparison.Ordinal);

        // Every script is a same-origin file under /admin/auth/ carrying the nonce, in this order.
        var scripts = Regex.Matches(html, "<script src=\"([^\"]+)\" nonce=\"([^\"]+)\"></script>");
        Assert.Equal(
            new[]
            {
                // First: it takes the link token out of the address bar before anything else can fail.
                "/admin/auth/admin-link.js",
                "/admin/auth/admin-dom.js",
                "/admin/auth/admin-password.js",
                "/admin/auth/admin-turnstile.js",
                "/admin/auth/bootstrap.js"
            },
            scripts.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
        Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
        Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onclick=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_WithoutJavaScript_ShowsOneSentenceAndNoForm()
    {
        using var response = await GetAsync(_unconfigured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal("Turn on JavaScript to set your password.", AdminBootstrapCopy.Noscript);
        var noscript = $"<noscript><p>{AdminBootstrapCopy.Noscript}</p></noscript>";
        var noscriptAt = html.IndexOf(noscript, StringComparison.Ordinal);
        var panelAt = html.IndexOf("<div id=\"bootstrap-panel\" hidden>", StringComparison.Ordinal);
        var formAt = html.IndexOf("<form ", StringComparison.Ordinal);
        var ledeAt = html.IndexOf("class=\"lede\"", StringComparison.Ordinal);
        var mainEndAt = html.IndexOf("</div></main>", StringComparison.Ordinal);

        Assert.True(noscriptAt > 0, "the noscript sentence must be in the page");
        Assert.Single(Regex.Matches(html, "<noscript>"));
        // Everything after that sentence is hidden until the script shows it: the three
        // link-check elements, then the panel that holds the lede and the form.
        var afterNoscript = WebUtility.HtmlDecode(html[(noscriptAt + noscript.Length)..panelAt]);
        Assert.Equal(
            $"<p id=\"link-status\" role=\"status\" hidden data-checking=\"{AdminPasswordResetCopy.CheckingLink}\"></p>"
            + $"<p id=\"link-alert\" hidden tabindex=\"-1\" data-retry=\"{AdminPasswordResetCopy.CheckRetry}\"></p>"
            + $"<button type=\"button\" id=\"link-retry\" class=\"secondary\" hidden data-busy=\"{AdminSignedOutCopy.Checking}\">"
            + $"{AdminPasswordResetCopy.TryAgain}</button>",
            afterNoscript);
        Assert.True(ledeAt > panelAt && formAt > panelAt, "the lede and the form are inside the hidden panel");
        Assert.EndsWith("</form></div>", html[..mainEndAt], StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<form "));
    }

    [Fact]
    public async Task Get_SetsNoStoreNoReferrer_AndIssuesTheAntiForgeryPair()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.FrameOptions, Header(response, "X-Frame-Options"));

        var setCookie = Assert.Single(SetCookies(response));
        Assert.StartsWith(AdminCookieNames.AntiForgery + "=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/admin", setCookie, StringComparison.OrdinalIgnoreCase);
        var cookieValue = setCookie.Split(';', 2)[0].Split('=', 2)[1];

        var meta = Assert.Single(Regex.Matches(html, "<meta name=\"dw-csrf\" content=\"([0-9A-F]+)\">"));
        var token = meta.Groups[1].Value;
        Assert.Equal(
            _configured.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(cookieValue),
            token);
        Assert.NotEqual(cookieValue, token);
        Assert.DoesNotContain(cookieValue, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Csp_WithASiteKey_AddsCloudflareForScriptAndFrame_AndNothingElse()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();
        var nonce = SingleNonce(html);

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Equal(
            "default-src 'none'; "
            + $"script-src 'nonce-{nonce}' https://challenges.cloudflare.com; "
            + $"style-src 'nonce-{nonce}'; "
            + "connect-src 'self'; "
            + "frame-src https://challenges.cloudflare.com; "
            + "frame-ancestors 'none'; "
            + "base-uri 'none'; "
            + "form-action 'self'; "
            + "object-src 'none'",
            csp);
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce, allowTurnstile: true), csp);
        Assert.Equal(2, Regex.Matches(csp, Regex.Escape(Cloudflare)).Count);
        AssertNoLooseSources(csp);

        // The page markup itself never names Cloudflare: the widget script adds its script tag.
        Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Csp_WithoutASiteKey_IsTheBasePolicy_WithNoCloudflare()
    {
        using var response = await GetAsync(_unconfigured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();
        var nonce = SingleNonce(html);

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Equal(
            "default-src 'none'; "
            + $"script-src 'nonce-{nonce}'; "
            + $"style-src 'nonce-{nonce}'; "
            + "connect-src 'self'; "
            + "frame-ancestors 'none'; "
            + "base-uri 'none'; "
            + "form-action 'self'; "
            + "object-src 'none'",
            csp);
        Assert.DoesNotContain(Cloudflare, csp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame-src", csp, StringComparison.Ordinal);
        AssertNoLooseSources(csp);
        Assert.NotEqual(AdminSecurityHeadersMiddleware.ContentSecurityPolicy, csp);
    }

    [Fact]
    public async Task Csp_NonceIsFreshOnEveryResponse()
    {
        using var first = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        using var second = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);

        var firstNonce = SingleNonce(await first.Content.ReadAsStringAsync());
        var secondNonce = SingleNonce(await second.Content.ReadAsStringAsync());

        Assert.NotEqual(firstNonce, secondNonce);
        Assert.True(Convert.FromBase64String(firstNonce).Length >= 16);
    }

    [Theory]
    [InlineData("/admin/reset/confirm")]
    [InlineData("/admin/link-expired?for=reset")]
    [InlineData("/admin/link-expired?for=setup")]
    public async Task ExistingPages_StillDoNotAllowCloudflare_EvenWithASiteKeyConfigured(string path)
    {
        using var response = await GetAsync(_configured, path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var nonce = Regex.Match(html, "nonce=\"([^\"]+)\"").Groups[1].Value;

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Equal(AdminPasswordResetEndpoints.ConfirmPageContentSecurityPolicy(nonce), csp);
        Assert.DoesNotContain(Cloudflare, csp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame-src", csp, StringComparison.Ordinal);
        Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TurnstileSiteKeyWebApplicationFactory.SiteKey, html, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedPolicy_WithoutTurnstile_IsExactlyTheResetPagesPolicy()
    {
        var nonce = AdminSignedOutPage.CreateNonce();

        Assert.Equal(
            AdminPasswordResetEndpoints.ConfirmPageContentSecurityPolicy(nonce),
            AdminSignedOutPage.ContentSecurityPolicy(nonce));
        Assert.Equal(
            AdminSignedOutPage.ContentSecurityPolicy(nonce),
            AdminSignedOutPage.ContentSecurityPolicy(nonce, allowTurnstile: false));
        Assert.Equal("https://challenges.cloudflare.com", AdminSignedOutPage.TurnstileOrigin);
    }

    [Fact]
    public async Task Markup_CarriesNoLinkTokenAndNoPrivateKey()
    {
        var linkToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        using var response = await GetAsync(
            _configured,
            $"{AdminBootstrapEndpoints.PagePath}?token={linkToken}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(linkToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, html, StringComparison.Ordinal);
        Assert.DoesNotContain(TurnstileOptions.SecretEnvironmentVariable, html, StringComparison.Ordinal);
        Assert.DoesNotContain("DEALOWARE_", html, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"token\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("siteverify", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", html, StringComparison.Ordinal);
        Assert.All(SetCookies(response), c => Assert.DoesNotContain(linkToken, c, StringComparison.Ordinal));
        Assert.All(
            response.Headers.SelectMany(h => h.Value),
            v => Assert.DoesNotContain(_configured.Secret, v, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SiteKey_WhenConfigured_IsRendered_AndSubmitStartsDisabled()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();

        // Trimmed, and rendered once.
        Assert.Contains(
            $"<div class=\"security-check\" id=\"security-check\" data-sitekey=\"{TurnstileSiteKeyWebApplicationFactory.SiteKey}\""
            + $" data-action=\"{TurnstileSiteKeyWebApplicationFactory.Action}\"",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, Regex.Escape(TurnstileSiteKeyWebApplicationFactory.SiteKey)));
        Assert.Contains(
            "<p id=\"security-check-status\" class=\"security-check-status\" role=\"status\" tabindex=\"-1\"></p>",
            html,
            StringComparison.Ordinal);
        Assert.Contains("<div id=\"security-check-widget\"></div>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<button type=\"button\" id=\"security-check-retry\" class=\"secondary\" hidden>{AdminSignedOutCopy.TurnstileRetry}</button>",
            html,
            StringComparison.Ordinal);
        AssertSubmitDisabledAfterTheWidget(html);
    }

    [Fact]
    public async Task SiteKey_WhenMissing_FailsClosed_SaysUnavailable_AndNeverNamesCloudflare()
    {
        using var response = await GetAsync(_unconfigured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("data-sitekey", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-action", html, StringComparison.Ordinal);
        Assert.Equal("The security check is unavailable.", AdminSignedOutCopy.TurnstileNotConfigured);
        Assert.Contains(
            "<p id=\"security-check-status\" class=\"security-check-status\" role=\"status\" tabindex=\"-1\">"
            + AdminSignedOutCopy.TurnstileNotConfigured + "</p>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(" id=\"security-check-retry\" class=\"secondary\" hidden>", html, StringComparison.Ordinal);
        Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
        AssertSubmitDisabledAfterTheWidget(html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TurnstileBlock_BlankSiteKey_IsNotConfigured(string? siteKey)
    {
        var options = new TurnstileOptions { SiteKey = siteKey, ExpectedAction = "ignored-without-a-key" };

        Assert.Null(options.ConfiguredSiteKey());
        var block = AdminSignedOutPage.TurnstileBlock(options);
        Assert.DoesNotContain("data-sitekey", block, StringComparison.Ordinal);
        Assert.DoesNotContain("data-action", block, StringComparison.Ordinal);
        Assert.Contains(">" + AdminSignedOutCopy.TurnstileNotConfigured + "</p>", block, StringComparison.Ordinal);
    }

    [Fact]
    public void TurnstileBlock_EncodesTheSiteKeyAndAction()
    {
        var block = AdminSignedOutPage.TurnstileBlock(new TurnstileOptions
        {
            SiteKey = "key\"><script>alert(1)</script>",
            ExpectedAction = "act\"'<&"
        });

        Assert.DoesNotContain("<script", block, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=\"turnstile\"", block, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-sitekey=\"key&quot;&gt;&lt;script&gt;alert(1)&lt;/script&gt;\"", block, StringComparison.Ordinal);
        Assert.Contains("data-action=\"act&quot;&#x27;&lt;&amp;\"", block, StringComparison.Ordinal);
        Assert.Equal("a&#x27;b · c…", AdminSignedOutPage.Encode("a'b · c…"));
    }

    [Fact]
    public async Task NoElementIdOrControlName_IsAGlobalThePageScriptsRead()
    {
        // An element id (and some element names) is also a property of window. One spelled
        // like a global a script reads would stand in for it: id="turnstile" would sit where
        // Cloudflare installs window.turnstile, and its script could then refuse to install.
        var globals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "turnstile", "AdminLink", "AdminDom", "AdminPassword", "AdminTurnstile",
            "window", "document", "location", "history", "fetch", "URL", "JSON", "Object", "Array",
            "AbortController", "setTimeout", "clearTimeout", "parseInt", "isNaN", "decodeURIComponent"
        };

        // Every name the scripts reach through their window parameter is on that list.
        var reached = new[] { "admin-link.js", "admin-dom.js", "admin-password.js", "admin-turnstile.js", "bootstrap.js" }
            .SelectMany(file => Regex.Matches(File.ReadAllText(AssetPath(file)), @"\bglobal\.([A-Za-z_$][\w$]*)"))
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.Contains("turnstile", reached);
        Assert.All(reached, name => Assert.Contains(name, globals));

        foreach (var factory in new[] { _configured, _unconfigured })
        {
            using var response = await GetAsync(factory, AdminBootstrapEndpoints.PagePath);
            var html = await response.Content.ReadAsStringAsync();
            var names = Regex.Matches(html, "\\s(?:id|name)=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

            Assert.Contains("security-check", names);
            Assert.Contains("newPassword", names);
            Assert.All(names, name => Assert.DoesNotContain(name, globals));
            Assert.DoesNotContain("id=\"turnstile\"", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name=\"turnstile\"", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Form_ShowsThePasswordRulesAsTheServerDefinesThem()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);

        Assert.Contains($"data-min-length=\"{AdminPasswordRules.MinCodePoints}\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-max-length=\"{AdminPasswordRules.MaxCodePoints}\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-too-short=\"{AdminPasswordRules.TooShortMessage}\"", text, StringComparison.Ordinal);
        Assert.Contains($"data-too-long=\"{AdminPasswordRules.TooLongMessage}\"", text, StringComparison.Ordinal);
        Assert.Contains(
            $"<span id=\"password-helper\" class=\"helper\">{AdminSignedOutCopy.PasswordHelper}</span>",
            html,
            StringComparison.Ordinal);
        // The helper states the server's limits; it cannot drift from the rule.
        Assert.StartsWith(
            $"Use {AdminPasswordRules.MinCodePoints} to {AdminPasswordRules.MaxCodePoints} characters.",
            AdminSignedOutCopy.PasswordHelper,
            StringComparison.Ordinal);
        Assert.Contains($"data-mismatch=\"{AdminSignedOutCopy.PasswordMismatch}\"", text, StringComparison.Ordinal);
        Assert.Contains($"data-retry=\"{AdminBootstrapCopy.SubmitRetry}\"", text, StringComparison.Ordinal);
        // No strength meter and no composition rules.
        Assert.DoesNotContain("<meter", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<progress", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pattern=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxlength", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Form_IsLabelledAndAnnounced()
    {
        using var response = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            $"<label id=\"new-password-label\" for=\"new-password\">{AdminBootstrapCopy.NewPasswordLabel}</label>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<label id=\"confirm-password-label\" for=\"confirm-password\">{AdminBootstrapCopy.ConfirmPasswordLabel}</label>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<input id=\"new-password\" name=\"newPassword\" type=\"password\" autocomplete=\"new-password\" required"
            + " aria-describedby=\"password-helper new-password-error\"",
            html,
            StringComparison.Ordinal);
        // Confirm password has no name: it is compared in the browser and never sent.
        Assert.Contains(
            "<input id=\"confirm-password\" type=\"password\" autocomplete=\"new-password\" required"
            + " aria-describedby=\"confirm-password-error\"",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, " name=\"newPassword\""));
        Assert.Single(Regex.Matches(html, "<input [^>]*name="));
        Assert.Contains(
            "<span id=\"new-password-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<span id=\"confirm-password-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "role=\"alert\""));
        Assert.Contains(
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>",
            html,
            StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("id=\"form-alert\"", StringComparison.Ordinal) < html.IndexOf("<form ", StringComparison.Ordinal),
            "the alert region sits above the form");
        // The toggle is "Show", named after its field, and aria-pressed alone says whether
        // the password is shown: there is no second label for the script to switch to.
        foreach (var field in new[] { "new-password", "confirm-password" })
        {
            Assert.Contains(
                $"<button type=\"button\" id=\"{field}-toggle\" class=\"secondary toggle\" aria-pressed=\"false\""
                + $" aria-controls=\"{field}\" aria-labelledby=\"{field}-toggle {field}-label\">"
                + $"{AdminSignedOutCopy.ShowPassword}</button>",
                html,
                StringComparison.Ordinal);
        }

        Assert.Equal("Show", AdminSignedOutCopy.ShowPassword);
        Assert.DoesNotContain("Hide", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hide", html, StringComparison.Ordinal);
        var toggleScript = File.ReadAllText(AssetPath("admin-password.js"));
        var bind = toggleScript[toggleScript.IndexOf("function bindToggle", StringComparison.Ordinal)..];
        bind = bind[..bind.IndexOf("global.AdminPassword", StringComparison.Ordinal)];
        Assert.Contains("button.setAttribute(\"aria-pressed\", reveal ? \"true\" : \"false\");", bind, StringComparison.Ordinal);
        Assert.DoesNotContain("setText", bind, StringComparison.Ordinal);
        Assert.DoesNotContain("textContent", bind, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-label", bind, StringComparison.Ordinal);

        Assert.Contains(
            $"<form id=\"bootstrap-form\" method=\"post\" action=\"{AdminBootstrapEndpoints.PagePath}\" novalidate",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onpaste", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<nav", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Copy_HasNoDurationCountOrLockWording()
    {
        using var configured = await GetAsync(_configured, AdminBootstrapEndpoints.PagePath);
        using var unconfigured = await GetAsync(_unconfigured, AdminBootstrapEndpoints.PagePath);
        var pages = new[]
        {
            WithoutRandomValues(await configured.Content.ReadAsStringAsync()),
            WithoutRandomValues(await unconfigured.Content.ReadAsStringAsync())
        };

        foreach (var page in pages)
        {
            foreach (var banned in new[] { "locked", "lockout", "minute", "hour", "attempt", "seconds", "try again in" })
            {
                Assert.DoesNotContain(banned, page, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("/admin/auth/signed-out.css", "text/css")]
    [InlineData("/admin/auth/admin-link.js", "text/javascript")]
    [InlineData("/admin/auth/admin-dom.js", "text/javascript")]
    [InlineData("/admin/auth/admin-password.js", "text/javascript")]
    [InlineData("/admin/auth/admin-turnstile.js", "text/javascript")]
    [InlineData("/admin/auth/bootstrap.js", "text/javascript")]
    public async Task PageAssets_AreServedSignedOut_WithNothingPrivateAndNoInlineCopy(string path, string mediaType)
    {
        using var response = await GetAsync(_configured, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DEALOWARE_", body, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain("siteverify", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerHTML", body, StringComparison.Ordinal);
        Assert.DoesNotContain("document.write", body, StringComparison.Ordinal);
        Assert.DoesNotContain("eval(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sessionStorage", body, StringComparison.Ordinal);
        Assert.DoesNotContain("document.cookie", body, StringComparison.Ordinal);
        // Copy lives in the C# constants classes and reaches the scripts as data attributes.
        foreach (var copy in new[]
                 {
                     AdminSignedOutCopy.TurnstileLoading, AdminSignedOutCopy.TurnstileFailed,
                     AdminSignedOutCopy.TurnstileExpired, AdminSignedOutCopy.TurnstileNotConfigured,
                     AdminSignedOutCopy.PasswordMismatch, AdminBootstrapCopy.SubmitRetry,
                     AdminPasswordRules.TooShortMessage, AdminPasswordRules.TooLongMessage,
                     AdminAuthCopy.VerificationFailed, AdminPasswordResetCopy.SignInGeneric,
                     AdminPasswordResetCopy.CheckingLink, AdminPasswordResetCopy.CheckRetry,
                     AdminSignedOutCopy.Checking
                 })
        {
            Assert.DoesNotContain(copy, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Stylesheet_LoadsNothingElse_AndKeepsAVisibleFocusRing()
    {
        var css = File.ReadAllText(AssetPath("signed-out.css"));

        Assert.Contains("Signed-out admin auth asset", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@import", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url(", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("outline: none", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("outline:none", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("outline: 0", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outline: 2px solid var(--focus);", css, StringComparison.Ordinal);
        Assert.Contains("outline-offset: 2px;", css, StringComparison.Ordinal);
        Assert.Contains("[hidden] { display: none !important; }", css, StringComparison.Ordinal);
    }

    [Fact]
    public void LinkScript_TakesTheTokenOutOfTheAddressBar_LikeTheResetPage_BeforeAnythingElse()
    {
        var link = File.ReadAllText(AssetPath("admin-link.js"));
        var reset = File.ReadAllText(AssetPath("reset-confirm.js"));
        var page = File.ReadAllText(AssetPath("bootstrap.js"));

        // The three steps, read the fragment, clear the address bar, pick out the token.
        foreach (var line in new[]
                 {
                     "var hash = location.hash;",
                     "history.replaceState(null, \"\", location.pathname);",
                     "var match = /[#&]token=([^&]*)/.exec(hash);",
                     "decodeURIComponent(match[1].replace(/\\+/g, \" \"))"
                 })
        {
            Assert.Contains(line, link, StringComparison.Ordinal);
        }

        // The reset page (S-A10) takes its token from this script too, and reads no fragment itself.
        Assert.Contains("var token = AdminLink.takeToken();", reset, StringComparison.Ordinal);
        Assert.DoesNotContain("location.hash", reset, StringComparison.Ordinal);

        // It is the first script of the page and needs nothing from the others,
        // so the address bar is cleared even when a later script fails to load.
        Assert.Equal("/admin/auth/admin-link.js", AdminSignedOutPage.LinkScriptPath);
        var firstStatement = link[link.IndexOf("\"use strict\";", StringComparison.Ordinal)..];
        Assert.True(
            firstStatement.IndexOf("history.replaceState", StringComparison.Ordinal)
            < firstStatement.IndexOf("global.AdminLink", StringComparison.Ordinal),
            "the address bar is cleared before the script does anything else");
        foreach (var other in new[] { "AdminDom", "AdminPassword", "AdminTurnstile", "fetch(", "document." })
        {
            Assert.DoesNotContain(other, link, StringComparison.Ordinal);
        }

        // The token is handed over once and then gone from the link script.
        Assert.Contains("var value = token;", link, StringComparison.Ordinal);
        Assert.Contains("token = \"\";", link, StringComparison.Ordinal);
        Assert.Contains("var token = AdminLink.takeToken();", page, StringComparison.Ordinal);
        Assert.DoesNotContain("location.hash", page, StringComparison.Ordinal);
        Assert.DoesNotContain("history.replaceState", page, StringComparison.Ordinal);
        Assert.True(
            page.IndexOf("AdminLink.takeToken()", StringComparison.Ordinal) < page.IndexOf("fetch(", StringComparison.Ordinal),
            "the token is taken before any request");
    }

    [Fact]
    public void PageScript_SendsOnlyTheContractFields_AndTrustsOnlyTheExactReplies()
    {
        var script = File.ReadAllText(AssetPath("bootstrap.js"));

        Assert.Contains("var API = \"/admin/api/auth/bootstrap\";", script, StringComparison.Ordinal);
        Assert.Contains("var EXPIRED = \"/admin/link-expired?for=setup\";", script, StringComparison.Ordinal);
        Assert.Contains("var SIGN_IN = \"/admin/sign-in\";", script, StringComparison.Ordinal);
        Assert.Equal("/admin/api/auth/bootstrap", AdminBootstrapEndpoints.ApiPath);
        Assert.Equal("/admin/link-expired?for=setup", AdminPasswordResetEndpoints.LinkExpiredSetupLocation);
        Assert.Equal("/admin/sign-in", AdminSignedOutExemptions.SignIn);
        Assert.Contains("\"X-CSRF-TOKEN\": csrf", script, StringComparison.Ordinal);
        Assert.Contains("meta[name=\"dw-csrf\"]", script, StringComparison.Ordinal);

        // One request function; two callers: the link check and the submission.
        Assert.Single(Regex.Matches(script, @"fetch\("));
        Assert.Single(Regex.Matches(script, @"JSON\.stringify\("));
        var bodies = Regex.Matches(script, @"\bpost\(\{(?<fields>[^}]*)\}\)")
            .Select(m => string.Join(",", Regex.Matches(m.Groups["fields"].Value, @"(\w+)\s*:").Select(f => f.Groups[1].Value)))
            .ToArray();
        Assert.Equal(new[] { "token,check", "token,newPassword,turnstileToken" }, bodies);
        Assert.Contains("post({ token: token, check: true })", script, StringComparison.Ordinal);

        // The form is shown only for exactly {"valid": true}; sign-in only for passwordSet === true.
        Assert.Contains("Object.keys(body).length === 1 && body.valid === true", script, StringComparison.Ordinal);
        Assert.Contains("if (body.passwordSet === true) location.replace(SIGN_IN);", script, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(script, @"location\.replace\(SIGN_IN\)").Count);
        // The form appears in one place only, after the check has said "usable".
        Assert.Single(Regex.Matches(script, @"AdminDom\.show\(panel, true\)"));
        Assert.Single(Regex.Matches(script, Regex.Escape("if (outcome === \"usable\") showForm();")));
        Assert.Single(Regex.Matches(script, @"\bshowForm\(\);"));
    }

    [Fact]
    public void WidgetScript_IsTheOnlyPlaceThatNamesCloudflare_AndNeverSkipsTheCheck()
    {
        var widget = File.ReadAllText(AssetPath("admin-turnstile.js"));

        Assert.Contains(
            "var API_SRC = \"https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit\";",
            widget,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(widget, Regex.Escape(Cloudflare)));
        foreach (var other in new[] { "admin-link.js", "admin-dom.js", "admin-password.js", "bootstrap.js", "signed-out.css" })
        {
            Assert.DoesNotContain(Cloudflare, File.ReadAllText(AssetPath(other)), StringComparison.OrdinalIgnoreCase);
        }

        // A token exists only in the ready state; there is no test hook or bypass in the shipped script.
        Assert.Contains("token = next === \"ready\" ? freshToken || \"\" : \"\";", widget, StringComparison.Ordinal);
        Assert.DoesNotContain("__ADMIN_AUTH_TEST__", widget, StringComparison.Ordinal);
        Assert.DoesNotContain("test-turnstile-token", widget, StringComparison.Ordinal);
        Assert.DoesNotContain("publicConfig", widget, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_WithoutAntiForgeryNoscriptOrScripts_RendersNeither()
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Response.Body = new MemoryStream();
        var antiforgery = new AdminAntiForgeryService(new byte[AdminAntiForgeryService.KeySizeBytes]);

        var result = AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = "A <title> & more",
            Heading = "A <heading>",
            BodyHtml = "<p id=\"body\">ok</p>"
        });
        await result.ExecuteAsync(http);

        http.Response.Body.Position = 0;
        var html = await new StreamReader(http.Response.Body).ReadToEndAsync();
        var nonce = SingleNonce(html);
        Assert.Equal("text/html; charset=utf-8", http.Response.ContentType);
        Assert.Equal("no-store", http.Response.Headers.CacheControl.ToString());
        Assert.Equal("no-referrer", http.Response.Headers["Referrer-Policy"].ToString());
        Assert.Equal(
            AdminSignedOutPage.ContentSecurityPolicy(nonce),
            http.Response.Headers.ContentSecurityPolicy.ToString());
        Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.DoesNotContain("dw-csrf", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<noscript", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.Ordinal);
        Assert.Contains("<title>A &lt;title&gt; &amp; more</title>", html, StringComparison.Ordinal);
        Assert.Contains("<h1 id=\"page-heading\">A &lt;heading&gt;</h1><p id=\"body\">ok</p></div></main>", html, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);
    }

    private static void AssertSubmitDisabledAfterTheWidget(string html)
    {
        var submit = $"<button type=\"submit\" id=\"bootstrap-submit\" disabled aria-describedby=\"security-check-status\">{AdminBootstrapCopy.Submit}</button>";
        var submitAt = html.IndexOf(submit, StringComparison.Ordinal);
        Assert.True(submitAt > 0, "the submit button is disabled in the markup");
        Assert.Single(Regex.Matches(html, "type=\"submit\""));
        Assert.True(
            html.IndexOf("id=\"security-check-widget\"", StringComparison.Ordinal) < submitAt
            && html.IndexOf("id=\"confirm-password\"", StringComparison.Ordinal)
            < html.IndexOf("id=\"security-check-widget\"", StringComparison.Ordinal),
            "the security check sits after the fields and directly above the submit button");
    }

    private static void AssertNoLooseSources(string csp)
    {
        Assert.DoesNotContain("unsafe-inline", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("strict-dynamic", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("*", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("data:", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("style-src 'self'", csp, StringComparison.Ordinal);
    }

    /// <summary>The page as text, minus the nonce and the anti-forgery token (random, not copy).</summary>
    private static string WithoutRandomValues(string html)
        => WebUtility.HtmlDecode(
            Regex.Replace(html, "(nonce|content)=\"[^\"]*\"", string.Empty));

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
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        return await client.SendAsync(request);
    }

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

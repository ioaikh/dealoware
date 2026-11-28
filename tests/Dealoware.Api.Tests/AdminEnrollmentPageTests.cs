using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// The two set-up pages a first sign-in leads through: S-A6
/// (GET /admin/setup/authenticator) and S-A7 (GET /admin/setup/recovery-codes).
/// Both are served only during a pending sign-in, so these requests carry the
/// cookie of a pending row made here, for an owner put into the state the
/// test needs. The key, its QR code and the recovery codes are in the page;
/// neither address answers with them as data, by GET or by POST.
/// What the pages do in a browser is covered by e2e/enrol.
/// </summary>
[Collection("WebAppTests")]
public class AdminEnrollmentPageTests : IClassFixture<TurnstileSiteKeyWebApplicationFactory>
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string Cloudflare = "challenges.cloudflare.com";
    private const string OwnerEmail = TestCoreOwner.Email;

    private const string Authenticator = AdminSignedOutExemptions.SetupAuthenticator;
    private const string RecoveryCodes = AdminSignedOutExemptions.SetupRecoveryCodes;

    private static readonly string[] AuthenticatorScripts =
    [
        // First: it takes a flag out of the address bar before anything else can fail.
        "/admin/auth/admin-arrival.js",
        "/admin/auth/admin-dom.js",
        "/admin/auth/admin-request.js",
        "/admin/auth/admin-clipboard.js",
        "/admin/auth/authenticator-setup.js"
    ];

    private static readonly string[] RecoveryCodesScripts =
    [
        "/admin/auth/admin-arrival.js",
        "/admin/auth/admin-request.js",
        "/admin/auth/admin-clipboard.js",
        "/admin/auth/recovery-codes.js"
    ];

    private readonly IsolatedWebApplicationFactory _unconfigured;
    private readonly TurnstileSiteKeyWebApplicationFactory _configured;

    public AdminEnrollmentPageTests(
        IsolatedWebApplicationFactory unconfigured,
        TurnstileSiteKeyWebApplicationFactory configured)
    {
        _unconfigured = unconfigured;
        _configured = configured;
    }

    /// <summary>What the owner's account row says when the page is asked for.</summary>
    public enum Owner
    {
        /// <summary>Password step done for the first time: a key is waiting to be confirmed.</summary>
        SettingUp,

        /// <summary>The authenticator was confirmed in this pending sign-in: the codes can still be shown.</summary>
        JustSetUp,

        /// <summary>Set up some time ago: nothing is left to show.</summary>
        SetUp,

        /// <summary>Neither set up nor holding a key to confirm.</summary>
        NothingWaiting,

        /// <summary>No account row at all.</summary>
        Missing
    }

    public static TheoryData<string, Owner, string, string> Pages => new()
    {
        { Authenticator, Owner.SettingUp, AdminEnrollmentCopy.AuthenticatorPageTitle, AdminEnrollmentCopy.AuthenticatorHeading },
        { RecoveryCodes, Owner.JustSetUp, AdminEnrollmentCopy.RecoveryCodesPageTitle, AdminEnrollmentCopy.RecoveryCodesHeading }
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_ReturnsTheHtmlPage_InTheSharedShell(string path, Owner state, string title, string heading)
    {
        var owner = await SeedAsync(_configured, state);
        using var response = await GetAsync(_configured, path, owner.Pending);

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
        Assert.Contains($"<h1 id=\"page-heading\">{heading}</h1>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<link rel=\"stylesheet\" href=\"{AdminSignedOutPage.StylesheetPath}\" nonce=\"{nonce}\">",
            html,
            StringComparison.Ordinal);

        // Every script is a same-origin file under /admin/auth/ carrying the nonce, in this order.
        var scripts = Regex.Matches(html, "<script src=\"([^\"]+)\" nonce=\"([^\"]+)\"></script>");
        Assert.Equal(
            path == Authenticator ? AuthenticatorScripts : RecoveryCodesScripts,
            scripts.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
        Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
        Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onclick=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onsubmit=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);

        // The data answer is gone: the page is not JSON, and names no JSON field of the old reply.
        Assert.DoesNotContain("\"secret\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("otpauthUrl", html, StringComparison.Ordinal);
        Assert.DoesNotContain("recoveryCodes", html, StringComparison.Ordinal);
        Assert.False(response.Headers.Contains(AdminAntiForgeryService.HeaderName));
    }

    [Fact]
    public void Titles_AreOnePerPage_AndTheHeadingsAreTheTitlesFirstWords()
    {
        Assert.Equal("Authenticator setup · Dealoware admin", AdminEnrollmentCopy.AuthenticatorPageTitle);
        Assert.Equal("Authenticator setup", AdminEnrollmentCopy.AuthenticatorHeading);
        Assert.Equal("Recovery codes · Dealoware admin", AdminEnrollmentCopy.RecoveryCodesPageTitle);
        Assert.Equal("Recovery codes", AdminEnrollmentCopy.RecoveryCodesHeading);

        // No two signed-out pages share a title.
        var titles = new[]
        {
            AdminSignInCopy.SignInPageTitle, AdminSignInCopy.CodePageTitle, AdminSignInCopy.RecoveryPageTitle,
            AdminBootstrapCopy.PageTitle, AdminEnrollmentCopy.AuthenticatorPageTitle, AdminEnrollmentCopy.RecoveryCodesPageTitle
        };
        Assert.Equal(titles.Length, titles.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_WithoutJavaScript_ShowsOneSentence_AndKeepsEverythingElseHidden(string path, Owner state, string title, string heading)
    {
        var owner = await SeedAsync(_unconfigured, state);
        using var response = await GetAsync(_unconfigured, path, owner.Pending);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(title, html, StringComparison.Ordinal);

        Assert.Equal("Turn on JavaScript to set up your authenticator.", AdminEnrollmentCopy.AuthenticatorNoscript);
        Assert.Equal("Turn on JavaScript to see your recovery codes.", AdminEnrollmentCopy.RecoveryCodesNoscript);
        var noscript = path == Authenticator ? AdminEnrollmentCopy.AuthenticatorNoscript : AdminEnrollmentCopy.RecoveryCodesNoscript;
        var panel = path == Authenticator ? "<div id=\"authenticator-panel\" hidden>" : "<div id=\"recovery-codes-panel\" hidden>";

        // Heading, then the one sentence, then the panel that the script alone can show.
        Assert.Contains(
            $"<h1 id=\"page-heading\">{heading}</h1><noscript><p>{noscript}</p></noscript>{panel}",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<noscript>"));
        Assert.Single(Regex.Matches(html, "<div id=\"[a-z-]+-panel\" hidden>"));

        // Everything else is inside that hidden panel: it runs to the end of the card.
        var panelAt = html.IndexOf(panel, StringComparison.Ordinal);
        var mainEndAt = html.IndexOf("</div></main>", StringComparison.Ordinal);
        var inside = html[panelAt..mainEndAt];
        Assert.Equal(Regex.Matches(inside, "<div[ >]").Count, Regex.Matches(inside, "</div>").Count);
        foreach (var hiddenWithIt in path == Authenticator
                     ? new[] { "id=\"authenticator-qr\"", "id=\"manual-key\"", "<form ", "id=\"code\"", "class=\"lede\"" }
                     : new[] { "id=\"recovery-codes-list\"", "id=\"codes-saved\"", "id=\"codes-continue\"", "class=\"lede\"" })
        {
            Assert.True(html.IndexOf(hiddenWithIt, StringComparison.Ordinal) > panelAt, hiddenWithIt);
            Assert.True(html.IndexOf(hiddenWithIt, StringComparison.Ordinal) < mainEndAt, hiddenWithIt);
        }
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_SetsNoStoreNoReferrer_AndIssuesTheAntiForgeryPair(string path, Owner state, string title, string heading)
    {
        var owner = await SeedAsync(_configured, state);
        using var response = await GetAsync(_configured, path, owner.Pending);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains(heading, html, StringComparison.Ordinal);

        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-cache", Header(response, "Pragma"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.FrameOptions, Header(response, "X-Frame-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.RobotsTag, Header(response, "X-Robots-Tag"));
        Assert.False(response.Headers.Contains("ETag"));
        Assert.False(response.Content.Headers.Contains("Last-Modified"));
        Assert.False(response.Content.Headers.Contains("Expires"));

        // The page sets one cookie, the anti-forgery one. It never sets or echoes the pending sign-in.
        var setCookie = Assert.Single(SetCookies(response));
        Assert.StartsWith(AdminCookieNames.AntiForgery + "=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/admin", setCookie, StringComparison.OrdinalIgnoreCase);
        var cookieValue = setCookie.Split(';', 2)[0].Split('=', 2)[1];

        var token = AdminPageAntiForgery.ReadToken(html);
        Assert.Equal(
            _configured.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(cookieValue),
            token);
        Assert.NotEqual(cookieValue, token);
        Assert.DoesNotContain(cookieValue, html, StringComparison.Ordinal);
        Assert.DoesNotContain(owner.Pending, html, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPendingAuthCookie.Name, html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Csp_IsTheBasePolicyWithAFreshNonce_AndNeverAllowsCloudflare(string path, Owner state, string title, string heading)
    {
        // With or without a Turnstile site key configured: these pages carry no security check.
        foreach (var factory in new[] { _configured, _unconfigured })
        {
            var owner = await SeedAsync(factory, state);
            using var response = await GetAsync(factory, path, owner.Pending);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains(title, html, StringComparison.Ordinal);
            Assert.Contains(heading, html, StringComparison.Ordinal);
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
            // The QR code is drawn in the page itself, so the policy needs no image source at all.
            Assert.DoesNotContain("img-src", csp, StringComparison.Ordinal);
            Assert.DoesNotContain("unsafe-inline", csp, StringComparison.Ordinal);
            Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
            Assert.DoesNotContain("data:", csp, StringComparison.Ordinal);
            Assert.DoesNotContain("*", csp, StringComparison.Ordinal);

            Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TurnstileSiteKeyWebApplicationFactory.SiteKey, html, StringComparison.Ordinal);
            Assert.DoesNotContain("security-check", html, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminSignedOutPage.TurnstileScriptPath, html, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminSignedOutPage.PasswordScriptPath, html, StringComparison.Ordinal);

            using var again = await GetAsync(factory, path, owner.Pending);
            var secondNonce = SingleNonce(await again.Content.ReadAsStringAsync());
            Assert.NotEqual(nonce, secondNonce);
            Assert.True(Convert.FromBase64String(nonce).Length >= 16);
        }
    }

    [Fact]
    public async Task Authenticator_ShowsTheKeyAsText_AndAQrCodeThatReadsBackAsTheOtpAuthAddressOfThatKey()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        using var response = await GetAsync(_configured, Authenticator, owner.Pending);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // The key the server holds for this owner, as text, in groups of four.
        Assert.Equal(32, owner.Base32Key.Length);
        Assert.Equal(owner.Base32Key, AdminEnrollmentPageReader.ReadKey(html));
        var groups = Enumerable.Range(0, 8).Select(i => owner.Base32Key.Substring(i * 4, 4)).ToArray();
        Assert.Contains(
            $"<dt>{AdminEnrollmentCopy.KeyLabel}</dt>"
            + "<dd><span id=\"manual-key\" class=\"key\" translate=\"no\">"
            + string.Join("<wbr>", groups.Select(g => $"<span>{g}</span>"))
            + "</span></dd>",
            html,
            StringComparison.Ordinal);
        Assert.Equal(AdminEnrollmentPages.GroupedKey(owner.Base32Key), string.Join("<wbr>", groups.Select(g => $"<span>{g}</span>")));

        // What the QR code must hold: the address authenticator apps read, with this owner and this key.
        var otpAuth = "otpauth://totp/Dealoware%3Acore-owner%40example.test?secret=" + owner.Base32Key
                      + "&issuer=Dealoware&digits=6&period=30&algorithm=SHA1";
        Assert.Equal(otpAuth, Rfc6238Totp.BuildOtpAuthUri(OwnerEmail, owner.Base32Key));

        // The modules drawn in the page are exactly the encoder's symbol for that address...
        var drawn = AdminEnrollmentPageReader.ReadQr(html);
        var expected = AdminQrCode.Encode(otpAuth);
        Assert.Equal(QrTestReader.RowsOf(QrTestReader.ModulesOf(expected)), QrTestReader.RowsOf(drawn));
        Assert.Contains(AdminEnrollmentPages.QrSvg(expected), html, StringComparison.Ordinal);

        // ...and a reader written apart from the encoder gets that address back out of them.
        var reading = QrTestReader.Read(drawn);
        Assert.Equal(otpAuth, Encoding.UTF8.GetString(reading.Data));
        Assert.Equal(8, reading.Version);

        // It is one image with a name, described by the line that offers the key instead.
        Assert.Contains(
            $"<svg id=\"authenticator-qr\" class=\"qr\" role=\"img\" aria-label=\"{AdminEnrollmentCopy.QrLabel}\""
            + " aria-describedby=\"manual-key-help\" viewBox=\"0 0 57 57\" shape-rendering=\"crispEdges\">"
            + "<rect width=\"57\" height=\"57\" fill=\"#fff\"/><path fill=\"#000\" d=\"M",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"</svg><p id=\"manual-key-help\" class=\"helper\">{AdminEnrollmentCopy.KeyHelp}</p><dl class=\"details\">",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<svg"));

        // The name the app will list it under: the label of that same address.
        Assert.Equal("Dealoware:" + TestCoreOwner.Email, AdminEnrollmentPages.AccountName(otpAuth));
        Assert.Contains(
            $"<dt>{AdminEnrollmentCopy.AccountNameLabel}</dt><dd id=\"account-name\" translate=\"no\">Dealoware:{TestCoreOwner.Email}</dd></dl>",
            html,
            StringComparison.Ordinal);
        Assert.Equal(string.Empty, AdminEnrollmentPages.AccountName("https://example.test/?x=1"));
    }

    [Fact]
    public async Task Authenticator_KeepsTheKeyOutOfEveryAddressAttributeAndHeader()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        using var response = await GetAsync(_configured, Authenticator, owner.Pending);
        var html = await response.Content.ReadAsStringAsync();

        // Once, as the text of the key element, and nowhere else: not whole, and not as the address the QR code holds.
        Assert.Equal(owner.Base32Key, AdminEnrollmentPageReader.ReadKey(html));
        Assert.DoesNotContain(owner.Base32Key, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("otpauth", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" value=", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" href=", html.Replace($" href=\"{AdminSignedOutPage.StylesheetPath}\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("<a ", html, StringComparison.Ordinal);
        foreach (var group in Enumerable.Range(0, 8).Select(i => owner.Base32Key.Substring(i * 4, 4)))
        {
            // No attribute carries a piece of it either: each group stands only as element text.
            Assert.DoesNotMatch("=\"[^\"]*" + Regex.Escape(group) + "[^\"]*\"", WithoutQrPathAndRandomValues(html));
        }

        Assert.All(
            response.Headers.Concat(response.Content.Headers).SelectMany(h => h.Value),
            value => Assert.DoesNotContain(owner.Base32Key, value, StringComparison.OrdinalIgnoreCase));
        Assert.False(response.Headers.Contains("Location"));

        // Nor anything else that is the server's alone.
        Assert.DoesNotContain(owner.PendingCipher!, html, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.TotpKey, html, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, html, StringComparison.Ordinal);
        Assert.DoesNotContain("DEALOWARE_", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticator_Form_HasOneNumericOneTimeCodeField_ACopyButton_AndOneAlertAboveTheForm()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        using var response = await GetAsync(_configured, Authenticator, owner.Pending);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains(
            $"<div id=\"authenticator-panel\" hidden><div id=\"authenticator-key\"><p class=\"lede\">{AdminEnrollmentCopy.AuthenticatorLede}</p><svg ",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<div class=\"actions\"><button type=\"button\" id=\"copy-key\" class=\"secondary\""
            + $" data-copied=\"{AdminEnrollmentCopy.Copied}\" data-copy-failed=\"{AdminEnrollmentCopy.CopyFailed}\">"
            + $"{AdminEnrollmentCopy.CopyKey}</button></div>"
            + "<p id=\"copy-status\" class=\"live\" role=\"status\"></p></div>"
            + "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>"
            + $"<form id=\"authenticator-form\" method=\"post\" action=\"{Authenticator}\" novalidate"
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\" data-no-sign-in=\"Unauthorized\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<label for=\"code\">{AdminSignInCopy.CodeLabel}</label>"
            + "<input id=\"code\" name=\"code\" type=\"text\" inputmode=\"numeric\""
            + " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required"
            + $" aria-describedby=\"code-error\" data-required=\"{AdminSignInCopy.CodeRequired}\">"
            + "<span id=\"code-error\" class=\"field-error\" aria-live=\"polite\"></span>"
            + "</div>"
            + $"<button type=\"submit\" id=\"authenticator-submit\">{AdminEnrollmentCopy.AuthenticatorSubmit}</button>"
            + "</form></div>",
            html,
            StringComparison.Ordinal);

        Assert.Single(Regex.Matches(html, "<form "));
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.Single(Regex.Matches(html, "type=\"submit\""));
        Assert.Single(Regex.Matches(html, "role=\"alert\""));
        Assert.Single(Regex.Matches(html, "role=\"status\""));
        Assert.Equal(2, Regex.Matches(html, "<button ").Count);
        Assert.DoesNotContain("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onpaste", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxlength", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pattern=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("autofocus", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticator_AskedForAgain_ShowsTheSameKey_AndIssuesNoNewOne()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);

        using var first = await GetAsync(_configured, Authenticator, owner.Pending);
        using var second = await GetAsync(_configured, Authenticator, owner.Pending);
        var firstHtml = await first.Content.ReadAsStringAsync();
        var secondHtml = await second.Content.ReadAsStringAsync();

        Assert.Equal(owner.Base32Key, AdminEnrollmentPageReader.ReadKey(firstHtml));
        Assert.Equal(owner.Base32Key, AdminEnrollmentPageReader.ReadKey(secondHtml));
        Assert.Equal(
            QrTestReader.RowsOf(AdminEnrollmentPageReader.ReadQr(firstHtml)),
            QrTestReader.RowsOf(AdminEnrollmentPageReader.ReadQr(secondHtml)));
        // Each page load has its own nonce and anti-forgery pair; the key is the one thing that stays.
        Assert.NotEqual(AdminPageAntiForgery.ReadToken(firstHtml), AdminPageAntiForgery.ReadToken(secondHtml));

        // A GET changes nothing: the row still holds the same waiting key and no confirmed one.
        using var scope = _configured.Services.CreateScope();
        var account = scope.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        Assert.Equal(owner.PendingCipher, account.PendingTotpSecretCipher);
        Assert.False(account.IsTotpEnrolled);
        Assert.Null(account.RecoveryCodesRevealCipher);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"code\":\"\"}")]
    [InlineData("{\"code\":\"   \"}")]
    [InlineData("{\"code\":null}")]
    // The code under another field's name is no code either.
    [InlineData("{\"totpCode\":\"123456\"}")]
    // No body at all.
    [InlineData(null)]
    public async Task PostWithoutACode_IsAnsweredAndCountedLikeAWrongCode_AndNeverWithTheKey(string? json)
    {
        // What a wrong code gets, and what it leaves behind.
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        var (cookies, token) = await OpenPageAsync(_configured, Authenticator, owner.Pending);
        var auditBefore = await CountFailedSecondStepsAsync(_configured);
        string wrongBody;
        string? wrongContentType;
        using (var wrong = await PostCodeAsync(_configured, cookies, token, "000000"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            wrongBody = await wrong.Content.ReadAsStringAsync();
            wrongContentType = wrong.Content.Headers.ContentType?.ToString();
        }

        Assert.Equal($"{{\"error\":\"{AdminAuthCopy.SignInFailure}\"}}", wrongBody);
        var countedForAWrongCode = await CountFailuresAsync(_configured);
        Assert.True(countedForAWrongCode > 0);
        Assert.Equal(auditBefore + 1, await CountFailedSecondStepsAsync(_configured));

        // The same request with no code in it, from the same starting point.
        owner = await SeedAsync(_configured, Owner.SettingUp);
        (cookies, token) = await OpenPageAsync(_configured, Authenticator, owner.Pending);
        using var response = await PostAsync(_configured, Authenticator, cookies, token, json);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(wrongBody, body);
        Assert.Equal(wrongContentType, response.Content.Headers.ContentType?.ToString());
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Empty(SetCookies(response));

        // The key is not in the answer in any form: not as text, not as an address, not under any name.
        Assert.DoesNotContain(owner.Base32Key, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("otpauth", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("digits", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enrolled", body, StringComparison.OrdinalIgnoreCase);
        Assert.All(
            response.Headers.Concat(response.Content.Headers).SelectMany(h => h.Value),
            value => Assert.DoesNotContain(owner.Base32Key, value, StringComparison.OrdinalIgnoreCase));

        // Counted and audited as the wrong code was, and nothing is set up.
        Assert.Equal(countedForAWrongCode, await CountFailuresAsync(_configured));
        Assert.Equal(auditBefore + 2, await CountFailedSecondStepsAsync(_configured));
        using var scope = _configured.Services.CreateScope();
        var account = scope.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        Assert.False(account.IsTotpEnrolled);
        Assert.Equal(owner.PendingCipher, account.PendingTotpSecretCipher);
    }

    [Fact]
    public async Task PostWithoutACode_FromAnOwnerWhoIsSetUp_IsToldSo_AsBefore()
    {
        var owner = await SeedAsync(_configured, Owner.JustSetUp);
        var (cookies, token) = await OpenPageAsync(_configured, RecoveryCodes, owner.Pending);

        using var response = await PostAsync(_configured, Authenticator, cookies, token, "{}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"enrolled\":true}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ACompletedPasswordReset_LeavesNoKeyWaitingToBeConfirmed()
    {
        // An owner who is set up (a reset needs the second factor), with a key still waiting on the row.
        const string newPassword = "radio station later!";
        var owner = await SeedAsync(_configured, Owner.SetUp);
        var resetToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(
                AdminPasswordResetEndpoints.TokenEntropyBytes))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using (var scope = _configured.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IAdminPasswordHasher>();
            var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
            var now = scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow;
            db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail)
                .SetPendingEnrollment(protector.Encrypt(Rfc6238Totp.GenerateSecret()));
            db.AdminResetIpCounters.RemoveRange(db.AdminResetIpCounters);
            db.AdminPasswordResetTokens.RemoveRange(db.AdminPasswordResetTokens);
            db.AdminCredentials.RemoveRange(db.AdminCredentials.Where(c => c.Email == OwnerEmail));
            db.AdminCredentials.Add(AdminCredential.Create(OwnerEmail, hasher.Hash("radio station prior!"), now));
            db.AdminPasswordResetTokens.Add(
                AdminPasswordResetToken.Create(OwnerEmail, AdminPasswordResetToken.HashRaw(resetToken), now));
            await db.SaveChangesAsync();
        }

        // The reset itself, as its page sends it: the link's token, the new password, a code.
        string cookie;
        string token;
        using (var page = await GetAsync(_configured, AdminSignedOutExemptions.ResetConfirm))
        {
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            token = AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync());
            cookie = Assert.Single(SetCookies(page)).Split(';', 2)[0];
        }

        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(owner.Base32Key), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using (var reset = await PostAsync(
                   _configured,
                   AdminSignedOutExemptions.ApiResetConfirm,
                   cookie,
                   token,
                   JsonSerializer.Serialize(new { token = resetToken, newPassword, totp })))
        {
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            Assert.Contains(AdminPasswordResetCopy.CompleteSucceeded, await reset.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        using var after = _configured.Services.CreateScope();
        var account = after.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        Assert.Null(account.PendingTotpSecretCipher);
        // The reset replaced the password and left the authenticator as it was.
        Assert.True(after.ServiceProvider.GetRequiredService<IAdminPasswordHasher>()
            .Verify(AdminPasswordRules.Normalize(newPassword), account.PasswordHash!));
        Assert.True(account.IsTotpEnrolled);
    }

    [Fact]
    public void SettingAPassword_DropsAKeyWaitingToBeConfirmed_WhicheverFlowSetsIt()
    {
        var account = AdminCoreOwnerAccount.Create(OwnerEmail);
        account.SetPendingEnrollment("a key shown under the old password");

        account.SetPasswordHash("hash of the new password");

        Assert.Null(account.PendingTotpSecretCipher);
        Assert.Equal("hash of the new password", account.PasswordHash);

        // Bootstrap sets the first password through that method (a reset's new one is covered above, end to end).
        Assert.Contains(
            "account.SetPasswordHash(passwordHash);",
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminBootstrapEndpoints.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheWholeSetUp_ThroughThePages_ConfirmsWithACode_ShowsTenCodes_AndSignsNobodyIn()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        using var page = await GetAsync(_configured, Authenticator, owner.Pending);
        var html = await page.Content.ReadAsStringAsync();
        var key = AdminEnrollmentPageReader.ReadKey(html);
        var token = AdminPageAntiForgery.ReadToken(html);
        var cookies = $"{Assert.Single(SetCookies(page)).Split(';', 2)[0]}; {AdminPendingAuthCookie.Name}={owner.Pending}";

        // A wrong code: the server's one sentence, as JSON, and nothing is set up.
        using (var refused = await PostCodeAsync(_configured, cookies, token, "000000"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
            Assert.Equal($"{{\"error\":\"{AdminAuthCopy.SignInFailure}\"}}", await refused.Content.ReadAsStringAsync());
        }

        // The code an authenticator app shows for the key on the page.
        var code = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(key), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using (var confirmed = await PostCodeAsync(_configured, cookies, token, code))
        {
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            Assert.Equal("application/json", confirmed.Content.Headers.ContentType?.MediaType);
            Assert.Equal("{\"enrolled\":true,\"next\":\"recovery-codes\"}", await confirmed.Content.ReadAsStringAsync());
            // Setting the authenticator up signs nobody in, and leaves the pending sign-in as it was.
            Assert.Empty(SetCookies(confirmed));
        }

        // The codes page, twice: the same ten codes while the pending sign-in lasts.
        using var codesPage = await GetAsync(_configured, RecoveryCodes, owner.Pending);
        var codes = await AdminEnrollmentPageReader.ReadRecoveryCodesAsync(codesPage);
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, c => Assert.Matches("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$", c));
        using var codesAgain = await GetAsync(_configured, RecoveryCodes, owner.Pending);
        Assert.Equal(codes, await AdminEnrollmentPageReader.ReadRecoveryCodesAsync(codesAgain));

        using var scope = _configured.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();
        // They are the codes whose hashes the server keeps, and no session exists.
        var stored = db.AdminRecoveryCodes.Select(c => c.CodeHash).ToList();
        Assert.Equal(codes.Select(hasher.Hash).Order(), stored.Order());
        Assert.Empty(db.AdminSessions.Where(s => s.Email == OwnerEmail));
        Assert.True(db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail).IsTotpEnrolled);

        // The authenticator page has nothing left to show: the browser is sent on, and the key is not sent again.
        using var keyPageAgain = await GetAsync(_configured, Authenticator, owner.Pending);
        Assert.Equal(HttpStatusCode.Found, keyPageAgain.StatusCode);
        Assert.Equal(RecoveryCodes, Assert.Single(keyPageAgain.Headers.GetValues("Location")));
        Assert.Equal(string.Empty, await keyPageAgain.Content.ReadAsStringAsync());
        Assert.Empty(SetCookies(keyPageAgain));
        Assert.Equal("no-store", Header(keyPageAgain, "Cache-Control"));
    }

    [Fact]
    public async Task RecoveryCodes_AreARealList_WithACopyButton_AndAContinueButtonThatStartsDisabled()
    {
        var owner = await SeedAsync(_configured, Owner.JustSetUp);
        using var response = await GetAsync(_configured, RecoveryCodes, owner.Pending);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(owner.RecoveryCodes, AdminEnrollmentPageReader.ReadRecoveryCodes(html));
        Assert.Equal(10, owner.RecoveryCodes.Count);
        Assert.Equal("Save these codes now. They are shown once.", AdminEnrollmentCopy.RecoveryCodesLede);
        Assert.Contains(
            "<div id=\"recovery-codes-panel\" hidden>"
            + $"<p class=\"lede\">{AdminEnrollmentCopy.RecoveryCodesLede}</p>"
            + $"<p>{AdminEnrollmentCopy.RecoveryCodesPurpose}</p>"
            + "<ul id=\"recovery-codes-list\" class=\"code-list\" role=\"list\" aria-labelledby=\"page-heading\" translate=\"no\">"
            + string.Concat(owner.RecoveryCodes.Select(c => $"<li>{c}</li>"))
            + "</ul>"
            + "<div class=\"actions\"><button type=\"button\" id=\"copy-codes\" class=\"secondary\""
            + $" data-copied=\"{AdminEnrollmentCopy.Copied}\" data-copy-failed=\"{AdminEnrollmentCopy.CopyFailed}\">"
            + $"{AdminEnrollmentCopy.CopyAllCodes}</button></div>"
            + "<p id=\"copy-status\" class=\"live\" role=\"status\"></p>"
            + "<div class=\"check\"><input id=\"codes-saved\" type=\"checkbox\" autocomplete=\"off\">"
            + $"<label for=\"codes-saved\">{AdminEnrollmentCopy.CodesSavedLabel}</label></div>"
            + $"<button type=\"button\" id=\"codes-continue\" disabled>{AdminEnrollmentCopy.RecoveryCodesContinue}</button>"
            + "</div></div></main>",
            html,
            StringComparison.Ordinal);

        Assert.Equal(10, Regex.Matches(html, "<li>").Count);
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.Equal(2, Regex.Matches(html, "<button ").Count);
        Assert.Single(Regex.Matches(html, " disabled>"));
        Assert.DoesNotContain("<form", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<a ", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" checked", html, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" value=", html, StringComparison.Ordinal);

        // Each code stands once, as the text of its list item, and in no attribute, address or header.
        foreach (var code in owner.RecoveryCodes)
        {
            Assert.Single(Regex.Matches(html, Regex.Escape(code)));
            Assert.All(
                response.Headers.Concat(response.Content.Headers).SelectMany(h => h.Value),
                value => Assert.DoesNotContain(code, value, StringComparison.OrdinalIgnoreCase));
        }

        // The authenticator key has no place on this page.
        Assert.DoesNotContain(owner.Base32Key, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("otpauth", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecoveryCodes_OnceTheyCanNoLongerBeShown_SayTheyWereShownOnce_AndLeadBackToSignIn()
    {
        // The next sign-in has started, or the owner set the authenticator up long ago: nothing is left to reveal.
        var owner = await SeedAsync(_configured, Owner.SetUp);
        using var response = await GetAsync(_configured, RecoveryCodes, owner.Pending);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Your recovery codes have already been shown. They are shown once.",
            AdminEnrollmentCopy.RecoveryCodesAlreadyShown);
        // Plain markup: the sentence and the link show with or without JavaScript, so there is no noscript line.
        Assert.Contains(
            $"<h1 id=\"page-heading\">{AdminEnrollmentCopy.RecoveryCodesHeading}</h1>"
            + "<div id=\"recovery-codes-gone\">"
            + $"<p class=\"lede\">{AdminEnrollmentCopy.RecoveryCodesAlreadyShown}</p>"
            + $"<div class=\"links\"><a id=\"back-link\" href=\"{AdminSignedOutExemptions.SignIn}\">{AdminSignInCopy.BackToSignIn}</a></div>"
            + "</div></div></main>",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("<noscript", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" hidden", html, StringComparison.Ordinal);
        Assert.Empty(AdminEnrollmentPageReader.ReadRecoveryCodes(html));
        Assert.DoesNotContain("<li>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<button", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<input", html, StringComparison.Ordinal);

        // Its one script only takes a flag out of the address bar. Nothing can be sent, so no anti-forgery pair.
        Assert.Equal(
            [AdminSignedOutPage.ArrivalScriptPath],
            Regex.Matches(html, "<script src=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.DoesNotContain(AdminSignedOutPage.CsrfMetaName, html, StringComparison.Ordinal);
        Assert.Empty(SetCookies(response));

        // And the authenticator page sends an owner who is set up on to this one.
        using var keyPage = await GetAsync(_configured, Authenticator, owner.Pending);
        Assert.Equal(HttpStatusCode.Found, keyPage.StatusCode);
        Assert.Equal(RecoveryCodes, Assert.Single(keyPage.Headers.GetValues("Location")));
    }

    [Fact]
    public async Task RecoveryCodes_StopBeingShown_WhenTheNextSignInStarts()
    {
        var owner = await SeedAsync(_configured, Owner.JustSetUp);
        using (var shown = await GetAsync(_configured, RecoveryCodes, owner.Pending))
        {
            Assert.Equal(owner.RecoveryCodes, await AdminEnrollmentPageReader.ReadRecoveryCodesAsync(shown));
        }

        // What step 1 of a sign-in does to the row: the codes held for showing are dropped.
        using (var scope = _configured.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail).ClearRecoveryCodesReveal();
            await db.SaveChangesAsync();
        }

        using var after = await GetAsync(_configured, RecoveryCodes, owner.Pending);
        var html = WebUtility.HtmlDecode(await after.Content.ReadAsStringAsync());
        Assert.Contains(AdminEnrollmentCopy.RecoveryCodesAlreadyShown, html, StringComparison.Ordinal);
        Assert.All(owner.RecoveryCodes, code => Assert.DoesNotContain(code, html, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(Authenticator)]
    [InlineData(RecoveryCodes)]
    public async Task SetUpPage_WithoutAPendingSignIn_SendsTheBrowserToStepOne_AndServesNoPage(string path)
    {
        // With no pending sign-in the session gate answers before the page handler runs.
        // For the GET of these two pages its answer is a redirect to step 1: a constant
        // address, no body, and no cookie set or cleared.
        var owner = await SeedAsync(_configured, path == Authenticator ? Owner.SettingUp : Owner.JustSetUp);
        Assert.Equal("/admin/sign-in#again", AdminSignInPages.SentBackLocation);
        var neverIssued = AdminPendingToken.Create();
        var used = await CreatePendingSignInAsync(_configured, consumed: true);
        var probe = "probe" + Guid.NewGuid().ToString("N");
        foreach (var (address, cookie) in new (string, string?)[]
                 {
                     (path, null),
                     (path, neverIssued),
                     // A used pending sign-in is no better than none.
                     (path, used),
                     // Nothing of the request reaches the target: not a query, not a cookie.
                     ($"{path}?returnPath=https%3A%2F%2F{probe}.example%2F&next=%2F{probe}", neverIssued)
                 })
        {
            using var response = await GetAsync(_configured, address, cookie);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(AdminSignInPages.SentBackLocation, Assert.Single(response.Headers.GetValues("Location")));
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.Empty(SetCookies(response));
            Assert.Equal("no-store", Header(response, "Cache-Control"));
            Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
            Assert.All(
                response.Headers.SelectMany(h => h.Value),
                v =>
                {
                    Assert.DoesNotContain(probe, v, StringComparison.Ordinal);
                    Assert.DoesNotContain(owner.Base32Key, v, StringComparison.OrdinalIgnoreCase);
                });
        }

        // With the pending sign-in a query string changes nothing: the page is served, and nothing of the query is in it.
        using (var withQuery = await GetAsync(_configured, $"{path}?next=%2F{probe}&email={probe}%40example.test", owner.Pending))
        {
            Assert.Equal(HttpStatusCode.OK, withQuery.StatusCode);
            Assert.DoesNotContain(probe, await withQuery.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            Assert.All(
                withQuery.Headers.SelectMany(h => h.Value),
                v => Assert.DoesNotContain(probe, v, StringComparison.Ordinal));
        }

        // A path that is not exactly the page is not sent back to step 1. One that does not
        // canonicalise gets the gate's 401 as before; an extra segment is a signed-in page
        // path like any other, which without a session goes to sign-in with the "ended" note.
        foreach (var notThePage in new[] { path + "/", path.Replace("/setup/", "/setup%2f", StringComparison.Ordinal) })
        {
            using var denied = await GetAsync(_configured, notThePage, owner.Pending);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.False(denied.Headers.Contains("Location"));
            Assert.Equal("{\"error\":\"Unauthorized\"}", await denied.Content.ReadAsStringAsync());
        }

        using (var extra = await GetAsync(_configured, path + "/extra", owner.Pending))
        {
            Assert.Equal(HttpStatusCode.Found, extra.StatusCode);
            Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(extra.Headers.GetValues("Location")));
            Assert.Equal(string.Empty, await extra.Content.ReadAsStringAsync());
        }

        // Another method on the page path is not a visit either.
        using var head = new HttpRequestMessage(HttpMethod.Head, path);
        head.Headers.Host = AdminHost;
        using var headResponse = await Client(_configured).SendAsync(head);
        Assert.Equal(HttpStatusCode.Unauthorized, headResponse.StatusCode);
        Assert.False(headResponse.Headers.Contains("Location"));

        // And with the pending sign-in the page itself is served.
        using var ok = await GetAsync(_configured, path, owner.Pending);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task TheRedirectedPages_AreExactlyTheGetRulesThatNeedAPendingSignIn()
    {
        // The session gate redirects the GET of a fixed list of pages. That list must stay the
        // list of GET rules gated by a pending sign-in: no more, no fewer.
        var gated = AdminSignedOutExemptions.Rules
            .Where(r => r.Method == HttpMethod.Get && r.Token == AdminSignedOutExemptions.TokenKind.PendingSignIn)
            .Select(r => r.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { Authenticator, RecoveryCodes, AdminSignedOutExemptions.SignInCode, AdminSignedOutExemptions.SignInRecovery },
            gated);

        await ClearSignInCountersAsync(_configured);
        foreach (var rule in AdminSignedOutExemptions.Rules.Where(r => r.Method == HttpMethod.Get))
        {
            using var response = await GetAsync(_configured, rule.Path);
            if (rule.Token == AdminSignedOutExemptions.TokenKind.PendingSignIn)
            {
                Assert.Equal(HttpStatusCode.Found, response.StatusCode);
                Assert.Equal(AdminSignInPages.SentBackLocation, Assert.Single(response.Headers.GetValues("Location")));
            }
            else
            {
                // Pages that need nothing are served; none of them is ever sent to step 1 by the gate.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(response.Headers.Contains("Location"));
            }
        }
    }

    [Fact]
    public async Task OnlyTheGetOfTheSetUpPages_IsRedirected_TheirPostsKeepTheGateWord()
    {
        await SeedAsync(_configured, Owner.SettingUp);
        Assert.Equal("Unauthorized", AdminDeny.GenericError);

        // A valid anti-forgery pair, and no pending sign-in.
        using var page = await GetAsync(_configured, AdminSignedOutExemptions.SignIn);
        var token = AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync());
        var antiForgeryCookie = Assert.Single(SetCookies(page)).Split(';', 2)[0];

        // Only the authenticator page takes a POST; the recovery codes page has no POST rule at all.
        foreach (var path in new[] { Authenticator, RecoveryCodes })
        {
            using var response = await PostCodeAsync(_configured, antiForgeryCookie, token, "123456", path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Location"));
            Assert.Equal("{\"error\":\"Unauthorized\"}", await response.Content.ReadAsStringAsync());
        }

        Assert.Null(AdminSignedOutExemptions.FindRule(RecoveryCodes, "POST"));
        Assert.NotNull(AdminSignedOutExemptions.FindRule(Authenticator, "POST"));
        Assert.NotEqual(AdminDeny.GenericError, AdminAuthCopy.SignInFailure);

        // The page knows that word from the server, as a data attribute, and treats only it as "start again".
        var script = File.ReadAllText(AssetPath("authenticator-setup.js"));
        Assert.Contains(
            "if (reply.status === 401 && error && error === form.getAttribute(\"data-no-sign-in\")) {",
            script,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"location\.replace\(SIGN_IN_AGAIN\)"));
        Assert.Contains($"var SIGN_IN_AGAIN = \"{AdminSignInPages.SentBackLocation}\";", script, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Authenticator)]
    [InlineData(RecoveryCodes)]
    public async Task SetUpPage_FromAThrottledAddress_StillGetsThe429_NotTheRedirectAndNotThePage(string path)
    {
        // The test client has no remote address, which the app counts as this one.
        const string address = "0.0.0.0";
        var owner = await SeedAsync(_configured, path == Authenticator ? Owner.SettingUp : Owner.JustSetUp);
        using (var scope = _configured.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var now = scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow;
            for (var i = 0; i < AdminAuthService.IpFailureLimit; i++)
            {
                db.AdminAuthFailureEvents.Add(AdminAuthFailureEvent.Create(AdminAuthScopes.LoginIp, address, now));
            }

            await db.SaveChangesAsync();
        }

        try
        {
            // With or without a pending sign-in: the throttle is looked at first.
            foreach (var cookie in new string?[] { null, owner.Pending })
            {
                using var response = await GetAsync(_configured, path, cookie);

                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.False(response.Headers.Contains("Location"));
                Assert.False(response.Headers.Contains("Retry-After"));
                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal($"{{\"error\":\"{AdminAuthCopy.SignInFailure}\"}}", body);
                Assert.DoesNotContain(owner.Base32Key, body, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            await ClearSignInCountersAsync(_configured);
        }
    }

    [Theory]
    // The gate lets the request through (there is a pending sign-in), but the page has nothing it may show.
    [InlineData(Authenticator, Owner.Missing)]
    [InlineData(Authenticator, Owner.NothingWaiting)]
    [InlineData(RecoveryCodes, Owner.Missing)]
    [InlineData(RecoveryCodes, Owner.NothingWaiting)]
    // The recovery codes page before the authenticator is set up.
    [InlineData(RecoveryCodes, Owner.SettingUp)]
    public async Task SetUpPage_WithNothingItMayShow_SendsTheBrowserToStepOne_WithoutACookie(string path, Owner state)
    {
        var owner = await SeedAsync(_configured, state);
        using var response = await GetAsync(_configured, path, owner.Pending);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(AdminSignInPages.SentBackLocation, Assert.Single(response.Headers.GetValues("Location")));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Empty(SetCookies(response));
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
    }

    [Theory]
    [InlineData(Authenticator, Owner.SettingUp)]
    [InlineData(RecoveryCodes, Owner.JustSetUp)]
    public async Task SetUpPage_WhoseStoredValueCannotBeRead_SendsTheBrowserToStepOne(string path, Owner state)
    {
        var owner = await SeedAsync(_configured, state);
        using (var scope = _configured.Services.CreateScope())
        {
            // Not something this server's key encrypted.
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var notOurs = Convert.ToBase64String(new byte[64]);
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
            if (state == Owner.SettingUp)
            {
                account.SetPendingEnrollment(notOurs);
            }
            else
            {
                account.CompleteEnrollment(account.TotpSecretCipher!, notOurs);
            }

            await db.SaveChangesAsync();
        }

        using var response = await GetAsync(_configured, path, owner.Pending);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(AdminSignInPages.SentBackLocation, Assert.Single(response.Headers.GetValues("Location")));
        Assert.Empty(SetCookies(response));
    }

    [Fact]
    public async Task NothingOfTheKeyOrTheCodes_ReachesALogLineOrAnAuditRow()
    {
        var owner = await SeedAsync(_configured, Owner.SettingUp);
        _configured.LogCollector.Clear();

        using var page = await GetAsync(_configured, Authenticator, owner.Pending);
        var html = await page.Content.ReadAsStringAsync();
        var key = AdminEnrollmentPageReader.ReadKey(html);
        var token = AdminPageAntiForgery.ReadToken(html);
        var cookies = $"{Assert.Single(SetCookies(page)).Split(';', 2)[0]}; {AdminPendingAuthCookie.Name}={owner.Pending}";
        using (var refused = await PostCodeAsync(_configured, cookies, token, "000000"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        }

        var code = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(key), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using (var confirmed = await PostCodeAsync(_configured, cookies, token, code))
        {
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        }

        using var codesPage = await GetAsync(_configured, RecoveryCodes, owner.Pending);
        var codes = await AdminEnrollmentPageReader.ReadRecoveryCodesAsync(codesPage);
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);
        using (var sentOn = await GetAsync(_configured, Authenticator, owner.Pending))
        {
            Assert.Equal(HttpStatusCode.Found, sentOn.StatusCode);
        }

        using (var sentBack = await GetAsync(_configured, RecoveryCodes))
        {
            Assert.Equal(HttpStatusCode.Found, sentBack.StatusCode);
        }

        var lines = _configured.LogCollector.Lines;
        Assert.NotEmpty(lines);
        var groups = Enumerable.Range(0, 8).Select(i => key.Substring(i * 4, 4)).ToArray();
        foreach (var line in lines)
        {
            Assert.DoesNotContain(key, line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("otpauth", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(owner.Pending, line, StringComparison.Ordinal);
            Assert.DoesNotContain(string.Join(' ', groups.Take(2)), line, StringComparison.OrdinalIgnoreCase);
            Assert.All(codes, c => Assert.DoesNotContain(c, line, StringComparison.OrdinalIgnoreCase));
            Assert.All(codes, c => Assert.DoesNotContain(AdminRecoveryCodes.Normalize(c), line, StringComparison.OrdinalIgnoreCase));
        }

        using var scope = _configured.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var audit = db.AdminAuditLog.ToList();
        Assert.Contains(audit, e => e.Action == AdminAuthEvents.TotpEnroll);
        foreach (var row in audit.Select(e => JsonSerializer.Serialize(e)))
        {
            Assert.DoesNotContain(key, row, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("otpauth", row, StringComparison.OrdinalIgnoreCase);
            Assert.All(codes, c => Assert.DoesNotContain(c, row, StringComparison.OrdinalIgnoreCase));
        }

        // At rest the key and the codes are not readable either.
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        Assert.DoesNotContain(key, JsonSerializer.Serialize(account), StringComparison.OrdinalIgnoreCase);
        Assert.All(codes, c => Assert.DoesNotContain(c, JsonSerializer.Serialize(account), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Copy_HasNoDurationCountOrLockWording_AndTheAlertStartsEmpty()
    {
        foreach (var (path, state) in new[] { (Authenticator, Owner.SettingUp), (RecoveryCodes, Owner.JustSetUp), (RecoveryCodes, Owner.SetUp) })
        {
            var owner = await SeedAsync(_configured, state);
            using var response = await GetAsync(_configured, path, owner.Pending);
            var page = WithoutQrPathAndRandomValues(WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));

            foreach (var banned in new[] { "locked", "lockout", "minute", "hour", "attempt", "seconds", "try again in", "remaining" })
            {
                Assert.DoesNotContain(banned, page, StringComparison.OrdinalIgnoreCase);
            }

            // What the alert says after a refusal is the server's sentence, not page copy.
            Assert.DoesNotContain(AdminAuthCopy.SignInFailure, page, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminAuthCopy.VerificationFailed, page, StringComparison.Ordinal);
            if (path == Authenticator)
            {
                Assert.Contains("role=\"alert\" tabindex=\"-1\" hidden></div>", page, StringComparison.Ordinal);
            }
        }

        // The pages' own sentences, word for word.
        Assert.Equal(
            "Scan the QR code with your authenticator app, or enter the key by hand. Then enter a 6-digit code to confirm.",
            AdminEnrollmentCopy.AuthenticatorLede);
        Assert.Equal(
            "Authenticator QR code for Dealoware admin. Scan with your authenticator app.",
            AdminEnrollmentCopy.QrLabel);
        Assert.Equal("If you can't scan the code, enter this key in your app instead.", AdminEnrollmentCopy.KeyHelp);
        Assert.Equal("Manual key", AdminEnrollmentCopy.KeyLabel);
        Assert.Equal("Account name", AdminEnrollmentCopy.AccountNameLabel);
        Assert.Equal("Copy key", AdminEnrollmentCopy.CopyKey);
        Assert.Equal("Confirm", AdminEnrollmentCopy.AuthenticatorSubmit);
        Assert.Equal("Each code signs you in once if you can't use your authenticator app.", AdminEnrollmentCopy.RecoveryCodesPurpose);
        Assert.Equal("Copy all", AdminEnrollmentCopy.CopyAllCodes);
        Assert.Equal("I have saved these recovery codes.", AdminEnrollmentCopy.CodesSavedLabel);
        Assert.Equal("Continue", AdminEnrollmentCopy.RecoveryCodesContinue);
        Assert.Equal("Copied.", AdminEnrollmentCopy.Copied);
        Assert.Equal("Couldn't copy. The text is selected so you can copy it yourself.", AdminEnrollmentCopy.CopyFailed);
        // Shared with the sign-in pages, not written again.
        Assert.Equal(AdminSignInCopy.CodeLabel, AdminEnrollmentCopy.CodeLabel);
        Assert.Equal(AdminSignInCopy.CodeRequired, AdminEnrollmentCopy.CodeRequired);
        Assert.Equal(AdminSignInCopy.SubmitRetry, AdminEnrollmentCopy.SubmitRetry);
        Assert.Equal(AdminSignInCopy.BackToSignIn, AdminEnrollmentCopy.BackToSignIn);
    }

    [Fact]
    public async Task NoElementIdOrControlName_IsAGlobalThePageScriptsRead_AndEveryClassIsInTheStylesheet()
    {
        var globals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AdminArrival", "AdminDom", "AdminRequest", "AdminClipboard",
            "window", "document", "location", "history", "navigator", "performance", "fetch", "URL", "JSON", "Promise",
            "AbortController", "setTimeout", "clearTimeout", "getSelection",
            // Names every window already has, which an id would be confused with.
            "name", "status", "top", "self", "parent", "origin", "length", "event", "closed", "frames", "opener"
        };

        // Every name the scripts reach through their window parameter is on that list.
        var reached = AuthenticatorScripts.Concat(RecoveryCodesScripts).Distinct()
            .Select(path => path[(path.LastIndexOf('/') + 1)..])
            .SelectMany(file => Regex.Matches(File.ReadAllText(AssetPath(file)), @"\bglobal\.([A-Za-z_$][\w$]*)"))
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.Contains("AdminClipboard", reached);
        Assert.Contains("getSelection", reached);
        Assert.All(reached, name => Assert.Contains(name, globals));

        var stylesheet = File.ReadAllText(AssetPath("signed-out.css"));
        foreach (var (path, state) in new[] { (Authenticator, Owner.SettingUp), (RecoveryCodes, Owner.JustSetUp), (RecoveryCodes, Owner.SetUp) })
        {
            var owner = await SeedAsync(_configured, state);
            using var response = await GetAsync(_configured, path, owner.Pending);
            var html = await response.Content.ReadAsStringAsync();
            var names = Regex.Matches(html, "\\s(?:id|name)=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

            Assert.Contains("page-heading", names);
            Assert.All(names, name => Assert.DoesNotContain(name, globals));
            var ids = Regex.Matches(html, "\\sid=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            // Every id another attribute points at is in the page.
            foreach (Match reference in Regex.Matches(html, "\\s(?:for|aria-describedby|aria-labelledby)=\"([^\"]+)\""))
            {
                Assert.All(reference.Groups[1].Value.Split(' '), id => Assert.Contains(id, ids));
            }

            foreach (var cssClass in Regex.Matches(html, "\\sclass=\"([^\"]+)\"").SelectMany(m => m.Groups[1].Value.Split(' ')).Distinct())
            {
                Assert.Matches("\\." + Regex.Escape(cssClass) + "\\b", stylesheet);
            }
        }
    }

    [Theory]
    [InlineData("/admin/auth/admin-clipboard.js")]
    [InlineData("/admin/auth/authenticator-setup.js")]
    [InlineData("/admin/auth/recovery-codes.js")]
    public async Task PageScripts_AreServedSignedOut_WithNothingPrivateAndNoInlineCopy(string path)
    {
        using var response = await GetAsync(_configured, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        Assert.Empty(SetCookies(response));
        var body = await response.Content.ReadAsStringAsync();
        foreach (var banned in new[]
                 {
                     "DEALOWARE_", "siteverify", Cloudflare, "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write",
                     "eval(", "new Function", "localStorage", "sessionStorage", "indexedDB", "document.cookie",
                     "location.search", "location.hash", "history.", "location.href", "window.open", "console.",
                     "XMLHttpRequest", "fetch(", "sendBeacon", "otpauth", "__ADMIN_AUTH_TEST__"
                 })
        {
            Assert.DoesNotContain(banned, body, StringComparison.Ordinal);
        }

        // Copy lives in the C# constants classes and reaches the scripts as data attributes.
        foreach (var copy in new[]
                 {
                     AdminAuthCopy.SignInFailure, AdminSignInCopy.SubmitRetry, AdminSignInCopy.CodeRequired,
                     AdminEnrollmentCopy.Copied, AdminEnrollmentCopy.CopyFailed, AdminEnrollmentCopy.AuthenticatorNoscript,
                     AdminEnrollmentCopy.RecoveryCodesNoscript, AdminEnrollmentCopy.KeyHelp, AdminEnrollmentCopy.CodesSavedLabel
                 })
        {
            Assert.DoesNotContain(copy, body, StringComparison.Ordinal);
        }

        // No wait time, count or lock wording in what the browser runs either.
        foreach (var banned in new[] { "locked", "lockout", "Retry-After", "countdown", "attempts" })
        {
            Assert.DoesNotContain(banned, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AuthenticatorScript_SendsOnlyTheCode_AndLeavesOnlyOnWhatTheReplySays()
    {
        var script = File.ReadAllText(AssetPath("authenticator-setup.js"));

        Assert.Contains($"var API = \"{Authenticator}\";", script, StringComparison.Ordinal);
        Assert.Contains($"var RECOVERY_CODES = \"{RecoveryCodes}\";", script, StringComparison.Ordinal);
        Assert.Contains("var ME = \"/admin/api/me\";", script, StringComparison.Ordinal);

        // One request carries anything: the code, and only the code.
        Assert.Single(Regex.Matches(script, @"AdminRequest\.post\("));
        Assert.Contains("AdminRequest.post(API, { code: value })", script, StringComparison.Ordinal);
        Assert.Contains("var value = field.value.replace(/\\s+/g, \"\");", script, StringComparison.Ordinal);
        Assert.Contains("if (!/^[0-9]{6}$/.test(value)) {", script, StringComparison.Ordinal);

        // The key is never asked for: the one GET goes to an address that knows nothing of set-up,
        // and only its status is looked at.
        Assert.Single(Regex.Matches(script, @"AdminRequest\.get\("));
        Assert.Contains("AdminRequest.get(ME).then(function (reply) {", script, StringComparison.Ordinal);
        Assert.Contains("if (reply.status === 200 || reply.status === 401) AdminArrival.reload();", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminRequest.get(API", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminRequest.get(RECOVERY_CODES", script, StringComparison.Ordinal);
        Assert.DoesNotContain("body.secret", script, StringComparison.Ordinal);
        Assert.DoesNotContain("recoveryCodes", script, StringComparison.Ordinal);

        // It moves on only when the reply says both things the server's reply says, never for a bare 200
        // and not for "enrolled" alone.
        Assert.Contains(
            "if (body.enrolled === true && body.next === \"recovery-codes\") location.replace(RECOVERY_CODES);",
            script,
            StringComparison.Ordinal);
        Assert.Contains("else afterUnclearReply();", script, StringComparison.Ordinal);
        Assert.DoesNotContain("reply.status >= 200", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".ok", script, StringComparison.Ordinal);
        // Two places leave the page for another: the recovery codes, and step 1 when no pending sign-in is left.
        Assert.Equal(2, Regex.Matches(script, @"location\.replace\(").Count);
        Assert.DoesNotContain("location.assign", script, StringComparison.Ordinal);
        Assert.DoesNotContain("location =", script, StringComparison.Ordinal);

        // A refusal is shown in the server's words and the field is cleared with it.
        Assert.Contains("if ((reply.status === 401 || reply.status === 429) && error) refused(error);", script, StringComparison.Ordinal);
        Assert.Contains("else if (reply.status >= 400 && reply.status < 500) sayRetry();", script, StringComparison.Ordinal);

        // A 403 means a stale anti-forgery pair: load the page anew, once.
        Assert.Contains("var mayReload = AdminArrival.flag() !== \"fresh\";", script, StringComparison.Ordinal);
        Assert.Contains("if (reply.status === 403 && mayReload) {", script, StringComparison.Ordinal);
        Assert.Contains("if (reply.status !== 403) mayReload = true;", script, StringComparison.Ordinal);
        // The other reload is the one that lets the server say where an unanswered code left things.
        Assert.Equal(2, Regex.Matches(script, @"AdminArrival\.reload\(\)").Count);
        Assert.Contains("if (unsettled) {", script, StringComparison.Ordinal);

        // Without its helpers the page says so and shows neither the key nor the form.
        var guardAt = script.IndexOf("if (typeof AdminArrival === \"undefined\"", StringComparison.Ordinal);
        Assert.True(guardAt > 0);
        foreach (var helper in new[] { "AdminDom", "AdminRequest", "AdminClipboard" })
        {
            Assert.Contains($"typeof {helper} === \"undefined\"", script, StringComparison.Ordinal);
            Assert.True(guardAt < script.IndexOf(helper + ".", StringComparison.Ordinal), helper);
        }

        Assert.Contains("keyPart.hidden = true;", script, StringComparison.Ordinal);
        Assert.Contains("form.hidden = true;", script, StringComparison.Ordinal);
        Assert.Contains("alertEl.textContent = form.getAttribute(\"data-retry\") || \"\";", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"AdminDom\.show\(panel, true\)"));
        Assert.True(
            script.IndexOf("form.addEventListener(\"submit\"", StringComparison.Ordinal)
            < script.IndexOf("AdminDom.show(panel, true)", StringComparison.Ordinal));

        // The copy button hands the clipboard the key as the page shows it, inside its click handler.
        Assert.Contains("copyKey.addEventListener(\"click\", function () {", script, StringComparison.Ordinal);
        Assert.Contains("AdminClipboard.copy(key.textContent || \"\", key).then(function (copied) {", script, StringComparison.Ordinal);
        Assert.Contains("copyKey.focus();", script, StringComparison.Ordinal);

        // What was typed, the key and its QR code do not stay in a page the browser keeps for its Back button.
        Assert.Matches(
            @"window\.addEventListener\(""pagehide"", function \(\) \{\s+field\.value = """";\s+AdminDom\.clear\(key\);\s+AdminDom\.clear\(qr\);\s+\}\);",
            script);
        Assert.Matches(
            @"window\.addEventListener\(""pageshow"", function \(event\) \{\s+if \(event\.persisted\) \{\s+location\.reload\(\);\s+return;\s+\}",
            script);

        // The page never submits by itself.
        Assert.DoesNotContain("requestSubmit", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".submit()", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"send\(value\);"));
    }

    [Fact]
    public void RecoveryCodesScript_ShowsTheCodesWhateverElseLoaded_AndContinueGivesThePendingSignInUp()
    {
        var script = File.ReadAllText(AssetPath("recovery-codes.js"));

        // "Continue" works only with the box ticked.
        Assert.Contains("next.disabled = leaving || !saved.checked;", script, StringComparison.Ordinal);
        Assert.Contains("saved.addEventListener(\"change\", syncContinue);", script, StringComparison.Ordinal);
        Assert.Contains("if (leaving || !saved.checked) return;", script, StringComparison.Ordinal);
        // A reload does not carry the tick over.
        Assert.Contains("saved.checked = false;\n  syncContinue();\n  panel.hidden = false;", script.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);

        // It then has the server expire the pending sign-in's cookie and puts step 1 in this page's place,
        // so this browser cannot be shown the codes again. It goes whatever the answer.
        Assert.Contains("var SIGN_OUT = \"/admin/api/auth/sign-out\";", script, StringComparison.Ordinal);
        Assert.Equal("/admin/api/auth/sign-out", AdminAuthEndpoints.SignOutPath);
        Assert.Contains($"var SIGN_IN = \"{AdminSignedOutExemptions.SignIn}\";", script, StringComparison.Ordinal);
        Assert.Contains("var leave = function () { location.replace(SIGN_IN); };", script, StringComparison.Ordinal);
        Assert.Contains("AdminRequest.post(SIGN_OUT, {}).then(leave, leave);", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"AdminRequest\.post\("));
        Assert.Single(Regex.Matches(script, @"location\.replace\("));
        Assert.DoesNotContain("AdminRequest.get(", script, StringComparison.Ordinal);
        Assert.DoesNotContain("location.assign", script, StringComparison.Ordinal);

        // The codes come first: no helper script is needed to show them or to leave.
        Assert.DoesNotContain("AdminDom", script, StringComparison.Ordinal);
        Assert.Contains("if (typeof AdminRequest === \"undefined\") {\n      leave();", script.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("if (typeof AdminClipboard === \"undefined\") {\n      copyAll.hidden = true;", script.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);

        // "Copy all": one code to a line, inside the click handler.
        Assert.Contains("return lines.join(\"\\n\");", script, StringComparison.Ordinal);
        Assert.Contains("AdminClipboard.copy(codesAsText(), list).then(function (copied) {", script, StringComparison.Ordinal);
        Assert.Contains("copyAll.focus();", script, StringComparison.Ordinal);

        // The codes do not stay in a page the browser keeps for its Back button.
        Assert.Matches(
            @"window\.addEventListener\(""pagehide"", function \(\) \{\s+while \(list\.firstChild\) list\.removeChild\(list\.firstChild\);\s+saved\.checked = false;\s+\}\);",
            script);
        Assert.Contains("if (event.persisted) location.reload();", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ClipboardScript_UsesTheAsynchronousClipboardFirst_ThenSelectsTheText_AndKeepsNothing()
    {
        var script = File.ReadAllText(AssetPath("admin-clipboard.js"));

        Assert.Contains("var clipboard = global.navigator.clipboard;", script, StringComparison.Ordinal);
        Assert.Contains("asked = clipboard.writeText(text);", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"writeText\("));
        // The fallback selects the element that shows the text and asks for the selection to be copied.
        Assert.Contains("range.selectNodeContents(node);", script, StringComparison.Ordinal);
        Assert.Contains("copied = document.execCommand(\"copy\") === true;", script, StringComparison.Ordinal);
        // Only a selection that did its job is put away; one the reader must copy by hand stays.
        Assert.Contains("if (copied) global.getSelection().removeAllRanges();", script, StringComparison.Ordinal);
        // It never rejects: the whole fallback, selecting included, runs where an error becomes "not copied".
        Assert.Matches(
            @"function copySelection\(node\) \{\s+try \{\s+if \(!select\(node\)\) return false;[^}]+return copied;\s+\} catch \(e\) \{\s+return false;\s+\}\s+\}",
            script);
        Assert.Contains("return Promise.resolve(copySelection(node));", script, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", script, StringComparison.Ordinal);
        // And it never reads the clipboard.
        Assert.DoesNotContain("readText", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".read(", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminRequest", script, StringComparison.Ordinal);
        Assert.Equal("/admin/auth/admin-clipboard.js", AdminSignedOutPage.ClipboardScriptPath);
    }

    /// <summary>The page without its QR path, nonce and anti-forgery token: none of them is copy.</summary>
    private static string WithoutQrPathAndRandomValues(string html)
        => Regex.Replace(html, "(nonce|content|d)=\"[^\"]*\"", string.Empty);

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

    private sealed record SeededOwner(
        string Pending,
        string Base32Key,
        string? PendingCipher,
        IReadOnlyList<string> RecoveryCodes);

    /// <summary>
    /// Puts the owner into the given state and gives them a pending sign-in,
    /// as step 1 leaves it: a row holding the hash, and the raw value for the
    /// cookie. Everything an earlier test left for this owner is cleared
    /// first, the throttle included, which would turn the page into a 429.
    /// </summary>
    private static async Task<SeededOwner> SeedAsync(IsolatedWebApplicationFactory factory, Owner state)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();

        db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes);
        db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths.Where(p => p.Email == OwnerEmail));
        db.AdminSessions.RemoveRange(db.AdminSessions.Where(s => s.Email == OwnerEmail));
        db.AdminCoreOwnerAccounts.RemoveRange(db.AdminCoreOwnerAccounts.Where(a => a.Email == OwnerEmail));
        db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
        db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
        await db.SaveChangesAsync();

        var key = Rfc6238Totp.GenerateSecret();
        string? pendingCipher = null;
        IReadOnlyList<string> codes = [];
        if (state != Owner.Missing)
        {
            var account = AdminCoreOwnerAccount.Create(OwnerEmail);
            switch (state)
            {
                case Owner.SettingUp:
                    pendingCipher = protector.Encrypt(key);
                    account.SetPendingEnrollment(pendingCipher);
                    break;
                case Owner.JustSetUp:
                    codes = AdminRecoveryCodes.Generate();
                    foreach (var code in codes)
                    {
                        db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(code)));
                    }

                    account.CompleteEnrollment(
                        protector.Encrypt(key),
                        protector.Encrypt(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(codes))));
                    break;
                case Owner.SetUp:
                    account.CompleteEnrollment(protector.Encrypt(key));
                    break;
            }

            db.AdminCoreOwnerAccounts.Add(account);
        }

        var raw = AdminPendingToken.Create();
        db.AdminPendingAuths.Add(AdminPendingAuth.Create(OwnerEmail, AdminPendingToken.Hash(raw)));
        await db.SaveChangesAsync();
        return new SeededOwner(raw, Rfc6238Totp.ToBase32(key), pendingCipher, codes);
    }

    private static async Task<string> CreatePendingSignInAsync(IsolatedWebApplicationFactory factory, bool consumed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var raw = AdminPendingToken.Create();
        var pending = AdminPendingAuth.Create(OwnerEmail, AdminPendingToken.Hash(raw));
        if (consumed)
        {
            pending.Consume();
        }

        db.AdminPendingAuths.Add(pending);
        await db.SaveChangesAsync();
        return raw;
    }

    private static async Task ClearSignInCountersAsync(IsolatedWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
        db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> GetAsync(
        IsolatedWebApplicationFactory factory,
        string path,
        string? pendingCookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (pendingCookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminPendingAuthCookie.Name}={pendingCookie}");
        }

        return await Client(factory).SendAsync(request);
    }

    /// <summary>The page's one request, as the page sends it: the code as JSON, with the page's anti-forgery token.</summary>
    private static async Task<HttpResponseMessage> PostCodeAsync(
        IsolatedWebApplicationFactory factory,
        string cookies,
        string token,
        string code,
        string path = Authenticator)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", cookies);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        request.Content = System.Net.Http.Json.JsonContent.Create(new { code });
        return await Client(factory).SendAsync(request);
    }

    /// <summary>A POST with the given JSON text as its body, or with no body at all.</summary>
    private static async Task<HttpResponseMessage> PostAsync(
        IsolatedWebApplicationFactory factory,
        string path,
        string cookies,
        string token,
        string? json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", cookies);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await Client(factory).SendAsync(request);
    }

    /// <summary>Opens one of the two pages and returns what its POST needs: the cookies and the anti-forgery token.</summary>
    private static async Task<(string Cookies, string Token)> OpenPageAsync(
        IsolatedWebApplicationFactory factory,
        string path,
        string pending)
    {
        using var page = await GetAsync(factory, path, pending);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var token = AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync());
        var antiForgery = Assert.Single(SetCookies(page)).Split(';', 2)[0];
        return ($"{antiForgery}; {AdminPendingAuthCookie.Name}={pending}", token);
    }

    /// <summary>Failures counted towards the account lock and the address throttle.</summary>
    private static Task<int> CountFailuresAsync(IsolatedWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminAuthFailureEvents.Count());
    }

    private static Task<int> CountFailedSecondStepsAsync(IsolatedWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminAuditLog.Count(e => e.Action == AdminAuthEvents.SecondFactorFailed));
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

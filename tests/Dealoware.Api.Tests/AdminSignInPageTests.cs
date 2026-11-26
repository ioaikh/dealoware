using System.Net;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// The three sign-in pages: S-A1 (GET /admin/sign-in), S-A2
/// (GET /admin/sign-in/code) and S-A3 (GET /admin/sign-in/recovery).
/// The default host has no Turnstile site key, which is the fail-closed case;
/// <see cref="TurnstileSiteKeyWebApplicationFactory"/> is the configured case.
/// S-A2 and S-A3 are served only with a pending sign-in, so those requests
/// carry the cookie of a pending row made here.
/// What the pages do in a browser is covered by e2e/sign-in.
/// </summary>
[Collection("WebAppTests")]
public class AdminSignInPageTests : IClassFixture<TurnstileSiteKeyWebApplicationFactory>
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string Cloudflare = "challenges.cloudflare.com";
    private const string OwnerEmail = "io@aiknowhow.com";

    private const string SignIn = AdminSignedOutExemptions.SignIn;
    private const string Code = AdminSignedOutExemptions.SignInCode;
    private const string Recovery = AdminSignedOutExemptions.SignInRecovery;

    private static readonly string[] SignInScripts =
    [
        // First: it takes a flag out of the address bar before anything else can fail.
        "/admin/auth/admin-arrival.js",
        "/admin/auth/admin-dom.js",
        "/admin/auth/admin-password.js",
        "/admin/auth/admin-turnstile.js",
        "/admin/auth/admin-request.js",
        "/admin/auth/sign-in.js"
    ];

    private static readonly string[] SecondStepScripts =
    [
        "/admin/auth/admin-arrival.js",
        "/admin/auth/admin-dom.js",
        "/admin/auth/admin-request.js",
        "/admin/auth/sign-in-code.js"
    ];

    private readonly IsolatedWebApplicationFactory _unconfigured;
    private readonly TurnstileSiteKeyWebApplicationFactory _configured;

    public AdminSignInPageTests(
        IsolatedWebApplicationFactory unconfigured,
        TurnstileSiteKeyWebApplicationFactory configured)
    {
        _unconfigured = unconfigured;
        _configured = configured;
    }

    public static TheoryData<string, string, string> Pages => new()
    {
        { SignIn, AdminSignInCopy.SignInPageTitle, AdminSignInCopy.SignInHeading },
        { Code, AdminSignInCopy.CodePageTitle, AdminSignInCopy.CodeHeading },
        { Recovery, AdminSignInCopy.RecoveryPageTitle, AdminSignInCopy.RecoveryHeading }
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_ReturnsTheHtmlPage_InTheSharedShell(string path, string title, string heading)
    {
        using var response = await GetPageAsync(_configured, path);

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
            path == SignIn ? SignInScripts : SecondStepScripts,
            scripts.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
        Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
        Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onclick=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onsubmit=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);

        // The placeholder answer is gone: no JSON, and the token is in the page only.
        Assert.DoesNotContain("csrfToken", html, StringComparison.Ordinal);
        Assert.DoesNotContain("\"screen\"", html, StringComparison.Ordinal);
        Assert.False(response.Headers.Contains(AdminAntiForgeryService.HeaderName));
    }

    [Fact]
    public void Titles_AreOnePerPage_AndTheStepOneButtonIsContinue()
    {
        Assert.Equal("Sign in · Dealoware admin", AdminSignInCopy.SignInPageTitle);
        Assert.Equal("Enter code · Dealoware admin", AdminSignInCopy.CodePageTitle);
        Assert.Equal("Recovery code · Dealoware admin", AdminSignInCopy.RecoveryPageTitle);
        Assert.Equal("Sign in", AdminSignInCopy.SignInHeading);
        // Step 1 signs nobody in yet; the binding label is "Continue". Step 2 keeps "Sign in".
        Assert.Equal("Continue", AdminSignInCopy.SignInSubmit);
        Assert.Equal("Sign in", AdminSignInCopy.SecondStepSubmit);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_WithoutJavaScript_ShowsOneSentenceAndNoForm(string path, string title, string heading)
    {
        using var response = await GetPageAsync(_unconfigured, path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, html, StringComparison.Ordinal);

        Assert.Equal("Turn on JavaScript to sign in.", AdminSignInCopy.Noscript);
        var panel = path == SignIn ? "<div id=\"sign-in-panel\" hidden>" : "<div id=\"second-step-panel\" hidden>";
        // Heading, then the one sentence, then the panel that the script alone can show.
        Assert.Contains(
            $"<h1 id=\"page-heading\">{heading}</h1>"
            + $"<noscript><p>{AdminSignInCopy.Noscript}</p></noscript>"
            + panel,
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<noscript>"));

        // Everything else, the form included, is inside that hidden panel.
        var panelAt = html.IndexOf(panel, StringComparison.Ordinal);
        var mainEndAt = html.IndexOf("</div></main>", StringComparison.Ordinal);
        Assert.EndsWith("</form></div>", html[..mainEndAt], StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<form "));
        Assert.True(html.IndexOf("<form ", StringComparison.Ordinal) > panelAt);
        Assert.True(html.IndexOf("class=\"lede\"", StringComparison.Ordinal) > panelAt);
        Assert.Single(Regex.Matches(html, "<div id=\"[a-z-]+-panel\" hidden>"));
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Get_SetsNoStoreNoReferrer_AndIssuesTheAntiForgeryPair(string path, string title, string heading)
    {
        using var response = await GetPageAsync(_configured, path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains(heading, html, StringComparison.Ordinal);

        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.FrameOptions, Header(response, "X-Frame-Options"));
        Assert.Equal(AdminSecurityHeadersMiddleware.RobotsTag, Header(response, "X-Robots-Tag"));

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
    }

    [Fact]
    public async Task SignInCsp_WithASiteKey_AddsCloudflareForScriptAndFrame_AndNothingElse()
    {
        using var response = await GetPageAsync(_configured, SignIn);
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
        Assert.Equal(2, Regex.Matches(csp, Regex.Escape(Cloudflare)).Count);
        AssertNoLooseSources(csp);

        // The page markup itself never names Cloudflare: the widget script adds its script tag.
        Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignInCsp_WithoutASiteKey_IsTheBasePolicy_WithNoCloudflare()
    {
        using var response = await GetPageAsync(_unconfigured, SignIn);
        var html = await response.Content.ReadAsStringAsync();

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Equal(BasePolicy(SingleNonce(html)), csp);
        Assert.DoesNotContain(Cloudflare, csp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame-src", csp, StringComparison.Ordinal);
        AssertNoLooseSources(csp);
    }

    [Theory]
    [InlineData(Code)]
    [InlineData(Recovery)]
    public async Task SecondStepCsp_NeverAllowsCloudflare_EvenWithASiteKeyConfigured(string path)
    {
        foreach (var factory in new[] { _configured, _unconfigured })
        {
            using var response = await GetPageAsync(factory, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();

            var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
            Assert.Equal(BasePolicy(SingleNonce(html)), csp);
            Assert.Equal(AdminPasswordResetEndpoints.ConfirmPageContentSecurityPolicy(SingleNonce(html)), csp);
            Assert.DoesNotContain(Cloudflare, csp, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("frame-src", csp, StringComparison.Ordinal);
            AssertNoLooseSources(csp);

            // No security check on this step: no widget, no site key, and its script is not loaded.
            Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TurnstileSiteKeyWebApplicationFactory.SiteKey, html, StringComparison.Ordinal);
            Assert.DoesNotContain("security-check", html, StringComparison.Ordinal);
            Assert.DoesNotContain("data-sitekey", html, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminSignedOutPage.TurnstileScriptPath, html, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminSignedOutPage.PasswordScriptPath, html, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Csp_NonceIsFreshOnEveryResponse(string path, string title, string heading)
    {
        using var first = await GetPageAsync(_configured, path);
        using var second = await GetPageAsync(_configured, path);
        var firstHtml = await first.Content.ReadAsStringAsync();
        Assert.Contains(title, firstHtml, StringComparison.Ordinal);
        Assert.Contains(heading, firstHtml, StringComparison.Ordinal);

        var firstNonce = SingleNonce(firstHtml);
        var secondNonce = SingleNonce(await second.Content.ReadAsStringAsync());

        Assert.NotEqual(firstNonce, secondNonce);
        Assert.True(Convert.FromBase64String(firstNonce).Length >= 16);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Markup_CarriesNothingPrivate_AndEchoesNothingFromTheAddress(string path, string title, string heading)
    {
        var probe = "probe" + Guid.NewGuid().ToString("N");
        var pending = await CreatePendingSignInAsync(_configured);

        // A query string never exempts the two second-step pages, so only step 1 is asked with one.
        var address = path == SignIn
            ? $"{path}?returnPath=https%3A%2F%2F{probe}.example%2F&next=%2F{probe}&email={probe}%40example.test&status={probe}"
            : path;
        using var response = await GetAsync(_configured, address, pending);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains(heading, html, StringComparison.Ordinal);

        Assert.DoesNotContain(probe, html, StringComparison.Ordinal);
        Assert.DoesNotContain("returnPath", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(pending, html, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPendingToken.Hash(pending), html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AdminPendingAuthCookie.Name, html, StringComparison.Ordinal);
        Assert.DoesNotContain(OwnerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", html, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, html, StringComparison.Ordinal);
        Assert.DoesNotContain(TurnstileOptions.SecretEnvironmentVariable, html, StringComparison.Ordinal);
        Assert.DoesNotContain("DEALOWARE_", html, StringComparison.Ordinal);
        Assert.DoesNotContain("siteverify", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("otpauth", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"hidden\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" value=", html, StringComparison.Ordinal);
        Assert.All(SetCookies(response), c => Assert.DoesNotContain(pending, c, StringComparison.Ordinal));
        Assert.All(
            response.Headers.SelectMany(h => h.Value),
            v =>
            {
                Assert.DoesNotContain(_configured.Secret, v, StringComparison.Ordinal);
                Assert.DoesNotContain(probe, v, StringComparison.Ordinal);
            });
    }

    [Theory]
    [InlineData(Code)]
    [InlineData(Recovery)]
    public async Task SecondStepPage_WithoutAPendingSignIn_SendsTheBrowserToStepOne_AndServesNoPage(string path)
    {
        // With no pending sign-in the session gate answers before the page handler runs.
        // For the GET of these two pages its answer is a redirect to step 1: a constant
        // address, no body, and no cookie set or cleared.
        await ClearSignInCountersAsync(_configured);
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
                v => Assert.DoesNotContain(probe, v, StringComparison.Ordinal));
        }

        // No loop: the target is always served, whatever the browser holds.
        using var stepOne = await GetAsync(_configured, AdminSignedOutExemptions.SignIn, neverIssued);
        Assert.Equal(HttpStatusCode.OK, stepOne.StatusCode);
        Assert.False(stepOne.Headers.Contains("Location"));
        Assert.Contains($"<title>{AdminSignInCopy.SignInPageTitle}</title>", await stepOne.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // A path that is not exactly one of the two pages gets the gate's 401 as before.
        var live = await CreatePendingSignInAsync(_configured);
        foreach (var notThePage in new[] { path + "/", path + "/extra", path.Replace("/sign-in/", "/sign-in%2f", StringComparison.Ordinal) })
        {
            using var denied = await GetAsync(_configured, notThePage, live);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.False(denied.Headers.Contains("Location"));
            Assert.Equal("{\"error\":\"Unauthorized\"}", await denied.Content.ReadAsStringAsync());
        }

        // And with a pending sign-in the page itself is served.
        using var ok = await GetAsync(_configured, path, live);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task OnlyTheGetOfTheTwoPages_IsRedirected_EveryOtherRequestWithoutAPendingSignInKeepsThe401()
    {
        await ClearSignInCountersAsync(_configured);
        Assert.Equal("Unauthorized", AdminDeny.GenericError);

        // The form's own paths and the JSON paths, by POST, with a valid anti-forgery pair.
        using var page = await GetAsync(_configured, SignIn);
        var token = AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync());
        var antiForgeryCookie = Assert.Single(SetCookies(page)).Split(';', 2)[0];
        foreach (var path in new[]
                 {
                     Code, Recovery,
                     AdminSignedOutExemptions.ApiSignInCode, AdminSignedOutExemptions.ApiSignInRecovery
                 })
        {
            using var post = new HttpRequestMessage(HttpMethod.Post, path);
            post.Headers.Host = AdminHost;
            post.Headers.TryAddWithoutValidation("Cookie", antiForgeryCookie);
            post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
            post.Content = System.Net.Http.Json.JsonContent.Create(new { code = "123456", recoveryCode = "AAAA-BBBB-CCCC" });
            using var response = await Client(_configured).SendAsync(post);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Location"));
            Assert.Equal("{\"error\":\"Unauthorized\"}", await response.Content.ReadAsStringAsync());
        }

        // Every other signed-out or signed-in GET without what it needs, the JSON twins included.
        foreach (var path in new[]
                 {
                     AdminSignedOutExemptions.ApiSignInCode, AdminSignedOutExemptions.ApiSignInRecovery,
                     AdminSignedOutExemptions.SetupAuthenticator, AdminSignedOutExemptions.SetupRecoveryCodes,
                     "/admin/", "/admin/api/me", "/admin/participants"
                 })
        {
            using var response = await GetAsync(_configured, path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Location"));
            Assert.Equal("{\"error\":\"Unauthorized\"}", await response.Content.ReadAsStringAsync());
        }

        // Another method on the page path is not a visit either.
        using var head = new HttpRequestMessage(HttpMethod.Head, Code);
        head.Headers.Host = AdminHost;
        using var headResponse = await Client(_configured).SendAsync(head);
        Assert.Equal(HttpStatusCode.Unauthorized, headResponse.StatusCode);
        Assert.False(headResponse.Headers.Contains("Location"));
    }

    [Theory]
    [InlineData(Code)]
    [InlineData(Recovery)]
    public async Task SecondStepPage_FromAThrottledAddress_StillGetsThe429_NotTheRedirect(string path)
    {
        // The test client has no remote address, which the app counts as this one.
        const string address = "0.0.0.0";
        await ClearSignInCountersAsync(_configured);
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
            foreach (var cookie in new string?[] { null, await CreatePendingSignInAsync(_configured, clearCounters: false) })
            {
                using var response = await GetAsync(_configured, path, cookie);

                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.False(response.Headers.Contains("Location"));
                Assert.False(response.Headers.Contains("Retry-After"));
                Assert.Equal(
                    $"{{\"error\":\"{AdminAuthCopy.SignInFailure}\"}}",
                    await response.Content.ReadAsStringAsync());
            }
        }
        finally
        {
            await ClearSignInCountersAsync(_configured);
        }
    }

    [Theory]
    [InlineData(Code, AdminSignedOutExemptions.ApiSignInCode)]
    [InlineData(Recovery, AdminSignedOutExemptions.ApiSignInRecovery)]
    public async Task SecondStepPost_WithoutAPendingSignIn_GetsTheGateWord_ThatThePageSendsBackToStepOne(
        string pagePath,
        string apiPath)
    {
        // The page is open, then the pending sign-in runs out (here: is used up).
        var pending = await CreatePendingSignInAsync(_configured);
        using var page = await GetAsync(_configured, pagePath, pending);
        var html = await page.Content.ReadAsStringAsync();
        var token = AdminPageAntiForgery.ReadToken(html);
        var antiForgeryCookie = Assert.Single(SetCookies(page)).Split(';', 2)[0];
        await ConsumePendingSignInsAsync(_configured);

        using var request = new HttpRequestMessage(HttpMethod.Post, apiPath);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{antiForgeryCookie}; {AdminPendingAuthCookie.Name}={pending}");
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        request.Content = System.Net.Http.Json.JsonContent.Create(new { code = "123456", recoveryCode = "AAAA-BBBB-CCCC" });
        using var response = await Client(_configured).SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"error\":\"Unauthorized\"}", body);
        Assert.NotEqual(AdminDeny.GenericError, AdminAuthCopy.SignInFailure);

        // The page knows that word from the server, as a data attribute, and treats only it as "start again".
        Assert.Contains($" data-no-sign-in=\"{AdminDeny.GenericError}\"", html, StringComparison.Ordinal);
        var script = File.ReadAllText(AssetPath("sign-in-code.js"));
        Assert.Contains(
            "if (reply.status === 401 && error && error === form.getAttribute(\"data-no-sign-in\")) {",
            script,
            StringComparison.Ordinal);
        // It goes to step 1 with the fixed note that step 1 has to be done again, so that page can say so.
        Assert.Single(Regex.Matches(script, @"location\.replace\(SIGN_IN_AGAIN\)"));
        Assert.Contains($"var SIGN_IN_AGAIN = \"{AdminSignInPages.SentBackLocation}\";", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignIn_OpensWithTheServerSentence_WhenTheBrowserWasSentBack_WithoutStorageCookieOrAnythingPrivateInTheAddress()
    {
        // The note is one fixed word after the address. It carries nothing about the visitor.
        Assert.Equal("again", AdminSignInPages.SentBackFlag);
        Assert.Equal(AdminSignedOutExemptions.SignIn + "#again", AdminSignInPages.SentBackLocation);

        // The sentence shown is the server's own, carried by the page, not by the address.
        using var response = await GetPageAsync(_configured, SignIn);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains($" data-sent-back=\"{AdminAuthCopy.SignInFailure}\">", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, Regex.Escape(AdminAuthCopy.SignInFailure)));
        var page = File.ReadAllText(AssetPath("sign-in.js"));
        Assert.Contains(
            "if (arrival === \"again\") showAlert(form.getAttribute(\"data-sent-back\") || \"\");",
            page,
            StringComparison.Ordinal);

        // The first script reads the flag, compares it with two constants and removes it. Nothing else.
        var arrival = File.ReadAllText(AssetPath("admin-arrival.js"));
        Assert.Contains("var AGAIN = \"#again\";", arrival, StringComparison.Ordinal);
        Assert.Contains("var FRESH = \"#fresh\";", arrival, StringComparison.Ordinal);
        Assert.Contains(
            "var flag = location.hash === AGAIN ? \"again\" : location.hash === FRESH ? \"fresh\" : \"\";",
            arrival,
            StringComparison.Ordinal);
        Assert.Contains(
            "if (flag) history.replaceState(null, \"\", location.pathname + location.search);",
            arrival,
            StringComparison.Ordinal);
        Assert.True(
            arrival.IndexOf("history.replaceState", StringComparison.Ordinal)
            < arrival.IndexOf("global.AdminArrival", StringComparison.Ordinal),
            "the address bar is cleared before the script does anything else");
        foreach (var other in new[]
                 {
                     "AdminDom", "AdminRequest", "AdminTurnstile", "fetch(", "document.", "decodeURIComponent",
                     "localStorage", "sessionStorage", "cookie", "setTimeout"
                 })
        {
            Assert.DoesNotContain(other, arrival, StringComparison.Ordinal);
        }

        Assert.Equal("/admin/auth/admin-arrival.js", AdminSignedOutPage.ArrivalScriptPath);
        Assert.Equal(AdminSignedOutPage.ArrivalScriptPath, SignInScripts[0]);
        Assert.Equal(AdminSignedOutPage.ArrivalScriptPath, SecondStepScripts[0]);
    }

    [Fact]
    public void PageScripts_ReloadOnceOnA403_ShowNoFormWithoutTheirHelpers_AndClearWhatWasTypedOnLeaving()
    {
        var arrival = File.ReadAllText(AssetPath("admin-arrival.js"));
        Assert.Contains(
            "history.replaceState(null, \"\", location.pathname + location.search + FRESH);",
            arrival,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(arrival, @"location\.reload\(\)"));

        foreach (var (file, field) in new[] { ("sign-in.js", "password"), ("sign-in-code.js", "field") })
        {
            var script = File.ReadAllText(AssetPath(file));

            // A 403 means a stale anti-forgery pair: load the page anew, once. A page that arrived
            // from such a reload waits for the server to accept a request before it may do it again.
            Assert.Contains("var mayReload = AdminArrival.flag() !== \"fresh\";", script, StringComparison.Ordinal);
            Assert.Contains("if (reply.status === 403 && mayReload) {", script, StringComparison.Ordinal);
            Assert.Contains("if (reply.status !== 403) mayReload = true;", script, StringComparison.Ordinal);
            Assert.Single(Regex.Matches(script, @"AdminArrival\.reload\(\)"));

            // Without its helpers the page says so and shows no form. The check comes before any helper is used.
            var guardAt = script.IndexOf("if (typeof AdminArrival === \"undefined\"", StringComparison.Ordinal);
            Assert.True(guardAt > 0, $"{file} checks that its helpers arrived");
            Assert.Contains("typeof AdminRequest === \"undefined\"", script, StringComparison.Ordinal);
            Assert.Contains("typeof AdminDom === \"undefined\"", script, StringComparison.Ordinal);
            Assert.Contains("form.hidden = true;", script, StringComparison.Ordinal);
            Assert.Contains("alertEl.textContent = form.getAttribute(\"data-retry\") || \"\";", script, StringComparison.Ordinal);
            Assert.True(guardAt < script.IndexOf("AdminDom.", StringComparison.Ordinal));
            Assert.True(guardAt < script.IndexOf("AdminArrival.flag()", StringComparison.Ordinal));
            Assert.True(guardAt < script.IndexOf("AdminDom.show(panel, true)", StringComparison.Ordinal));

            // What was typed does not stay in a page the browser keeps for its Back button.
            Assert.Matches(
                @"window\.addEventListener\(""pagehide"", function \(\) \{\s+" + field + @"\.value = """";\s+\}\);",
                script);
        }

        Assert.Contains("typeof AdminTurnstile === \"undefined\"", File.ReadAllText(AssetPath("sign-in.js")), StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload", File.ReadAllText(AssetPath("sign-in.js")), StringComparison.Ordinal);

        // A step 2 page the browser brings back from its Back-button store asks the server for itself
        // again, and the server serves it only while the pending sign-in lasts. On a page loaded anew
        // by Back or Forward, whatever the browser put back into the field is removed.
        var stepTwo = File.ReadAllText(AssetPath("sign-in-code.js"));
        Assert.Matches(
            @"window\.addEventListener\(""pageshow"", function \(event\) \{\s+if \(event\.persisted\) \{\s+location\.reload\(\);\s+return;\s+\}",
            stepTwo);
        Assert.Single(Regex.Matches(stepTwo, @"location\.reload\(\)"));
        Assert.Contains(
            "if (visits.length > 0 && visits[0].type === \"back_forward\") field.value = \"\";",
            stepTwo,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignIn_SiteKey_WhenConfigured_IsRendered_AndSubmitStartsDisabled()
    {
        using var response = await GetPageAsync(_configured, SignIn);
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
        AssertSignInSubmitDisabledAfterTheWidget(html);
    }

    [Fact]
    public async Task SignIn_SiteKey_WhenMissing_FailsClosed_SaysUnavailable_AndNeverNamesCloudflare()
    {
        using var response = await GetPageAsync(_unconfigured, SignIn);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("data-sitekey", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-action", html, StringComparison.Ordinal);
        Assert.Contains(
            "<p id=\"security-check-status\" class=\"security-check-status\" role=\"status\" tabindex=\"-1\">"
            + AdminSignedOutCopy.TurnstileNotConfigured + "</p>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(" id=\"security-check-retry\" class=\"secondary\" hidden>", html, StringComparison.Ordinal);
        Assert.DoesNotContain(Cloudflare, html, StringComparison.OrdinalIgnoreCase);
        AssertSignInSubmitDisabledAfterTheWidget(html);
    }

    [Fact]
    public async Task SignIn_Form_IsLabelledAndAnnounced_WithTheRightFieldAttributes()
    {
        using var response = await GetPageAsync(_configured, SignIn);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains($"<p class=\"lede\">{AdminSignInCopy.SignInLede}</p>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<form id=\"sign-in-form\" method=\"post\" action=\"{SignIn}\" novalidate"
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\" data-sent-back=\"{AdminAuthCopy.SignInFailure}\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<label for=\"email\">{AdminSignInCopy.EmailLabel}</label>"
            + "<input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"username\" inputmode=\"email\""
            + " autocapitalize=\"none\" spellcheck=\"false\" required aria-describedby=\"email-error\""
            + $" data-required=\"{AdminSignInCopy.EmailRequired}\" data-invalid=\"{AdminSignInCopy.EmailInvalid}\">"
            + "<span id=\"email-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<label id=\"password-label\" for=\"password\">{AdminSignInCopy.PasswordLabel}</label>"
            + "<div class=\"field-row\">"
            + "<input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required"
            + $" aria-describedby=\"password-error\" data-required=\"{AdminSignInCopy.PasswordRequired}\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<span id=\"password-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);

        // The show toggle is the first screen's, unchanged: one fixed label, named after its field.
        Assert.Contains(
            "<button type=\"button\" id=\"password-toggle\" class=\"secondary toggle\" aria-pressed=\"false\""
            + " aria-controls=\"password\" aria-labelledby=\"password-toggle password-label\">"
            + $"{AdminSignedOutCopy.ShowPassword}</button>",
            html,
            StringComparison.Ordinal);
        Assert.Equal(AdminSignedOutPage.PasswordToggle("password"), Regex.Match(html, "<button type=\"button\" id=\"password-toggle\".*?</button>").Value);
        Assert.DoesNotContain("Hide", html, StringComparison.Ordinal);

        // One alert region, above the form; two fields, nothing else to send.
        Assert.Single(Regex.Matches(html, "role=\"alert\""));
        Assert.Contains(
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div><form ",
            html,
            StringComparison.Ordinal);
        Assert.Equal(
            new[] { "email", "password" },
            Regex.Matches(html, "<input [^>]*name=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Equal(2, Regex.Matches(html, "<input ").Count);

        // The link to the reset-request page comes after the button.
        var link = $"<div class=\"links\"><a id=\"reset-link\" href=\"{AdminSignedOutExemptions.Reset}\">{AdminSignInCopy.ForgotPassword}</a></div>";
        Assert.Contains(
            $"<button type=\"submit\" id=\"sign-in-submit\" disabled aria-describedby=\"security-check-status\">{AdminSignInCopy.SignInSubmit}</button>"
            + link + "</form></div>",
            html,
            StringComparison.Ordinal);
        Assert.Equal("/admin/reset", AdminSignedOutExemptions.Reset);
        Assert.Single(Regex.Matches(html, "<a "));

        Assert.DoesNotContain("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onpaste", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxlength", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pattern=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("autofocus", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CodePage_Form_HasOneNumericOneTimeCodeField_NoSecurityCheck_AndItsTwoLinks()
    {
        using var response = await GetPageAsync(_configured, Code);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains($"<p class=\"lede\">{AdminSignInCopy.CodeLede}</p>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<form id=\"second-step-form\" method=\"post\" action=\"{Code}\" novalidate data-step=\"code\""
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\" data-no-sign-in=\"Unauthorized\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"<label for=\"code\">{AdminSignInCopy.CodeLabel}</label>"
            + "<input id=\"code\" name=\"code\" type=\"text\" inputmode=\"numeric\""
            + " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required"
            + $" aria-describedby=\"code-error\" data-required=\"{AdminSignInCopy.CodeRequired}\">"
            + "<span id=\"code-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);
        AssertSecondStepTail(
            html,
            AdminSignInCopy.UseRecoveryCode,
            Recovery);
    }

    [Fact]
    public async Task RecoveryPage_Form_HasOneFreeTextOneTimeCodeField_AndItsTwoLinks()
    {
        using var response = await GetPageAsync(_configured, Recovery);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains($"<p class=\"lede\">{AdminSignInCopy.RecoveryLede}</p>", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<form id=\"second-step-form\" method=\"post\" action=\"{Recovery}\" novalidate data-step=\"recovery\""
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\" data-no-sign-in=\"Unauthorized\">",
            html,
            StringComparison.Ordinal);
        // Letters and digits: no numeric keypad on this one.
        Assert.Contains(
            $"<label for=\"recovery-code\">{AdminSignInCopy.RecoveryLabel}</label>"
            + "<input id=\"recovery-code\" name=\"recoveryCode\" type=\"text\""
            + " autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required"
            + $" aria-describedby=\"recovery-code-error\" data-required=\"{AdminSignInCopy.RecoveryRequired}\">"
            + "<span id=\"recovery-code-error\" class=\"field-error\" aria-live=\"polite\"></span>",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("inputmode", html, StringComparison.OrdinalIgnoreCase);
        AssertSecondStepTail(
            html,
            AdminSignInCopy.UseAuthenticatorCode,
            Code);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Copy_HasNoDurationCountOrLockWording_AndNamesNoFactorInTheAlert(string path, string title, string heading)
    {
        using var configured = await GetPageAsync(_configured, path);
        using var unconfigured = await GetPageAsync(_unconfigured, path);
        var pages = new[]
        {
            WithoutRandomValues(await configured.Content.ReadAsStringAsync()),
            WithoutRandomValues(await unconfigured.Content.ReadAsStringAsync())
        };

        foreach (var page in pages)
        {
            Assert.Contains(title, page, StringComparison.Ordinal);
            Assert.Contains(heading, page, StringComparison.Ordinal);
            foreach (var banned in new[] { "locked", "lockout", "minute", "hour", "attempt", "seconds", "try again in", "remaining" })
            {
                Assert.DoesNotContain(banned, page, StringComparison.OrdinalIgnoreCase);
            }

            // The alert starts empty: what it says after a refusal is the server's sentence, not page copy.
            // S-A1 holds that sentence once, as data, for a browser that was sent back to it; nothing shows it by default.
            Assert.Contains("role=\"alert\" tabindex=\"-1\" hidden></div>", page, StringComparison.Ordinal);
            Assert.Equal(path == SignIn ? 1 : 0, Regex.Matches(page, Regex.Escape(AdminAuthCopy.SignInFailure)).Count);
            Assert.DoesNotContain(">" + AdminAuthCopy.SignInFailure, page, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminAuthCopy.VerificationFailed, page, StringComparison.Ordinal);
        }

        // The retry sentence must not be mistaken for the server's refusal.
        Assert.Equal("That didn't go through. Wait a moment and try again.", AdminSignInCopy.SubmitRetry);
        Assert.DoesNotContain("sign you in", AdminSignInCopy.SubmitRetry, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("details", AdminSignInCopy.SubmitRetry, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoElementIdOrControlName_IsAGlobalThePageScriptsRead()
    {
        // An element id (and some element names) is also a property of window. One spelled
        // like a global a script reads would stand in for it: id="turnstile" would sit where
        // Cloudflare installs window.turnstile, and its script could then refuse to install.
        var globals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "turnstile", "AdminArrival", "AdminDom", "AdminPassword", "AdminTurnstile", "AdminRequest",
            "window", "document", "location", "history", "fetch", "URL", "JSON", "Object", "Array",
            "AbortController", "setTimeout", "clearTimeout", "parseInt", "isNaN", "decodeURIComponent",
            // Names every window already has, which an id would be confused with.
            "name", "status", "top", "self", "parent", "origin", "length", "event", "closed", "frames", "opener"
        };

        // Every name the scripts reach through their window parameter is on that list.
        var reached = SignInScripts.Concat(SecondStepScripts).Distinct()
            .Select(path => path[(path.LastIndexOf('/') + 1)..])
            .SelectMany(file => Regex.Matches(File.ReadAllText(AssetPath(file)), @"\bglobal\.([A-Za-z_$][\w$]*)"))
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.Contains("turnstile", reached);
        Assert.Contains("AdminRequest", reached);
        Assert.Contains("AdminArrival", reached);
        Assert.All(reached, name => Assert.Contains(name, globals));

        foreach (var factory in new[] { _configured, _unconfigured })
        {
            foreach (var path in new[] { SignIn, Code, Recovery })
            {
                using var response = await GetPageAsync(factory, path);
                var html = await response.Content.ReadAsStringAsync();
                var names = Regex.Matches(html, "\\s(?:id|name)=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

                Assert.Contains("form-alert", names);
                Assert.All(names, name => Assert.DoesNotContain(name, globals));
                var ids = Regex.Matches(html, "\\sid=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
                Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
                Assert.DoesNotContain("id=\"turnstile\"", html, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("name=\"turnstile\"", html, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("/admin/auth/admin-arrival.js")]
    [InlineData("/admin/auth/admin-request.js")]
    [InlineData("/admin/auth/sign-in.js")]
    [InlineData("/admin/auth/sign-in-code.js")]
    public async Task PageScripts_AreServedSignedOut_WithNothingPrivateAndNoInlineCopy(string path)
    {
        using var response = await GetAsync(_configured, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(AdminSecurityHeadersMiddleware.Nosniff, Header(response, "X-Content-Type-Options"));
        Assert.Empty(SetCookies(response));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DEALOWARE_", body, StringComparison.Ordinal);
        Assert.DoesNotContain(_configured.Secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain("siteverify", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Cloudflare, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerHTML", body, StringComparison.Ordinal);
        Assert.DoesNotContain("outerHTML", body, StringComparison.Ordinal);
        Assert.DoesNotContain("insertAdjacentHTML", body, StringComparison.Ordinal);
        Assert.DoesNotContain("document.write", body, StringComparison.Ordinal);
        Assert.DoesNotContain("eval(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("new Function", body, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sessionStorage", body, StringComparison.Ordinal);
        Assert.DoesNotContain("indexedDB", body, StringComparison.Ordinal);
        Assert.DoesNotContain("document.cookie", body, StringComparison.Ordinal);
        if (path != AdminSignedOutPage.ArrivalScriptPath)
        {
            // Only the arrival script looks at the address, and only for its two fixed flags.
            Assert.DoesNotContain("location.search", body, StringComparison.Ordinal);
            Assert.DoesNotContain("location.hash", body, StringComparison.Ordinal);
            Assert.DoesNotContain("history.", body, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("location.href", body, StringComparison.Ordinal);
        Assert.DoesNotContain("window.open", body, StringComparison.Ordinal);
        Assert.DoesNotContain("__ADMIN_AUTH_TEST__", body, StringComparison.Ordinal);

        // Copy lives in the C# constants classes and reaches the scripts as data attributes.
        foreach (var copy in new[]
                 {
                     AdminAuthCopy.SignInFailure, AdminAuthCopy.VerificationFailed, AdminSignInCopy.SubmitRetry,
                     AdminSignInCopy.EmailRequired, AdminSignInCopy.EmailInvalid, AdminSignInCopy.PasswordRequired,
                     AdminSignInCopy.CodeRequired, AdminSignInCopy.RecoveryRequired, AdminSignInCopy.Noscript,
                     AdminSignedOutCopy.TurnstileLoading, AdminSignedOutCopy.TurnstileNotConfigured
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
    public void RequestScript_PostsJsonWithTheAntiForgeryToken_ToThisSiteOnly_AndFollowsNoRedirect()
    {
        var script = File.ReadAllText(AssetPath("admin-request.js"));

        // One place sends anything, for the POST and for the GET alike.
        Assert.Single(Regex.Matches(script, @"fetch\("));
        Assert.Contains("return fetch(path, options).then(", script, StringComparison.Ordinal);
        Assert.Contains("options.credentials = \"same-origin\";", script, StringComparison.Ordinal);
        Assert.Contains("options.redirect = \"error\";", script, StringComparison.Ordinal);

        // No request mode is set. With mode "same-origin" under the pages' no-referrer policy,
        // Firefox and Safari send "Origin: null" on a POST and the server's origin check refuses it.
        // The pages stay on this site through their own policy (connect-src 'self') and constant paths.
        Assert.DoesNotContain("options.mode", script, StringComparison.Ordinal);
        Assert.DoesNotContain("mode:", script, StringComparison.Ordinal);
        Assert.DoesNotContain("no-cors", script, StringComparison.Ordinal);
        Assert.Equal("no-referrer", AdminSecurityHeadersMiddleware.ReferrerPolicy);
        Assert.Contains("options.cache = \"no-store\";", script, StringComparison.Ordinal);
        Assert.Contains("var TIMEOUT_MS = 15000;", script, StringComparison.Ordinal);

        Assert.Contains("method: \"POST\"", script, StringComparison.Ordinal);
        Assert.Contains("\"X-CSRF-TOKEN\": csrfToken()", script, StringComparison.Ordinal);
        Assert.Contains("meta[name=\"dw-csrf\"]", script, StringComparison.Ordinal);
        // The GET sends nothing of the page's: no token, no body.
        Assert.Contains("return send(path, { method: \"GET\" });", script, StringComparison.Ordinal);
        Assert.Equal("X-CSRF-TOKEN", AdminAntiForgeryService.HeaderName);
        Assert.Equal("dw-csrf", AdminSignedOutPage.CsrfMetaName);
    }

    [Fact]
    public void SignInScript_SendsOnlyTheContractFields_AndMovesOnOnlyForTheTwoNamedSteps()
    {
        var script = File.ReadAllText(AssetPath("sign-in.js"));

        Assert.Contains("var API = \"/admin/api/auth/sign-in\";", script, StringComparison.Ordinal);
        Assert.Contains("var CODE_PAGE = \"/admin/sign-in/code\";", script, StringComparison.Ordinal);
        Assert.Contains("var ENROLL_PAGE = \"/admin/setup/authenticator\";", script, StringComparison.Ordinal);
        Assert.Equal("/admin/api/auth/sign-in", AdminAuthEndpoints.SignInPath);
        Assert.Equal("/admin/sign-in/code", AdminSignedOutExemptions.SignInCode);
        Assert.Equal("/admin/setup/authenticator", AdminSignedOutExemptions.SetupAuthenticator);

        // One request, three fields. No return path: nothing hands this page one.
        var post = Assert.Single(Regex.Matches(script, @"AdminRequest\.post\(API, \{(?<fields>[^}]*)\}\)"));
        Assert.Equal(
            new[] { "email", "password", "turnstileToken" },
            Regex.Matches(post.Groups["fields"].Value, @"(\w+)\s*:").Select(f => f.Groups[1].Value).ToArray());
        Assert.DoesNotContain("fetch(", script, StringComparison.Ordinal);

        // The page leaves only for the two exact replies, never for a bare 200.
        Assert.Contains("if (body.next === \"totp\") location.replace(CODE_PAGE);", script, StringComparison.Ordinal);
        Assert.Contains("else if (body.next === \"enroll\") location.replace(ENROLL_PAGE);", script, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(script, @"location\.").Count);
        Assert.DoesNotContain("reply.status >= 200", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".ok", script, StringComparison.Ordinal);

        // A refusal is shown in the server's words, and the password is cleared with it.
        Assert.Contains("if ((reply.status === 401 || reply.status === 429) && error) refused(error);", script, StringComparison.Ordinal);
        Assert.Contains("password.value = \"\";", script, StringComparison.Ordinal);
        Assert.DoesNotContain("email.value = \"\"", script, StringComparison.Ordinal);
        // The form goes on screen in one place, after the submit handler is in place.
        Assert.Single(Regex.Matches(script, @"AdminDom\.show\(panel, true\)"));
        Assert.True(
            script.IndexOf("form.addEventListener(\"submit\"", StringComparison.Ordinal)
            < script.IndexOf("AdminDom.show(panel, true)", StringComparison.Ordinal));
    }

    [Fact]
    public void SecondStepScript_SendsOneField_AndFollowsOnlyAVerifiedReplyToAPlainAdminPath()
    {
        var script = File.ReadAllText(AssetPath("sign-in-code.js"));

        Assert.Contains("api: \"/admin/api/auth/sign-in/code\",", script, StringComparison.Ordinal);
        Assert.Contains("api: \"/admin/api/auth/sign-in/recovery\",", script, StringComparison.Ordinal);
        Assert.Equal("/admin/api/auth/sign-in/code", AdminAuthEndpoints.SignInCodePath);
        Assert.Equal("/admin/api/auth/sign-in/recovery", AdminAuthEndpoints.SignInRecoveryPath);
        Assert.Contains("body: function (value) { return { code: value }; }", script, StringComparison.Ordinal);
        Assert.Contains("body: function (value) { return { recoveryCode: value }; }", script, StringComparison.Ordinal);
        // Two requests can be sent: the code, and the one that gives the pending sign-in up.
        Assert.Equal(2, Regex.Matches(script, @"AdminRequest\.post\(").Count);
        Assert.Contains("AdminRequest.post(step.api, step.body(value))", script, StringComparison.Ordinal);
        Assert.Contains("AdminRequest.post(SIGN_OUT, {}).then(leave, leave);", script, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch(", script, StringComparison.Ordinal);
        Assert.Equal("code", AdminSignInPages.CodeStep);
        Assert.Equal("recovery", AdminSignInPages.RecoveryStep);

        // Signed in only when the reply says so; the path it names is checked before it is followed.
        Assert.Contains("if (body.verified === true) location.replace(nextPath(body.next));", script, StringComparison.Ordinal);
        Assert.Contains("var HOME = \"/admin/\";", script, StringComparison.Ordinal);
        Assert.Equal("/admin/", AdminReturnPath.Stats);
        Assert.Contains($"var MAX_PATH_LENGTH = {AdminReturnPath.MaxLength};", script, StringComparison.Ordinal);
        Assert.Contains(
            "var PLAIN_ADMIN_PATH = /^\\/admin\\/(?:[A-Za-z0-9-]+(?:\\/[A-Za-z0-9-]+)*)?$/;",
            script,
            StringComparison.Ordinal);
        Assert.Contains("target.origin !== location.origin", script, StringComparison.Ordinal);

        // With no clear answer the page asks whether it is signed in, and believes only the owner's own answer.
        Assert.Contains("var ME = \"/admin/api/me\";", script, StringComparison.Ordinal);
        Assert.Contains($"var SIGNED_IN_AS = \"{AdminSessionMiddleware.CoreOwnerRole}\";", script, StringComparison.Ordinal);
        Assert.Contains(
            "if (reply.status === 200 && body.principal === SIGNED_IN_AS) location.replace(HOME);",
            script,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"AdminRequest\.get\("));

        // "Back to sign in" has the server expire the pending sign-in's cookie, then puts step 1 in
        // this page's place, so the Back button does not lead here again. It goes whatever the answer.
        Assert.Contains("var SIGN_OUT = \"/admin/api/auth/sign-out\";", script, StringComparison.Ordinal);
        Assert.Equal("/admin/api/auth/sign-out", AdminAuthEndpoints.SignOutPath);
        Assert.Contains("var SIGN_IN = \"/admin/sign-in\";", script, StringComparison.Ordinal);
        Assert.Contains("var leave = function () { location.replace(SIGN_IN); };", script, StringComparison.Ordinal);
        Assert.Contains("var back = AdminDom.el(\"back-link\");", script, StringComparison.Ordinal);
        Assert.Contains("back.addEventListener(\"click\", function (event) {", script, StringComparison.Ordinal);

        // Four places leave the page: to the path a verified reply names, to /admin/ when the server
        // says this browser is signed in after all, to step 1 when no pending sign-in is left, and
        // to step 1 by "Back to sign in". Nowhere else.
        Assert.Equal(4, Regex.Matches(script, @"location\.replace\(").Count);
        Assert.DoesNotContain("location.assign", script, StringComparison.Ordinal);
        Assert.DoesNotContain("location =", script, StringComparison.Ordinal);

        // The page never submits by itself.
        Assert.DoesNotContain("requestSubmit", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".submit()", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"send\(value\);"));
    }

    [Theory]
    [InlineData("/admin/")]
    [InlineData("/admin/participants")]
    [InlineData("/admin/artifacts")]
    [InlineData("/admin/negotiations")]
    [InlineData("/admin/offers")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/participants/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59")]
    [InlineData("/admin/audit/0B6C1F0E-3A54-4C58-9F0E-6F1D2C3B4A59")]
    public void BrowserPathRule_AcceptsEveryPathTheServerCanSend(string path)
    {
        // What the server stores as the return path, and sends as "next", always fits the browser's rule.
        Assert.True(AdminReturnPath.TryValidate(path, out var stored));
        Assert.Matches(BrowserPathRule(), stored);
    }

    [Fact]
    public void BrowserPathRule_RefusesEverythingElse()
    {
        var rule = BrowserPathRule();
        foreach (var path in new[]
                 {
                     "https://evil.example/",
                     "//evil.example/admin/",
                     @"/\evil.example",
                     @"/admin/\evil.example",
                     "/admin//evil.example",
                     "javascript:alert(1)",
                     "/admin/../health",
                     "/admin/./participants",
                     "/admin/%2e%2e/health",
                     "/admin/%2Fparticipants",
                     "/admin/participants?next=//evil.example",
                     "/admin/participants#x",
                     "/admin/participants/",
                     "/admin",
                     "/administrator",
                     "/health",
                     "admin/participants",
                     " /admin/",
                     "/admin/ ",
                     "/admin/" + (char)0x0A,
                     "/admin/participants" + (char)0x0A,
                     "/admin/part" + (char)0x00 + "icipants",
                     "/admin/part" + (char)0x200B + "icipants",
                     "/admin/p" + (char)0xE4 + "rticipants",
                     "/admin/participants:8443",
                     string.Empty
                 })
        {
            Assert.False(rule.IsMatch(path), "the browser must not follow " + Uri.EscapeDataString(path));
        }
    }

    /// <summary>The rule sign-in-code.js applies to "next", read out of the script itself.</summary>
    private static Regex BrowserPathRule()
    {
        var script = File.ReadAllText(AssetPath("sign-in-code.js"));
        var source = Regex.Match(script, @"var PLAIN_ADMIN_PATH = /(?<rule>.+)/;").Groups["rule"].Value;
        Assert.NotEqual(string.Empty, source);
        // JavaScript's $ is the very end of the text; in .NET that is \z.
        Assert.EndsWith("$", source, StringComparison.Ordinal);
        return new Regex(source[..^1] + "\\z", RegexOptions.CultureInvariant);
    }

    private static void AssertSignInSubmitDisabledAfterTheWidget(string html)
    {
        var submit = $"<button type=\"submit\" id=\"sign-in-submit\" disabled aria-describedby=\"security-check-status\">{AdminSignInCopy.SignInSubmit}</button>";
        var submitAt = html.IndexOf(submit, StringComparison.Ordinal);
        Assert.True(submitAt > 0, "the submit button is disabled in the markup");
        Assert.Single(Regex.Matches(html, "type=\"submit\""));
        Assert.True(
            html.IndexOf("id=\"security-check-widget\"", StringComparison.Ordinal) < submitAt
            && html.IndexOf("id=\"password\"", StringComparison.Ordinal)
            < html.IndexOf("id=\"security-check-widget\"", StringComparison.Ordinal),
            "the security check sits after the fields and directly above the submit button");
    }

    private static void AssertSecondStepTail(string html, string otherStepText, string otherStepPath)
    {
        // The button is usable as rendered (no security check gates it), then the two links, in this order.
        Assert.Contains(
            $"<button type=\"submit\" id=\"second-step-submit\">{AdminSignInCopy.SecondStepSubmit}</button>"
            + "<div class=\"links\">"
            + $"<a id=\"other-step-link\" href=\"{otherStepPath}\">{otherStepText}</a>"
            + $"<a id=\"back-link\" href=\"{SignIn}\">{AdminSignInCopy.BackToSignIn}</a>"
            + "</div></form></div>",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "type=\"submit\""));
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.Equal(2, Regex.Matches(html, "<a ").Count);
        Assert.Single(Regex.Matches(html, "role=\"alert\""));
        Assert.Contains(
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div><form ",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"password\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onpaste", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxlength", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pattern=", html, StringComparison.OrdinalIgnoreCase);
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

    private static string BasePolicy(string nonce)
        => "default-src 'none'; "
           + $"script-src 'nonce-{nonce}'; "
           + $"style-src 'nonce-{nonce}'; "
           + "connect-src 'self'; "
           + "frame-ancestors 'none'; "
           + "base-uri 'none'; "
           + "form-action 'self'; "
           + "object-src 'none'";

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

    /// <summary>
    /// A pending sign-in as step 1 leaves it: a row holding the hash, and the
    /// raw value for the cookie. Earlier tests in this collection may have left
    /// this address throttled, which would turn the page request into a 429.
    /// </summary>
    private static async Task<string> CreatePendingSignInAsync(
        IsolatedWebApplicationFactory factory,
        bool consumed = false,
        bool clearCounters = true)
    {
        if (clearCounters)
        {
            await ClearSignInCountersAsync(factory);
        }

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

    private static async Task ConsumePendingSignInsAsync(IsolatedWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        foreach (var pending in db.AdminPendingAuths.Where(p => p.ConsumedAt == null).ToList())
        {
            pending.Consume();
        }

        await db.SaveChangesAsync();
    }

    private static async Task ClearSignInCountersAsync(IsolatedWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
        db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
        await db.SaveChangesAsync();
    }

    /// <summary>GET of one of the three pages; the two second-step pages get a fresh pending sign-in.</summary>
    private static async Task<HttpResponseMessage> GetPageAsync(IsolatedWebApplicationFactory factory, string path)
        => await GetAsync(factory, path, path == SignIn ? null : await CreatePendingSignInAsync(factory));

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

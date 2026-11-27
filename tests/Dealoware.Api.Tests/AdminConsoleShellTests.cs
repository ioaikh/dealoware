using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T04 piece 1, the signed-in console shell (<see cref="AdminSignedInPage"/>)
/// and the pages on it: S-B1 Stats, the four lists (their own cases are in
/// AdminListPageTests), the routed page a later piece fills, S-D3;
/// the session gate's answer to a browser without a session; the page kept
/// across sign-in (TD-ADM-177); the signed-in files behind the gate; sign-out
/// (TD-ADM-175). What the pages do in a browser is covered by e2e/console.
/// </summary>
[Collection("WebAppTests")]
public partial class AdminConsoleShellTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminConsoleShellTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Every page this piece routes: path, title, h1, the nav entry it marks current.</summary>
    public static TheoryData<string, string, string, string?> Pages => new()
    {
        { "/admin/", "Stats · Dealoware admin", "Stats", "/admin/" },
        { "/admin/participants", "Participants · Dealoware admin", "Participants", "/admin/participants" },
        { "/admin/artifacts", "Artifacts · Dealoware admin", "Artifacts", "/admin/artifacts" },
        { "/admin/negotiations", "Negotiations · Dealoware admin", "Negotiations", "/admin/negotiations" },
        { "/admin/offers", "Offers · Dealoware admin", "Offers", "/admin/offers" },
        // S-A12 Security settings has forms of its own: AdminSecuritySettingsPageTests holds it to this shell.
        { "/admin/no-such-page", "Page not found · Dealoware admin", "We can't find that page.", null }
    };

    private static readonly string[] NavOrder =
    [
        "/admin/", "/admin/participants", "/admin/artifacts", "/admin/negotiations", "/admin/offers", "/admin/audit",
        "/admin/settings/security"
    ];

    private static readonly string[] NavLabels =
    [
        "Stats", "Participants", "Artifacts", "Negotiations", "Offers", "Audit log", "Security settings"
    ];

    // ---- the shell ----

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task SignedInPage_IsTheSharedShell_WithItsHeadersNavSkipLinkAndOneLiveRegion(
        string path, string title, string heading, string? current)
    {
        var session = await SeedSessionAsync();
        using var response = await GetAsync(path, session);

        Assert.Equal(current is null ? HttpStatusCode.NotFound : HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);
        var nonce = SingleNonce(html);
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce), Header(response, "Content-Security-Policy"));

        Assert.StartsWith("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">", html, StringComparison.Ordinal);
        Assert.Matches("<meta name=\"dw-csrf\" content=\"[0-9A-F]{64}\">", html);
        // The site-wide no-referrer policy holds here too: no page says where it came from.
        Assert.DoesNotContain("name=\"referrer\"", html, StringComparison.Ordinal);
        Assert.Contains($"<title>{title}</title>", text, StringComparison.Ordinal);
        Assert.EndsWith(" · Dealoware admin", title, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<h1[ >]"));
        Assert.Contains($"<h1 id=\"page-heading\">{heading}</h1>", text, StringComparison.Ordinal);
        Assert.Contains(
            $"<link rel=\"stylesheet\" href=\"/admin/assets/console.css\" nonce=\"{nonce}\">",
            html,
            StringComparison.Ordinal);

        // The skip link is the first thing in the body, so the first thing Tab reaches, and leads to main.
        Assert.Contains("</head><body><a class=\"skip-link\" href=\"#main\">Skip to main content</a>", html, StringComparison.Ordinal);
        Assert.Contains("<main id=\"main\" class=\"console-main\" tabindex=\"-1\"><h1 ", html, StringComparison.Ordinal);

        // The nav: the seven pages in order (TD-ADM-UI-na-X03), the current one marked, then Sign out as a POST form.
        var links = NavLinks().Matches(html).Select(m => (Href: m.Groups["href"].Value, Current: m.Groups["current"].Success, Label: m.Groups["label"].Value)).ToArray();
        Assert.Equal(NavOrder, links.Select(l => l.Href).ToArray());
        Assert.Equal(NavLabels, links.Select(l => WebUtility.HtmlDecode(l.Label)).ToArray());
        Assert.Equal(current is null ? [] : new[] { current }, links.Where(l => l.Current).Select(l => l.Href).ToArray());
        // A list page marks its current page of rows too (TD-ADM-UI-na-B06), outside the nav.
        var isList = AdminListPages.All.Any(l => l.Path == path);
        if (!isList)
            Assert.Equal(current is null ? 0 : 1, Regex.Matches(html, "aria-current=").Count);
        Assert.Contains(
            "<form id=\"sign-out-form\" method=\"post\" action=\"/admin/sign-out\""
            + $" data-retry=\"{AdminSignInCopy.SubmitRetry}\">"
            + $"<input type=\"hidden\" name=\"dw_csrf\" value=\"{AdminPageAntiForgery.ReadToken(html)}\">"
            + "<button type=\"submit\" id=\"sign-out\" class=\"secondary\">Sign out</button></form>",
            text,
            StringComparison.Ordinal);
        // The list pages have their filters; every other page only the sign-out field.
        if (!isList)
            Assert.Single(Regex.Matches(html, "<input "));
        Assert.True(
            html.IndexOf("id=\"sign-out-form\"", StringComparison.Ordinal)
            > html.IndexOf("href=\"/admin/settings/security\"", StringComparison.Ordinal));

        // One polite live region in the page, empty until a script fills it. A list keeps the space
        // of the count it says after a move, so nothing jumps; any other page has no empty box.
        var liveClass = isList ? "live live-reserved" : "live";
        Assert.Contains($"<p id=\"console-status\" class=\"{liveClass}\" role=\"status\" aria-live=\"polite\"></p>", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "aria-live="));
        Assert.Single(Regex.Matches(html, "role=\"(status|alert)\""));

        // Static scripts only, each with the nonce, the shell's first.
        var scripts = Regex.Matches(html, "<script src=\"([^\"]+)\" nonce=\"([^\"]+)\"></script>");
        Assert.Equal(
            isList
                ? new[] { "/admin/auth/admin-request.js", "/admin/assets/console.js", "/admin/assets/session-timer.js", "/admin/auth/admin-dom.js", "/admin/assets/console-delete.js", "/admin/assets/console-list.js" }
                : new[] { "/admin/auth/admin-request.js", "/admin/assets/console.js", "/admin/assets/session-timer.js" },
            scripts.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
        Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
        foreach (var banned in new[] { "<style", " style=", " onclick=", " onsubmit=", " onload=", "javascript:", "<iframe" })
        {
            Assert.DoesNotContain(banned, html, StringComparison.OrdinalIgnoreCase);
        }

        Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);
        // Nothing about the owner or the session is in the page.
        Assert.DoesNotContain(OwnerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(session.ToString("D"), html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EveryNavPageIsBuilt_AndS_D3_LinksToStats()
    {
        var session = await SeedSessionAsync();

        // Every page in the nav is built: none is a placeholder any more.
        foreach (var path in NavOrder)
        {
            using var response = await GetAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("not built yet", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        // S-D3 for an unknown page and a path under a list or a section.
        foreach (var path in new[] { "/admin/nothing-here", "/admin/participants/not-an-id", "/admin/settings", "/admin/nothing.html" })
        {
            using var response = await GetAsync(path, session);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var text = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("<h1 id=\"page-heading\">We can't find that page.</h1>", text, StringComparison.Ordinal);
            Assert.Contains("<a id=\"go-to-stats\" href=\"/admin/\">Go to Stats</a>", text, StringComparison.Ordinal);
            Assert.Contains("<title>Page not found · Dealoware admin</title>", text, StringComparison.Ordinal);
            // The page names nothing of what was asked for.
            Assert.DoesNotContain(path, text.Replace("href=\"/admin/settings/security\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        }

        // A GET of the sign-out path keeps its 405 and signs nobody out.
        using (var getSignOut = await GetAsync(AdminSignedOutExemptions.SignOut, session))
        {
            Assert.Equal(HttpStatusCode.MethodNotAllowed, getSignOut.StatusCode);
        }

        Assert.True(await IsSignedInAsync(session), "a GET of /admin/sign-out signs nobody out");

        // The JSON routes and file folders keep their own answers: S-D3 is for pages only.
        foreach (var path in new[] { "/admin/api/nothing-here", "/admin/assets/nothing.js", "/admin/auth/nothing.js" })
        {
            using var response = await GetAsync(path, session);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("page-heading", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        // A JSON route asked with the wrong method still says so.
        using var wrongMethod = await GetAsync(AdminSignedOutExemptions.ApiSignIn, session);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
    }

    [Fact]
    public async Task BareAdmin_IsSentToStats()
    {
        var session = await SeedSessionAsync();
        using var response = await GetAsync("/admin", session);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/", Assert.Single(response.Headers.GetValues("Location")));
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Stats_ShowsTheFiveTotalsOfTheReadApi_AsText_WithTheTimeInEt()
    {
        var session = await SeedSessionAsync();
        using var api = await GetAsync("/admin/api/stats", session);
        Assert.Equal(HttpStatusCode.OK, api.StatusCode);
        var totals = await api.Content.ReadFromJsonAsync<JsonElement>();
        using var page = await GetAsync("/admin/", session);
        var html = await page.Content.ReadAsStringAsync();

        foreach (var (id, label, key) in new[]
                 {
                     ("stat-participants", "Participants", "participants"),
                     ("stat-open-negotiations", "Open negotiations", "openNegotiations"),
                     ("stat-offers", "Offers", "offers"),
                     ("stat-accepts", "Accepts", "accepts"),
                     ("stat-declines", "Declines", "declines")
                 })
        {
            var count = totals.GetProperty(key).GetInt32();
            Assert.Matches(
                $"<div class=\"stat\"><dt id=\"{id}-label\"><a href=\"[^\"]+\">{label}</a></dt><dd id=\"{id}\">{count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}</dd></div>",
                html);
        }

        var clock = _factory.Services.GetRequiredService<IAdminClock>();
        var asOf = AsOf().Match(html);
        Assert.True(asOf.Success, "the As of line");
        Assert.Equal(clock.UtcNow.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"), asOf.Groups["utc"].Value);
        Assert.Equal(EasternTime.Format(clock.UtcNow), asOf.Groups["et"].Value);
        Assert.Matches(@"^[A-Z][a-z]{2} \d{1,2}, \d{4}, \d{1,2}:\d{2} (AM|PM) ET$", asOf.Groups["et"].Value);
    }

    [Fact]
    public async Task Stats_CountsChange_WithTheData_AndLeaveSoftDeletedRowsOut()
    {
        var session = await SeedSessionAsync();
        var before = await StatOnPageAsync(session, "stat-participants");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var kept = Dealoware.Domain.Participants.Participant.Create("Console-Kept");
            var gone = Dealoware.Domain.Participants.Participant.Create("Console-Gone");
            db.Participants.AddRange(kept, gone);
            await db.SaveChangesAsync();
            await db.Participants.Where(p => p.Id == gone.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
        }

        Assert.Equal(before + 1, await StatOnPageAsync(session, "stat-participants"));
    }

    [Theory]
    [InlineData("2026-07-01T12:00:00Z", "Jul 1, 2026, 8:00 AM ET")]
    [InlineData("2026-01-15T17:30:00Z", "Jan 15, 2026, 12:30 PM ET")]
    [InlineData("2026-03-08T06:59:00Z", "Mar 8, 2026, 1:59 AM ET")]
    [InlineData("2026-03-08T07:00:00Z", "Mar 8, 2026, 3:00 AM ET")]
    [InlineData("2026-11-01T05:59:00Z", "Nov 1, 2026, 1:59 AM ET")]
    [InlineData("2026-11-01T06:00:00Z", "Nov 1, 2026, 1:00 AM ET")]
    public void EasternTime_IsNewYorkTime_AcrossDaylightSaving(string utc, string expected)
        => Assert.Equal(expected, EasternTime.Format(DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task SignedInPages_ReuseTheBrowsersAntiForgeryCookie_SoAnotherTabsTokenStaysGood()
    {
        var session = await SeedSessionAsync();
        using var first = await GetAsync("/admin/", session);
        var afCookie = Assert.Single(SetCookies(first), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
        Assert.Contains("path=/admin", afCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", afCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", afCookie, StringComparison.OrdinalIgnoreCase);
        var pair = afCookie.Split(';', 2)[0];
        var token = AdminPageAntiForgery.ReadToken(await first.Content.ReadAsStringAsync());

        using var second = await GetAsync("/admin/offers", session, pair);
        Assert.Empty(SetCookies(second));
        Assert.Equal(token, AdminPageAntiForgery.ReadToken(await second.Content.ReadAsStringAsync()));

        // A cookie not of the shape the server makes is not used: a new pair is issued.
        using var odd = await GetAsync("/admin/offers", session, AdminAntiForgeryService.CookieName + "=abc");
        Assert.Single(SetCookies(odd), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
    }

    // ---- the gate: a browser without a valid session ----

    public static TheoryData<string> SessionStates => new()
    {
        "none", "not-a-guid", "unknown", "idle-expired", "absolute-expired", "second-step-not-done", "another-email", "signed-out"
    };

    [Theory]
    [MemberData(nameof(SessionStates))]
    public async Task PageGet_WithoutAValidSession_GoesToSignIn_WithTheSessionEndedNote_AndNoPage(string state)
    {
        var cookie = await SessionCookieForAsync(state);
        foreach (var path in new[]
                 {
                     "/admin/", "/admin", "/admin/participants", "/admin/artifacts", "/admin/negotiations", "/admin/offers",
                     "/admin/settings/security", "/admin/no-such-page", "/admin/sign-out",
                     "/admin/participants/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59"
                 })
        {
            using var response = await GetAsync(path, cookie: cookie);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("/admin/sign-in#ended", Assert.Single(response.Headers.GetValues("Location")));
            Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.Equal("no-store", Header(response, "Cache-Control"));
            // No session cookie and no anti-forgery pair is set; the only cookie is the kept page.
            Assert.All(SetCookies(response), c => Assert.StartsWith(AdminReturnCookie.Name + "=", c, StringComparison.Ordinal));
            Assert.Single(SetCookies(response));
        }
    }

    [Theory]
    [MemberData(nameof(SessionStates))]
    public async Task EveryJsonPath_EveryOtherMethod_AndEveryFile_KeepThe401_WithoutASession(string state)
    {
        var cookie = await SessionCookieForAsync(state);
        foreach (var path in new[]
                 {
                     "/admin/api/me", "/admin/api/stats", "/admin/api/participants", "/admin/api/nothing-here",
                     "/admin/assets/console.js", "/admin/assets/console.css", "/admin/ASSETS/console.js",
                     "/admin/auth/no-extension"
                 })
        {
            using var response = await GetAsync(path, cookie: cookie);
            AssertGate401(response);
        }

        // HEAD of a page, and a POST to it that passes the origin and anti-forgery checks.
        using var head = Request(HttpMethod.Head, "/admin/", cookie);
        using var headResponse = await Client().SendAsync(head);
        Assert.Equal(HttpStatusCode.Unauthorized, headResponse.StatusCode);
        Assert.False(headResponse.Headers.Contains("Location"));

        var (afPair, token) = await AntiForgeryFromSignInAsync();
        foreach (var path in new[] { "/admin/", "/admin/participants", "/admin/no-such-page" })
        {
            using var post = Request(HttpMethod.Post, path, Join(cookie, afPair));
            post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
            post.Content = JsonContent.Create(new { });
            using var response = await Client().SendAsync(post);
            AssertGate401(response);
        }
    }

    [Fact]
    public async Task APageThatDoesNotCanonicalise_KeepsThe401()
    {
        foreach (var path in new[] { "/admin/participants/", "/admin//participants", "/admin/participants;x=1", "/admin/participants%2fx" })
        {
            using var response = await GetAsync(path);
            AssertGate401(response);
        }
    }

    // ---- the page kept across sign-in (TD-ADM-177) ----

    [Fact]
    public async Task TheRedirect_KeepsTheAskedPage_InAShortLivedCookieForTheSignInRoutesOnly_NeverInTheAddress()
    {
        var probe = "probe" + Guid.NewGuid().ToString("N");
        using var response = await GetAsync($"/admin/offers?status=open&sort=amount&limit=999&{probe}=1&q={probe}");

        Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
        var set = Assert.Single(SetCookies(response));
        var attributes = set.Split(';').Select(a => a.Trim()).ToArray();
        Assert.Equal(
            "dw_admin_return=" + Uri.EscapeDataString($"/admin/offers?sort=amount&q={probe}&status=Open"),
            attributes[0]);
        Assert.Contains("path=/admin/api/auth/sign-in", attributes, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("max-age=600", attributes, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("secure", attributes, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("httponly", attributes, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", attributes, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(attributes, a => a.StartsWith("domain=", StringComparison.OrdinalIgnoreCase));
        // The address the browser goes to says nothing of it.
        Assert.DoesNotContain(probe, string.Join(" ", response.Headers.GetValues("Location")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/admin/settings/security")]
    [InlineData("/admin/no-such-page")]
    [InlineData("/admin/sign-out")]
    [InlineData("/admin/participants/not-an-id")]
    [InlineData("/admin/audit/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a5")]
    public async Task APageNoSignInReturnsTo_RemovesAnyKeptPage(string path)
    {
        using var response = await GetAsync(path);
        Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
        var set = Assert.Single(SetCookies(response));
        Assert.StartsWith("dw_admin_return=;", set, StringComparison.Ordinal);
        Assert.Contains("expires=Thu, 01 Jan 1970", set, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/admin/api/auth/sign-in", set, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignIn_ReturnsToTheKeptPage_WithItsCheckedQuery_AndForgetsIt()
    {
        var (password, secret) = await SeedOwnerAsync();
        var client = CookieClient();

        // Signed out, the owner opens a filtered list: the gate keeps it and sends the browser on.
        using var asked = await client.SendAsync(Request(HttpMethod.Get, "/admin/negotiations?status=closed&bogus=1&offset=-3&sort=status"));
        Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(asked.Headers.GetValues("Location")));

        // Step 1 from the sign-in page: nothing about the page is in the address or the body.
        var token = await SignInPageTokenAsync(client);
        using var step1 = await client.SendAsync(Post(AdminSignedOutExemptions.ApiSignIn, token, new
        {
            email = OwnerEmail,
            password,
            turnstileToken = FakeTurnstileVerifier.ValidToken
        }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        Assert.Equal("/admin/negotiations?sort=status&status=Closed", await StoredReturnPathAsync());

        using var step2 = await client.SendAsync(Post(AdminSignedOutExemptions.ApiSignInCode, token, new
        {
            code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow))
        }));
        Assert.Equal(HttpStatusCode.OK, step2.StatusCode);
        var reply = await step2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(reply.GetProperty("verified").GetBoolean());
        Assert.Equal("/admin/negotiations?sort=status&status=Closed", reply.GetProperty("next").GetString());
        var forgotten = Assert.Single(SetCookies(step2), c => c.StartsWith(AdminReturnCookie.Name + "=", StringComparison.Ordinal));
        Assert.StartsWith("dw_admin_return=;", forgotten, StringComparison.Ordinal);
        Assert.Contains("path=/admin/api/auth/sign-in", forgotten, StringComparison.OrdinalIgnoreCase);

        // And the page is there.
        using var landed = await client.SendAsync(Request(HttpMethod.Get, "/admin/negotiations?sort=status&status=Closed"));
        Assert.Equal(HttpStatusCode.OK, landed.StatusCode);
    }

    [Theory]
    [InlineData("%2F%2Fevil.example")]
    [InlineData("https%3A%2F%2Fevil.example%2Fadmin%2F")]
    [InlineData("%2Fadmin%2Fsign-in")]
    [InlineData("%2Fadmin%2F%2e%2e%2Fhealth")]
    [InlineData("%2Fadmin%2Fparticipants%23x")]
    [InlineData("%2Fadmin%5Cevil")]
    public async Task ATamperedKeptPage_IsCheckedAgainAtStepOne_AndLandsOnStats(string cookieValue)
    {
        var (password, _) = await SeedOwnerAsync();
        var client = CookieClient();
        var token = await SignInPageTokenAsync(client);
        using var step1 = Post(AdminSignedOutExemptions.ApiSignIn, token, new
        {
            email = OwnerEmail,
            password,
            turnstileToken = FakeTurnstileVerifier.ValidToken
        });
        step1.Headers.TryAddWithoutValidation("Cookie", $"{AdminReturnCookie.Name}={cookieValue}");
        using var response = await client.SendAsync(step1);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/admin/", await StoredReturnPathAsync());
    }

    // ---- the signed-in files ----

    [Theory]
    [InlineData("console.js", "text/javascript")]
    [InlineData("console.css", "text/css")]
    public async Task SignedInFiles_AreServedOnlyWithASession(string file, string mediaType)
    {
        var onDisk = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, file));

        using var signedOut = await GetAsync("/admin/assets/" + file);
        AssertGate401(signedOut);
        Assert.DoesNotContain(onDisk[..40], await signedOut.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var signedIn = await GetAsync("/admin/assets/" + file, await SeedSessionAsync());
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        Assert.Equal(mediaType, signedIn.Content.Headers.ContentType?.MediaType);
        Assert.Equal(onDisk, await signedIn.Content.ReadAsStringAsync());
        Assert.Equal("no-store", Header(signedIn, "Cache-Control"));
    }

    [Fact]
    public async Task OnlyTheSignedOutAssets_AreServedBeforeTheGate()
    {
        // The signed-out helpers stay public.
        using var helper = await GetAsync(AdminSignedOutPage.RequestScriptPath);
        Assert.Equal(HttpStatusCode.OK, helper.StatusCode);

        // The signed-in files are not in wwwroot at all, and wwwroot/admin holds the signed-out folder only.
        var webRoot = Path.Combine(RepoRoot(), "src", "Dealoware.Api", "wwwroot", "admin");
        Assert.Equal(new[] { "auth" }, Directory.GetDirectories(webRoot).Select(Path.GetFileName).ToArray());
        Assert.Empty(Directory.GetFiles(webRoot));

        // The pipeline: before the gate, static files only outside /admin or under /admin/auth;
        // the signed-in folder after the gate.
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "Program.cs"));
        var gated = program.IndexOf("app.UseWhen(", StringComparison.Ordinal);
        var gate = program.IndexOf("app.UseMiddleware<AdminSessionMiddleware>();", StringComparison.Ordinal);
        var signedIn = program.IndexOf("RequestPath = AdminSignedInPage.AssetPrefix", StringComparison.Ordinal);
        Assert.True(gated > 0 && gated < gate && gate < signedIn);
        Assert.Contains(
            "context => !context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix)\n"
            + "               || context.Request.Path.StartsWithSegments(AdminSignedOutExemptions.AuthStaticPrefix),",
            program.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(program, @"UseStaticFiles\(\);"));
    }

    // ---- sign-out (TD-ADM-175) ----

    [Fact]
    public async Task SignOut_EndsTheSession_ClearsTheCookies_AuditsOnce_AndIsIdempotent()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using var first = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair));
        first.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        first.Content = JsonContent.Create(new { });
        using var response = await Client().SendAsync(first);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"signedOut\":true}", await response.Content.ReadAsStringAsync());
        var cleared = SetCookies(response);
        foreach (var name in new[] { AdminSessionCookie.Name, AdminPendingAuthCookie.Name, AdminAntiForgeryService.CookieName })
        {
            var cookie = Assert.Single(cleared, c => c.StartsWith(name + "=;", StringComparison.Ordinal));
            Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/admin", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        }

        Assert.False(await SessionRowExistsAsync(session));
        Assert.False(await IsSignedInAsync(session));
        var audit = await SignOutAuditAsync();
        Assert.Equal(before + 1, audit.Count);
        var row = audit.OrderByDescending(a => a.Timestamp).First();
        Assert.Equal("sign_out", row.Action);
        Assert.Equal(AdminAuditActions.SignOut, row.Action);
        Assert.Equal(OwnerEmail, row.ActorEmail);
        Assert.Null(row.ReasonClass);
        Assert.False(string.IsNullOrEmpty(row.IpHmac));

        // Again, with the same old cookies: the same answer, nothing more written.
        using var again = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair));
        again.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        again.Content = JsonContent.Create(new { });
        using var againResponse = await Client().SendAsync(again);
        Assert.Equal(HttpStatusCode.OK, againResponse.StatusCode);
        Assert.Equal("{\"signedOut\":true}", await againResponse.Content.ReadAsStringAsync());
        Assert.Equal(before + 1, await CountSignOutAuditAsync());

        // The old cookie opens no page: the browser goes to sign-in.
        using var page = await GetAsync("/admin/", session);
        Assert.Equal(HttpStatusCode.Found, page.StatusCode);
    }

    [Fact]
    public async Task SignOut_WithoutTheToken_OrFromAnotherSite_OrByGet_SignsNobodyOut()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using (var noToken = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair)))
        {
            using var response = await Client().SendAsync(noToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        using (var crossSite = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair)))
        {
            crossSite.Headers.Remove("Origin");
            crossSite.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
            crossSite.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
            using var response = await _factory.CreateDefaultClient().SendAsync(crossSite);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        using (var get = await GetAsync(AdminSignedOutExemptions.SignOut, session))
        {
            Assert.Equal(HttpStatusCode.MethodNotAllowed, get.StatusCode);
            Assert.Empty(SetCookies(get).Where(c => c.StartsWith(AdminSessionCookie.Name, StringComparison.Ordinal)));
        }

        Assert.True(await IsSignedInAsync(session));
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    [Fact]
    public async Task GivingUpASignInInProgress_WritesNoSignOutAudit()
    {
        var before = await CountSignOutAuditAsync();
        var (afPair, token) = await AntiForgeryFromSignInAsync();
        using var post = Request(HttpMethod.Post, AdminSignedOutExemptions.ApiSignOut, afPair);
        post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        post.Content = JsonContent.Create(new { });
        using var response = await Client().SendAsync(post);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    [Theory]
    [InlineData("idle-expired")]
    [InlineData("absolute-expired")]
    [InlineData("another-email")]
    public async Task SignOut_OfASessionTheGateWouldNotAccept_EndsItButWritesNoAudit(string state)
    {
        var sessionPair = (await SessionCookieForAsync(state))!;
        var session = Guid.Parse(sessionPair.Split('=', 2)[1]);
        var (afPair, token) = await AntiForgeryFromSignInAsync();
        var before = await CountSignOutAuditAsync();

        using var post = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(sessionPair, afPair));
        post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        post.Content = JsonContent.Create(new { });
        using var response = await Client().SendAsync(post);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"signedOut\":true}", await response.Content.ReadAsStringAsync());
        Assert.False(await SessionRowExistsAsync(session));
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    // ---- sign-out without JavaScript: the form's hidden field, on POST /admin/sign-out alone ----

    private static HttpContent SignOutForm(params (string Key, string Value)[] fields)
        => new FormUrlEncodedContent(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    [Fact]
    public async Task SignOutForm_WithItsHiddenField_SignsOut_AndAnswersWithTheSignInPage()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using var post = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair));
        post.Content = SignOutForm((AdminAntiForgeryService.SignOutFormField, token));
        using var response = await Client().SendAsync(post);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/admin/sign-in#signed-out", Assert.Single(response.Headers.GetValues("Location")));
        Assert.Equal(AdminSignInPages.SignedOutLocation, Assert.Single(response.Headers.GetValues("Location")));
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Contains(SetCookies(response), c => c.StartsWith(AdminSessionCookie.Name + "=;", StringComparison.Ordinal));
        Assert.False(await SessionRowExistsAsync(session));
        Assert.False(await IsSignedInAsync(session));
        Assert.Equal(before + 1, await CountSignOutAuditAsync());
    }

    public static TheoryData<string, string> BadSignOutForms => new()
    {
        { "missing", string.Empty },
        { "wrong", new string('A', 64) },
        { "twice", "twice" },
        { "other-name", "other-name" },
        { "multipart", "multipart" },
        { "too-long", "too-long" },
        { "no-cookie", "no-cookie" }
    };

    [Theory]
    [MemberData(nameof(BadSignOutForms))]
    public async Task SignOutForm_WithoutAGoodHiddenField_IsRefused_AndTheSessionStays(string kind, string _)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var cookies = kind == "no-cookie" ? SessionPair(session) : Join(SessionPair(session), afPair);
        using var post = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, cookies);
        post.Content = kind switch
        {
            "missing" => SignOutForm(("unrelated", "1")),
            "wrong" => SignOutForm((AdminAntiForgeryService.SignOutFormField, new string('A', 64))),
            "twice" => SignOutForm((AdminAntiForgeryService.SignOutFormField, token), (AdminAntiForgeryService.SignOutFormField, token)),
            "other-name" => SignOutForm(("__RequestVerificationToken", token)),
            "multipart" => new MultipartFormDataContent { { new StringContent(token), AdminAntiForgeryService.SignOutFormField } },
            "too-long" => SignOutForm((AdminAntiForgeryService.SignOutFormField, token), ("pad", new string('x', AdminAntiForgeryService.SignOutFormMaxBytes))),
            _ => SignOutForm((AdminAntiForgeryService.SignOutFormField, token))
        };
        using var response = await Client().SendAsync(post);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.True(await IsSignedInAsync(session));
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("null")]
    [InlineData(null)]
    public async Task SignOutForm_FromAnotherSite_OrWithoutAnOrigin_IsRefused_EvenWithAGoodField(string? origin)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        using var post = Request(HttpMethod.Post, AdminSignedOutExemptions.SignOut, Join(SessionPair(session), afPair));
        if (origin is not null)
            post.Headers.TryAddWithoutValidation("Origin", origin);
        post.Content = SignOutForm((AdminAntiForgeryService.SignOutFormField, token));
        // A client without the test factory's default Origin header.
        using var client = _factory.Server.CreateClient();
        using var response = await client.SendAsync(post);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsSignedInAsync(session));
    }

    [Theory]
    [InlineData("/admin/api/auth/sign-out")]
    [InlineData("/admin/SIGN-OUT/")]
    [InlineData("/admin/reset")]
    [InlineData("/admin/api/auth/sign-in")]
    [InlineData("/admin/participants")]
    public async Task TheFormField_IsReadByPostSignOutAlone(string path)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();
        using var post = Request(HttpMethod.Post, path, Join(SessionPair(session), afPair));
        post.Content = SignOutForm((AdminAntiForgeryService.SignOutFormField, token), ("email", OwnerEmail));
        using var response = await Client().SendAsync(post);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsSignedInAsync(session));
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    // ---- the sign-out form under no-referrer: "Origin: null", taken by Sec-Fetch-* on that route alone ----

    /// <summary>
    /// A request as a browser sends the sign-out form with JavaScript off under
    /// the no-referrer policy: the given Origin and Sec-Fetch-* headers (null
    /// for absent), the session and anti-forgery cookies, and the body.
    /// Sent with a client that adds no Origin of its own.
    /// </summary>
    private async Task<HttpResponseMessage> PostAsBrowserFormAsync(
        string path,
        string cookies,
        HttpContent content,
        string? origin = "null",
        string? fetchSite = "same-origin",
        string? fetchMode = "navigate",
        string? headerToken = null)
    {
        using var post = Request(HttpMethod.Post, path, cookies);
        if (origin is not null)
            post.Headers.TryAddWithoutValidation("Origin", origin);
        if (fetchSite is not null)
            post.Headers.TryAddWithoutValidation("Sec-Fetch-Site", fetchSite);
        if (fetchMode is not null)
            post.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", fetchMode);
        if (headerToken is not null)
            post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, headerToken);
        post.Content = content;
        using var client = _factory.Server.CreateClient();
        return await client.SendAsync(post);
    }

    [Fact]
    public async Task NullOriginSignOutForm_FromThisSite_BySecFetch_SignsOut()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using var response = await PostAsBrowserFormAsync(
            AdminSignedOutExemptions.SignOut,
            Join(SessionPair(session), afPair)!,
            SignOutForm((AdminAntiForgeryService.SignOutFormField, token)));

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal(AdminSignInPages.SignedOutLocation, Assert.Single(response.Headers.GetValues("Location")));
        Assert.False(await SessionRowExistsAsync(session));
        Assert.Equal(before + 1, await CountSignOutAuditAsync());
    }

    [Theory]
    [InlineData("cross-site", "navigate")]
    [InlineData("same-site", "navigate")]
    [InlineData("none", "navigate")]
    [InlineData(null, "navigate")]
    [InlineData("same-origin", null)]
    [InlineData("same-origin", "cors")]
    [InlineData("same-origin", "no-cors")]
    [InlineData("SAME-ORIGIN", "navigate")]
    [InlineData("same-origin, same-origin", "navigate")]
    public async Task NullOriginSignOutForm_IsRefused_UnlessTheBrowserSaysSameOriginNavigation(string? site, string? mode)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using var response = await PostAsBrowserFormAsync(
            AdminSignedOutExemptions.SignOut,
            Join(SessionPair(session), afPair)!,
            SignOutForm((AdminAntiForgeryService.SignOutFormField, token)),
            fetchSite: site,
            fetchMode: mode);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsSignedInAsync(session));
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    /// <summary>Constraint 1: only an Origin of exactly "null"; any other present Origin must match the console exactly.</summary>
    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("https://admin.core.dealoware.com:443")]
    [InlineData("http://admin.core.dealoware.com")]
    [InlineData("https://ADMIN.core.dealoware.com")]
    [InlineData("NULL")]
    [InlineData("null ")]
    [InlineData("null, null")]
    public async Task OnlyAnOriginOfExactlyNull_TakesTheSecFetchBranch(string origin)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);

        using var response = await PostAsBrowserFormAsync(
            AdminSignedOutExemptions.SignOut,
            Join(SessionPair(session), afPair)!,
            SignOutForm((AdminAntiForgeryService.SignOutFormField, token)),
            origin: origin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsSignedInAsync(session));
    }

    /// <summary>Constraint 2: the branch is scoped by the same canonical path as the form token, POST /admin/sign-out only.</summary>
    [Theory]
    [InlineData("/admin/api/auth/sign-out")]
    [InlineData("/admin/SIGN-OUT/")]
    [InlineData("/admin/sign-out/extra")]
    [InlineData("/admin/sign-out%2f")]
    [InlineData("/admin/reset")]
    [InlineData("/admin/api/auth/sign-in")]
    [InlineData("/admin/participants")]
    public async Task TheSecFetchBranch_IsForPostSignOutAlone(string path)
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var before = await CountSignOutAuditAsync();

        using var response = await PostAsBrowserFormAsync(
            path,
            Join(SessionPair(session), afPair)!,
            SignOutForm((AdminAntiForgeryService.SignOutFormField, token), ("email", OwnerEmail)),
            headerToken: token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsSignedInAsync(session));
        Assert.Equal(before, await CountSignOutAuditAsync());
    }

    /// <summary>Constraint 3: on the null-origin branch the form's field is mandatory; a header token does not do.</summary>
    [Fact]
    public async Task OnTheNullOriginBranch_AValidHeaderToken_WithoutTheFormField_IsRefused()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        var cookies = Join(SessionPair(session), afPair)!;

        // Urlencoded without the field, the header carrying a good token.
        using (var response = await PostAsBrowserFormAsync(
                   AdminSignedOutExemptions.SignOut, cookies, SignOutForm(("unrelated", "1")), headerToken: token))
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // A wrong field next to a good header token.
        using (var response = await PostAsBrowserFormAsync(
                   AdminSignedOutExemptions.SignOut,
                   cookies,
                   SignOutForm((AdminAntiForgeryService.SignOutFormField, new string('A', 64))),
                   headerToken: token))
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // JSON, as the script sends it, but with "Origin: null": not a form, so not the branch.
        using (var response = await PostAsBrowserFormAsync(
                   AdminSignedOutExemptions.SignOut, cookies, JsonContent.Create(new { }), headerToken: token))
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        Assert.True(await IsSignedInAsync(session));
    }

    [Fact]
    public async Task TheScriptsSignOut_IsUnchanged_RealOriginAndHeaderToken_GetJson()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);

        using var response = await PostAsBrowserFormAsync(
            AdminSignedOutExemptions.SignOut,
            Join(SessionPair(session), afPair)!,
            JsonContent.Create(new { }),
            origin: AdminHostOptions.ProductionAdminOrigin,
            fetchMode: "cors",
            headerToken: token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"signedOut\":true}", await response.Content.ReadAsStringAsync());
        Assert.False(await SessionRowExistsAsync(session));
    }

    // ---- the page kept across sign-in: an encoded "?" or "#" is part of the path ----

    [Theory]
    [InlineData("/admin/offers%3Fstatus=Open")]
    [InlineData("/admin/offers%3F/x")]
    [InlineData("/admin/offers%23x")]
    [InlineData("/admin/participants%3fq=a?q=b")]
    public async Task AnEncodedQueryOrFragmentMark_InThePath_IsNotKept(string path)
    {
        using var response = await GetAsync(path);
        Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
        var set = Assert.Single(SetCookies(response));
        Assert.StartsWith("dw_admin_return=;", set, StringComparison.Ordinal);
        Assert.Contains("expires=Thu, 01 Jan 1970", set, StringComparison.OrdinalIgnoreCase);
    }

    // ---- the browser scripts, read as text ----

    [Fact]
    public void ShellScript_SignsOutByPost_FollowsOnlyTheExactReply_AndKeepsToTheHouseRules()
    {
        // Line endings as in the repository, whatever a Windows checkout made of them.
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "console.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("var SIGN_OUT = \"/admin/sign-out\";", script, StringComparison.Ordinal);
        Assert.Contains($"var SIGNED_OUT = \"{AdminSignInPages.SignedOutLocation}\";", script, StringComparison.Ordinal);
        Assert.Contains($"var SESSION_ENDED = \"{AdminSignInPages.SessionEndedLocation}\";", script, StringComparison.Ordinal);
        Assert.Contains("AdminRequest.post(SIGN_OUT, {})", script, StringComparison.Ordinal);
        Assert.Contains(
            "if (reply.status === 200 && body.signedOut === true) location.replace(SIGNED_OUT);",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "if (reply.status === 401 && body.error === GATE_DENIED) {\n        location.replace(SESSION_ENDED);",
            script.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(script, @"location\.replace\(").Count);
        // A refused token while still signed in: the page loads itself anew once, under #fresh.
        Assert.Contains("else afterUnclearReply(reply.status === 403);", script, StringComparison.Ordinal);
        Assert.Contains(
            "} else if (tokenRefused && !reloaded && reply.status === 200 && body.principal === SIGNED_IN_AS) {",
            script,
            StringComparison.Ordinal);
        Assert.Contains("var reloaded = location.hash === FRESH;", script, StringComparison.Ordinal);
        Assert.Contains("if (reloaded) history.replaceState(null, \"\", location.pathname + location.search);", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"location\.reload\(\);\n      \}"));
        Assert.Contains("if (event.persisted) location.reload();", script, StringComparison.Ordinal);
        Assert.Contains("event.preventDefault();", script, StringComparison.Ordinal);

        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "fetch(", "eval(", "new Function", "location.href =",
                     "location.assign", ".submit()"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuditNames_HoldSignOut()
    {
        Assert.Equal("sign_out", AdminAuditActions.SignOut);
        Assert.Equal(AdminAuditActions.SignOut, AdminAuthEvents.SignOut);
    }

    // ---- helpers ----

    private static void AssertGate401(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.Equal("{\"error\":\"Unauthorized\"}", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        Assert.Empty(SetCookies(response));
    }

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private HttpClient CookieClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    private static HttpRequestMessage Request(HttpMethod method, string path, string? cookie = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private static HttpRequestMessage Post(string path, string token, object body)
    {
        var request = Request(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task<HttpResponseMessage> GetAsync(string path, Guid? session = null, string? cookie = null)
    {
        var pairs = Join(session is { } id ? SessionPair(id) : null, cookie);
        using var request = Request(HttpMethod.Get, path, pairs);
        return await Client().SendAsync(request);
    }

    private static string SessionPair(Guid session) => $"{AdminSessionCookie.Name}={session:D}";

    private static string? Join(string? a, string? b)
        => string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : a + "; " + b;

    private async Task<Guid> SeedSessionAsync(
        string email = OwnerEmail,
        bool secondStepDone = true,
        TimeSpan? createdAgo = null,
        TimeSpan? lastActiveAgo = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow;
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip", now: now - (createdAgo ?? TimeSpan.Zero));
        if (lastActiveAgo is { } idle)
            session.UpdateActivity(now - idle);
        if (secondStepDone)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    /// <summary>The Cookie header for a session in the named state; null for none.</summary>
    private async Task<string?> SessionCookieForAsync(string state)
    {
        switch (state)
        {
            case "none":
                return null;
            case "not-a-guid":
                return AdminSessionCookie.Name + "=not-a-session";
            case "unknown":
                return SessionPair(Guid.NewGuid());
            case "idle-expired":
                return SessionPair(await SeedSessionAsync(createdAgo: TimeSpan.FromMinutes(31)));
            case "absolute-expired":
                return SessionPair(await SeedSessionAsync(createdAgo: TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1), lastActiveAgo: TimeSpan.FromMinutes(1)));
            case "second-step-not-done":
                return SessionPair(await SeedSessionAsync(secondStepDone: false));
            case "another-email":
                return SessionPair(await SeedSessionAsync(email: "someone.else@example.test"));
            case "signed-out":
                var id = await SeedSessionAsync();
                using (var scope = _factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
                    db.AdminSessions.Remove(db.AdminSessions.Single(s => s.Id == id));
                    await db.SaveChangesAsync();
                }

                return SessionPair(id);
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }
    }

    private async Task<bool> IsSignedInAsync(Guid session)
    {
        using var response = await GetAsync("/admin/api/me", session);
        return response.StatusCode == HttpStatusCode.OK;
    }

    private async Task<bool> SessionRowExistsAsync(Guid session)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessions.AnyAsync(s => s.Id == session);
    }

    private async Task<List<AdminAuditEntry>> SignOutAuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuditLog.AsNoTracking().Where(a => a.Action == AdminAuditActions.SignOut).ToListAsync();
    }

    private async Task<int> CountSignOutAuditAsync() => (await SignOutAuditAsync()).Count;

    private async Task<int> StatOnPageAsync(Guid session, string id)
    {
        using var page = await GetAsync("/admin/", session);
        var html = await page.Content.ReadAsStringAsync();
        var match = Regex.Match(html, $"<dd id=\"{id}\">([0-9,]+)</dd>");
        Assert.True(match.Success);
        return int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The anti-forgery pair a signed-in page hands out: the cookie pair and the meta token.</summary>
    private async Task<(string Pair, string Token)> AntiForgeryFromPageAsync(Guid session)
    {
        using var page = await GetAsync("/admin/", session);
        var cookie = Assert.Single(SetCookies(page), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
        return (cookie.Split(';', 2)[0], AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync()));
    }

    private async Task<(string Pair, string Token)> AntiForgeryFromSignInAsync()
    {
        using var page = await GetAsync(AdminSignedOutExemptions.SignIn);
        var cookie = Assert.Single(SetCookies(page), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
        return (cookie.Split(';', 2)[0], AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync()));
    }

    private static async Task<string> SignInPageTokenAsync(HttpClient client)
    {
        using var page = await client.SendAsync(Request(HttpMethod.Get, AdminSignedOutExemptions.SignIn));
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        return AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync());
    }

    /// <summary>The owner with a password and an enrolled authenticator, and nothing left from earlier sign-ins.</summary>
    private async Task<(string Password, byte[] Key)> SeedOwnerAsync()
    {
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();

        var existing = db.AdminCoreOwnerAccounts.SingleOrDefault(a => a.Email == OwnerEmail);
        if (existing is not null)
        {
            db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes.Where(c => c.AccountId == existing.Id));
            db.AdminCoreOwnerAccounts.Remove(existing);
        }

        db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths.Where(p => p.Email == OwnerEmail));
        db.AdminAuthFailureEvents.RemoveRange(db.AdminAuthFailureEvents);
        db.AdminAuthLockouts.RemoveRange(db.AdminAuthLockouts);
        await db.SaveChangesAsync();

        var key = Rfc6238Totp.GenerateSecret();
        var account = AdminCoreOwnerAccount.Create(OwnerEmail);
        account.SetPasswordHash(AdminPasswordHasher.Hash(password));
        account.MarkRecoveryCodesIssued();
        account.CompleteEnrollment(protector.Encrypt(key));
        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
        return (password, key);
    }

    private async Task<string> StoredReturnPathAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return (await db.AdminPendingAuths.AsNoTracking()
                .Where(p => p.Email == OwnerEmail)
                .OrderByDescending(p => p.CreatedAt)
                .FirstAsync())
            .ReturnPath;
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return string.Join(",", values);
        return response.Content.Headers.TryGetValues(name, out var contentValues) ? string.Join(",", contentValues) : string.Empty;
    }

    private static IReadOnlyList<string> SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    private static string SingleNonce(string html)
    {
        var nonces = Regex.Matches(html, "nonce=\"([^\"]+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();
        return Assert.Single(nonces);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [GeneratedRegex("<li><a href=\"(?<href>[^\"]+)\"(?<current> aria-current=\"page\")?>(?<label>[^<]+)</a></li>")]
    private static partial Regex NavLinks();

    [GeneratedRegex("<p id=\"stats-as-of\" class=\"as-of\">As of <time class=\"ts\" datetime=\"(?<utc>[^\"]+)\" tabindex=\"0\" title=\"[^\"]+ UTC\" data-utc=\"[^\"]+ UTC\">(?<et>[^<]+)</time></p>")]
    private static partial Regex AsOf();
}

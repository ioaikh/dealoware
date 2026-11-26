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
/// T04 S-A12 Security settings page at /admin/settings/security
/// (<see cref="AdminSecuritySettingsPage"/>) and the UXR-A40 recovery-code
/// banner (<see cref="AdminRecoveryNotice"/>, TD-ADM-UI-auth-16): what the
/// server sends. What the page does in a browser is e2e/console's
/// security-settings.spec.ts; the actions it drives are
/// <see cref="AdminSecuritySettingsTests"/>.
/// </summary>
[Collection("WebAppTests")]
public partial class AdminSecuritySettingsPageTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = "io@aiknowhow.com";
    private const string PagePath = "/admin/settings/security";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminSecuritySettingsPageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- The page ----

    [Fact]
    public async Task Sa12Page_IsTheSignedInShell_NoStoreNonceCsp_SecuritySettingsCurrent_StaticScriptsOnly()
    {
        var session = await SeedSessionAsync();
        using var response = await GetAsync(PagePath, session);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);
        var nonce = SingleNonce(html);
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce), Header(response, "Content-Security-Policy"));
        Assert.DoesNotContain("challenges.cloudflare.com", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);

        Assert.StartsWith("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">", html, StringComparison.Ordinal);
        Assert.Matches("<meta name=\"dw-csrf\" content=\"[0-9A-F]{64}\">", html);
        Assert.Contains("<title>Security settings · Dealoware admin</title>", text, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<h1[ >]"));
        Assert.Contains("<h1 id=\"page-heading\">Security settings</h1>", html, StringComparison.Ordinal);
        Assert.Contains("</head><body><a class=\"skip-link\" href=\"#main\">Skip to main content</a>", html, StringComparison.Ordinal);

        // The nav marks Security settings, and nothing else.
        var current = Regex.Matches(html, "<li><a href=\"([^\"]+)\" aria-current=\"page\">([^<]+)</a></li>");
        var marked = Assert.Single(current);
        Assert.Equal(PagePath, marked.Groups[1].Value);
        Assert.Equal("Security settings", marked.Groups[2].Value);
        Assert.Single(Regex.Matches(html, "aria-current="));

        // The shell's live region, first in main after the h1; the forms' own regions are inside their sections.
        Assert.Contains(
            "<h1 id=\"page-heading\">Security settings</h1><p id=\"console-status\" class=\"live\" role=\"status\" aria-live=\"polite\"></p>",
            html,
            StringComparison.Ordinal);

        // Both stylesheets and every script carry the nonce; the helpers are the signed-out pages' own files.
        Assert.Equal(
            new[] { "/admin/assets/console.css", "/admin/assets/security-settings.css" },
            Regex.Matches(html, $"<link rel=\"stylesheet\" href=\"([^\"]+)\" nonce=\"{Regex.Escape(nonce)}\">").Select(m => m.Groups[1].Value).ToArray());
        Assert.Equal(2, Regex.Matches(html, "<link ").Count);
        var scripts = Regex.Matches(html, "<script src=\"([^\"]+)\" nonce=\"([^\"]+)\"></script>");
        Assert.Equal(
            new[]
            {
                "/admin/auth/admin-request.js", "/admin/assets/console.js",
                "/admin/auth/admin-dom.js", "/admin/auth/admin-password.js", "/admin/auth/admin-clipboard.js",
                "/admin/assets/security-settings.js"
            },
            scripts.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(scripts, m => Assert.Equal(nonce, m.Groups[2].Value));
        Assert.Equal(scripts.Count, Regex.Matches(html, "<script").Count);
        foreach (var banned in new[] { "<style", " style=", " onclick=", " onsubmit=", " onload=", "javascript:", "<iframe", "<svg" })
        {
            Assert.DoesNotContain(banned, html, StringComparison.OrdinalIgnoreCase);
        }

        // With JavaScript off: one sentence; the settings stay hidden until the script runs.
        Assert.Contains($"<noscript><p>{AdminSecuritySettingsPageCopy.Noscript}</p></noscript>", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"security-settings\" class=\"settings\" hidden", html, StringComparison.Ordinal);
        // Every form posts to this page (a GET route: 405), never to an address that could carry what was typed.
        var forms = Regex.Matches(html, "<form id=\"([^\"]+)\" method=\"post\" action=\"([^\"]+)\" novalidate");
        Assert.Equal(new[] { "password-form", "new-key-form", "step-up-form" }, forms.Select(m => m.Groups[1].Value).ToArray());
        Assert.All(forms, m => Assert.Equal(PagePath, m.Groups[2].Value));
        Assert.Equal(forms.Count + 1, Regex.Matches(html, "<form ").Count);
        Assert.EndsWith("</body></html>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sa12Page_HoldsNoCodeKeyCountOrOwnerData_EveryPanelIsEmpty()
    {
        var (password, key, codes) = await SeedOwnerAsync();
        var session = await SeedSessionAsync();
        using var response = await GetAsync(PagePath, session);
        var html = await response.Content.ReadAsStringAsync();

        foreach (var secret in codes.Append(password).Append(Rfc6238Totp.ToBase32(key)).Append(OwnerEmail).Append(session.ToString("D")))
        {
            Assert.DoesNotContain(secret, html, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("otpauth", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dealoware:", html, StringComparison.Ordinal);
        // The count, the lists, the key, the account name, the QR code and the activity table are filled by the script.
        Assert.Contains("data-many=\"{0} codes left\"></p>", html, StringComparison.Ordinal);
        Assert.Contains("<ul id=\"codes-new-list\" class=\"code-list\" role=\"list\" aria-labelledby=\"codes-new-lede\" translate=\"no\"></ul>", html, StringComparison.Ordinal);
        Assert.Contains("<ul id=\"reenrol-codes-list\" class=\"code-list\" role=\"list\" aria-labelledby=\"reenrol-codes-lede\" translate=\"no\"></ul>", html, StringComparison.Ordinal);
        Assert.Contains("<span id=\"manual-key\" class=\"key\" translate=\"no\"></span>", html, StringComparison.Ordinal);
        Assert.Contains("<dd id=\"account-name\" translate=\"no\"></dd>", html, StringComparison.Ordinal);
        Assert.Matches("<div id=\"new-key-qr\" class=\"qr-slot\" data-label=\"[^\"]+\"></div>", html);
        Assert.Contains("<tbody id=\"activity-rows\"></tbody>", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"new-key\" class=\"new-key\" hidden>", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"codes-new\" class=\"new-codes\" hidden>", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"reenrol-codes\" class=\"new-codes\" hidden>", html, StringComparison.Ordinal);
        // Every value carried for the script is a fixed sentence; no field has a value.
        Assert.DoesNotContain(" value=\"", html.Replace($"name=\"{AdminAntiForgeryService.SignOutFormField}\" value=\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sa12Page_PasswordForm_NoteV14_FieldOrder_Attributes_AndTheSetupPagesFields()
    {
        var session = await SeedSessionAsync();
        using var response = await GetAsync(PagePath, session);
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);
        var form = Regex.Match(html, "<form id=\"password-form\".*?</form>").Value;
        Assert.NotEmpty(form);

        Assert.Contains("<h2 id=\"password-heading\">Password</h2>", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"submit\" id=\"password-submit\">Change password</button>", form, StringComparison.Ordinal);
        Assert.Contains(" aria-describedby=\"password-alert\"", form, StringComparison.Ordinal);
        Assert.Contains("<p id=\"password-alert\" class=\"form-alert\" role=\"status\" aria-live=\"polite\"></p>", form, StringComparison.Ordinal);
        Assert.Contains(
            $"data-changed=\"{AdminSignedOutPage.Encode(AdminSecuritySettingsCopy.PasswordChanged)}\"",
            form,
            StringComparison.Ordinal);

        // (1) current, (2) new, (3) confirm, (4) code, then its switch.
        var order = new[] { "id=\"current-password\"", "id=\"new-password\"", "id=\"confirm-password\"", "id=\"password-code\"", "id=\"password-recovery-code\"", "id=\"password-factor-switch\"", "id=\"password-submit\"" }
            .Select(id => form.IndexOf(id, StringComparison.Ordinal)).ToArray();
        Assert.DoesNotContain(-1, order);
        Assert.Equal(order.Order().ToArray(), order);

        Assert.Contains("<label id=\"current-password-label\" for=\"current-password\">Current password</label>", form, StringComparison.Ordinal);
        Assert.Contains("<input id=\"current-password\" type=\"password\" autocomplete=\"current-password\" required", form, StringComparison.Ordinal);
        Assert.Contains("<label id=\"confirm-password-label\" for=\"confirm-password\">Confirm new password</label>", form, StringComparison.Ordinal);
        Assert.Contains($"data-mismatch=\"{AdminSignedOutPage.Encode("These passwords don't match.")}\"", form, StringComparison.Ordinal);
        Assert.Matches("<input id=\"password-code\" type=\"text\" inputmode=\"numeric\" autocomplete=\"one-time-code\"", form);
        Assert.Matches("<input id=\"password-recovery-code\" type=\"text\" autocomplete=\"one-time-code\"", form);
        Assert.Contains("<div class=\"field\" id=\"password-recovery-code-field\" hidden>", form, StringComparison.Ordinal);
        Assert.Contains(">Use a recovery code instead</button>", form, StringComparison.Ordinal);
        // Confirm new password and the current password are never named: only what the script sends leaves.
        Assert.DoesNotContain("name=\"currentPassword\"", form, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(form, " name=\""));

        // A show toggle on every password field (note v1.4 (c)), the shared one.
        foreach (var id in new[] { "current-password", "new-password", "confirm-password", "step-up-password" })
        {
            Assert.Contains(AdminSignedOutPage.PasswordToggle(id), html, StringComparison.Ordinal);
        }

        // New password is S-A10's field (S-A4's), byte for byte: same helper, length data and messages.
        var resetPage = await (await GetAsync(AdminPasswordResetEndpoints.ConfirmPagePath)).Content.ReadAsStringAsync();
        var newField = Regex.Match(resetPage, "<div class=\"field\"><label id=\"new-password-label\".*?<span id=\"new-password-error\" class=\"field-error\" aria-live=\"polite\"></span></div>").Value;
        Assert.NotEmpty(newField);
        Assert.Contains(newField, form, StringComparison.Ordinal);
        Assert.Contains(AdminSignedOutCopy.PasswordHelper, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sa12Page_Sections_StepUpDialog_AndActivityPanel_AreThere()
    {
        var session = await SeedSessionAsync();
        using var response = await GetAsync(PagePath, session);
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);

        Assert.Equal(
            new[] { "Password", "Recovery codes", "Authenticator", "Recent sign-in activity", "Confirm it's you" },
            Regex.Matches(text, "<h2 id=\"[^\"]+\">([^<]+)</h2>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("<button type=\"button\" id=\"codes-create\">Create new codes</button>", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"button\" id=\"authenticator-start\">Set up again</button>", html, StringComparison.Ordinal);
        Assert.Contains("data-one=\"1 code left\" data-many=\"{0} codes left\"", html, StringComparison.Ordinal);
        Assert.Contains("<label for=\"codes-new-saved\">I have saved these recovery codes.</label>", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"button\" id=\"codes-new-done\" disabled>Continue</button>", html, StringComparison.Ordinal);

        Assert.Contains(
            "<dialog id=\"step-up-dialog\" class=\"step-up\" aria-labelledby=\"step-up-heading\" aria-describedby=\"step-up-lede\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains("<input id=\"step-up-password\" type=\"password\" autocomplete=\"current-password\" required", html, StringComparison.Ordinal);
        Assert.Contains("id=\"step-up-factor-switch\"", html, StringComparison.Ordinal);

        Assert.Contains("<p id=\"activity-empty\" hidden>No sign-in activity yet.</p>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"col\">Time</th><th scope=\"col\">Event</th><th scope=\"col\">Network (hashed)</th>", html, StringComparison.Ordinal);
        Assert.Contains("<caption id=\"activity-caption\">", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"lock-icon\" aria-hidden=\"true\">!</span><strong>Lock notice:</strong>", html, StringComparison.Ordinal);
        // A label for every event the panel can list, and nothing else.
        Assert.Equal(
            AdminSecuritySettingsService.ActivityActions.Order(StringComparer.Ordinal).ToArray(),
            Regex.Matches(html, "<li data-event=\"([^\"]+)\">").Select(m => m.Groups[1].Value).Order(StringComparer.Ordinal).ToArray());
        Assert.Contains("<li data-event=\"signin.second_factor_failed\">Code failed after correct password</li>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sa12Files_AreServedOnlyWithASession()
    {
        var session = await SeedSessionAsync();
        foreach (var file in new[] { AdminSecuritySettingsPage.ScriptPath, AdminSecuritySettingsPage.StylesheetPath, AdminRecoveryNotice.ScriptPath })
        {
            using (var signedOut = await GetAsync(file))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
                Assert.Equal("{\"error\":\"Unauthorized\"}", await signedOut.Content.ReadAsStringAsync());
            }

            using var signedIn = await GetAsync(file, session);
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
            Assert.StartsWith(
                file.EndsWith(".css", StringComparison.Ordinal) ? "/* Signed-in admin console asset." : "/**",
                await signedIn.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Sa12Scripts_PutTextInByTextContentOnly_StoreNothing_AndUseTheSharedRequestHelper()
    {
        foreach (var name in new[] { "security-settings.js", "recovery-notice.js" })
        {
            var script = ReadAsset(name);
            foreach (var banned in new[]
                     {
                         "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "DOMParser", "createContextualFragment",
                         "localStorage", "sessionStorage", "indexedDB", "document.cookie", "console.log", "console.error", "console.warn",
                         "console.info", "console.debug", "mode:", "fetch(",
                         "eval(", "new Function", "location.href =", "location.assign", ".submit()", "?code", "&code"
                     })
            {
                Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
            }

            Assert.Contains("AdminRequest.post(", script, StringComparison.Ordinal);
        }

        var page = ReadAsset("security-settings.js");
        // Leaves only for the sign-in page's fixed notes, and on exact replies.
        Assert.Contains("var SIGN_IN_CHANGED = \"/admin/sign-in#changed\";", page, StringComparison.Ordinal);
        Assert.Contains("var SIGN_IN_ENDED = \"/admin/sign-in#ended\";", page, StringComparison.Ordinal);
        Assert.Contains("var SIGN_IN_REFUSED = \"/admin/sign-in#again\";", page, StringComparison.Ordinal);
        Assert.Contains(
            "if (data.signedOut === true && text(data.message) && text(data.message) === passwordForm.getAttribute(\"data-changed\")) {",
            page,
            StringComparison.Ordinal);
        // Everything secret leaves the page with it.
        Assert.Contains("window.addEventListener(\"pagehide\", clearSecrets);", page, StringComparison.Ordinal);
        // The QR code is built from checked parts, never parsed as markup.
        Assert.Contains("document.createElementNS(SVG, \"svg\")", page, StringComparison.Ordinal);
        Assert.Contains("/<path fill=\"#000\" d=\"([Mhvz0-9 -]+)\"\\/>/", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Sa12Copy_FromTheDocs_WhereTheyGiveIt()
    {
        Assert.Equal("Change password", AdminSecuritySettingsPageCopy.ChangePassword);
        Assert.Equal("Current password", AdminSecuritySettingsPageCopy.CurrentPasswordLabel);
        Assert.Equal("Confirm new password", AdminSecuritySettingsPageCopy.ConfirmNewPasswordLabel);
        Assert.Equal("Create new codes", AdminSecuritySettingsPageCopy.CreateNewCodes);
        Assert.Equal("Set up again", AdminSecuritySettingsPageCopy.SetUpAgain);
        Assert.Equal("Recent sign-in activity", AdminSecuritySettingsPageCopy.ActivityHeading);
        Assert.Equal("No sign-in activity yet.", AdminSecuritySettingsPageCopy.ActivityEmpty);
        Assert.Equal("Manage recovery codes", AdminSecuritySettingsPageCopy.ManageRecoveryCodes);
        Assert.Equal("These passwords don't match.", AdminSignedOutCopy.PasswordMismatch);
        Assert.Equal("You signed in with a recovery code. You have 1 recovery code left.", AdminSecuritySettingsPageCopy.RecoveryNotice(1));
        Assert.Equal("You signed in with a recovery code. You have 9 recovery codes left.", AdminSecuritySettingsPageCopy.RecoveryNotice(9));
    }

    // ---- The recovery-code banner (UXR-A40, TD-ADM-UI-auth-16) ----

    [Fact]
    public async Task RecoveryCodeSignIn_ShowsTheBanner_WithTheServerCount_OnEveryPage_UntilDismissed()
    {
        var (password, _, codes) = await SeedOwnerAsync();
        var session = await SignInAsync(password, recoveryCode: codes[3]);

        Assert.True(await NoticeExistsAsync(session));
        var left = await UnusedCodesAsync();
        Assert.Equal(codes.Count - 1, left);

        foreach (var path in new[] { "/admin/", "/admin/offers", PagePath })
        {
            using var page = await GetAsync(path, session);
            var html = await page.Content.ReadAsStringAsync();
            var text = WebUtility.HtmlDecode(html);
            Assert.Contains(
                "<div id=\"recovery-notice\" class=\"recovery-notice\"" +
                $" data-retry=\"{AdminSignedOutPage.Encode(AdminSignInCopy.SubmitRetry)}\">" +
                $"<p id=\"recovery-notice-text\">{AdminSignedOutPage.Encode(AdminSecuritySettingsPageCopy.RecoveryNotice(left))}</p>" +
                "<p class=\"recovery-notice-actions\">" +
                "<a id=\"recovery-notice-manage\" href=\"/admin/settings/security\">Manage recovery codes</a>" +
                "<button type=\"button\" id=\"recovery-notice-dismiss\" class=\"secondary\" hidden aria-describedby=\"recovery-notice-text\">Dismiss</button>" +
                "</p></div>",
                html,
                StringComparison.Ordinal);
            Assert.Contains($"You have {left} recovery codes left.", text, StringComparison.Ordinal);
            // Right after the shell's live region, before the page's own content.
            Assert.Contains("aria-live=\"polite\"></p><div id=\"recovery-notice\"", html, StringComparison.Ordinal);
            Assert.Contains($"<script src=\"{AdminRecoveryNotice.ScriptPath}\" nonce=\"", html, StringComparison.Ordinal);
            Assert.Single(Regex.Matches(html, "id=\"recovery-notice\""));
            // The count only: never a code.
            foreach (var code in codes)
                Assert.DoesNotContain(code, html, StringComparison.OrdinalIgnoreCase);
        }

        // The count is the store's, read for each page: one more code used, one fewer shown.
        await UseCodeAsync(codes[7]);
        using (var page = await GetAsync("/admin/", session))
        {
            Assert.Contains(
                AdminSecuritySettingsPageCopy.RecoveryNotice(left - 1),
                WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync()),
                StringComparison.Ordinal);
        }

        // Dismiss: one POST behind the gate, the Origin check and the anti-forgery token.
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        using (var noToken = Request(HttpMethod.Post, AdminRecoveryNotice.DismissPath, Join(SessionPair(session), afPair)))
        {
            noToken.Content = JsonContent.Create(new { });
            using var refused = await Client().SendAsync(noToken);
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        }

        Assert.True(await NoticeExistsAsync(session));

        using (var dismiss = Request(HttpMethod.Post, AdminRecoveryNotice.DismissPath, Join(SessionPair(session), afPair)))
        {
            dismiss.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
            dismiss.Content = JsonContent.Create(new { });
            using var done = await Client().SendAsync(dismiss);
            Assert.Equal(HttpStatusCode.OK, done.StatusCode);
            Assert.Equal("no-store", Header(done, "Cache-Control"));
            Assert.Equal("{\"dismissed\":true}", await done.Content.ReadAsStringAsync());
        }

        Assert.False(await NoticeExistsAsync(session));
        foreach (var path in new[] { "/admin/", PagePath })
        {
            using var page = await GetAsync(path, session);
            var html = await page.Content.ReadAsStringAsync();
            Assert.DoesNotContain("recovery-notice", html, StringComparison.Ordinal);
        }

        // The session goes on; its banner does not come back.
        Assert.True(await IsSignedInAsync(session));
    }

    [Fact]
    public async Task AuthenticatorCodeSignIn_ShowsNoBanner()
    {
        var (password, key, _) = await SeedOwnerAsync();
        var session = await SignInAsync(password, totpKey: key);

        Assert.False(await NoticeExistsAsync(session));
        using var page = await GetAsync("/admin/", session);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.DoesNotContain("recovery-notice", html, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminRecoveryNotice.ScriptPath, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBanner_BelongsToItsSession_AndGoesWithIt()
    {
        var (password, key, codes) = await SeedOwnerAsync();
        var withCode = await SignInAsync(password, recoveryCode: codes[0]);
        Assert.True(await NoticeExistsAsync(withCode));

        // A later sign-in with an authenticator code in another browser has no banner, and the first keeps its own.
        var withTotp = await SignInAsync(password, totpKey: key);
        using (var page = await GetAsync("/admin/", withTotp))
        {
            Assert.DoesNotContain("recovery-notice", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        using (var page = await GetAsync("/admin/", withCode))
        {
            Assert.Contains("id=\"recovery-notice\"", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        // Dismissing in one session touches no other.
        var (afPair, token) = await AntiForgeryFromPageAsync(withTotp);
        using (var dismiss = Request(HttpMethod.Post, AdminRecoveryNotice.DismissPath, Join(SessionPair(withTotp), afPair)))
        {
            dismiss.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
            dismiss.Content = JsonContent.Create(new { });
            using var done = await Client().SendAsync(dismiss);
            Assert.Equal("{\"dismissed\":true}", await done.Content.ReadAsStringAsync());
        }

        Assert.True(await NoticeExistsAsync(withCode));

        // The row goes with its session (sign-out, expiry, a password change).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            await db.AdminSessions.Where(s => s.Id == withCode).ExecuteDeleteAsync();
        }

        Assert.False(await NoticeExistsAsync(withCode));
    }

    [Fact]
    public async Task Dismiss_WithoutASession_IsTheGates401()
    {
        var session = await SeedSessionAsync();
        var (afPair, token) = await AntiForgeryFromPageAsync(session);
        using var request = Request(HttpMethod.Post, AdminRecoveryNotice.DismissPath, afPair);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        request.Content = JsonContent.Create(new { });
        using var response = await Client().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("{\"error\":\"Unauthorized\"}", await response.Content.ReadAsStringAsync());
    }

    // ---- helpers ----

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private static HttpRequestMessage Request(HttpMethod method, string path, string? cookie = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private async Task<HttpResponseMessage> GetAsync(string path, Guid? session = null)
    {
        using var request = Request(HttpMethod.Get, path, session is { } id ? SessionPair(id) : null);
        return await Client().SendAsync(request);
    }

    private static string SessionPair(Guid session) => $"{AdminSessionCookie.Name}={session:D}";

    private static string? Join(string? a, string? b)
        => string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : a + "; " + b;

    private async Task<Guid> SeedSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow;
        var session = AdminSession.Create(OwnerEmail, ipHmac: "testhmac-not-an-ip", now: now);
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<bool> IsSignedInAsync(Guid session)
    {
        using var response = await GetAsync("/admin/api/me", session);
        return response.StatusCode == HttpStatusCode.OK;
    }

    private async Task<(string Pair, string Token)> AntiForgeryFromPageAsync(Guid session)
    {
        using var page = await GetAsync("/admin/", session);
        var cookie = Assert.Single(SetCookies(page), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
        return (cookie.Split(';', 2)[0], AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync()));
    }

    /// <summary>The owner with a password, an enrolled authenticator and ten recovery codes; nothing left from earlier sign-ins.</summary>
    private async Task<(string Password, byte[] Key, IReadOnlyList<string> Codes)> SeedOwnerAsync()
    {
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();

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
        var codes = AdminRecoveryCodes.Generate();
        var account = AdminCoreOwnerAccount.Create(OwnerEmail);
        account.SetPasswordHash(AdminPasswordHasher.Hash(password));
        account.MarkRecoveryCodesIssued();
        account.CompleteEnrollment(protector.Encrypt(key));
        db.AdminCoreOwnerAccounts.Add(account);
        foreach (var code in codes)
            db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(code)));
        await db.SaveChangesAsync();
        return (password, key, codes);
    }

    /// <summary>Both sign-in steps through the API, as the pages send them. Returns the new session.</summary>
    private async Task<Guid> SignInAsync(string password, string? recoveryCode = null, byte[]? totpKey = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        using var signInPage = await client.SendAsync(Request(HttpMethod.Get, AdminSignedOutExemptions.SignIn));
        var token = AdminPageAntiForgery.ReadToken(await signInPage.Content.ReadAsStringAsync());

        using var step1 = await client.SendAsync(Post(AdminSignedOutExemptions.ApiSignIn, token, new
        {
            email = OwnerEmail,
            password,
            turnstileToken = FakeTurnstileVerifier.ValidToken
        }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var step2 = recoveryCode is not null
            ? await client.SendAsync(Post(AdminSignedOutExemptions.ApiSignInRecovery, token, new { recoveryCode }))
            : await client.SendAsync(Post(AdminSignedOutExemptions.ApiSignInCode, token, new
            {
                code = Rfc6238Totp.ComputeCode(totpKey!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow))
            }));
        Assert.Equal(HttpStatusCode.OK, step2.StatusCode);
        var session = Assert.Single(SetCookies(step2), c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
        return Guid.Parse(session.Split(';', 2)[0][(AdminSessionCookie.Name.Length + 1)..]);
    }

    private static HttpRequestMessage Post(string path, string token, object body)
    {
        var request = Request(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task<bool> NoticeExistsAsync(Guid session)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessionRecoveryNotices.AsNoTracking().AnyAsync(n => n.SessionId == session);
    }

    private async Task<int> UnusedCodesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var account = await db.AdminCoreOwnerAccounts.AsNoTracking().SingleAsync(a => a.Email == OwnerEmail);
        return await db.AdminRecoveryCodes.AsNoTracking().CountAsync(c => c.AccountId == account.Id && c.UsedAt == null);
    }

    private async Task UseCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAdminCoreOwnerAccountRepository>();
        var hash = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>().Hash(code);
        var account = await db.AdminCoreOwnerAccounts.AsNoTracking().SingleAsync(a => a.Email == OwnerEmail);
        Assert.True(await accounts.TryRedeemRecoveryCodeAsync(account.Id, hash, DateTimeOffset.UtcNow));
    }

    private static string ReadAsset(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, name))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
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
}

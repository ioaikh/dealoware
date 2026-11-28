using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T05: edit mode on the detail pages (S-B6), as the server writes it
/// (AdminEditPage). Which controls a record's page carries, and which it never
/// does; that every one starts hidden; that the page's edit token is the one
/// the edit API takes; encoding; nothing secret in the page; the script's
/// house rules. What the page does in a browser is in e2e/console/edit.spec.ts.
/// Case ids: TD-ADM-UI-na-B10, na-expire, na-B11, na-B12.
/// </summary>
[Collection("WebAppTests")]
public class AdminEditPageTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = TestCoreOwner.Email;

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminEditPageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- seeding ----

    private sealed record Graph(
        Participant Alpha,
        Participant Beta,
        Participant Deleted,
        Artifact Artifact,
        Artifact TwoEntities,
        Negotiation Open,
        Negotiation Closed,
        Negotiation Lapsed,
        Negotiation Bare,
        Offer OpenOffer,
        Offer AcceptedOffer,
        Offer DeletedOffer);

    private async Task<Graph> SeedAsync(string prefix, string? alphaName = null, string? entityName = null, string? description = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var alpha = Participant.Create(alphaName ?? $"{prefix}-Alpha");
        alpha.UpdateLoginEmail($"{prefix}-login@example.test");
        var beta = Participant.Create($"{prefix}-Beta");
        beta.Deactivate();
        var deleted = Participant.Create($"{prefix}-Gone");
        db.Participants.AddRange(alpha, beta, deleted);

        var artifact = Artifact.Create(alpha.Sub, [SubjectEntity.Create(entityName ?? $"{prefix}-Widget", description ?? $"{prefix} first line")], "sell");
        var two = Artifact.Create(beta.Sub,
            [SubjectEntity.Create($"{prefix}-Frame", "frame text"), SubjectEntity.Create($"{prefix}-Wheel", "wheel text")], "sell");
        db.Artifacts.AddRange(artifact, two);

        var ends = new DateTimeOffset(2099, 7, 1, 16, 30, 0, TimeSpan.Zero);
        var (open, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy", endsAt: ends);
        var (closed, _) = Negotiation.Create(two.Id, beta.Sub, alpha.Sub, "sell", "buy");
        closed!.Close();
        var (lapsed, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (bare, _) = Negotiation.Create(two.Id, alpha.Sub, beta.Sub, "sell", "buy");
        db.Negotiations.AddRange(open!, closed, lapsed!, bare!);

        var (openOffer, _) = Offer.Create(open!.Id, alpha.Sub, beta.Sub, 1234.5m, "USD", $"{prefix}-terms");
        var (second, _) = Offer.Create(open.Id, beta.Sub, alpha.Sub, 20m, "EUR", null);
        var (third, _) = Offer.Create(open.Id, alpha.Sub, beta.Sub, 30m, "EUR", null);
        var (accepted, _) = Offer.Create(open.Id, beta.Sub, alpha.Sub, 40m, "EUR", $"{prefix}-accepted");
        accepted!.Accept(alpha.Sub);
        var (deletedOffer, _) = Offer.Create(open.Id, alpha.Sub, beta.Sub, 50m, "USD", null);
        db.Offers.AddRange(openOffer!, second!, third!, accepted, deletedOffer!);

        var (strategy, _) = Strategy.Create(alpha.Sub, $"{prefix}-strat", $"{prefix}-STRATEGY-TEXT");
        db.Strategies.Add(strategy!);
        await db.SaveChangesAsync();

        var at = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        await db.Participants.Where(p => p.Id == deleted.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer!.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));
        // Its end time has passed while its status still says Open: it can only be expired.
        await db.Negotiations.Where(n => n.Id == lapsed!.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.EndsAt, at));

        return new Graph(alpha, beta, deleted, artifact, two, open, closed, lapsed!, bare!, openOffer!, accepted, deletedOffer!);
    }

    private async Task<Guid> SessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(OwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false,
        BaseAddress = new Uri("https://localhost")
    });

    private async Task<(HttpResponseMessage Response, string Html)> PageAsync(string path, Guid session)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}");
        var response = await Client().SendAsync(request);
        return (response, await response.Content.ReadAsStringAsync());
    }

    /// <summary>One PATCH as the page sends it: the session, the anti-forgery pair, the Origin, If-Match.</summary>
    private async Task<HttpResponseMessage> PatchAsync(string path, object body, Guid session, string ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, path);
        request.Headers.Host = AdminHost;
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}; {AdminAntiForgeryCookie.Name}={nonce}");
        request.Headers.TryAddWithoutValidation(
            AdminAntiForgeryCookie.HeaderName,
            _factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(nonce));
        request.Headers.TryAddWithoutValidation("Origin", AdminHostOptions.ProductionAdminOrigin);
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await Client().SendAsync(request);
    }

    private static string Attribute(string html, string id, string name)
    {
        var tag = Regex.Match(html, $"<[a-z]+ (?:[^>]* )?id=\"{Regex.Escape(id)}\"[^>]*>").Value;
        Assert.NotEqual(string.Empty, tag);
        return WebUtility.HtmlDecode(Regex.Match(tag, $" {Regex.Escape(name)}=\"([^\"]*)\"").Groups[1].Value);
    }

    /// <summary>The ids of the buttons in the actions bar, in order.</summary>
    private static string[] Actions(string html)
    {
        var bar = Regex.Match(html, "<div id=\"record-actions\" class=\"record-actions\" hidden>(.*?)</div>").Groups[1].Value;
        return Regex.Matches(bar, "<button type=\"button\" id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
    }

    /// <summary>The API name of every field the page lets the owner change.</summary>
    private static string[] Keys(string html)
        => Regex.Matches(html, "data-key=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();

    private static string[] Scripts(string html)
        => Regex.Matches(html, "<script src=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();

    // ---- per type: what may change, and nothing else ----

    [Fact]
    public async Task Participant_DisplayNameAndActiveOrSuspended_AllHidden_WithTheReadsOwnEditToken()
    {
        var prefix = $"ep1-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (response, html) = await PageAsync($"/admin/participants/{g.Alpha.Id:D}", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["edit-button", "delete-button"], Actions(html));
        Assert.Equal(["displayName", "isActive"], Keys(html));
        Assert.Contains($"<input type=\"text\" id=\"edit-displayName\" data-key=\"displayName\" maxlength=\"256\" autocomplete=\"off\" spellcheck=\"false\" aria-describedby=\"edit-displayName-error\" value=\"{prefix}-Alpha\">", html, StringComparison.Ordinal);
        Assert.Contains("<input type=\"radio\" id=\"edit-isActive-active\" name=\"isActive\" value=\"true\" checked", html, StringComparison.Ordinal);
        Assert.Contains("<input type=\"radio\" id=\"edit-isActive-suspended\" name=\"isActive\" value=\"false\" aria-describedby", html, StringComparison.Ordinal);
        Assert.Contains("<label for=\"edit-displayName\">Display name</label>", html, StringComparison.Ordinal);
        Assert.Contains("<legend>Status</legend>", html, StringComparison.Ordinal);

        // Everything the script shows starts hidden: without JavaScript the page only reads.
        Assert.Contains("<div id=\"record-actions\" class=\"record-actions\" hidden>", html, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(html, "<div class=\"edit\" hidden>").Count);
        Assert.DoesNotMatch("<div class=\"edit\">", html);
        Assert.Contains("<div id=\"edit-buttons\" class=\"edit-buttons\" hidden><button type=\"submit\" id=\"edit-save\" disabled>Save</button><button type=\"button\" id=\"edit-cancel\" class=\"secondary\">Cancel</button></div></form>", html, StringComparison.Ordinal);
        // The form has no method or action: nothing can send it but the script. A browser keeps no typed value of it.
        Assert.Contains("<form id=\"edit-form\" class=\"edit-form\" autocomplete=\"off\" novalidate>", html, StringComparison.Ordinal);
        Assert.Contains("<dialog id=\"discard-dialog\" class=\"edit-dialog\" aria-modal=\"true\" aria-labelledby=\"discard-dialog-title\">", html, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"discard-dialog-title\">Discard your changes?</h2>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"action-dialog\"", html, StringComparison.Ordinal);
        Assert.Equal(
            ["/admin/auth/admin-request.js", "/admin/assets/console.js", "/admin/assets/session-timer.js", "/admin/auth/admin-dom.js", "/admin/assets/console-edit.js", "/admin/assets/console-delete.js"],
            Scripts(html));

        // The page's edit token is the detail read's ETag, and the edit API takes it.
        Assert.Equal($"/admin/api/participants/{g.Alpha.Id:D}", Attribute(html, "record", "data-api"));
        var etag = Attribute(html, "record", "data-etag");
        Assert.Matches("^\"[0-9]+-[0-9a-f]{16}\"$", etag);
        var readRequest = new HttpRequestMessage(HttpMethod.Get, $"/admin/api/participants/{g.Alpha.Id:D}");
        readRequest.Headers.Host = AdminHost;
        readRequest.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}");
        using var read = await Client().SendAsync(readRequest);
        Assert.Equal(etag, read.Headers.ETag!.ToString());

        using var saved = await PatchAsync($"/admin/api/participants/{g.Alpha.Id:D}", new { displayName = $"{prefix}-Renamed" }, session, etag);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        // The same token again: the record changed after the page was written.
        using var stale = await PatchAsync($"/admin/api/participants/{g.Alpha.Id:D}", new { isActive = false }, session, etag);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(AdminEditCopy.Conflict, Attribute(html, "record", "data-conflict"));

        // A suspended participant: Suspended is the one checked.
        (_, html) = await PageAsync($"/admin/participants/{g.Beta.Id:D}", session);
        Assert.Contains("value=\"false\" checked", html, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"true\" checked", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Artifact_SubjectEntityNameAndDescription_AndOwner_PickTheEntityWhenThereAreSeveral()
    {
        var prefix = $"ep2-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix, description: "\nstarts on a new line");
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/artifacts/{g.Artifact.Id:D}", session);
        Assert.Equal(["edit-button", "delete-button"], Actions(html));
        Assert.Equal(["name", "description", "ownerParticipantId"], Keys(html).OrderBy(k => k == "ownerParticipantId").ToArray());
        // One entity: its id rides along; no choice to make.
        Assert.Contains($"<input type=\"hidden\" id=\"edit-entityId\" value=\"{g.Artifact.Entities[0].Id:D}\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<select id=\"edit-entityId\">", html, StringComparison.Ordinal);
        Assert.Contains("<label for=\"edit-name\">Name (required)</label>", html, StringComparison.Ordinal);
        Assert.Contains($"aria-required=\"true\" aria-describedby=\"edit-name-error\" value=\"{prefix}-Widget\">", html, StringComparison.Ordinal);
        Assert.Contains("maxlength=\"4096\"", html, StringComparison.Ordinal);
        // The parser drops the first line break after the tag: the page adds one, so the text's own stays.
        Assert.Contains("aria-describedby=\"edit-description-error\">\n&#xA;starts on a new line</textarea>", html, StringComparison.Ordinal);
        // The owner: a typeahead over the participants, the stored value the owner's Core identifier.
        Assert.Contains($"<input type=\"hidden\" id=\"edit-ownerParticipantId\" data-key=\"ownerParticipantId\" value=\"{g.Alpha.Sub}\" data-initial=\"{g.Alpha.Sub}\">", html, StringComparison.Ordinal);
        Assert.Contains($"role=\"combobox\"", html, StringComparison.Ordinal);
        Assert.Matches($"id=\"edit-owner-picker\"[^>]* value=\"{prefix}-Alpha\">", html);
        Assert.Equal("/admin/api/participants", Attribute(html, "edit-owner", "data-source"));

        (_, html) = await PageAsync($"/admin/artifacts/{g.TwoEntities.Id:D}", session);
        Assert.Contains("<label for=\"edit-entityId\">Subject entity</label><select id=\"edit-entityId\">", html, StringComparison.Ordinal);
        var options = Regex.Matches(html, "<option value=\"([^\"]+)\" data-name=\"([^\"]*)\" data-description=\"([^\"]*)\"( selected)?>([^<]*)</option>");
        Assert.Equal(2, options.Count);
        Assert.Equal(
            g.TwoEntities.Entities.Select(e => (e.Id.ToString("D"), e.Name, e.Description)).OrderBy(e => e.Item1),
            options.Select(m => (m.Groups[1].Value, m.Groups[2].Value, WebUtility.HtmlDecode(m.Groups[3].Value))).OrderBy(e => e.Item1));
        Assert.Single(options, m => m.Groups[4].Success);
    }

    [Fact]
    public async Task Negotiation_EndsAtInET_CloseAndExpireWithTheServersCount_OnlyWhileOpen()
    {
        var prefix = $"ep3-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/negotiations/{g.Open.Id:D}", session);
        Assert.Equal(["edit-button", "close-button", "expire-button", "delete-button"], Actions(html));
        Assert.Equal(["endsAt"], Keys(html));
        // 16:30 UTC on 1 July is 12:30 in New York (daylight time).
        Assert.Contains("<input type=\"datetime-local\" id=\"edit-endsAt\" data-key=\"endsAt\" aria-describedby=\"edit-endsAt-hint edit-endsAt-error\" value=\"2099-07-01T12:30\">", html, StringComparison.Ordinal);
        Assert.Contains("<p id=\"edit-endsAt-hint\" class=\"hint\">Eastern Time (ET).</p>", html, StringComparison.Ordinal);
        // Three open offers; the accepted and the deleted one are not counted.
        var id = g.Open.Id.ToString("D");
        Assert.Equal($"Expire negotiation {id}?", Attribute(html, "expire-button", "data-title"));
        Assert.Equal("3 open offers will be cancelled.", Attribute(html, "expire-button", "data-body"));
        Assert.Equal("Expire negotiation", Attribute(html, "expire-button", "data-confirm"));
        Assert.Equal("Cancel", Attribute(html, "expire-button", "data-cancel"));
        Assert.Equal("Expiring…", Attribute(html, "expire-button", "data-busy"));
        Assert.Equal("Expired", Attribute(html, "expire-button", "data-status"));
        Assert.Equal($"Close negotiation {id}?", Attribute(html, "close-button", "data-title"));
        Assert.Equal("3 open offers will be cancelled.", Attribute(html, "close-button", "data-body"));
        Assert.Equal("Closing…", Attribute(html, "close-button", "data-busy"));
        Assert.Equal("Closed", Attribute(html, "close-button", "data-status"));
        Assert.Equal($"Negotiation {id} expired.", Attribute(html, "record", "data-note-expired"));
        Assert.Equal($"Negotiation {id} closed.", Attribute(html, "record", "data-note-closed"));
        Assert.Contains("<dialog id=\"action-dialog\" class=\"edit-dialog\" aria-modal=\"true\" aria-labelledby=\"action-dialog-title\" aria-describedby=\"action-dialog-body\">", html, StringComparison.Ordinal);

        // No open offers: the count says so; no end time: an empty box.
        (_, html) = await PageAsync($"/admin/negotiations/{g.Bare.Id:D}", session);
        Assert.Equal("No open offers will be cancelled.", Attribute(html, "expire-button", "data-body"));
        Assert.Contains("data-key=\"endsAt\" aria-describedby=\"edit-endsAt-hint edit-endsAt-error\" value=\"\">", html, StringComparison.Ordinal);

        // Past its end time: it can only be expired. No new end time, no Close.
        (_, html) = await PageAsync($"/admin/negotiations/{g.Lapsed.Id:D}", session);
        Assert.Equal(["expire-button", "delete-button"], Actions(html));
        Assert.Empty(Keys(html));
        Assert.DoesNotContain("id=\"edit-form\"", html, StringComparison.Ordinal);

        // Closed is final: nothing to change but Delete (T06b); the page can still say what was just done.
        (_, html) = await PageAsync($"/admin/negotiations/{g.Closed.Id:D}", session);
        Assert.Equal(["delete-button"], Actions(html));
        Assert.DoesNotContain("id=\"action-dialog\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"discard-dialog\"", html, StringComparison.Ordinal);
        Assert.Empty(Keys(html));
        Assert.Equal($"Negotiation {g.Closed.Id:D} closed.", Attribute(html, "record", "data-note-closed"));
    }

    [Fact]
    public async Task Offer_CanOnlyBeCancelled_NeverItsAmountCurrencyOrTerms()
    {
        var prefix = $"ep4-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/offers/{g.OpenOffer.Id:D}", session);
        Assert.Equal(["cancel-offer-button", "delete-button"], Actions(html));
        Assert.Empty(Keys(html));
        Assert.DoesNotContain("id=\"edit-form\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"edit-button\"", html, StringComparison.Ordinal);
        // The only input is the sign-out form's; no textarea, no select: amount, currency and terms stay as sent.
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.DoesNotContain("<textarea", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", html, StringComparison.Ordinal);
        foreach (var field in new[] { "amount", "currency", "terms" })
            Assert.DoesNotContain($"\"{field}\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Cancelled", Attribute(html, "cancel-offer-button", "data-status"));
        Assert.Equal($"Cancel offer {g.OpenOffer.Id:D}?", Attribute(html, "cancel-offer-button", "data-title"));
        Assert.Equal(AdminConsoleCopy.CancelOfferBody, Attribute(html, "cancel-offer-button", "data-body"));
        Assert.Equal("Keep offer", Attribute(html, "cancel-offer-button", "data-cancel"));
        Assert.Equal($"Offer {g.OpenOffer.Id:D} cancelled.", Attribute(html, "record", "data-note-cancelled"));
        Assert.DoesNotContain("data-note-saved", html, StringComparison.Ordinal);
        // The amount and terms still read as text.
        Assert.Contains($"<span class=\"free-text\">{prefix}-terms</span>", html, StringComparison.Ordinal);

        // Accepted is final: no action but Delete (T06b).
        (_, html) = await PageAsync($"/admin/offers/{g.AcceptedOffer.Id:D}", session);
        Assert.Equal(["delete-button"], Actions(html));
        Assert.Empty(Keys(html));
    }

    [Fact]
    public async Task ASoftDeletedRecord_ShowsNoEditControl_AndLoadsNoEditScript()
    {
        var prefix = $"ep5-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();
        // An artifact and an open negotiation that would each have controls, soft-deleted.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var at = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
            await db.Artifacts.Where(a => a.Id == g.TwoEntities.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, at));
            await db.Negotiations.Where(n => n.Id == g.Bare.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, at));
        }

        foreach (var path in new[]
                 {
                     $"/admin/participants/{g.Deleted.Id:D}?includeDeleted=true",
                     $"/admin/artifacts/{g.TwoEntities.Id:D}?includeDeleted=true",
                     $"/admin/negotiations/{g.Bare.Id:D}?includeDeleted=true",
                     $"/admin/offers/{g.DeletedOffer.Id:D}?includeDeleted=true"
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("badge-deleted", html, StringComparison.Ordinal);
            Assert.DoesNotContain("id=\"record\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<button type=\"button\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("data-etag", html, StringComparison.Ordinal);
            Assert.Empty(Keys(html));
            Assert.DoesNotContain(AdminEditPage.ScriptPath, html, StringComparison.Ordinal);
            Assert.DoesNotContain("<dialog", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<textarea", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<select", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task EveryValue_IsEncoded_AndNoCredentialIsInThePage()
    {
        var prefix = $"ep6-{Guid.NewGuid():N}";
        const string hostile = "<script>alert(1)</script>\"'&<img src=x>";
        var g = await SeedAsync(prefix, alphaName: hostile + prefix, entityName: hostile + "e", description: hostile + "d");
        var session = await SessionAsync();
        var encoded = AdminSignedOutPage.Encode(hostile);

        foreach (var path in new[] { $"/admin/participants/{g.Alpha.Id:D}", $"/admin/artifacts/{g.Artifact.Id:D}", $"/admin/negotiations/{g.Open.Id:D}" })
        {
            var (_, html) = await PageAsync(path, session);
            Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<img src=x>", html, StringComparison.Ordinal);
            Assert.Equal(Regex.Matches(html, "<script src=").Count, Regex.Matches(html, "<script").Count);
            foreach (var banned in new[] { "<style", " style=", " onclick=", " onsubmit=", " onload=", " oninput=", "javascript:" })
                Assert.DoesNotContain(banned, html, StringComparison.OrdinalIgnoreCase);

            // Nothing about the owner, the session or a strategy; the token is not the bare version.
            Assert.DoesNotContain(OwnerEmail, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(session.ToString("D"), html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("STRATEGY-TEXT", html, StringComparison.Ordinal);
            Assert.DoesNotContain("version", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", html, StringComparison.OrdinalIgnoreCase);
            // The anti-forgery token is in the meta and the sign-out form, nowhere else.
            Assert.Equal(2, Regex.Matches(html, AdminPageAntiForgery.ReadToken(html)).Count);

            if (path.Contains("participants", StringComparison.Ordinal))
                Assert.Contains($"value=\"{encoded}{prefix}\">", html, StringComparison.Ordinal);
            if (path.Contains("artifacts", StringComparison.Ordinal))
            {
                Assert.Contains($"value=\"{encoded}e\">", html, StringComparison.Ordinal);
                Assert.Contains($">\n{encoded}d</textarea>", html, StringComparison.Ordinal);
                Assert.Matches($"id=\"edit-owner-picker\"[^>]* value=\"{Regex.Escape(encoded + prefix)}\">", html);
            }
        }
    }

    // ---- the script, read as text ----

    [Fact]
    public void EditScript_KeepsToTheHouseRules()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "console-edit.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "eval(", "new Function", "setInterval", "XMLHttpRequest",
                     ".submit()", "confirm(", "alert(", "prompt(", "beforeunload"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }

        // One write: the PATCH of this record, with the edit token and the anti-forgery token; no redirect followed.
        Assert.Single(Regex.Matches(script, @"fetch\("));
        Assert.Single(Regex.Matches(script, "method: \"PATCH\""));
        Assert.Contains("\"If-Match\": etag", script, StringComparison.Ordinal);
        Assert.Contains("\"X-CSRF-TOKEN\": csrfToken()", script, StringComparison.Ordinal);
        Assert.Contains("redirect: \"error\"", script, StringComparison.Ordinal);
        Assert.Contains("var SESSION_ENDED = \"" + AdminSignInPages.SessionEndedLocation + "\";", script, StringComparison.Ordinal);
        // Escape is the page's own, in the capture phase, never during text composition.
        Assert.Contains("if (event.isComposing) return;", script, StringComparison.Ordinal);
        // What the page announces after it loaded itself anew comes from its history entry's state, never the address.
        Assert.DoesNotContain("location.hash.indexOf", script, StringComparison.Ordinal);
        Assert.Contains("history.replaceState({ dwNote: note }", script, StringComparison.Ordinal);
        Assert.Contains("}, true);", script, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}

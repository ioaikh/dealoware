using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dealoware.Api.Tests;

/// <summary>
/// T06b: the delete dialog of the console, as the server writes it
/// (AdminDeletePage): which detail page and which list row has a Delete, and
/// which never does; that each starts hidden; the dialog's words and where
/// the page goes afterwards; encoding; that the page's way of deleting is the
/// delete API's; the script's house rules. What the dialog does in a browser
/// is in e2e/console/delete.spec.ts. Case ids: TD-ADM-UI-del-01 to del-06.
/// </summary>
[Collection("WebAppTests")]
public class AdminDeletePageTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = TestCoreOwner.Email;

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminDeletePageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- seeding ----

    private sealed record Graph(
        Participant Alpha,
        Participant Beta,
        Participant Gone,
        Artifact Artifact,
        Artifact Lonely,
        Negotiation Open,
        Negotiation Closed,
        Offer OpenOffer,
        Offer AcceptedOffer,
        Offer DeletedOffer);

    private async Task<Graph> SeedAsync(string prefix, string? alphaName = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var alpha = Participant.Create(alphaName ?? $"{prefix}-Alpha");
        var beta = Participant.Create($"{prefix}-Beta");
        var gone = Participant.Create($"{prefix}-Gone");
        db.Participants.AddRange(alpha, beta, gone);

        var artifact = Artifact.Create(alpha.Sub, [SubjectEntity.Create($"{prefix}-Widget", "text")], "sell");
        var lonely = Artifact.Create(beta.Sub, [SubjectEntity.Create($"{prefix}-Lonely", "text")], "sell");
        db.Artifacts.AddRange(artifact, lonely);

        var (open, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (closed, _) = Negotiation.Create(artifact.Id, beta.Sub, alpha.Sub, "sell", "buy");
        closed!.Close();
        db.Negotiations.AddRange(open!, closed);

        var (openOffer, _) = Offer.Create(open!.Id, alpha.Sub, beta.Sub, 10m, "USD", null);
        var (accepted, _) = Offer.Create(open.Id, beta.Sub, alpha.Sub, 20m, "USD", null);
        accepted!.Accept(alpha.Sub);
        var (deletedOffer, _) = Offer.Create(open.Id, alpha.Sub, beta.Sub, 30m, "USD", null);
        db.Offers.AddRange(openOffer!, accepted, deletedOffer!);
        await db.SaveChangesAsync();

        var at = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        await db.Participants.Where(p => p.Id == gone.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer!.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));
        return new Graph(alpha, beta, gone, artifact, lonely, open, closed, openOffer!, accepted, deletedOffer!);
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

    private async Task<(HttpResponseMessage Response, string Html)> PageAsync(string path, Guid? session)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (session is { } s)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={s:D}");
        var response = await Client().SendAsync(request);
        return (response, await response.Content.ReadAsStringAsync());
    }

    /// <summary>One request as console-delete.js sends it: the session, the anti-forgery pair, the Origin, and the token header if given.</summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, Guid session, string? confirmToken = null, WebApplicationFactory<Program>? host = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}; {AdminAntiForgeryCookie.Name}={nonce}");
        request.Headers.TryAddWithoutValidation(
            AdminAntiForgeryCookie.HeaderName,
            _factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(nonce));
        request.Headers.TryAddWithoutValidation("Origin", AdminHostOptions.ProductionAdminOrigin);
        if (confirmToken is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteEndpoints.ConfirmTokenHeader, confirmToken);
        var client = host is null
            ? Client()
            : host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false, BaseAddress = new Uri("https://localhost") });
        return await client.SendAsync(request);
    }

    private static string Tag(string html, string id)
    {
        var tag = Regex.Match(html, $"<[a-z]+ (?:[^>]* )?id=\"{Regex.Escape(id)}\"[^>]*>").Value;
        Assert.NotEqual(string.Empty, tag);
        return tag;
    }

    private static string Attribute(string html, string id, string name)
        => WebUtility.HtmlDecode(Regex.Match(Tag(html, id), $" {Regex.Escape(name)}=\"([^\"]*)\"").Groups[1].Value);

    private static string[] Actions(string html)
    {
        var bar = Regex.Match(html, "<div id=\"record-actions\" class=\"record-actions\" hidden>(.*?)</div>").Groups[1].Value;
        return Regex.Matches(bar, "<button type=\"button\" id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
    }

    private static string[] Scripts(string html)
        => Regex.Matches(html, "<script src=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();

    /// <summary>The list rows' Delete: (record id, name for the control, the hidden words).</summary>
    private static (string Id, string Name, string Hidden)[] RowDeletes(string html)
        => Regex.Matches(html,
                "<span class=\"row-actions\"><button type=\"button\" class=\"secondary delete-open\" data-delete-id=\"([^\"]+)\" data-delete-name=\"([^\"]*)\" hidden>Delete<span class=\"visually-hidden\"> ([^<]*)</span></button></span>")
            .Select(m => (m.Groups[1].Value, WebUtility.HtmlDecode(m.Groups[2].Value), WebUtility.HtmlDecode(m.Groups[3].Value)))
            .ToArray();

    // ---- the detail pages ----

    [Fact]
    public async Task DetailPages_DeleteBesideEditAndTheActions_HiddenUntilTheScript_WithTheDialogAndItsWords()
    {
        var prefix = $"dp1-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        foreach (var (path, kind, id, name, actions) in new (string, AdminListKind, Guid, string, string[])[]
                 {
                     ($"/admin/participants/{g.Alpha.Id:D}", AdminListKind.Participants, g.Alpha.Id, $"{prefix}-Alpha", ["edit-button", "delete-button"]),
                     ($"/admin/artifacts/{g.Lonely.Id:D}", AdminListKind.Artifacts, g.Lonely.Id, $"{prefix}-Lonely", ["edit-button", "delete-button"]),
                     ($"/admin/negotiations/{g.Open.Id:D}", AdminListKind.Negotiations, g.Open.Id, g.Open.Id.ToString("D"), ["edit-button", "close-button", "expire-button", "delete-button"]),
                     ($"/admin/offers/{g.OpenOffer.Id:D}", AdminListKind.Offers, g.OpenOffer.Id, g.OpenOffer.Id.ToString("D"), ["cancel-offer-button", "delete-button"]),
                     // Nothing left to change: Delete is the only action.
                     ($"/admin/negotiations/{g.Closed.Id:D}", AdminListKind.Negotiations, g.Closed.Id, g.Closed.Id.ToString("D"), ["delete-button"]),
                     ($"/admin/offers/{g.AcceptedOffer.Id:D}", AdminListKind.Offers, g.AcceptedOffer.Id, g.AcceptedOffer.Id.ToString("D"), ["delete-button"])
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(actions, Actions(html));
            Assert.Contains(
                $"<button type=\"button\" id=\"delete-button\" class=\"secondary delete-open\" data-delete-id=\"{id:D}\" data-delete-name=\"{name}\" hidden>Delete</button></div>",
                html, StringComparison.Ordinal);

            // The dialog: empty, its words in data-copy, and where the page goes afterwards.
            Assert.Contains(
                "<dialog id=\"delete-dialog\" class=\"edit-dialog delete-dialog\" aria-modal=\"true\" aria-labelledby=\"delete-dialog-title\" aria-describedby=\"delete-dialog-content\"",
                html, StringComparison.Ordinal);
            Assert.Contains(
                "<div class=\"edit-dialog-inner\"><h2 id=\"delete-dialog-title\"></h2><div id=\"delete-dialog-content\"></div><div id=\"delete-dialog-field\"></div><div id=\"delete-dialog-actions\" class=\"edit-dialog-actions\"></div></div></dialog>",
                html, StringComparison.Ordinal);
            Assert.Single(Regex.Matches(html, "<dialog id=\"delete-dialog\""));
            Assert.Equal(AdminDeletePage.ApiType(kind), Attribute(html, "delete-dialog", "data-type"));
            Assert.Equal("/admin/" + AdminDeletePage.ApiType(kind), Attribute(html, "delete-dialog", "data-return"));
            var copy = JsonSerializer.Deserialize<Dictionary<string, string>>(Attribute(html, "delete-dialog", "data-copy"))!;
            Assert.Equal(AdminDeletePage.Copy(kind), copy);
            // No stored value rides in the words.
            Assert.DoesNotContain(prefix, Attribute(html, "delete-dialog", "data-copy"), StringComparison.Ordinal);

            var scripts = Scripts(html);
            Assert.Equal(AdminDeletePage.ScriptPath, scripts[^1]);
            Assert.Contains(AdminSignedOutPage.DomScriptPath, scripts);
            // No live region in the dialog's markup: the script adds its alert line; the shell's region stays the page's one.
            var deleteDialog = Regex.Match(html, "<dialog id=\"delete-dialog\".*?</dialog>").Value;
            Assert.DoesNotMatch("role=|aria-live", deleteDialog);
            Assert.Single(Regex.Matches(html, "aria-live="));
        }
    }

    [Fact]
    public async Task DetailPage_TheListsStateRidesAlong_SoAfterADeleteThePageGoesBackToTheListAsItWas()
    {
        var prefix = $"dp2-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/offers/{g.OpenOffer.Id:D}?sort=created&dir=asc&includeDeleted=true", session);
        Assert.Equal("/admin/offers?sort=created&dir=asc&includeDeleted=true", Attribute(html, "delete-dialog", "data-return"));
        Assert.Equal(Attribute(html, "back-to-list", "href"), Attribute(html, "delete-dialog", "data-return"));
    }

    [Fact]
    public async Task ASoftDeletedRecord_HasNoDelete_NoDialog_AndLoadsNoDeleteScript()
    {
        var prefix = $"dp3-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        foreach (var path in new[]
                 {
                     $"/admin/participants/{g.Gone.Id:D}?includeDeleted=true",
                     $"/admin/offers/{g.DeletedOffer.Id:D}?includeDeleted=true"
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("badge-deleted", html, StringComparison.Ordinal);
            Assert.DoesNotContain("delete-open", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<dialog", html, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminDeletePage.ScriptPath, html, StringComparison.Ordinal);
            Assert.DoesNotContain("<button type=\"button\"", html, StringComparison.Ordinal);
        }
    }

    // ---- the lists ----

    [Fact]
    public async Task ListRows_EachHasAHiddenDeleteNamedForItsRow_InItsLastCell_NoneForASoftDeletedRow_TheColumnsUnchanged()
    {
        var prefix = $"dp4-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/participants?q={prefix}&includeDeleted=true", session);
        var rows = RowDeletes(html);
        Assert.Equal(
            new[] { (g.Alpha.Id.ToString("D"), $"{prefix}-Alpha"), (g.Beta.Id.ToString("D"), $"{prefix}-Beta") }.OrderBy(r => r.Item1),
            rows.Select(r => (r.Id, r.Name)).OrderBy(r => r.Item1));
        Assert.All(rows, r => Assert.Equal(r.Name, r.Hidden));
        // The soft-deleted one is listed, with no Delete.
        Assert.Contains($"{prefix}-Gone", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"data-delete-id=\"{g.Gone.Id:D}\"", html, StringComparison.Ordinal);
        // Each Delete closes its row's last cell; no column is added.
        Assert.Equal(2, Regex.Matches(html, "</button></span></td></tr>").Count);
        Assert.Equal(5, Regex.Matches(html, "<th scope=\"col\"").Count);

        // The dialog, once, and after a delete the list as it is now.
        Assert.Single(Regex.Matches(html, "<dialog id=\"delete-dialog\""));
        Assert.Equal($"/admin/participants?q={prefix}&includeDeleted=true", Attribute(html, "delete-dialog", "data-return"));
        Assert.Equal(
            ["/admin/auth/admin-request.js", "/admin/assets/console.js", "/admin/assets/session-timer.js", "/admin/auth/admin-dom.js", AdminDeletePage.ScriptPath, AdminListPages.ScriptPath],
            Scripts(html));

        // Offers and negotiations are named by their ID.
        (_, html) = await PageAsync($"/admin/offers?negotiationId={g.Open.Id:D}", session);
        Assert.Equal(
            new[] { g.OpenOffer.Id, g.AcceptedOffer.Id }.Select(i => (i.ToString("D"), i.ToString("D"))).OrderBy(r => r.Item1),
            RowDeletes(html).Select(r => (r.Id, r.Hidden)).OrderBy(r => r.Item1));

        // An empty list still has the dialog: it says what was just deleted.
        (_, html) = await PageAsync($"/admin/artifacts?q={prefix}-nothing-matches", session);
        Assert.Empty(RowDeletes(html));
        Assert.Single(Regex.Matches(html, "<dialog id=\"delete-dialog\""));
    }

    [Fact]
    public async Task EveryValue_IsEncoded_InTheControlsAndTheirHiddenNames()
    {
        var prefix = $"dp5-{Guid.NewGuid():N}";
        const string hostile = "<script>alert(1)</script>\"'&<img src=x>";
        var g = await SeedAsync(prefix, alphaName: hostile + prefix);
        var session = await SessionAsync();
        var encoded = AdminSignedOutPage.Encode(hostile + prefix);

        foreach (var path in new[] { $"/admin/participants/{g.Alpha.Id:D}", $"/admin/participants?q={prefix}" })
        {
            var (_, html) = await PageAsync(path, session);
            Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<img src=x>", html, StringComparison.Ordinal);
            Assert.Equal(Regex.Matches(html, "<script src=").Count, Regex.Matches(html, "<script").Count);
            Assert.Contains($"data-delete-name=\"{encoded}\"", html, StringComparison.Ordinal);
            foreach (var banned in new[] { "<style", " style=", " onclick=", " onsubmit=", " onload=", "javascript:" })
                Assert.DoesNotContain(banned, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(OwnerEmail, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(session.ToString("D"), html, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, Regex.Matches(html, AdminPageAntiForgery.ReadToken(html)).Count);
        }

        var (_, list) = await PageAsync($"/admin/participants?q={prefix}", session);
        Assert.Contains($"<span class=\"visually-hidden\"> {encoded}</span></button>", list, StringComparison.Ordinal);
    }

    // ---- the page's way of deleting is the API's ----

    [Fact]
    public async Task ThePagesDelete_IntentThenDeleteWithTheTokenHeader_DeletesOnce_AndThePageThenHasNoDelete()
    {
        var prefix = $"dp6-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();
        var (_, html) = await PageAsync($"/admin/artifacts/{g.Lonely.Id:D}", session);
        var api = "/admin/api/" + Attribute(html, "delete-dialog", "data-type") + "/" + Attribute(html, "delete-button", "data-delete-id");

        using var intent = await SendAsync(HttpMethod.Post, api + "/delete-intent", session);
        Assert.Equal(HttpStatusCode.OK, intent.StatusCode);
        using var body = JsonDocument.Parse(await intent.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("confirmToken").GetString()!;
        Assert.Matches("^[0-9a-f]{64}$", token);
        Assert.Equal($"{prefix}-Lonely", body.RootElement.GetProperty("identity").GetProperty("name").GetString());

        using var deleted = await SendAsync(HttpMethod.Delete, api, session, token);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        using var done = JsonDocument.Parse(await deleted.Content.ReadAsStringAsync());
        // The exact answer the script moves on: this type and this id.
        Assert.Equal("artifacts", done.RootElement.GetProperty("entityType").GetString());
        Assert.Equal(g.Lonely.Id, done.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Object, done.RootElement.GetProperty("deleted").ValueKind);

        // The same token again: the script's "expired" answer.
        using var replay = await SendAsync(HttpMethod.Delete, api, session, token);
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Contains(AdminDeleteCopy.TokenInvalidError, await replay.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // Gone from its page; with the toggle, shown without a Delete.
        var (gone, _) = await PageAsync($"/admin/artifacts/{g.Lonely.Id:D}", session);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        (_, html) = await PageAsync($"/admin/artifacts/{g.Lonely.Id:D}?includeDeleted=true", session);
        Assert.DoesNotContain("delete-open", html, StringComparison.Ordinal);

        // An artifact negotiations use: the intent is the blocked answer the dialog shows.
        using var blocked = await SendAsync(HttpMethod.Post, $"/admin/api/artifacts/{g.Artifact.Id:D}/delete-intent", session);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        using var block = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        Assert.Equal(AdminDeleteCopy.BlockedError, block.RootElement.GetProperty("error").GetString());
        Assert.Equal(2, block.RootElement.GetProperty("negotiationCount").GetInt32());
    }

    // TD-ADM-102 through the page's path: the real audit insert fails, the whole delete rolls back,
    // and the server's answer is the exact 500 the script reads as "Nothing was deleted." with
    // Retry; the same token then deletes once the fault is gone.
    [Fact]
    public async Task AuditInsertFails_TheAnswerIsTheScriptsServerFault_NothingDeleted_RetryWithTheSameTokenDeletes()
    {
        var prefix = $"dp7-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();
        var api = $"/admin/api/offers/{g.OpenOffer.Id:D}";
        using var intent = await SendAsync(HttpMethod.Post, api + "/delete-intent", session);
        using var intentBody = JsonDocument.Parse(await intent.Content.ReadAsStringAsync());
        var token = intentBody.RootElement.GetProperty("confirmToken").GetString()!;

        await using (var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                     {
                         services.RemoveAll<IAdminAuditRepository>();
                         services.AddScoped<IAdminAuditRepository, FailingAuditRepository>();
                     })))
        {
            failing.Services.GetRequiredService<FakeAdminClock>().UtcNow = _factory.Services.GetRequiredService<FakeAdminClock>().UtcNow;
            using var refused = await SendAsync(HttpMethod.Delete, api, session, token, failing);
            Assert.Equal(HttpStatusCode.InternalServerError, refused.StatusCode);
            using var fault = JsonDocument.Parse(await refused.Content.ReadAsStringAsync());
            Assert.Equal(AdminSecurityHeadersMiddleware.GenericErrorMessage, fault.RootElement.GetProperty("error").GetString());
        }

        var (page, _) = await PageAsync($"/admin/offers/{g.OpenOffer.Id:D}", session);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        using var retry = await SendAsync(HttpMethod.Delete, api, session, token);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    /// <summary>An audit log that cannot be written to.</summary>
    private sealed class FailingAuditRepository : IAdminAuditRepository
    {
        public Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit insert failed");

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListPageAsync(
            AdminAuditListCriteria criteria,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<AdminAuditEntry>, int)>(([], 0));
    }

    [Fact]
    public async Task WithoutASession_NoDeleteScript()
    {
        var (response, _) = await PageAsync(AdminDeletePage.ScriptPath, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        (response, _) = await PageAsync(AdminDeletePage.ScriptPath, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (signedIn, script) = await PageAsync(AdminDeletePage.ScriptPath, await SessionAsync());
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        Assert.Contains("X-Admin-Confirm-Token", script, StringComparison.Ordinal);
    }

    // ---- the words ----

    [Fact]
    public void TheDialogsWords_AreTheTestDesigns_WhereItGivesThem()
    {
        Assert.Equal("Delete", AdminDeleteCopy.Delete);
        Assert.Equal("Delete negotiation 1234?", AdminDeleteCopy.DialogTitle(AdminDeleteCopy.NegotiationNoun, "1234"));
        Assert.Equal("Delete participant Alex Rivera?", AdminDeleteCopy.DialogTitle(AdminDeleteCopy.ParticipantNoun, "Alex Rivera"));
        Assert.Equal("Delete negotiation", AdminDeleteCopy.ConfirmLabel(AdminDeleteCopy.NegotiationNoun));
        Assert.Equal("Cancel", AdminDeleteCopy.Cancel);
        Assert.Equal("Deleting…", AdminDeleteCopy.Deleting);
        Assert.Equal("This can't be restored from the admin.", AdminDeleteCopy.Undo);
        Assert.Equal("This confirmation is no longer valid. Review the details again.", AdminDeleteCopy.Expired);
        Assert.Equal("This record changed after this dialog opened. Nothing was deleted. Review the details again.", AdminDeleteCopy.ChangedReview);
        Assert.Equal("We couldn't confirm the delete. Try again to see whether it went through.", AdminDeleteCopy.UnclearDelete);
        Assert.Equal("We can't find this record. It may have been deleted already.", AdminDeleteCopy.NotFound);
        Assert.Equal("This artifact can't be deleted", AdminDeleteCopy.BlockedTitle);
        Assert.Equal("It is used by these negotiations:", AdminDeleteCopy.BlockedLede);
        Assert.Equal("Close", AdminDeleteCopy.Close);
        Assert.Equal("Type Alex Rivera to confirm", AdminDeleteCopy.TypeToConfirm("Alex Rivera"));
        Assert.Equal("Participant Alex Rivera deleted.", AdminDeleteCopy.DeletedNote(AdminDeleteCopy.ParticipantNoun, "Alex Rivera"));
        Assert.Equal("Offer 1234 deleted.", AdminDeleteCopy.DeletedNote(AdminDeleteCopy.OfferNoun, "1234"));
        Assert.Equal("Nothing was deleted.", AdminDeleteCopy.NothingDeleted);
        Assert.Equal("Retry", AdminDeleteCopy.Retry);

        // Each template has its one placeholder, which the script fills in once.
        foreach (var kind in Enum.GetValues<AdminListKind>())
        {
            var copy = AdminDeletePage.Copy(kind);
            foreach (var key in new[] { "title", "id", "typed", "note" })
                Assert.Single(Regex.Matches(copy[key], Regex.Escape(AdminDeleteCopy.NamePlaceholder)));
            foreach (var key in new[] { "negotiationsMany", "offersMany", "openMany", "openAmongMany", "blockedMore" })
                Assert.Single(Regex.Matches(copy[key], Regex.Escape(AdminDeleteCopy.CountPlaceholder)));
            Assert.All(copy.Values, v => Assert.False(string.IsNullOrWhiteSpace(v)));
        }
    }

    // ---- the script, read as text ----

    [Fact]
    public void DeleteScript_KeepsToTheHouseRules()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "console-delete.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "eval(", "new Function", "setInterval", "XMLHttpRequest",
                     ".submit()", "confirm(", "alert(", "prompt(", "beforeunload", "location.assign", "location.href ="
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }

        // One fetch: the intent and the DELETE, with the anti-forgery token; the confirm token only ever in its header.
        Assert.Single(Regex.Matches(script, @"fetch\("));
        Assert.Contains("send(\"POST\", apiPath(current.id) + \"/delete-intent\")", script, StringComparison.Ordinal);
        Assert.Contains("send(\"DELETE\", apiPath(id), intent.token)", script, StringComparison.Ordinal);
        Assert.Contains("headers[TOKEN_HEADER] = confirmToken;", script, StringComparison.Ordinal);
        Assert.Contains("var TOKEN_HEADER = \"" + AdminDeleteEndpoints.ConfirmTokenHeader + "\";", script, StringComparison.Ordinal);
        Assert.Contains("\"X-CSRF-TOKEN\": csrfToken()", script, StringComparison.Ordinal);
        Assert.Contains("redirect: \"error\"", script, StringComparison.Ordinal);
        Assert.Contains("var SESSION_ENDED = \"" + AdminSignInPages.SessionEndedLocation + "\";", script, StringComparison.Ordinal);

        // The answers it reads are the server's own codes.
        Assert.Contains("var TOKEN_INVALID = \"" + AdminDeleteCopy.TokenInvalidError + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var BLOCKED = \"" + AdminDeleteCopy.BlockedError + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var CHANGED = \"" + AdminDeny.GenericConflict + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var NOT_FOUND = \"" + AdminDeny.GenericNotFound + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var BAD_REQUEST = \"" + AdminDeny.GenericBadRequest + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var SERVER_FAULT = \"" + AdminSecurityHeadersMiddleware.GenericErrorMessage + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var NAME = \"" + AdminDeleteCopy.NamePlaceholder + "\";", script, StringComparison.Ordinal);
        Assert.Contains("var COUNT = \"" + AdminDeleteCopy.CountPlaceholder + "\";", script, StringComparison.Ordinal);

        // Escape is the page's own, in the capture phase, never during text composition.
        Assert.Contains("if (event.isComposing) return;", script, StringComparison.Ordinal);
        Assert.Contains("}, true);", script, StringComparison.Ordinal);
        // What the list announces comes from its history entry's state, never the address.
        Assert.DoesNotContain("location.hash.indexOf", script, StringComparison.Ordinal);
        Assert.Contains("history.replaceState(state, \"\", returnTo);", script, StringComparison.Ordinal);
        Assert.Contains("toList({ dwDelete: \"deleted\", name: intent.name });", script, StringComparison.Ordinal);
        Assert.Contains("toList({ dwDelete: \"missing\" });", script, StringComparison.Ordinal);
        // The note is taken off the history entry as soon as the list has read it.
        Assert.Contains("history.replaceState(null, \"\", location.pathname + location.search + location.hash);", script, StringComparison.Ordinal);
        // An unclear answer to the DELETE never says it did not go through.
        Assert.Contains("afterUnclearReply(deleteFailed, words(\"unclearDelete\"))", script, StringComparison.Ordinal);
        // Text goes in through textContent only.
        Assert.DoesNotContain(".innerText", script, StringComparison.Ordinal);
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

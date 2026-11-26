using System.Data.Common;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T07 (A7 Step 15): the audit log viewer as the server writes it
/// (AdminAuditPages): S-D1 the list at /admin/audit and S-D2 one entry at
/// /admin/audit/{id}. What the pages do in a browser (the script, keyboard,
/// axe, three engines) is in e2e/console/audit.spec.ts. Case ids:
/// TD-ADM-UI-na-X02, TD-ADM-UI-na-X03 (the nav entry), TD-ADM-101 (step 2:
/// read, never change), TD-ADM-053 (the IP), TD-ADM-064 (a read writes no
/// entry), TD-ADM-150 (nothing that must stay hidden on a page).
/// </summary>
[Collection("WebAppTests")]
public partial class AdminAuditPageTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = "io@aiknowhow.com";
    private const string ClientIp = "203.0.113.64";
    private const string Changed = "<span class=\"badge badge-changed\">Changed</span>";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditPageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- seeding ----

    private static int _days;

    /// <summary>A day no other test writes on: every test filters its list to its own.</summary>
    private sealed record OwnDay(DateOnly Date)
    {
        public static OwnDay New() => new(new DateOnly(1960, 1, 1).AddDays(7 * Interlocked.Increment(ref _days)));

        public string Text => Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        public string Query => $"from={Text}&to={Text}";

        /// <summary>Noon in New York or an hour off it, plus the given minutes: the same ET day either way.</summary>
        public DateTimeOffset At(int minutes) => new DateTimeOffset(Date.Year, Date.Month, Date.Day, 17, 0, 0, TimeSpan.Zero).AddMinutes(minutes);
    }

    /// <summary>
    /// Writes one entry. <paramref name="alter"/> changes the row before it is written (the log takes
    /// no change afterwards), past the write path's own rules: what a row could hold if they were bypassed.
    /// </summary>
    private async Task<AdminAuditEntry> SeedAsync(Func<IAdminAuditRecorder, AdminAuditEntry> make, Action<AdminAuditEntry>? alter = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var entry = make(scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>());
        alter?.Invoke(entry);
        db.AdminAuditLog.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    private async Task<Guid> SessionAsync(IServiceProvider? services = null, string email = OwnerEmail, bool secondStepDone = true)
    {
        using var scope = (services ?? _factory.Services).CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip");
        if (secondStepDone)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static HttpRequestMessage Get(string path, Guid? session)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (session is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return request;
    }

    private async Task<(HttpResponseMessage Response, string Html)> PageAsync(string path, Guid? session, WebApplicationFactory<Program>? factory = null)
    {
        var client = (factory ?? _factory).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var response = await client.SendAsync(Get(path, session));
        return (response, await response.Content.ReadAsStringAsync());
    }

    private static string Header(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values) ? string.Join(",", values)
            : response.Content.Headers.TryGetValues(name, out var content) ? string.Join(",", content) : string.Empty;

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", string.Empty));

    private static string[] HeaderTexts(string html)
        => Regex.Matches(html, "<th scope=\"col\"[^>]*>(.*?)</th>")
            .Select(m => Text(Regex.Replace(m.Groups[1].Value, "<span class=\"visually-hidden\"[^>]*>.*?</span>", string.Empty)).Trim())
            .ToArray();

    private static string[] Rows(string html)
        => Regex.Matches(html, "<tbody>(.*?)</tbody>", RegexOptions.Singleline).Single().Groups[1].Value
            .Split("</tr>", StringSplitOptions.RemoveEmptyEntries);

    private static string[] Cells(string row)
        => Regex.Matches(row, "<td>(.*?)</td>", RegexOptions.Singleline).Select(m => m.Groups[1].Value).ToArray();

    private static string Canonical(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "data-canonical=\"([^\"]*)\"").Groups[1].Value);

    private static string Range(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "<p id=\"list-range\" class=\"range\">([^<]*)</p>").Groups[1].Value);

    private static string Panel(string html, string id)
        => Regex.Match(html, $"<div id=\"{id}\" class=\"snapshot\">(.*?)</div>(?=<div id=\"snapshot-after\"|</div></section>)", RegexOptions.Singleline).Groups[1].Value;

    // ---- S-D1: what the list is ----

    [Fact]
    public async Task TdAdmUiNaX02_TheList_HasItsColumnsFiltersAndPaging_OnTheSharedShell_AndNoSearchBox()
    {
        var session = await SessionAsync();
        var (response, html) = await PageAsync("/admin/audit", session);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        var nonce = Regex.Match(html, "nonce=\"([^\"]+)\"").Groups[1].Value;
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce), Header(response, "Content-Security-Policy"));
        var text = WebUtility.HtmlDecode(html);
        Assert.Contains("<title>Audit log · Dealoware admin</title>", text, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<h1[ >]"));
        Assert.Contains("<h1 id=\"page-heading\">Audit log</h1>", html, StringComparison.Ordinal);
        Assert.Contains(
            "<p class=\"lede\">Read-only record of admin authentication and entity changes. This log cannot be edited or deleted.</p>",
            html,
            StringComparison.Ordinal);

        // TD-ADM-UI-na-X03: the nav lists Audit log, between Offers and Security settings, and marks it.
        var links = Regex.Matches(html, "<li><a href=\"([^\"]+)\"( aria-current=\"page\")?>([^<]+)</a></li>");
        Assert.Equal(
            new[] { "Stats", "Participants", "Artifacts", "Negotiations", "Offers", "Audit log", "Security settings" },
            links.Select(m => m.Groups[3].Value).ToArray());
        Assert.Equal("/admin/audit", Assert.Single(links, m => m.Groups[2].Success).Groups[1].Value);

        // The columns, in order; the one order the log has is on the Time heading.
        Assert.Equal(new[] { "Time (ET)", "Actor", "Action", "Entity type", "Entity ID", "Outcome" }, HeaderTexts(html));
        Assert.Contains("<th scope=\"col\" aria-sort=\"descending\">Time (ET)<span class=\"visually-hidden\"> (sorted descending)</span></th>", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "aria-sort="));
        Assert.DoesNotContain("sort-button", html, StringComparison.Ordinal);
        Assert.Contains("<caption id=\"list-caption\">Audit log</caption>", html, StringComparison.Ordinal);

        // Filters: every action of the fixed list as a checkbox, the entity type, the range of days.
        var boxes = Regex.Matches(html, "<li><input type=\"checkbox\" id=\"(action-[a-z0-9_-]+)\" name=\"action\" value=\"([a-z0-9._]+)\" data-auto><label for=\"\\1\">\\2</label></li>");
        Assert.Equal(AdminAuditOutcome.Actions, boxes.Select(m => m.Groups[2].Value).ToArray());
        Assert.Equal(boxes.Count, boxes.Select(m => m.Groups[1].Value).Distinct().Count());
        Assert.Contains("<fieldset class=\"field statuses wide\"><legend>Action</legend>", html, StringComparison.Ordinal);
        Assert.Contains(
            "<label for=\"entityType\">Entity type</label><select id=\"entityType\" name=\"entityType\" data-auto>"
            + "<option value=\"\">All types</option><option value=\"Participant\">Participant</option><option value=\"Artifact\">Artifact</option>"
            + "<option value=\"Negotiation\">Negotiation</option><option value=\"Offer\">Offer</option></select>",
            html,
            StringComparison.Ordinal);
        Assert.Contains("<label for=\"from\">From</label><input type=\"date\" id=\"from\" name=\"from\" value=\"\">", html, StringComparison.Ordinal);
        Assert.Contains("<label for=\"to\">To</label><input type=\"date\" id=\"to\" name=\"to\" value=\"\">", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"submit\" id=\"apply-filters\">Apply filters</button>", html, StringComparison.Ordinal);

        // Paging: 50 (chosen), 100, 200 only.
        Assert.Contains(
            "<label for=\"limit\">Page size</label><select id=\"limit\" name=\"limit\" data-auto>"
            + "<option value=\"50\" selected>50</option><option value=\"100\">100</option><option value=\"200\">200</option></select>",
            html,
            StringComparison.Ordinal);

        // No search box (test design OQ15), no actor filter, no sort control: only these fields go to the server.
        Assert.DoesNotContain("type=\"search\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"text\"", html, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "action", "dw_csrf", "entityType", "from", "limit", "to" },
            Regex.Matches(html, " name=\"([A-Za-z_]+)\"").Select(m => m.Groups[1].Value).Where(n => n != "viewport" && n != "dw-csrf").Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray());

        // The shell's scripts, then this page's own; nothing inline; one live region.
        Assert.Equal(
            new[] { "/admin/auth/admin-request.js", "/admin/assets/console.js", "/admin/assets/session-timer.js", "/admin/assets/console-audit.js" },
            Regex.Matches(html, "<script src=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Equal(Regex.Matches(html, "<script src=").Count, Regex.Matches(html, "<script").Count);
        Assert.Single(Regex.Matches(html, "aria-live="));
        Assert.DoesNotContain(" style=", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" onclick", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheList_ReadsExactlyTheKeysTheWayBackAfterSignInKeeps()
    {
        Assert.Equal(AdminReturnQuery.KeysFor(AdminAuditPages.ListPath), AdminAuditPages.QueryKeys);
        Assert.Equal("/admin/audit", AdminAuditPages.ListPath);
        Assert.Equal(AdminAuditListCriteria.DefaultLimit, AdminAuditState.PageSizes[0]);
        Assert.Equal(AdminAuditListCriteria.MaxLimit, AdminAuditState.PageSizes[^1]);
    }

    // ---- S-D1: what the list shows ----

    [Fact]
    public async Task Rows_ShowTimeActorActionEntityAndOutcome_NewestFirst_AndNeverTheIp()
    {
        var day = OwnDay.New();
        var typed = $"typed.at.sign-in.{Guid.NewGuid():N}@example.test";
        var person = Participant.Create($"audit-{Guid.NewGuid():N}");
        var other = Participant.Create($"audit-{Guid.NewGuid():N}");
        var gone = Participant.Create($"audit-gone-{Guid.NewGuid():N}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.AddRange(person, other, gone);
            await db.SaveChangesAsync();
            await db.Participants.Where(p => p.Id == gone.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
        }

        var never = Guid.NewGuid();
        var failed = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, typed, ClientIp, AdminAuditActions.ReasonBadPassword, day.At(1)));
        var locked = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, typed, ClientIp, AdminAuditActions.ReasonLocked, day.At(2)));
        var edit = await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Participant", person.Id,
            "{\"DisplayName\":\"SNAPSHOT-ONLY-ON-THE-ENTRY-PAGE\"}", "{\"DisplayName\":\"After\"}", timestamp: day.At(3)));
        var deleted = await SeedAsync(r => r.Entity(AdminAuditActions.EntityDelete, OwnerEmail, ClientIp, "Participant", gone.Id, "{\"DisplayName\":\"x\"}", timestamp: day.At(4)));
        var orphan = await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Offer", never, timestamp: day.At(5)));
        var noActor = await SeedAsync(r => r.Auth(AdminAuditActions.ResetCompleteFailed, string.Empty, ClientIp, "invalid_link", day.At(6)));
        var session = await SessionAsync();

        var (response, html) = await PageAsync($"/admin/audit?{day.Query}", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = Rows(html);
        // Newest first.
        Assert.Equal(
            new[] { noActor.Id, orphan.Id, deleted.Id, edit.Id, locked.Id, failed.Id },
            rows.Select(r => Guid.Parse(Regex.Match(r, "href=\"/admin/audit/([0-9a-f-]{36})").Groups[1].Value)).ToArray());
        Assert.Equal("Showing 1\u20136 of 6", Range(html));
        Assert.Contains("data-announce=\"6 results\"", html, StringComparison.Ordinal);

        var cells = Cells(rows[5]);
        Assert.Equal(6, cells.Length);
        // Time in ET with its label, the UTC value in its tooltip.
        Assert.Matches("^<time class=\"ts\" datetime=\"\\d{4}-\\d{2}-\\d{2}T17:01:00Z\" tabindex=\"0\" title=\"\\d{4}-\\d{2}-\\d{2} 17:01 UTC\" data-utc=\"\\d{4}-\\d{2}-\\d{2} 17:01 UTC\">[A-Z][a-z]{2} \\d{1,2}, \\d{4}, (12|1):01 PM ET</time>$", cells[0]);
        // The actor: what was typed at the failed sign-in, as text.
        Assert.Equal(typed, cells[1]);
        // The action opens the entry, keeping the list's state.
        Assert.Equal($"<a class=\"entry\" href=\"/admin/audit/{failed.Id:D}?from={day.Text}&amp;to={day.Text}\">login_failure</a>", cells[2]);
        // An authentication event names no record.
        Assert.Equal("—", cells[3]);
        Assert.Equal("—", cells[4]);
        Assert.Equal("<span class=\"badge badge-failure\">Failure</span>", cells[5]);

        Assert.Equal("<span class=\"badge badge-denied\">Denied</span>", Cells(rows[4])[5]);
        Assert.Equal("—", Cells(rows[0])[1]);

        // A record that exists is a link to it; a soft-deleted one and one that never was open nothing.
        cells = Cells(rows[3]);
        Assert.Equal(OwnerEmail, cells[1]);
        Assert.Equal("Participant", cells[3]);
        Assert.Equal($"<a class=\"id\" href=\"/admin/participants/{person.Id:D}\">{person.Id:D}</a>", cells[4]);
        Assert.Equal("<span class=\"badge badge-success\">Success</span>", cells[5]);
        Assert.Equal($"<span class=\"id\">{gone.Id:D}</span> <span class=\"badge badge-deleted\">Deleted</span>", Cells(rows[2])[4]);
        Assert.Equal($"<span class=\"id\">{never:D}</span>", Cells(rows[1])[4]);
        Assert.DoesNotContain($"href=\"/admin/participants/{gone.Id:D}", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"/admin/offers/{never:D}", html, StringComparison.Ordinal);

        // TD-ADM-053: no address, no hash and no part of it in the list; no snapshot, no reason class either.
        Assert.DoesNotContain(ClientIp, html, StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113", html, StringComparison.Ordinal);
        Assert.DoesNotContain(failed.IpHmac, WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain(failed.IpHmac[..AdminAuditEndpoints.IpHmacPrefixLength], WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain("SNAPSHOT-ONLY-ON-THE-ENTRY-PAGE", html, StringComparison.Ordinal);
        Assert.DoesNotContain("bad_password", html, StringComparison.Ordinal);
        // The typed address is in its cell, never in an address, a title or an attribute.
        Assert.Equal(2, Regex.Matches(html, Regex.Escape(typed)).Count);
        Assert.DoesNotContain(typed, Regex.Replace(html, "<td>[^<]*</td>", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Filters_ChangeTheServerQuery_AndEachChipRemovesOnlyItsOwn()
    {
        var day = OwnDay.New();
        var next = new OwnDay(day.Date.AddDays(1));
        var offer = Guid.NewGuid();
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginSuccess, OwnerEmail, ClientIp, null, day.At(1)));
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuditActions.ReasonBadPassword, day.At(2)));
        await SeedAsync(r => r.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp, null, day.At(3)));
        await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Offer", offer, timestamp: day.At(4)));
        await SeedAsync(r => r.Entity(AdminAuditActions.EntityDelete, OwnerEmail, ClientIp, "Negotiation", Guid.NewGuid(), timestamp: day.At(5)));
        // The first and the last minute of the next ET day (04:00 and 03:59 UTC bound a summer day, 05:00 and 04:59 a winter one).
        var startOfNext = EasternTime.StartOfDayUtc(next.Date);
        await SeedAsync(r => r.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp, null, startOfNext));
        await SeedAsync(r => r.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp, null, startOfNext.AddSeconds(-1)));
        var session = await SessionAsync();

        async Task<string[]> ActionsAsync(string query)
        {
            var (response, html) = await PageAsync("/admin/audit?" + query, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return html.Contains("id=\"list-miss\"", StringComparison.Ordinal)
                ? []
                : Rows(html).Select(r => Text(Cells(r)[2])).ToArray();
        }

        // The range of days is ET calendar days, both ends included.
        Assert.Equal(new[] { "sign_out", "entity_delete", "entity_edit", "sign_out", "login_failure", "login_success" }, await ActionsAsync(day.Query));
        Assert.Equal(new[] { "sign_out" }, await ActionsAsync(next.Query));
        Assert.Equal(7, (await ActionsAsync($"from={day.Text}&to={next.Text}")).Length);
        // One action, several actions, an entity type, and both together.
        Assert.Equal(new[] { "sign_out", "sign_out" }, await ActionsAsync($"{day.Query}&action=sign_out"));
        Assert.Equal(new[] { "sign_out", "sign_out", "login_failure" }, await ActionsAsync($"{day.Query}&action=sign_out,login_failure"));
        Assert.Equal(new[] { "entity_edit" }, await ActionsAsync($"{day.Query}&entityType=Offer"));
        Assert.Equal(new[] { "entity_delete" }, await ActionsAsync($"{day.Query}&entityType=Negotiation&action=entity_edit,entity_delete"));
        Assert.Empty(await ActionsAsync($"{day.Query}&entityType=Artifact"));

        var (_, page) = await PageAsync($"/admin/audit?action=sign_out,login_failure&entityType=Offer&{day.Query}&limit=100&offset=100", session);
        Assert.Equal($"/admin/audit?action=login_failure%2Csign_out&entityType=Offer&from={day.Text}&to={day.Text}&limit=100", Canonical(page));
        Assert.Contains("id=\"action-login_failure\" name=\"action\" value=\"login_failure\" checked data-auto>", page, StringComparison.Ordinal);
        Assert.Contains("id=\"action-sign_out\" name=\"action\" value=\"sign_out\" checked data-auto>", page, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(page, " checked data-auto>").Count);
        Assert.Contains("<option value=\"Offer\" selected>Offer</option>", page, StringComparison.Ordinal);
        Assert.Contains($"id=\"from\" name=\"from\" value=\"{day.Text}\"", page, StringComparison.Ordinal);
        Assert.Contains("<option value=\"100\" selected>100</option>", page, StringComparison.Ordinal);

        var chips = Regex.Matches(page, "<li class=\"chip\"><span>([^<]+)</span><a href=\"([^\"]+)\" aria-label=\"([^\"]+)\" data-focus=\"main\">Remove</a></li>")
            .Select(m => (Text: WebUtility.HtmlDecode(m.Groups[1].Value), Href: WebUtility.HtmlDecode(m.Groups[2].Value), Name: m.Groups[3].Value))
            .ToArray();
        var shownDay = day.Date.ToString("MMM d, yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal(
            new[] { "Action: login_failure, sign_out", "Entity type: Offer", $"From: {shownDay}", $"To: {shownDay}" },
            chips.Select(c => c.Text).ToArray());
        Assert.Equal(
            new[] { "Remove Action filter", "Remove Entity type filter", "Remove From filter", "Remove To filter" },
            chips.Select(c => c.Name).ToArray());
        Assert.Equal($"/admin/audit?entityType=Offer&from={day.Text}&to={day.Text}&limit=100", chips[0].Href);
        Assert.Equal($"/admin/audit?action=login_failure%2Csign_out&from={day.Text}&to={day.Text}&limit=100", chips[1].Href);
        Assert.Equal($"/admin/audit?action=login_failure%2Csign_out&entityType=Offer&to={day.Text}&limit=100", chips[2].Href);
        Assert.Contains("<a id=\"clear-all\" class=\"button-link secondary\" href=\"/admin/audit?limit=100\" data-focus=\"main\">Clear all filters</a>", page, StringComparison.Ordinal);

        // A filter miss says so, with Clear filters; it is not an error and not the empty log.
        var (miss, missPage) = await PageAsync($"/admin/audit?{day.Query}&entityType=Artifact&limit=200", session);
        Assert.Equal(HttpStatusCode.OK, miss.StatusCode);
        Assert.Contains("<p id=\"list-miss\">No results match these filters.</p>", missPage, StringComparison.Ordinal);
        Assert.Contains("<a id=\"clear-filters\" class=\"button-link\" href=\"/admin/audit?limit=200\" data-focus=\"main\">Clear filters</a>", missPage, StringComparison.Ordinal);
        Assert.Contains("data-announce=\"0 results\"", missPage, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"pager\"", missPage, StringComparison.Ordinal);
        Assert.DoesNotContain("No audit entries yet.", missPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhatTheListDoesNotTake_IsDropped_AndTheListShows_NeverAnError()
    {
        var session = await SessionAsync();

        // An action off the fixed list, another entity type, a search, an actor, a sort, a page size over 200.
        var (response, html) = await PageAsync(
            "/admin/audit?action=nope,drop_table&entityType=Strategy&q=findme&actorEmail=someone%40example.test&sort=actorEmail&limit=500&offset=-2&bogus=%3Cx%3E", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/admin/audit?limit=200", Canonical(html));
        Assert.Contains("<option value=\"200\" selected>200</option>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"chips\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" checked", html, StringComparison.Ordinal);
        Assert.DoesNotContain("findme", html, StringComparison.Ordinal);
        Assert.DoesNotContain("someone@example.test", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain("&lt;x&gt;", html, StringComparison.Ordinal);

        // A known name stays, in any letter case, in the list's order; the rest goes. A size between two is the next one up.
        (_, html) = await PageAsync("/admin/audit?action=SIGN_OUT,nope,login_failure&entityType=offer&limit=75", session);
        Assert.Equal("/admin/audit?action=login_failure%2Csign_out&entityType=Offer&limit=100", Canonical(html));

        // A form sent without JavaScript repeats action and sends its empty boxes: the same list.
        (_, html) = await PageAsync("/admin/audit?action=sign_out&action=login_success&entityType=&from=&to=&limit=50", session);
        Assert.Equal("/admin/audit?action=login_success%2Csign_out", Canonical(html));

        // Days outside the years the list takes, and a day that is not one.
        (response, html) = await PageAsync("/admin/audit?from=0001-01-01&to=9999-12-31", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/admin/audit", Canonical(html));
        (response, html) = await PageAsync("/admin/audit?from=yesterday&to=2026-13-45", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/admin/audit", Canonical(html));

        // Hand-made junk in every key: still the page.
        (response, _) = await PageAsync("/admin/audit?action=%00%01&entityType=%27--&from=%E2%80%8B&to=1e9&offset=1e9&limit=-5", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        (response, _) = await PageAsync("/admin/audit?%zz=%&action=%", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Paging_Is50ByDefault_And200AtMost_WithTheRangeAndThePages_AndAPagePastTheEndShowsTheLast()
    {
        var day = OwnDay.New();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var recorder = scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>();
            for (var i = 0; i < 230; i++)
                db.AdminAuditLog.Add(recorder.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp, null, day.At(0).AddSeconds(i)));
            await db.SaveChangesAsync();
        }

        var session = await SessionAsync();
        var (_, html) = await PageAsync($"/admin/audit?{day.Query}", session);
        Assert.Equal("Showing 1\u201350 of 230", Range(html));
        Assert.Equal(50, Rows(html).Length);
        Assert.Contains("<span class=\"page-off\">Previous</span>", html, StringComparison.Ordinal);
        Assert.Contains("data-announce=\"230 results\"", html, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/audit?{day.Query}&offset=50", session);
        Assert.Equal("Showing 51\u2013100 of 230", Range(html));
        Assert.Contains($"<a id=\"page-prev\" href=\"/admin/audit?from={day.Text}&amp;to={day.Text}\" rel=\"prev\" data-focus=\"page-prev\">Previous</a>", html, StringComparison.Ordinal);
        Assert.Contains($"<a id=\"page-next\" href=\"/admin/audit?from={day.Text}&amp;to={day.Text}&amp;offset=100\" rel=\"next\" data-focus=\"page-next\">Next</a>", html, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "Page 1", "Page 2", "Page 3", "Page 5" },
            Regex.Matches(html, "<a id=\"page-\\d+\" href=\"[^\"]+\" aria-label=\"(Page \\d+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("aria-label=\"Page 2\" aria-current=\"page\"", html, StringComparison.Ordinal);
        // The second page continues where the first stopped: newest first, none twice.
        var times = Regex.Matches(html, "datetime=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(times.OrderByDescending(t => t, StringComparer.Ordinal), times);
        Assert.EndsWith("17:02:59Z", times[0], StringComparison.Ordinal);

        // 200 a page at most, whatever the address asks for.
        (_, html) = await PageAsync($"/admin/audit?{day.Query}&limit=5000", session);
        Assert.Equal("Showing 1\u2013200 of 230", Range(html));
        Assert.Equal(200, Rows(html).Length);
        Assert.Equal($"/admin/audit?from={day.Text}&to={day.Text}&limit=200", Canonical(html));

        // Past the last page: the last page, and the address says so. An offset between pages starts its page.
        (_, html) = await PageAsync($"/admin/audit?{day.Query}&offset=99999&limit=100", session);
        Assert.Equal("Showing 201\u2013230 of 230", Range(html));
        Assert.Equal($"/admin/audit?from={day.Text}&to={day.Text}&offset=200&limit=100", Canonical(html));
        Assert.Contains("<span class=\"page-off\">Next</span>", html, StringComparison.Ordinal);
        (_, html) = await PageAsync($"/admin/audit?{day.Query}&offset=173&limit=100", session);
        Assert.Equal("Showing 101\u2013200 of 230", Range(html));
    }

    [Fact]
    public async Task TdAdm064_ReadingTheLogOrAnEntry_WritesNoEntry()
    {
        var seeded = await SeedAsync(r => r.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp));
        var session = await SessionAsync();
        int Count()
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminAuditLog.Count();
        }

        var before = Count();
        foreach (var path in new[] { "/admin/audit", "/admin/audit?action=sign_out&limit=200", $"/admin/audit/{seeded.Id:D}", $"/admin/audit/{Guid.NewGuid():D}" })
            await PageAsync(path, session);
        Assert.Equal(before, Count());
    }

    // ---- without a session ----

    [Fact]
    public async Task WithoutASession_NoPageNoEntryNoScript_AndTheListComesBackAfterSignInWithItsKeptKeysOnly()
    {
        var marker = $"only-with-a-session-{Guid.NewGuid():N}";
        var seeded = await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, marker, ClientIp, "Offer", Guid.NewGuid(), "{\"OfferTerms\":\"" + marker + "\"}"));

        // No cookie, a session nobody has, one whose second step is not done, and one of another address.
        foreach (Guid? session in new Guid?[]
                 {
                     null,
                     Guid.NewGuid(),
                     await SessionAsync(secondStepDone: false),
                     await SessionAsync(email: "other@example.com")
                 })
        {
            foreach (var path in new[]
                     {
                         "/admin/audit", "/admin/audit?action=entity_edit", $"/admin/audit/{seeded.Id:D}",
                         $"/admin/audit/{Guid.NewGuid():D}", "/admin/audit/not-an-id"
                     })
            {
                var (response, html) = await PageAsync(path, session);
                Assert.Equal(HttpStatusCode.Found, response.StatusCode);
                Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
                Assert.Equal(string.Empty, html);
            }

            var (script, body) = await PageAsync("/admin/assets/console-audit.js", session);
            Assert.Equal(HttpStatusCode.Unauthorized, script.StatusCode);
            Assert.Equal("{\"error\":\"Unauthorized\"}", body);
        }

        // TD-ADM-177: the list's own keys are kept for the way back; a search, an actor and a sort are not.
        var (asked, _) = await PageAsync("/admin/audit?action=sign_out,login_failure&entityType=Offer&q=x&actorEmail=a%40example.test&sort=action&limit=100", null);
        var kept = Assert.Single(asked.Headers.GetValues("Set-Cookie")).Split(';')[0];
        Assert.Equal("dw_admin_return=" + Uri.EscapeDataString("/admin/audit?action=login_failure%2Csign_out&entityType=Offer&limit=100"), kept);
        // An entry comes back as its page, without a query.
        (asked, _) = await PageAsync($"/admin/audit/{seeded.Id:D}?action=sign_out", null);
        Assert.Equal("dw_admin_return=" + Uri.EscapeDataString($"/admin/audit/{seeded.Id:D}"), Assert.Single(asked.Headers.GetValues("Set-Cookie")).Split(';')[0]);
    }

    // ---- S-D2: one entry ----

    [Fact]
    public async Task TdAdm101_AnEntry_ShowsItsFields_BeforeAndAfterWithChangedOnWhatDiffers_AndTheIpPrefixHereOnly()
    {
        var person = Participant.Create($"audit-{Guid.NewGuid():N}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.Add(person);
            await db.SaveChangesAsync();
        }

        var at = new DateTimeOffset(2026, 7, 1, 16, 30, 0, TimeSpan.Zero);
        var seeded = await SeedAsync(r => r.Entity(
            AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Participant", person.Id,
            beforeSnapshot: "{\"DisplayName\":\"Old name\",\"ContactEmail\":\"same@example.test\",\"ParticipantActive\":true,\"Note\":\"line one\\nline two\"}",
            afterSnapshot: "{\"DisplayName\":\"New name\",\"ContactEmail\":\"same@example.test\",\"ParticipantActive\":false,\"EntityVersion\":2}",
            timestamp: at));
        var session = await SessionAsync();

        var (response, html) = await PageAsync($"/admin/audit/{seeded.Id:D}?action=entity_edit&entityType=Participant&offset=100&limit=100", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        var nonce = Regex.Match(html, "nonce=\"([^\"]+)\"").Groups[1].Value;
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce), Header(response, "Content-Security-Policy"));
        // The title is the screen's name: nothing of the entry is in it.
        Assert.Contains("<title>Audit entry · Dealoware admin</title>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("<h1 id=\"page-heading\">Audit entry</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<li><a href=\"/admin/audit\" aria-current=\"page\">Audit log</a></li>", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "aria-current="));
        // Back to the list as it was.
        Assert.Contains("<a id=\"back-to-list\" href=\"/admin/audit?action=entity_edit&amp;entityType=Participant&amp;offset=100&amp;limit=100\">Back to audit log</a>", html, StringComparison.Ordinal);

        var entry = Regex.Match(html, "<dl id=\"entry\" class=\"details audit-details\">(.*?)</dl>", RegexOptions.Singleline).Groups[1].Value;
        Assert.Equal(
            new[] { "Time (ET)", "Actor", "Action", "Entity type", "Entity ID", "Outcome", "IP HMAC prefix" },
            Regex.Matches(entry, "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("<time class=\"ts\" datetime=\"2026-07-01T16:30:00Z\" tabindex=\"0\" title=\"2026-07-01 16:30 UTC\" data-utc=\"2026-07-01 16:30 UTC\">Jul 1, 2026, 12:30 PM ET</time>", entry, StringComparison.Ordinal);
        Assert.Contains($"<dt>Actor</dt><dd>{OwnerEmail}</dd>", entry, StringComparison.Ordinal);
        Assert.Contains("<dt>Action</dt><dd>entity_edit</dd>", entry, StringComparison.Ordinal);
        Assert.Contains("<dt>Entity type</dt><dd>Participant</dd>", entry, StringComparison.Ordinal);
        Assert.Contains($"<dt>Entity ID</dt><dd><a class=\"id\" href=\"/admin/participants/{person.Id:D}\">{person.Id:D}</a></dd>", entry, StringComparison.Ordinal);
        Assert.Contains("<dt>Outcome</dt><dd><span class=\"badge badge-success\">Success</span></dd>", entry, StringComparison.Ordinal);

        // TD-ADM-053: the first 12 characters of the keyed hash, and no more of it.
        var prefix = seeded.IpHmac[..AdminAuditEndpoints.IpHmacPrefixLength];
        Assert.Contains($"<dt>IP HMAC prefix</dt><dd><span id=\"ip-prefix\" class=\"id\">{AdminSignedOutPage.Encode(prefix)}</span></dd>", entry, StringComparison.Ordinal);
        var shown = WebUtility.HtmlDecode(html);
        Assert.Single(Regex.Matches(shown, Regex.Escape(prefix)));
        Assert.DoesNotContain(seeded.IpHmac, shown, StringComparison.Ordinal);
        Assert.DoesNotContain(seeded.IpHmac[..(AdminAuditEndpoints.IpHmacPrefixLength + 1)], shown, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientIp, html, StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113", html, StringComparison.Ordinal);

        // Before and after: every field of either on both sides, "Changed" in words on those that differ.
        Assert.Contains("<h2 id=\"snapshots-heading\">Before and after</h2>", html, StringComparison.Ordinal);
        var before = Panel(html, "snapshot-before");
        var after = Panel(html, "snapshot-after");
        Assert.StartsWith("<h3>Before</h3><dl class=\"details\">", before, StringComparison.Ordinal);
        Assert.StartsWith("<h3>After</h3><dl class=\"details\">", after, StringComparison.Ordinal);
        var fields = new[] { "DisplayName", "ContactEmail", "ParticipantActive", "Note", "EntityVersion" };
        Assert.Equal(fields, Regex.Matches(before, "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Equal(fields, Regex.Matches(after, "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains($"<dt>DisplayName</dt><dd><span class=\"free-text\">Old name</span>{Changed}</dd>", before, StringComparison.Ordinal);
        Assert.Contains($"<dt>DisplayName</dt><dd><span class=\"free-text\">New name</span>{Changed}</dd>", after, StringComparison.Ordinal);
        Assert.Contains("<dt>ContactEmail</dt><dd><span class=\"free-text\">same@example.test</span></dd>", before, StringComparison.Ordinal);
        Assert.Contains("<dt>ContactEmail</dt><dd><span class=\"free-text\">same@example.test</span></dd>", after, StringComparison.Ordinal);
        Assert.Contains($"<dt>ParticipantActive</dt><dd><span class=\"free-text\">true</span>{Changed}</dd>", before, StringComparison.Ordinal);
        Assert.Contains($"<dt>ParticipantActive</dt><dd><span class=\"free-text\">false</span>{Changed}</dd>", after, StringComparison.Ordinal);
        // Text as it was written: the line break kept (the stylesheet shows it), nothing read as markup.
        Assert.Contains($"<dt>Note</dt><dd><span class=\"free-text\">{AdminSignedOutPage.Encode("line one\nline two")}</span>{Changed}</dd>", before, StringComparison.Ordinal);
        // A field one side does not have shows a dash there, and is changed.
        Assert.Contains($"<dt>Note</dt><dd><span class=\"free-text\">—</span>{Changed}</dd>", after, StringComparison.Ordinal);
        Assert.Contains($"<dt>EntityVersion</dt><dd><span class=\"free-text\">—</span>{Changed}</dd>", before, StringComparison.Ordinal);
        Assert.Contains($"<dt>EntityVersion</dt><dd><span class=\"free-text\">2</span>{Changed}</dd>", after, StringComparison.Ordinal);
        Assert.Equal(8, Regex.Matches(html, Regex.Escape(Changed)).Count);

        AssertReadOnly(html);
    }

    [Fact]
    public async Task AnEntry_WithoutSnapshots_HasNoBeforeAndAfter_AndOneWithOneSide_SaysSoOnTheOther()
    {
        var typed = $"typed.{Guid.NewGuid():N}@example.test";
        var failed = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, typed, ClientIp, AdminAuditActions.ReasonLocked));
        var gone = Guid.NewGuid();
        var deleted = await SeedAsync(r => r.Entity(AdminAuditActions.EntityDelete, OwnerEmail, ClientIp, "Offer", gone, beforeSnapshot: "{\"OfferTerms\":\"as it was\",\"OfferAmount\":12.5}"));
        var empty = await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Offer", gone, "{}", "{}"));
        var command = await SeedAsync(_ => AdminAuditEntry.CreateAuthEvent(AdminAuthEvents.BootstrapLinkIssued, OwnerEmail, AdminAuditEntry.CommandOrigin));
        var session = await SessionAsync();

        // An authentication event: its reason class and outcome; no snapshots section at all.
        var (response, html) = await PageAsync($"/admin/audit/{failed.Id:D}", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new[] { "Time (ET)", "Actor", "Action", "Entity type", "Entity ID", "Outcome", "Reason", "IP HMAC prefix" },
            Regex.Matches(html, "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains($"<dt>Actor</dt><dd>{typed}</dd>", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Entity type</dt><dd>—</dd>", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Entity ID</dt><dd>—</dd>", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Outcome</dt><dd><span class=\"badge badge-denied\">Denied</span></dd>", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Reason</dt><dd>locked</dd>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"snapshots\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Before", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Changed<", html, StringComparison.Ordinal);
        Assert.Contains("<a id=\"back-to-list\" href=\"/admin/audit\">Back to audit log</a>", html, StringComparison.Ordinal);
        // The typed address is text in its one place: not in the title, an address or an attribute.
        Assert.Single(Regex.Matches(html, Regex.Escape(typed)));
        AssertReadOnly(html);

        // A delete keeps what was there; the other side says it has nothing; nothing is marked changed.
        (_, html) = await PageAsync($"/admin/audit/{deleted.Id:D}", session);
        var before = Panel(html, "snapshot-before");
        Assert.Contains("<dt>OfferTerms</dt><dd><span class=\"free-text\">as it was</span></dd>", before, StringComparison.Ordinal);
        Assert.Contains("<dt>OfferAmount</dt><dd><span class=\"free-text\">12.5</span></dd>", before, StringComparison.Ordinal);
        Assert.Equal("<h3>After</h3><p class=\"none\">No allowed fields.</p>", Panel(html, "snapshot-after"));
        Assert.DoesNotContain(">Changed<", html, StringComparison.Ordinal);
        // The record is not there: its id is text, not a link.
        Assert.Contains($"<dt>Entity ID</dt><dd><span class=\"id\">{gone:D}</span></dd>", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"/admin/offers/{gone:D}", html, StringComparison.Ordinal);
        AssertReadOnly(html);

        // Two snapshots with no field in them.
        (_, html) = await PageAsync($"/admin/audit/{empty.Id:D}", session);
        Assert.Equal(2, Regex.Matches(html, "<p class=\"none\">No allowed fields.</p>").Count);

        // An entry written by an operator command has no address to hash: a dash, not the stored marker.
        Assert.Equal(AdminAuditEntry.CommandOrigin, command.IpHmac);
        (_, html) = await PageAsync($"/admin/audit/{command.Id:D}", session);
        Assert.Contains("<dt>IP HMAC prefix</dt><dd>—</dd>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"ip-prefix\"", html, StringComparison.Ordinal);
        var shownEntry = Regex.Match(html, "<dl id=\"entry\".*?</dl>", RegexOptions.Singleline).Value;
        Assert.DoesNotContain(AdminAuditEntry.CommandOrigin, shownEntry, StringComparison.Ordinal);
        Assert.Contains("<dt>Action</dt><dd>bootstrap_link_issued</dd>", shownEntry, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownOrMalformedId_IsS_D3_WithA404_AndShowsNothingOfTheLog()
    {
        var marker = $"never-on-s-d3-{Guid.NewGuid():N}";
        var seeded = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, marker, ClientIp, AdminAuditActions.ReasonBadPassword));
        var session = await SessionAsync();

        foreach (var path in new[]
                 {
                     $"/admin/audit/{Guid.NewGuid():D}",
                     $"/admin/audit/{Guid.NewGuid():D}?action=login_failure",
                     "/admin/audit/not-an-id",
                     "/admin/audit/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a5",
                     "/admin/audit/1",
                     $"/admin/audit/{seeded.Id:D}/edit",
                     $"/admin/audit/{seeded.Id:D}/delete",
                     "/admin/audit/new"
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var text = WebUtility.HtmlDecode(html);
            Assert.Contains("<h1 id=\"page-heading\">We can't find that page.</h1>", text, StringComparison.Ordinal);
            Assert.Contains("<a id=\"go-to-stats\" href=\"/admin/\">Go to Stats</a>", text, StringComparison.Ordinal);
            Assert.Contains("<title>Page not found · Dealoware admin</title>", text, StringComparison.Ordinal);
            Assert.DoesNotContain("aria-current=", html, StringComparison.Ordinal);
            Assert.DoesNotContain(marker, html, StringComparison.Ordinal);
            Assert.DoesNotContain("login_failure", html, StringComparison.Ordinal);
        }
    }

    // ---- read-only (TD-ADM-101 step 2) ----

    /// <summary>No control that could change anything: the only form, button and field are Sign out's.</summary>
    private static void AssertReadOnly(string html)
    {
        Assert.Equal("<form id=\"sign-out-form\"", Assert.Single(Regex.Matches(html, "<form[^>]*?(?= method)")).Value);
        Assert.Equal("id=\"sign-out\"", Assert.Single(Regex.Matches(html, "<button[^>]*>")).Value.Split(' ')[2]);
        Assert.Single(Regex.Matches(html, "<input "));
        Assert.DoesNotContain("<select", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<textarea", html, StringComparison.Ordinal);
        Assert.DoesNotContain("contenteditable", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("(?i)>\\s*(edit|delete|remove|save)\\b[^<]*</(a|button)>", html);
    }

    [Fact]
    public async Task TdAdm101_TheViewerOnlyReads_NoEditOrDeleteControl_AndNoMethodButGetIsAnswered()
    {
        var day = OwnDay.New();
        var seeded = await SeedAsync(r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Offer", Guid.NewGuid(), "{\"OfferTerms\":\"a\"}", "{\"OfferTerms\":\"b\"}", timestamp: day.At(0)));
        var session = await SessionAsync();

        // The list: its only forms are the GET filter form and Sign out; its only buttons Apply filters and Sign out.
        var (_, list) = await PageAsync($"/admin/audit?{day.Query}", session);
        Assert.Equal(
            new[] { "<form id=\"sign-out-form\" method=\"post\"", "<form id=\"list-form\" class=\"list-form\" method=\"get\"" },
            Regex.Matches(list, "<form[^>]*? method=\"[a-z]+\"").Select(m => m.Value).ToArray());
        Assert.Equal(
            new[] { "sign-out", "apply-filters" },
            Regex.Matches(list, "<button type=\"submit\" id=\"([a-z-]+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Equal(2, Regex.Matches(list, "<button").Count);
        Assert.DoesNotMatch("(?i)>\\s*(edit|delete|save)\\b[^<]*</(a|button)>", list);
        // The anti-forgery token is in the meta and the sign-out form only: the GET form sends none.
        var token = AdminPageAntiForgery.ReadToken(list);
        Assert.Equal(2, Regex.Matches(list, token).Count);
        Assert.DoesNotContain(token, Regex.Match(list, "<form id=\"list-form\".*?</form>", RegexOptions.Singleline).Value, StringComparison.Ordinal);

        var (_, entry) = await PageAsync($"/admin/audit/{seeded.Id:D}", session);
        AssertReadOnly(entry);

        // No page route takes anything but GET, with or without the anti-forgery token.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        using var signIn = await client.SendAsync(Get(AdminSignedOutExemptions.SignIn, null));
        var csrf = await AdminPageAntiForgery.ReadTokenAsync(signIn);
        var csrfCookie = signIn.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(AdminAntiForgeryCookie.Name + "=", StringComparison.Ordinal)).Split(';', 2)[0];
        foreach (var path in new[] { "/admin/audit", $"/admin/audit/{seeded.Id:D}" })
        {
            foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
            {
                var request = new HttpRequestMessage(method, path);
                request.Headers.Host = AdminHost;
                request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}; {csrfCookie}");
                request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, csrf);
                request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }

        using var check = _factory.Services.CreateScope();
        var stored = await check.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == seeded.Id);
        Assert.Equal("{\"OfferTerms\":\"a\"}", stored.BeforeSnapshot);
        Assert.Equal("{\"OfferTerms\":\"b\"}", stored.AfterSnapshot);
    }

    // ---- what a page may show ----

    [Fact]
    public async Task EveryValue_IsEncoded_OnTheListAndOnTheEntry()
    {
        var day = OwnDay.New();
        const string hostile = "<script>alert(1)</script>\"'&<img src=x>";
        var seeded = await SeedAsync(r => r.Entity(
            AdminAuditActions.EntityEdit, "actor" + hostile, ClientIp, "T" + hostile, Guid.NewGuid(),
            beforeSnapshot: System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?> { ["Name" + hostile] = "before" + hostile, ["Nested"] = new { Inner = hostile } }),
            afterSnapshot: System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?> { ["Name" + hostile] = "after" + hostile }),
            timestamp: day.At(0)),
            // An action and a reason class as no code writes them, straight into the row.
            entry =>
            {
                Set(entry, nameof(AdminAuditEntry.Action), "a" + hostile);
                Set(entry, nameof(AdminAuditEntry.ReasonClass), "r" + hostile);
            });

        var session = await SessionAsync();
        foreach (var path in new[] { $"/admin/audit?{day.Query}", $"/admin/audit/{seeded.Id:D}" })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<img src=x>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("\"'&<", html, StringComparison.Ordinal);
            Assert.Contains("actor&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
            Assert.Contains("a&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
            Assert.Contains("T&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
            Assert.Equal(Regex.Matches(html, "<script src=").Count, Regex.Matches(html, "<script").Count);
        }

        var (_, entry) = await PageAsync($"/admin/audit/{seeded.Id:D}", session);
        Assert.Contains("<dt>Name&lt;script&gt;alert(1)&lt;/script&gt;", entry, StringComparison.Ordinal);
        Assert.Contains("<span class=\"free-text\">before&lt;script&gt;alert(1)&lt;/script&gt;", entry, StringComparison.Ordinal);
        Assert.Contains("<span class=\"free-text\">after&lt;script&gt;alert(1)&lt;/script&gt;", entry, StringComparison.Ordinal);
        Assert.Contains("<dt>Reason</dt><dd>r&lt;script&gt;", entry, StringComparison.Ordinal);
        // An entity type that is not one of the four names no record page.
        Assert.DoesNotContain("<a class=\"id\"", entry, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TdAdm150_ASnapshotKeyThatLooksLikeACredential_OrNamesAFieldTheOwnerMayNotRead_NeverReachesAPage()
    {
        var day = OwnDay.New();
        const string hidden = "MUST-STAY-HIDDEN";
        var seeded = await SeedAsync(
            r => r.Entity(AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Participant", Guid.NewGuid(), "{}", "{}", timestamp: day.At(0)),
            // Straight into the row, past the write path that would have stripped them.
            entry =>
            {
                Set(entry, nameof(AdminAuditEntry.BeforeSnapshot),
                    "{\"DisplayName\":\"Shown before\",\"passwordHash\":\"" + hidden + "-1\",\"totpSeed\":\"" + hidden + "-2\","
                    + "\"api_key\":\"" + hidden + "-3\",\"nested\":{\"recoveryCode\":\"" + hidden + "-4\",\"kept\":\"nested kept\"},"
                    + "\"StrategyBody\":\"" + hidden + "-5\",\"sessionToken\":\"" + hidden + "-6\","
                    + "\"strategy_body\":\"" + hidden + "-10\",\"STRATEGYBODY\":\"" + hidden + "-11\"}");
                Set(entry, nameof(AdminAuditEntry.AfterSnapshot),
                    "{\"DisplayName\":\"Shown after\",\"Authorization\":\"Bearer " + hidden + "-7\",\"strategyBody\":\"" + hidden + "-8\",\"ipHmacKey\":\"" + hidden + "-9\"}");
            });
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Contains(hidden, (await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == seeded.Id)).BeforeSnapshot, StringComparison.Ordinal);
        }

        var session = await SessionAsync();
        var (response, entry) = await PageAsync($"/admin/audit/{seeded.Id:D}", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, list) = await PageAsync($"/admin/audit?{day.Query}", session);
        Assert.Contains($"/admin/audit/{seeded.Id:D}", list, StringComparison.Ordinal);
        foreach (var html in new[] { entry, list })
        {
            Assert.DoesNotContain(hidden, html, StringComparison.Ordinal);
            foreach (var key in new[] { "passwordHash", "totpSeed", "api_key", "recoveryCode", "StrategyBody", "strategy_body", "sessionToken", "Authorization", "ipHmacKey" })
                Assert.DoesNotContain(key, html, StringComparison.OrdinalIgnoreCase);
        }

        // What may be read is still there, and only that.
        Assert.Equal(new[] { "DisplayName", "nested" }, Regex.Matches(Panel(entry, "snapshot-before"), "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("<span class=\"free-text\">Shown before</span>", entry, StringComparison.Ordinal);
        Assert.Contains("<span class=\"free-text\">Shown after</span>", entry, StringComparison.Ordinal);
        Assert.Contains("nested kept", entry, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TdAdm150_AFailedStepUp_ShowsNoReason_SoTheLogDoesNotTellWhetherAGuessedPasswordWasRight()
    {
        var day = OwnDay.New();
        var withheld = new List<AdminAuditEntry>();
        foreach (var (action, reason) in new[]
                 {
                     (AdminAuditActions.StepUpFailed, AdminAuthReason.Bad2Fa),
                     (AdminAuditActions.StepUpFailed, AdminAuthReason.BadPassword),
                     (AdminAuditActions.PasswordChangeFailed, AdminAuthReason.Bad2Fa),
                     (AdminAuditActions.PasswordChangeFailed, AdminAuthReason.BadPassword)
                 })
        {
            var minute = withheld.Count;
            withheld.Add(await SeedAsync(r => r.Auth(action, OwnerEmail, ClientIp, reason, day.At(minute))));
        }

        // Shown as before: a step-up refused by a lock, and a sign-in whose code was wrong.
        var lockedStepUp = await SeedAsync(r => r.Auth(AdminAuditActions.StepUpFailed, OwnerEmail, ClientIp, AdminAuthReason.Locked, day.At(10)));
        var signIn = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuthReason.Bad2Fa, day.At(11)));
        var session = await SessionAsync();

        var shown = new Dictionary<Guid, string>();
        foreach (var entry in withheld)
        {
            // The stored row keeps its reason; the page gives none.
            Assert.Contains(entry.ReasonClass, new[] { AdminAuthReason.Bad2Fa, AdminAuthReason.BadPassword });
            var (response, html) = await PageAsync($"/admin/audit/{entry.Id:D}", session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                new[] { "Time (ET)", "Actor", "Action", "Entity type", "Entity ID", "Outcome", "IP HMAC prefix" },
                Regex.Matches(html, "<dt>([^<]+)</dt>").Select(m => m.Groups[1].Value).ToArray());
            Assert.Contains($"<dt>Action</dt><dd>{entry.Action}</dd>", html, StringComparison.Ordinal);
            Assert.Contains("<dt>Outcome</dt><dd><span class=\"badge badge-failure\">Failure</span></dd>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("bad_2fa", html, StringComparison.Ordinal);
            Assert.DoesNotContain("bad_password", html, StringComparison.Ordinal);
            Assert.DoesNotContain(">Reason<", html, StringComparison.Ordinal);
            shown[entry.Id] = Regex.Match(html, "<dt>Actor</dt>.*</dl>", RegexOptions.Singleline).Value;
        }

        // A wrong password and a wrong code read the same, from the actor on: nothing tells one from the other.
        Assert.Equal(shown[withheld[0].Id], shown[withheld[1].Id]);
        Assert.Equal(shown[withheld[2].Id], shown[withheld[3].Id]);

        var (_, list) = await PageAsync($"/admin/audit?{day.Query}", session);
        Assert.Equal(6, Rows(list).Length);
        Assert.DoesNotContain("bad_2fa", list, StringComparison.Ordinal);
        Assert.DoesNotContain("bad_password", list, StringComparison.Ordinal);
        Assert.Equal(5, Regex.Matches(list, "<span class=\"badge badge-failure\">Failure</span>").Count);

        // A reason that tells nothing of a guess still shows.
        var (_, locked) = await PageAsync($"/admin/audit/{lockedStepUp.Id:D}", session);
        Assert.Contains("<dt>Reason</dt><dd>locked</dd>", locked, StringComparison.Ordinal);
        Assert.Contains("<span class=\"badge badge-denied\">Denied</span>", locked, StringComparison.Ordinal);
        var (_, failedSignIn) = await PageAsync($"/admin/audit/{signIn.Id:D}", session);
        Assert.Contains("<dt>Reason</dt><dd>bad_2fa</dd>", failedSignIn, StringComparison.Ordinal);
    }

    private static void Set(AdminAuditEntry entry, string property, object? value)
        => typeof(AdminAuditEntry).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entry, value);

    // ---- the script, read as text ----

    [Fact]
    public void AuditScript_KeepsToTheHouseRules_AndAsksTheServerForNothing()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "console-audit.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "fetch(", "eval(", "new Function", "setInterval", "XMLHttpRequest",
                     ".submit()", "AdminRequest", "sendBeacon", "WebSocket", "EventSource", "/admin/api/"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }

        // One timer, run once: the count said a moment after a move.
        Assert.Single(Regex.Matches(script, @"setTimeout\("));
        Assert.Contains("setTimeout(function () { live.textContent = count; }, 100);", script, StringComparison.Ordinal);
        // The only way it leaves the page: to an address under the form's own path.
        Assert.Single(Regex.Matches(script, @"location\.assign\("));
        Assert.DoesNotContain("location.replace", script, StringComparison.Ordinal);
        Assert.DoesNotContain("location.href =", script, StringComparison.Ordinal);
        Assert.Contains("if (url.origin !== location.origin || url.pathname !== path) return;", script, StringComparison.Ordinal);
        Assert.StartsWith("/admin/assets/", AdminAuditPages.ScriptPath, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    // ---- an empty log, and a log that cannot be read ----

    [Fact]
    public async Task AnEmptyLog_SaysNoAuditEntriesYet_AndALogThatCannotBeRead_SaysSo_WithRetry_KeepingItsFilters()
    {
        await using var factory = new FaultyStoreFactory();
        var session = await SessionAsync(factory.Services);

        var (response, html) = await PageAsync("/admin/audit", session, factory);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<td colspan=\"6\" class=\"state\"><p id=\"list-empty\">No audit entries yet.</p></td>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("No results match these filters.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"pager\"", html, StringComparison.Ordinal);
        // The page size is not a filter: still the empty log.
        (_, html) = await PageAsync("/admin/audit?limit=100", session, factory);
        Assert.Contains("No audit entries yet.", html, StringComparison.Ordinal);

        factory.Fault.FailWhen = sql => sql.Contains("FROM \"AdminAuditLog\"", StringComparison.Ordinal);
        (response, html) = await PageAsync("/admin/audit?action=sign_out&entityType=Offer&from=2026-11-01", session, factory);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Contains("<p id=\"list-error\">We couldn't load the audit log.</p>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("<a id=\"retry\" class=\"button-link\" href=\"/admin/audit?action=sign_out&amp;entityType=Offer&amp;from=2026-11-01\">Retry</a>", html, StringComparison.Ordinal);
        // The filters are still in the form, and the shell is whole.
        Assert.Contains("id=\"action-sign_out\" name=\"action\" value=\"sign_out\" checked", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"Offer\" selected>Offer</option>", html, StringComparison.Ordinal);
        Assert.Contains("<h1 id=\"page-heading\">Audit log</h1>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", html, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-announce", html, StringComparison.Ordinal);

        // Retry, once the store answers again.
        factory.Fault.FailWhen = null;
        (response, html) = await PageAsync("/admin/audit?action=sign_out&entityType=Offer&from=2026-11-01", session, factory);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No results match these filters.", html, StringComparison.Ordinal);
    }

    /// <summary>A store of its own, empty, whose reads can be made to fail.</summary>
    private sealed class FaultyStoreFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new($"Data Source=AuditFault_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

        public FaultInjector Fault { get; } = new();

        public FaultyStoreFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(IpHasher.KeyEnvironmentVariable, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.UseSetting(TotpSecretProtector.KeyEnvironmentVariable, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.UseSetting(AdminRecoveryCodeHasher.KeyEnvironmentVariable, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services
                             .Where(d => d.ServiceType == typeof(DbContextOptions<DealowareDbContext>)
                                         || d.ServiceType == typeof(DbContextOptions)
                                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<DealowareDbContext>)
                                         || d.ServiceType == typeof(DealowareDbContext))
                             .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<DealowareDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                    options.AddInterceptors(Fault);
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }
    }

    /// <summary>Fails every read whose SQL matches <see cref="FailWhen"/>, as a store that stopped answering would.</summary>
    private sealed class FaultInjector : DbCommandInterceptor
    {
        public Func<string, bool>? FailWhen { get; set; }

        private void Check(DbCommand command)
        {
            if (FailWhen is { } fail && fail(command.CommandText))
                throw new InjectedStoreFault();
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Check(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Check(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Check(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Check(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class InjectedStoreFault : DbException
    {
        public InjectedStoreFault()
            : base("The store did not answer.")
        {
        }
    }
}

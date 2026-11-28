using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
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
/// T04 piece 2: the list pages S-B2 to S-B5 and the detail pages S-B6, as the
/// server writes them (AdminListPages), and the Stats links to them. What the
/// pages do in a browser (the script, keyboard, axe, three engines) is in
/// e2e/console. Case ids: TD-ADM-060 to 066, 140; TD-ADM-UI-na-B01 to B09,
/// B14, B15, X04.
/// </summary>
[Collection("WebAppTests")]
public partial class AdminListPageTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = TestCoreOwner.Email;

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminListPageTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- seeding ----

    private sealed record Graph(
        Participant Alpha,
        Participant Beta,
        Participant Deleted,
        Artifact Artifact,
        Artifact OtherArtifact,
        Negotiation Open,
        Negotiation Closed,
        Negotiation DeletedNegotiation,
        Offer OpenOffer,
        Offer AcceptedOffer,
        Offer SupersededOffer,
        Offer DeletedOffer);

    private async Task<Graph> SeedAsync(string prefix, string? alphaName = null, string? artifactName = null, string? terms = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var alpha = Participant.Create(alphaName ?? $"{prefix}-Alpha");
        alpha.UpdateLoginEmail($"{prefix}-login@example.test");
        alpha.UpdateContactEmail($"{prefix}-contact@example.test");
        var beta = Participant.Create($"{prefix}-Beta");
        var deleted = Participant.Create($"{prefix}-Gone");
        db.Participants.AddRange(alpha, beta, deleted);

        var artifact = Artifact.Create(alpha.Sub, [SubjectEntity.Create(artifactName ?? $"{prefix}-Widget", $"{prefix} first line\nsecond <i>line</i>")], "sell");
        var other = Artifact.Create(beta.Sub, [SubjectEntity.Create($"{prefix}-Gadget", "g")], "buy");
        db.Artifacts.AddRange(artifact, other);

        var (open, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (closed, _) = Negotiation.Create(other.Id, beta.Sub, alpha.Sub, "sell", "buy");
        closed!.Close();
        var (deletedNegotiation, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        db.Negotiations.AddRange(open!, closed, deletedNegotiation!);

        var (openOffer, _) = Offer.Create(open!.Id, alpha.Sub, beta.Sub, 1234.5m, "USD", terms ?? $"{prefix}-terms");
        var (accepted, _) = Offer.Create(open.Id, beta.Sub, alpha.Sub, 20m, "EUR", $"{prefix}-accepted");
        accepted!.Accept(alpha.Sub);
        var (superseded, _) = Offer.Create(closed.Id, alpha.Sub, beta.Sub, 30m, "USD", $"{prefix}-superseded");
        superseded!.Supersede();
        var (deletedOffer, _) = Offer.Create(open.Id, alpha.Sub, beta.Sub, 40m, "USD", $"{prefix}-deleted");
        db.Offers.AddRange(openOffer!, accepted, superseded, deletedOffer!);

        var (strategy, _) = Strategy.Create(alpha.Sub, $"{prefix}-strat", $"{prefix}-STRATEGY-TEXT");
        db.Strategies.Add(strategy!);
        await db.SaveChangesAsync();

        var at = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        await db.Participants.Where(p => p.Id == deleted.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Negotiations.Where(n => n.Id == deletedNegotiation!.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer!.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));

        return new Graph(alpha, beta, deleted, artifact, other, open, closed, deletedNegotiation!, openOffer!, accepted, superseded, deletedOffer!);
    }

    private async Task<Guid> SessionAsync(IServiceProvider? services = null)
    {
        using var scope = (services ?? _factory.Services).CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(OwnerEmail, ipHmac: "testhmac-not-an-ip");
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
            .Select(m => Text(Regex.Replace(m.Groups[1].Value, "<span class=\"(visually-hidden|sort-mark)\"[^>]*>.*?</span>", string.Empty)).Trim())
            .ToArray();

    private static string[] Rows(string html)
        => Regex.Matches(html, "<tbody>(.*?)</tbody>", RegexOptions.Singleline).Single().Groups[1].Value
            .Split("</tr>", StringSplitOptions.RemoveEmptyEntries);

    private static string Canonical(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "data-canonical=\"([^\"]*)\"").Groups[1].Value);

    private static string Range(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "<p id=\"list-range\" class=\"range\">([^<]*)</p>").Groups[1].Value);

    private static int TotalOf(string html)
    {
        var range = Range(html);
        if (range.Length == 0)
            return 0;
        return int.Parse(range[(range.LastIndexOf(" of ", StringComparison.Ordinal) + 4)..], System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture);
    }

    // ---- what each list is (B01 to B04, B06, B08) ----

    public static TheoryData<string, string[], string[], string[], string> Lists => new()
    {
        {
            "/admin/participants",
            ["Name", "ID", "Status", "Created", "Updated"],
            ["name", "created", "updated"],
            ["Active", "Suspended"],
            "Search by display name or Core identifier"
        },
        {
            "/admin/artifacts",
            ["Name", "Owner participant", "Created", "Updated"],
            ["name", "created", "updated"],
            [],
            "Search by artifact name"
        },
        {
            "/admin/negotiations",
            ["ID", "Artifact", "Participants", "Status", "Created", "Updated"],
            ["status", "created", "updated"],
            ["Open", "Closed", "Expired"],
            "Search by artifact or participant name"
        },
        {
            "/admin/offers",
            ["ID", "Negotiation ID", "Artifact", "Offering participant", "Amount", "Status", "Created", "Updated"],
            ["amount", "status", "created", "updated"],
            ["Open", "Accepted", "Declined", "Superseded (countered)", "Cancelled", "Withdrawn"],
            "Search by artifact, offering participant, or negotiation id"
        }
    };

    [Theory]
    [MemberData(nameof(Lists))]
    public async Task EachList_HasItsColumnsSortsStatusesAndControls_OpeningOnUpdatedNewestFirst(
        string path, string[] columns, string[] sorts, string[] statuses, string searchLabel)
    {
        var session = await SessionAsync();
        var (response, html) = await PageAsync(path, session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // B01: the columns in order.
        Assert.Equal(columns, HeaderTexts(html));
        // B02: the sortable ones are buttons in th scope="col"; aria-sort on the sorted one only.
        Assert.Equal(sorts.OrderBy(s => s), Regex.Matches(html, "id=\"sort-([a-z]+)\" class=\"sort-button\"").Select(m => m.Groups[1].Value).OrderBy(s => s));
        Assert.Single(Regex.Matches(html, "aria-sort="));
        Assert.Matches("<th scope=\"col\" aria-sort=\"descending\"><form class=\"sort-form\" method=\"get\" action=\"" + path + "\">.*?<button type=\"submit\" id=\"sort-updated\" class=\"sort-button\">Updated<span class=\"sort-mark\" aria-hidden=\"true\">.</span><span class=\"visually-hidden\"> \\(sorted descending\\)</span></button>", html);
        // The Updated header asks for the other direction.
        Assert.Matches("<input type=\"hidden\" name=\"dir\" value=\"asc\"><button type=\"submit\" id=\"sort-updated\"", html);

        // B03: exactly this list's statuses, never Deleted.
        var options = Regex.Matches(html, "<input type=\"checkbox\" id=\"status-([A-Za-z]+)\" name=\"status\" value=\"([A-Za-z]+)\"[^>]*><label for=\"status-[A-Za-z]+\">([^<]+)</label>")
            .Select(m => WebUtility.HtmlDecode(m.Groups[3].Value)).ToArray();
        Assert.Equal(statuses.OrderBy(s => s), options.OrderBy(s => s));
        Assert.DoesNotContain("value=\"Deleted\"", html, StringComparison.Ordinal);

        // B04: amount and price only on Offers.
        Assert.Equal(path == "/admin/offers", html.Contains("id=\"amountMin\"", StringComparison.Ordinal));
        Assert.Equal(path == "/admin/offers", html.Contains("id=\"sort-amount\"", StringComparison.Ordinal));
        Assert.DoesNotContain("price", html, StringComparison.OrdinalIgnoreCase);

        // B07: a visible label that names what the search matches.
        Assert.Contains($"<label for=\"q\">{WebUtility.HtmlEncode(searchLabel)}</label>", html, StringComparison.Ordinal);
        // B06: 50 (chosen), 100, 200 only.
        Assert.Equal(
            new[] { "50", "100", "200" },
            Regex.Matches(html, "<option value=\"(\\d+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("<option value=\"50\" selected>50</option>", html, StringComparison.Ordinal);
        // B08: the toggle, off.
        Assert.Contains("<input type=\"checkbox\" id=\"includeDeleted\" name=\"includeDeleted\" value=\"true\" data-auto><label for=\"includeDeleted\">Show deleted</label>", html, StringComparison.Ordinal);
        // The pickers this list has.
        Assert.Equal(path != "/admin/participants", html.Contains("id=\"participant-picker\" role=\"combobox\"", StringComparison.Ordinal));
        Assert.Equal(path is "/admin/negotiations" or "/admin/offers", html.Contains("id=\"artifact-picker\" role=\"combobox\"", StringComparison.Ordinal));
        Assert.Equal(path is "/admin/negotiations" or "/admin/offers", html.Contains("id=\"negotiationId\" name=\"negotiationId\"", StringComparison.Ordinal));
    }

    [Fact]
    public void EachListPage_ReadsExactlyTheKeysTheWayBackAfterSignInKeeps()
    {
        foreach (var list in AdminListPages.All)
            Assert.Equal(AdminReturnQuery.KeysFor(list.Path), list.QueryKeys);
    }

    // ---- what each list shows ----

    [Fact]
    public async Task Rows_AreTheReadApis_WithNamesStatusBadgesAndEtTimes()
    {
        var prefix = $"lp1-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/negotiations?q={prefix}-Widget", session);
        var rows = Rows(html);
        var open = Assert.Single(rows, r => r.Contains(g.Open.Id.ToString("D"), StringComparison.Ordinal));
        Assert.Contains($"<a class=\"id\" href=\"/admin/negotiations/{g.Open.Id:D}?q={prefix}-Widget\">{g.Open.Id:D}</a>", open, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"/admin/artifacts/{g.Artifact.Id:D}\">{prefix}-Widget</a>", open, StringComparison.Ordinal);
        Assert.Contains($"{prefix}-Alpha", open, StringComparison.Ordinal);
        Assert.Contains($"{prefix}-Beta", open, StringComparison.Ordinal);
        Assert.Contains("<span class=\"badge badge-open\">Open</span>", open, StringComparison.Ordinal);
        // B15: every time in ET, the UTC value in its tooltip; the element takes the focus.
        Assert.Matches("<time class=\"ts\" datetime=\"\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}Z\" tabindex=\"0\" title=\"\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2} UTC\" data-utc=\"\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2} UTC\">[A-Z][a-z]{2} \\d{1,2}, \\d{4}, \\d{1,2}:\\d{2} (AM|PM) ET</time>", open);
        // The soft-deleted one stays out without the toggle.
        Assert.DoesNotContain(g.DeletedNegotiation.Id.ToString("D"), html, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/offers?q={prefix}-", session);
        var offer = Assert.Single(Rows(html), r => r.Contains($"/admin/offers/{g.OpenOffer.Id:D}", StringComparison.Ordinal));
        Assert.Contains($"<a class=\"id\" href=\"/admin/negotiations/{g.Open.Id:D}\">{g.Open.Id:D}</a>", offer, StringComparison.Ordinal);
        Assert.Contains("1,234.50 USD", offer, StringComparison.Ordinal);
        // Free text stays on the detail page.
        Assert.DoesNotContain($"{prefix}-terms", html, StringComparison.Ordinal);
        var superseded = Assert.Single(Rows(html), r => r.Contains($"/admin/offers/{g.SupersededOffer.Id:D}", StringComparison.Ordinal));
        Assert.Contains("<span class=\"badge badge-superseded\">Superseded (countered)</span>", superseded, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/participants?q={prefix}-", session);
        Assert.Contains($"{prefix}-Alpha", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"badge badge-active\">Active</span>", html, StringComparison.Ordinal);
        // No email address in a list (they are on the detail page).
        Assert.DoesNotContain("@example.test", html, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/artifacts?q={prefix}-", session);
        var artifact = Assert.Single(Rows(html), r => r.Contains($"/admin/artifacts/{g.Artifact.Id:D}", StringComparison.Ordinal));
        Assert.Contains($">{prefix}-Widget</a>", artifact, StringComparison.Ordinal);
        Assert.Contains($"<td>{prefix}-Alpha</td>", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain($"{prefix} first line", html, StringComparison.Ordinal);

        foreach (var path in new[] { "/admin/participants", "/admin/artifacts", "/admin/negotiations", "/admin/offers" })
        {
            (_, html) = await PageAsync($"{path}?q={prefix}", session);
            Assert.DoesNotContain("STRATEGY-TEXT", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ShowDeleted_ShowsTheDeletedBadgeBesideTheStatus_WithTheDeletedTimeInEt()
    {
        var prefix = $"lp2-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/offers?q={prefix}-Widget&includeDeleted=true", session);
        Assert.Contains("<input type=\"checkbox\" id=\"includeDeleted\" name=\"includeDeleted\" value=\"true\" checked data-auto>", html, StringComparison.Ordinal);
        var row = Assert.Single(Rows(html), r => r.Contains($"/admin/offers/{g.DeletedOffer.Id:D}", StringComparison.Ordinal));
        Assert.Contains(
            "<span class=\"badges\"><span class=\"badge badge-open\">Open</span><span class=\"badge badge-deleted\">Deleted</span>"
            + "<time class=\"ts\" datetime=\"2026-07-01T12:00:00Z\" tabindex=\"0\" title=\"2026-07-01 12:00 UTC\" data-utc=\"2026-07-01 12:00 UTC\">Jul 1, 2026, 8:00 AM ET</time></span>",
            row,
            StringComparison.Ordinal);
        // The row's link keeps the toggle, so its detail opens.
        Assert.Contains($"href=\"/admin/offers/{g.DeletedOffer.Id:D}?q={prefix}-Widget&amp;includeDeleted=true\"", row, StringComparison.Ordinal);
        Assert.Equal($"/admin/offers?q={prefix}-Widget&includeDeleted=true", Canonical(html));
    }

    [Fact]
    public async Task EveryValue_IsEncoded()
    {
        var prefix = $"lp3-{Guid.NewGuid():N}";
        const string hostile = "<script>alert(1)</script>\"'&<img src=x>";
        var g = await SeedAsync(prefix, alphaName: prefix + hostile, artifactName: prefix + "-A" + hostile, terms: prefix + hostile);
        var session = await SessionAsync();

        foreach (var path in new[]
                 {
                     $"/admin/participants?q={prefix}",
                     $"/admin/artifacts?q={prefix}",
                     $"/admin/negotiations?q={prefix}",
                     $"/admin/offers?q={prefix}",
                     $"/admin/participants/{g.Alpha.Id:D}",
                     $"/admin/artifacts/{g.Artifact.Id:D}",
                     $"/admin/negotiations/{g.Open.Id:D}",
                     $"/admin/offers/{g.OpenOffer.Id:D}",
                     $"/admin/offers?participant={Uri.EscapeDataString(g.Alpha.Sub)}&q=" + Uri.EscapeDataString(hostile)
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<img src=x>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("\"'&<", html, StringComparison.Ordinal);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
            Assert.Equal(Regex.Matches(html, "<script src=").Count, Regex.Matches(html, "<script").Count);
        }
    }

    [Fact]
    public async Task WhatAListDoesNotTake_IsDropped_AndTheDefaultListShows_NeverAnError()
    {
        var session = await SessionAsync();

        // TD-ADM-062 / B04: a negotiation status that is not one, an amount sort, an amount filter, a page size over 200.
        var (response, html) = await PageAsync(
            "/admin/negotiations?status=Withdrawn,Accepted,Deleted&sort=amount&dir=asc&amountMin=5&price=1&limit=500&offset=-2&bogus=%3Cx%3E", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/admin/negotiations?limit=200", Canonical(html));
        Assert.Contains("<th scope=\"col\" aria-sort=\"descending\"><form class=\"sort-form\" method=\"get\" action=\"/admin/negotiations\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain(" checked", html.Replace("id=\"includeDeleted\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("<option value=\"200\" selected>200</option>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("&lt;x&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"chips\"", html, StringComparison.Ordinal);

        // A status the list has stays, the rest goes; a sort a header offers stays.
        (_, html) = await PageAsync("/admin/offers?status=withdrawn,Deleted,open&sort=amount&dir=asc&limit=75", session);
        Assert.Equal("/admin/offers?sort=amount&dir=asc&limit=100&status=Open%2CWithdrawn", Canonical(html));
        Assert.Contains("<th scope=\"col\" aria-sort=\"ascending\">", html, StringComparison.Ordinal);
        Assert.Contains("id=\"status-Open\" name=\"status\" value=\"Open\" checked", html, StringComparison.Ordinal);
        Assert.Contains("id=\"status-Withdrawn\" name=\"status\" value=\"Withdrawn\" checked", html, StringComparison.Ordinal);

        // A form sent without JavaScript repeats status and sends its empty boxes: the same list.
        (_, html) = await PageAsync("/admin/offers?q=&status=Open&status=Accepted&negotiationId=&amountMin=&createdFrom=&includeDeleted=true&limit=50", session);
        Assert.Equal("/admin/offers?includeDeleted=true&status=Open%2CAccepted", Canonical(html));

        // A sort no header offers (the API takes it) is not this page's.
        (_, html) = await PageAsync("/admin/negotiations?sort=participant", session);
        Assert.Equal("/admin/negotiations", Canonical(html));

        // Hand-made junk in every key: still the page.
        (response, _) = await PageAsync("/admin/participants?q=%00%01&sort=%27--&dir=sideways&offset=1e9&limit=-5&includeDeleted=maybe&status=%E2%80%8B&createdFrom=yesterday", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchSortAndEveryFilter_ChangeTheServerQuery()
    {
        var prefix = $"lp4-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        // Name sort ascending (B02).
        var (_, html) = await PageAsync($"/admin/participants?q={prefix}-&sort=name&dir=asc", session);
        var names = Regex.Matches(html, $"<td>({prefix}-[A-Za-z]+)</td>").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        Assert.Equal(2, names.Length);

        // Owner participant on Artifacts (B02, B05): the owner's artifacts only, named on its chip.
        (_, html) = await PageAsync($"/admin/artifacts?q={prefix}-&participant={Uri.EscapeDataString(g.Beta.Sub)}", session);
        Assert.Contains($"{prefix}-Gadget", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"{prefix}-Widget", html, StringComparison.Ordinal);
        Assert.Contains($"<li class=\"chip\"><span>Owner participant: {prefix}-Beta</span>", html, StringComparison.Ordinal);
        Assert.Contains($"value=\"{prefix}-Beta\"><input type=\"hidden\" id=\"participant\" name=\"participant\" value=\"{g.Beta.Sub}\">", html, StringComparison.Ordinal);

        // Artifact on Offers.
        (_, html) = await PageAsync($"/admin/offers?artifact={g.OtherArtifact.Id:D}", session);
        Assert.Equal(1, TotalOf(html));
        Assert.Contains($"<li class=\"chip\"><span>Artifact: {prefix}-Gadget</span>", html, StringComparison.Ordinal);

        // Exact negotiation id, amount min and max (B05).
        (_, html) = await PageAsync($"/admin/offers?negotiationId={g.Open.Id:D}&amountMin=100&amountMax=2000", session);
        Assert.Equal(1, TotalOf(html));
        Assert.Contains($"/admin/offers/{g.OpenOffer.Id:D}", html, StringComparison.Ordinal);

        // Status, several at once.
        (_, html) = await PageAsync($"/admin/offers?negotiationId={g.Open.Id:D}&status=Accepted,Open", session);
        Assert.Equal(2, TotalOf(html));
        Assert.Contains("<li class=\"chip\"><span>Status: Open, Accepted</span>", html, StringComparison.Ordinal);

        // Dates are ET calendar days: today's rows are in today, not in tomorrow.
        var today = EasternTime.ToEastern(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var tomorrow = EasternTime.ToEastern(DateTimeOffset.UtcNow.AddDays(1)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        (_, html) = await PageAsync($"/admin/negotiations?q={prefix}-&createdFrom={today}&createdTo={today}", session);
        Assert.Equal(2, TotalOf(html));
        Assert.Contains($"value=\"{today}\"", html, StringComparison.Ordinal);
        (_, html) = await PageAsync($"/admin/negotiations?q={prefix}-&updatedFrom={tomorrow}", session);
        Assert.Contains("<p id=\"list-miss\">No results match these filters.</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chips_OnePerFilter_EachRemovesOnlyItself_AndClearAllKeepsSortSizeAndToggle()
    {
        var prefix = $"lp5-{Guid.NewGuid():N}";
        await SeedAsync(prefix);
        var session = await SessionAsync();

        var (_, html) = await PageAsync($"/admin/offers?q={prefix}&status=Open&amountMin=5&sort=amount&limit=100&includeDeleted=true&offset=100", session);
        var chips = Regex.Matches(html, "<li class=\"chip\"><span>([^<]+)</span><a href=\"([^\"]+)\" aria-label=\"([^\"]+)\" data-focus=\"q\">Remove</a></li>")
            .Select(m => (Text: WebUtility.HtmlDecode(m.Groups[1].Value), Href: WebUtility.HtmlDecode(m.Groups[2].Value), Name: m.Groups[3].Value))
            .ToArray();
        Assert.Equal(new[] { $"Search: {prefix}", "Status: Open", "Amount min: 5" }, chips.Select(c => c.Text).ToArray());
        Assert.Equal(new[] { "Remove Search filter", "Remove Status filter", "Remove Amount min filter" }, chips.Select(c => c.Name).ToArray());
        Assert.Equal("/admin/offers?sort=amount&limit=100&includeDeleted=true&status=Open&amountMin=5", chips[0].Href);
        Assert.Equal($"/admin/offers?sort=amount&q={prefix}&limit=100&includeDeleted=true&amountMin=5", chips[1].Href);
        Assert.Contains("<a id=\"clear-all\" class=\"button-link secondary\" href=\"/admin/offers?sort=amount&amp;limit=100&amp;includeDeleted=true\" data-focus=\"q\">Clear all filters</a>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Paging_ShowsTheRange_PreviousNextAndNumberedPages_AndAPagePastTheEndShowsTheLast()
    {
        var prefix = $"lp6-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            for (var i = 0; i < 120; i++)
                db.Participants.Add(Participant.Create($"{prefix}-{i:D3}"));
            await db.SaveChangesAsync();
        }

        var session = await SessionAsync();
        var (_, html) = await PageAsync($"/admin/participants?q={prefix}-&offset=50", session);
        Assert.Equal("Showing 51\u2013100 of 120", Range(html));
        Assert.Equal(50, Rows(html).Length);
        Assert.Contains($"<a id=\"page-prev\" href=\"/admin/participants?q={prefix}-\" rel=\"prev\" data-focus=\"page-prev\">Previous</a>", html, StringComparison.Ordinal);
        Assert.Contains($"<a id=\"page-next\" href=\"/admin/participants?q={prefix}-&amp;offset=100\" rel=\"next\" data-focus=\"page-next\">Next</a>", html, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "Page 1", "Page 2", "Page 3" },
            Regex.Matches(html, "<a id=\"page-\\d+\" href=\"[^\"]+\" aria-label=\"(Page \\d+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("aria-label=\"Page 2\" aria-current=\"page\"", html, StringComparison.Ordinal);

        // Past the last page: the last page, and the address says so.
        (_, html) = await PageAsync($"/admin/participants?q={prefix}-&offset=5000", session);
        Assert.Equal("Showing 101\u2013120 of 120", Range(html));
        Assert.Equal($"/admin/participants?q={prefix}-&offset=100", Canonical(html));
        Assert.Contains("<span class=\"page-off\">Next</span>", html, StringComparison.Ordinal);

        // An offset between pages starts its page; 100 rows a page.
        (_, html) = await PageAsync($"/admin/participants?q={prefix}-&offset=73&limit=100", session);
        Assert.Equal("Showing 1\u2013100 of 120", Range(html));
        Assert.Contains("<span class=\"page-off\">Previous</span>", html, StringComparison.Ordinal);

        // The first page has no Previous link.
        (_, html) = await PageAsync($"/admin/participants?q={prefix}-", session);
        Assert.Equal("Showing 1\u201350 of 120", Range(html));
        Assert.Contains("data-announce=\"120 results\"", html, StringComparison.Ordinal);
        // The live region keeps the space of that count from the start, so nothing jumps when it is said.
        Assert.Contains("<p id=\"console-status\" class=\"live live-reserved\" role=\"status\" aria-live=\"polite\"></p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFilterMiss_SaysSo_WithClearFilters_NotAnError()
    {
        var session = await SessionAsync();
        var miss = $"nomatch-{Guid.NewGuid():N}";
        foreach (var path in new[] { "/admin/participants", "/admin/artifacts", "/admin/negotiations", "/admin/offers" })
        {
            var (response, html) = await PageAsync($"{path}?q={miss}&limit=100", session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<p id=\"list-miss\">No results match these filters.</p>", html, StringComparison.Ordinal);
            Assert.Contains($"<a id=\"clear-filters\" class=\"button-link\" href=\"{path}?limit=100\" data-focus=\"q\">Clear filters</a>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("id=\"pager\"", html, StringComparison.Ordinal);
            Assert.Contains("data-announce=\"0 results\"", html, StringComparison.Ordinal);
        }

        // B03: Withdrawn alone with none of them: the same miss.
        var (_, withdrawn) = await PageAsync($"/admin/offers?status=Withdrawn&q={miss}", session);
        Assert.Contains("No results match these filters.", withdrawn, StringComparison.Ordinal);
    }

    // ---- without a session ----

    [Theory]
    [InlineData("/admin/participants?q=x")]
    [InlineData("/admin/offers?status=Open&includeDeleted=true")]
    [InlineData("/admin/negotiations/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59")]
    [InlineData("/admin/artifacts/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59?includeDeleted=true")]
    [InlineData("/admin/assets/console-list.js")]
    public async Task WithoutASession_NoPageNoDataNoScript(string path)
    {
        var prefix = $"lp7-{Guid.NewGuid():N}";
        await SeedAsync(prefix);
        foreach (Guid? session in new Guid?[] { null, Guid.NewGuid() })
        {
            var (response, html) = await PageAsync(path, session);
            if (path.StartsWith("/admin/assets/", StringComparison.Ordinal))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("{\"error\":\"Unauthorized\"}", html);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Found, response.StatusCode);
                Assert.Equal(AdminSignInPages.SessionEndedLocation, Assert.Single(response.Headers.GetValues("Location")));
                Assert.Equal(string.Empty, html);
            }
        }
    }

    // ---- detail pages (S-B6) ----

    [Fact]
    public async Task Details_ShowTheRecordAsText_UnderTheShellsRules()
    {
        var prefix = $"lp8-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix, terms: $"{prefix} line one\nline <b>two</b>");
        var session = await SessionAsync();

        var (response, html) = await PageAsync($"/admin/participants/{g.Alpha.Id:D}?q={prefix}&sort=name", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        var nonce = Regex.Match(html, "nonce=\"([^\"]+)\"").Groups[1].Value;
        Assert.Equal(AdminSignedOutPage.ContentSecurityPolicy(nonce), Header(response, "Content-Security-Policy"));
        Assert.Contains("<title>Participant details · Dealoware admin</title>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("<h1 id=\"page-heading\">Participant details</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<li><a href=\"/admin/participants\" aria-current=\"page\">Participants</a></li>", html, StringComparison.Ordinal);
        Assert.Contains($"<a id=\"back-to-list\" href=\"/admin/participants?sort=name&amp;q={prefix}\">Back to list</a>", html, StringComparison.Ordinal);
        var text = Text(html);
        Assert.Contains($"Login email{prefix}-login@example.test", text, StringComparison.Ordinal);
        Assert.Contains($"Contact email{prefix}-contact@example.test", text, StringComparison.Ordinal);
        Assert.Contains($"Core identifier{g.Alpha.Sub}", text, StringComparison.Ordinal);
        Assert.Contains("StatusActive", text, StringComparison.Ordinal);
        // No delete control. The one other form is edit mode's (T05, AdminEditPageTests): it has no
        // method or action, so without JavaScript nothing can send it.
        Assert.Equal(
            ["sign-out-form", "edit-form"],
            Regex.Matches(html, "<form id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray());
        Assert.Contains("<form id=\"edit-form\" class=\"edit-form\" autocomplete=\"off\" novalidate>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("STRATEGY-TEXT", html, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/offers/{g.OpenOffer.Id:D}", session);
        text = Text(html);
        // Free text as text: markup encoded, the line break kept (the stylesheet shows it).
        Assert.Contains($"<span class=\"free-text\">{AdminSignedOutPage.Encode($"{prefix} line one\nline <b>two</b>")}</span>", html, StringComparison.Ordinal);
        Assert.Contains("line &lt;b&gt;two&lt;/b&gt;", html, StringComparison.Ordinal);
        Assert.Contains($"Offering participant{prefix}-Alpha ({g.Alpha.Sub})", text, StringComparison.Ordinal);
        Assert.Contains($"Receiving participant{prefix}-Beta ({g.Beta.Sub})", text, StringComparison.Ordinal);
        Assert.Contains("Amount1,234.50 USD", text, StringComparison.Ordinal);
        Assert.Contains($"<a class=\"id\" href=\"/admin/negotiations/{g.Open.Id:D}\">", html, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"/admin/artifacts/{g.Artifact.Id:D}\">{prefix}-Widget</a>", html, StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/artifacts/{g.Artifact.Id:D}", session);
        Assert.Contains($"<span class=\"free-text\">{AdminSignedOutPage.Encode($"{prefix} first line\nsecond <i>line</i>")}</span>", html, StringComparison.Ordinal);
        Assert.Contains("second &lt;i&gt;line&lt;/i&gt;", html, StringComparison.Ordinal);
        Assert.Contains($"Owner participant{prefix}-Alpha ({g.Alpha.Sub})", Text(html), StringComparison.Ordinal);

        (_, html) = await PageAsync($"/admin/negotiations/{g.Open.Id:D}", session);
        text = Text(html);
        Assert.Contains($"Participant A{prefix}-Alpha", text, StringComparison.Ordinal);
        Assert.Contains($"Participant B{prefix}-Beta", text, StringComparison.Ordinal);
        Assert.Contains("StatusOpen", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADeletedRecord_IsS_D3_WithTheToggleOff_AndShownWithIt()
    {
        var prefix = $"lp9-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        foreach (var path in new[]
                 {
                     $"/admin/participants/{g.Deleted.Id:D}",
                     $"/admin/negotiations/{g.DeletedNegotiation.Id:D}",
                     $"/admin/offers/{g.DeletedOffer.Id:D}",
                     $"/admin/offers/{Guid.NewGuid():D}",
                     $"/admin/offers/{Guid.NewGuid():D}?includeDeleted=true"
                 })
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("<h1 id=\"page-heading\">We can't find that page.</h1>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
            Assert.DoesNotContain(prefix, html, StringComparison.Ordinal);
            Assert.DoesNotContain("Deleted", html, StringComparison.Ordinal);
        }

        var (shown, page) = await PageAsync($"/admin/offers/{g.DeletedOffer.Id:D}?includeDeleted=true", session);
        Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        Assert.Contains("<span class=\"badge badge-open\">Open</span><span class=\"badge badge-deleted\">Deleted</span><time class=\"ts\" datetime=\"2026-07-01T12:00:00Z\"", page, StringComparison.Ordinal);
        Assert.Contains($"<a id=\"back-to-list\" href=\"/admin/offers?includeDeleted=true\">Back to list</a>", page, StringComparison.Ordinal);
        Assert.Contains($"href=\"/admin/negotiations/{g.Open.Id:D}?includeDeleted=true\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADeletedRecordsName_StaysHiddenWithTheToggleOff_InRowsChipsLinksAndDetails()
    {
        var prefix = $"lp11-{Guid.NewGuid():N}";
        var alpha = Participant.Create($"{prefix}-Alpha");
        var gone = Participant.Create($"{prefix}-GonePerson");
        var goneThing = Artifact.Create(gone.Sub, [SubjectEntity.Create($"{prefix}-GoneThing", "d")], "sell");
        var (live, _) = Negotiation.Create(goneThing.Id, alpha.Sub, gone.Sub, "sell", "buy");
        var (goneNegotiation, _) = Negotiation.Create(goneThing.Id, alpha.Sub, gone.Sub, "sell", "buy");
        var (orphanOffer, _) = Offer.Create(goneNegotiation!.Id, alpha.Sub, gone.Sub, 6m, "USD", "t");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.AddRange(alpha, gone);
            db.Artifacts.Add(goneThing);
            db.Negotiations.AddRange(live!, goneNegotiation);
            db.Offers.Add(orphanOffer!);
            await db.SaveChangesAsync();
            var at = DateTimeOffset.UtcNow;
            await db.Participants.Where(p => p.Id == gone.Id).ExecuteUpdateAsync(u => u.SetProperty(p => p.DeletedAt, at));
            await db.Artifacts.Where(a => a.Id == goneThing.Id).ExecuteUpdateAsync(u => u.SetProperty(a => a.DeletedAt, at));
            await db.Negotiations.Where(n => n.Id == goneNegotiation.Id).ExecuteUpdateAsync(u => u.SetProperty(n => n.DeletedAt, at));
        }

        var session = await SessionAsync();
        const string deleted = "<span class=\"badge badge-deleted\">Deleted</span>";

        // Toggle off: identifiers and "Deleted", no name, no link to a page that would be S-D3.
        var pages = new[]
        {
            $"/admin/negotiations?negotiationId={live!.Id:D}",
            $"/admin/offers?participant={Uri.EscapeDataString(alpha.Sub)}",
            $"/admin/artifacts?participant={Uri.EscapeDataString(gone.Sub)}",
            $"/admin/offers?artifact={goneThing.Id:D}",
            $"/admin/negotiations/{live.Id:D}",
            $"/admin/offers/{orphanOffer!.Id:D}"
        };
        foreach (var path in pages)
        {
            var (response, html) = await PageAsync(path, session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain($"{prefix}-GonePerson", html, StringComparison.Ordinal);
            Assert.DoesNotContain($"{prefix}-GoneThing", html, StringComparison.Ordinal);
            Assert.DoesNotContain($"href=\"/admin/artifacts/{goneThing.Id:D}", html, StringComparison.Ordinal);
            Assert.DoesNotContain($"href=\"/admin/negotiations/{goneNegotiation.Id:D}", html, StringComparison.Ordinal);
        }

        var (_, row) = await PageAsync(pages[0], session);
        Assert.Contains($"<span class=\"id\">{goneThing.Id:D}</span> {deleted}", row, StringComparison.Ordinal);
        Assert.Contains($"{gone.Sub} {deleted}", row, StringComparison.Ordinal);
        Assert.Contains($"{prefix}-Alpha", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[1], session);
        Assert.Contains($"<span class=\"id\">{goneNegotiation.Id:D}</span> {deleted}", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[2], session);
        Assert.Contains($"<li class=\"chip\"><span>Owner participant: {gone.Sub}</span>{deleted}", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[3], session);
        Assert.Contains($"<li class=\"chip\"><span>Artifact: {goneThing.Id:D}</span>{deleted}", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[4], session);
        Assert.Contains($"Participant B</dt><dd>{gone.Sub} {deleted}", row, StringComparison.Ordinal);

        // Toggle on: the names, and links that carry the toggle.
        (_, row) = await PageAsync(pages[0] + "&includeDeleted=true", session);
        Assert.Contains($"<a href=\"/admin/artifacts/{goneThing.Id:D}?includeDeleted=true\">{prefix}-GoneThing</a>", row, StringComparison.Ordinal);
        Assert.Contains($"{prefix}-GonePerson", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[1] + "&includeDeleted=true", session);
        Assert.Contains($"<a class=\"id\" href=\"/admin/negotiations/{goneNegotiation.Id:D}?includeDeleted=true\">", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[2] + "&includeDeleted=true", session);
        Assert.Contains($"<li class=\"chip\"><span>Owner participant: {prefix}-GonePerson</span><a ", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[3] + "&includeDeleted=true", session);
        Assert.Contains($"<li class=\"chip\"><span>Artifact: {prefix}-GoneThing</span><a ", row, StringComparison.Ordinal);
        (_, row) = await PageAsync(pages[4] + "?includeDeleted=true", session);
        Assert.Contains($"{prefix}-GonePerson", row, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"/admin/artifacts/{goneThing.Id:D}?includeDeleted=true\">{prefix}-GoneThing</a>", row, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("createdTo=9999-12-31")]
    [InlineData("updatedTo=9999-12-31")]
    [InlineData("createdFrom=0001-01-01")]
    [InlineData("updatedFrom=0001-01-01&createdTo=9999-12-31T23:59:59Z")]
    public async Task ADateOutsideTheYearsTheListTakes_IsDropped_NeverAnError(string query)
    {
        var session = await SessionAsync();
        foreach (var list in AdminListPages.All)
        {
            var (response, html) = await PageAsync($"{list.Path}?{query}", session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(list.Path, Canonical(html));
        }

        // The first and last years it takes, both ends of a day.
        var (edge, page) = await PageAsync("/admin/offers?createdFrom=1900-01-01&createdTo=2999-12-31", session);
        Assert.Equal(HttpStatusCode.OK, edge.StatusCode);
        Assert.Equal("/admin/offers?createdFrom=1900-01-01&createdTo=2999-12-31", Canonical(page));
    }

    [Fact]
    public async Task NoGetForm_CarriesTheAntiForgeryToken_OnlyTheSignOutPostDoes()
    {
        var session = await SessionAsync();
        foreach (var list in AdminListPages.All)
        {
            var (_, html) = await PageAsync($"{list.Path}?q=x&status=Open&sort=created", session);
            var token = AdminPageAntiForgery.ReadToken(html);
            var forms = Regex.Matches(html, "<form[^>]*>.*?</form>", RegexOptions.Singleline).Select(m => m.Value).ToList();
            Assert.True(forms.Count > 2);
            foreach (var form in forms.Where(f => f.Contains("method=\"get\"", StringComparison.Ordinal)))
            {
                Assert.DoesNotContain(token, form, StringComparison.Ordinal);
                Assert.DoesNotContain(AdminAntiForgeryService.SignOutFormField, form, StringComparison.Ordinal);
            }

            var withToken = forms.Where(f => f.Contains(token, StringComparison.Ordinal)).ToList();
            Assert.Equal("sign-out-form", Regex.Match(Assert.Single(withToken), "<form id=\"([^\"]+)\" method=\"post\"").Groups[1].Value);
            // The token is in the meta and the sign-out form, nowhere else.
            Assert.Equal(2, Regex.Matches(html, token).Count);
        }
    }

    // ---- Stats links (B09) ----

    [Fact]
    public async Task EachStatsTotal_LinksToItsListFiltered_WhoseTotalIsTheSame_AndRefreshCountsAgain()
    {
        await SeedAsync($"lp10-{Guid.NewGuid():N}");
        var session = await SessionAsync();
        var (_, stats) = await PageAsync("/admin/", session);

        foreach (var (id, label, list) in new[]
                 {
                     ("stat-participants", "Participants", "/admin/participants"),
                     ("stat-open-negotiations", "Open negotiations", "/admin/negotiations?status=Open"),
                     ("stat-offers", "Offers", "/admin/offers"),
                     ("stat-accepts", "Accepts", "/admin/offers?status=Accepted"),
                     ("stat-declines", "Declines", "/admin/offers?status=Declined")
                 })
        {
            Assert.Contains($"<dt id=\"{id}-label\"><a href=\"{list}\">{label}</a></dt>", stats, StringComparison.Ordinal);
            var count = int.Parse(Regex.Match(stats, $"<dd id=\"{id}\">([0-9,]+)</dd>").Groups[1].Value, System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture);
            var (_, page) = await PageAsync(list, session);
            Assert.Equal(count, TotalOf(page));
        }

        Assert.Contains("<a id=\"stats-refresh\" class=\"button-link secondary\" href=\"/admin/\">Refresh</a>", stats, StringComparison.Ordinal);
    }

    // ---- the script, read as text ----

    [Fact]
    public void ListScript_KeepsToTheHouseRules()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "console-list.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "fetch(", "eval(", "new Function", "setInterval", "XMLHttpRequest",
                     ".submit()"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }

        // Its only request: one page of picker matches, through the shared helper.
        Assert.Single(Regex.Matches(script, @"AdminRequest\.get\("));
        Assert.Contains("var MATCHES = 10;", script, StringComparison.Ordinal);
        Assert.Contains("\"&limit=\" + MATCHES", script, StringComparison.Ordinal);
        Assert.Contains("var PAUSE_MS = 300;", script, StringComparison.Ordinal);
        Assert.Contains("location.replace(SESSION_ENDED);", script, StringComparison.Ordinal);
        Assert.Contains("var SESSION_ENDED = \"" + AdminSignInPages.SessionEndedLocation + "\";", script, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    // ---- an empty store, and a store that fails (B14, X04, B09) ----

    [Fact]
    public async Task AnEmptyStore_SaysNoTypeYet_AndStatsShowZero()
    {
        await using var factory = new FaultyStoreFactory();
        var session = await SessionAsync(factory.Services);
        foreach (var list in AdminListPages.All)
        {
            var (response, html) = await PageAsync(list.Path, session, factory);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains($"<p id=\"list-empty\">No {list.TypeLabel} yet.</p>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("No results match these filters.", html, StringComparison.Ordinal);

            // With the toggle on (not a filter), still the empty table.
            (_, html) = await PageAsync(list.Path + "?includeDeleted=true", session, factory);
            Assert.Contains($"No {list.TypeLabel} yet.", html, StringComparison.Ordinal);
        }

        var (_, stats) = await PageAsync("/admin/", session, factory);
        Assert.Equal(5, Regex.Matches(stats, "<dd id=\"stat-[a-z-]+\">0</dd>").Count);
    }

    [Fact]
    public async Task AListThatCannotBeRead_SaysSo_WithRetry_AndKeepsItsFilters()
    {
        await using var factory = new FaultyStoreFactory();
        var session = await SessionAsync(factory.Services);
        factory.Fault.FailWhen = sql => sql.Contains("FROM \"Offers\"", StringComparison.Ordinal);

        var (response, html) = await PageAsync("/admin/offers?q=lamp&status=Open&sort=amount", session, factory);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Contains("<p id=\"list-error\">We couldn't load offers.</p>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("<a id=\"retry\" class=\"button-link\" href=\"/admin/offers?sort=amount&amp;q=lamp&amp;status=Open\">Retry</a>", html, StringComparison.Ordinal);
        // The filters are still in the form, and the shell is whole.
        Assert.Contains("<input type=\"search\" id=\"q\" name=\"q\" value=\"lamp\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"status-Open\" name=\"status\" value=\"Open\" checked", html, StringComparison.Ordinal);
        Assert.Contains("<h1 id=\"page-heading\">Offers</h1>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", html, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-announce", html, StringComparison.Ordinal);
        // No count is said, so no space is kept for one.
        Assert.Contains("<p id=\"console-status\" class=\"live\" role=\"status\" aria-live=\"polite\"></p>", html, StringComparison.Ordinal);

        // Retry, once the store answers again.
        factory.Fault.FailWhen = null;
        (response, html) = await PageAsync("/admin/offers?q=lamp&status=Open&sort=amount", session, factory);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No results match these filters.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AStatsTotalThatCannotBeCounted_SaysSoOnThatTileOnly()
    {
        await using var factory = new FaultyStoreFactory();
        var session = await SessionAsync(factory.Services);
        // The Accepts count (offers with status 1) fails; nothing else does.
        factory.Fault.FailWhen = sql => sql.Contains("FROM \"Offers\"", StringComparison.Ordinal)
                                       && Regex.IsMatch(sql, "\"Status\" = 1\\b");

        var (response, html) = await PageAsync("/admin/", session, factory);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<dd id=\"stat-accepts\" class=\"stat-error\">Couldn't load <a href=\"/admin/\">Retry</a></dd>", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        foreach (var id in new[] { "stat-participants", "stat-open-negotiations", "stat-offers", "stat-declines" })
            Assert.Contains($"<dd id=\"{id}\">0</dd>", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "stat-error"));
    }

    /// <summary>A store of its own, empty, whose reads can be made to fail.</summary>
    private sealed class FaultyStoreFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new($"Data Source=ListFault_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

        public FaultInjector Fault { get; } = new();

        public FaultyStoreFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(TestCoreOwner.ConfigurationKey, TestCoreOwner.Email);
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

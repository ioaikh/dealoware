using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Api.Tests;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace Dealoware.AdminUi.Tests;

internal sealed class AdminUiHarness : IAsyncLifetime
{
    public const string FakeOrigin = "https://admin.test.local";
    private const string AdminHost = "admin.core.dealoware.com";

    private IsolatedWebApplicationFactory? _factory;
    private HttpClient? _client;
    private readonly SemaphoreSlim _fulfillGate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IsolatedWebApplicationFactory Factory => _factory!;

    public async Task InitializeAsync()
    {
        DenyLiveHost();
        _factory = new IsolatedWebApplicationFactory();
        _client = _factory.CreateClient();
        _playwright = await Playwright.CreateAsync();
        try
        {
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        }
        catch (PlaywrightException)
        {
            Microsoft.Playwright.Program.Main(["install", "chromium"]);
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
        _client?.Dispose();
        _fulfillGate.Dispose();
        _factory?.Dispose();
    }

    public static void DenyLiveHost()
    {
        foreach (var name in new[] { "PLAYWRIGHT_BASE_URL", "ADMIN_BASE_URL", "ADMIN_UI_BASE_URL" })
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                continue;
            if (value.Contains("admin.core.dealoware.com", StringComparison.OrdinalIgnoreCase)
                && Environment.GetEnvironmentVariable("ALLOW_LIVE_ADMIN_CORE") != "1")
            {
                throw new InvalidOperationException(
                    $"{name} must be a local instance. Live admin.core is forbidden.");
            }
        }
    }

    public async Task<Guid> SeedSessionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create("io@aiknowhow.com", "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    public async Task<SeededEditGraph> SeedGraphAsync(string prefix, int openOffers = 1, bool acceptedOffer = false)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var a = Participant.Create($"{prefix}-A");
        var b = Participant.Create($"{prefix}-B");
        db.Participants.AddRange(a, b);
        var artifact = Artifact.Create(a.Sub, [SubjectEntity.Create($"{prefix}-Name", $"{prefix}-Desc")], "sell");
        db.Artifacts.Add(artifact);
        var (openN, e1) = Negotiation.Create(artifact.Id, a.Sub, b.Sub, "sell", "buy", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(2));
        var (closedN, e2) = Negotiation.Create(artifact.Id, a.Sub, b.Sub, "sell", "buy");
        Assert.Empty(e1);
        Assert.Empty(e2);
        closedN!.Close();
        db.Negotiations.AddRange(openN!, closedN);

        var offers = new List<Offer>();
        for (var i = 0; i < openOffers; i++)
        {
            var from = i % 2 == 0 ? a.Sub : b.Sub;
            var to = i % 2 == 0 ? b.Sub : a.Sub;
            var (offer, err) = Offer.Create(openN!.Id, from, to, 10 + i, "USD", $"{prefix}-open-{i}");
            Assert.Empty(err);
            offers.Add(offer!);
        }

        Offer? accepted = null;
        if (acceptedOffer)
        {
            var (offer, err) = Offer.Create(openN!.Id, b.Sub, a.Sub, 99m, "USD", $"{prefix}-accepted");
            Assert.Empty(err);
            offer!.Accept(a.Sub);
            accepted = offer;
            offers.Add(offer);
        }

        db.Offers.AddRange(offers);
        await db.SaveChangesAsync();
        return new SeededEditGraph(a, b, artifact, openN!, closedN, offers, accepted);
    }

    public async Task<(IBrowserContext Context, IPage Page)> NewPageAsync(Guid? sessionId)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            BaseURL = FakeOrigin,
            JavaScriptEnabled = true
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", route => FulfillAsync(route, sessionId));
        return (context, page);
    }

    private async Task FulfillAsync(IRoute route, Guid? sessionId)
    {
        // One in-memory SQLite connection: serialize TestServer calls so HTML/CSS/JS
        // plus the session-gated API fetch cannot overlap.
        await _fulfillGate.WaitAsync();
        try
        {
            var uri = new Uri(route.Request.Url);
            var message = new HttpRequestMessage(new HttpMethod(route.Request.Method), uri.PathAndQuery);
            message.Headers.Host = AdminHost;
            if (sessionId is { } id)
                message.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");

            foreach (var header in route.Request.Headers)
            {
                if (header.Key.Equals("If-Match", StringComparison.OrdinalIgnoreCase))
                    message.Headers.TryAddWithoutValidation("If-Match", header.Value);
            }

            if (route.Request.PostData != null)
            {
                message.Content = new StringContent(route.Request.PostData, Encoding.UTF8, "application/json");
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            using var response = await _client!.SendAsync(message);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var headers = new Dictionary<string, string>();
            if (response.Content.Headers.ContentType is { } ct)
                headers["content-type"] = ct.ToString();
            if (response.Headers.ETag is { } etag)
                headers["etag"] = etag.ToString();
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = (int)response.StatusCode,
                BodyBytes = bytes,
                Headers = headers
            });
        }
        finally
        {
            _fulfillGate.Release();
        }
    }

    public static async Task WriteAxeAndAssertAsync(IPage page, string caseId)
    {
        var results = await page.RunAxe();
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        var dirs = new List<string> { Path.Combine("test-results", "axe") };
        if (Directory.Exists("/opt/cursor/artifacts"))
            dirs.Add(Path.Combine("/opt/cursor/artifacts", "axe"));
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, $"{caseId}.json"), json);
        }

        var violations = (results.Violations ?? []).ToArray();
        var html = await page.ContentAsync();
        var isHtmlPage = html.Contains("<html", StringComparison.OrdinalIgnoreCase)
                         && html.Contains("<title>", StringComparison.OrdinalIgnoreCase);
        if (!isHtmlPage)
            return;

        Assert.True(
            violations.Length == 0,
            $"{caseId} axe failed: {string.Join("; ", violations.Select(v => $"{v.Id}: {v.Description}"))}");
    }

    internal sealed record SeededEditGraph(
        Participant PartyA,
        Participant PartyB,
        Artifact Artifact,
        Negotiation OpenNegotiation,
        Negotiation ClosedNegotiation,
        List<Offer> Offers,
        Offer? AcceptedOffer);
}

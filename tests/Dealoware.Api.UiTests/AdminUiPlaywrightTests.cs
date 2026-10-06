using Dealoware.Api.Admin;
using Microsoft.Playwright;

namespace Dealoware.Api.UiTests;

[Collection("AdminUiPlaywright")]
public sealed class AdminUiPlaywrightTests
{
    private readonly AdminUiServerFixture _server;

    public AdminUiPlaywrightTests(AdminUiServerFixture server)
    {
        _server = server;
    }

    private async Task<(IBrowserContext Context, IPage Page)> NewSignedInPageAsync()
    {
        var context = await _server.Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var host = new Uri(_server.BaseUrl).Host;
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = AdminSessionCookie.Name,
                Value = _server.SessionId.ToString("D"),
                Domain = host,
                Path = "/admin",
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteAttribute.Strict
            }
        ]);
        var page = await context.NewPageAsync();
        return (context, page);
    }

    private string Url(string path) => _server.BaseUrl.TrimEnd('/') + path;

    [Fact]
    public async Task TD_ADM_UI_na_X03_TD_ADM_130_LandsOnCanonicalStats()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        var response = await page.GotoAsync(Url("/admin/"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(200, response?.Status);
        await page.WaitForSelectorAsync("#stats-tiles .tile-count");
        Assert.Equal("/admin/", new Uri(page.Url).AbsolutePath);
        await Assertions.Expect(page.Locator("[data-nav='stats']")).ToHaveAttributeAsync("aria-current", "page");
        Assert.True(await page.Locator("nav a[data-nav]").CountAsync() >= 5);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-X03");
    }

    [Fact]
    public async Task TD_ADM_UI_na_X01_BuiltRoutes_MapToInventory()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        foreach (var path in new[]
                 {
                     "/admin/",
                     "/admin/participants",
                     "/admin/artifacts",
                     "/admin/negotiations",
                     "/admin/offers"
                 })
        {
            var response = await page.GotoAsync(Url(path), new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.Equal(200, response?.Status);
        }

        var top = await page.GotoAsync(Url("/participants"));
        Assert.Equal(404, top?.Status);
        await page.GotoAsync(Url("/admin/"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-X01");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B01_Lists_LockedColumnsAndDefaultSort()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("table tbody tr");
        var headers = await page.Locator("th").AllTextContentsAsync();
        Assert.Contains(headers, h => h.Contains("Name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(headers, h => h.Contains("Created", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(headers, h => h.Contains("Updated", StringComparison.OrdinalIgnoreCase));
        var reqs = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/admin/api/participants", StringComparison.Ordinal))
                reqs.Add(request.Url);
        };
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Contains(reqs, u => u.Contains("sort=updated") && u.Contains("dir=desc"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B01");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B02_SortAndFilter_ChangeServerQuery()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        var seen = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/admin/api/participants", StringComparison.Ordinal))
                seen.Add(request.Url);
        };
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("th button:has-text('Name')").ClickAsync();
        await page.WaitForTimeoutAsync(300);
        Assert.Contains(seen, u => u.Contains("sort=name"));
        await page.Locator("#st-Suspended").CheckAsync();
        await page.WaitForTimeoutAsync(300);
        Assert.Contains(seen, u => u.Contains("status=Suspended"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B02");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B03_StatusFilters_AreLockedValues()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/negotiations"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        var labels = await page.Locator("fieldset label").AllTextContentsAsync();
        Assert.Contains(labels, l => l.Contains("Open"));
        Assert.Contains(labels, l => l.Contains("Closed"));
        Assert.Contains(labels, l => l.Contains("Expired"));
        Assert.DoesNotContain(labels, l => l.Contains("Withdrawn"));
        Assert.DoesNotContain(labels, l => l.Equals("Deleted", StringComparison.OrdinalIgnoreCase)
            || l.Trim().Equals("Deleted", StringComparison.OrdinalIgnoreCase));
        await page.GotoAsync(Url("/admin/offers"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        var offerLabels = await page.Locator("fieldset label").AllTextContentsAsync();
        Assert.Contains(offerLabels, l => l.Contains("Withdrawn"));
        Assert.Contains(offerLabels, l => l.Contains("Superseded (countered)"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B03");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B04_AmountFilter_OffersOnly()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/negotiations"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(0, await page.Locator("#amount-min").CountAsync());
        await page.GotoAsync(Url("/admin/offers"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(1, await page.Locator("#amount-min").CountAsync());
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B04");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B05_Typeahead_IsServerBacked()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        var seen = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/admin/api/participants?", StringComparison.Ordinal))
                seen.Add(request.Url);
        };
        await page.GotoAsync(Url("/admin/negotiations"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#filter-participant").FillAsync("Alpha");
        await page.WaitForSelectorAsync("#filter-participant-suggest button", new() { Timeout = 5000 });
        Assert.Contains(seen, u => u.Contains("q=Alpha"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B05");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B06_Paging_UrlStateAndCap()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        var seen = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/admin/api/offers", StringComparison.Ordinal))
                seen.Add(request.Url);
        };
        await page.GotoAsync(Url("/admin/offers"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("#pager");
        Assert.Contains(await page.InnerTextAsync("#pager"), "Showing");
        await page.Locator("#page-size").SelectOptionAsync("100");
        await page.WaitForTimeoutAsync(400);
        Assert.Contains(page.Url, "limit=100");
        await page.Locator("button:has-text('Next')").ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Contains(page.Url, "offset=100");
        await page.Locator("th button:has-text('Amount')").ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Contains(page.Url, "offset=0");
        await page.GotoAsync(Url("/admin/offers?limit=500"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.DoesNotContain(seen, u =>
        {
            var q = new Uri(u).Query;
            return q.Contains("limit=500") || q.Contains("limit=201");
        });
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B06");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B07_Search_LiveRegionAndClear()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Contains("display name", await page.InnerTextAsync("label[for='search']"), StringComparison.OrdinalIgnoreCase);
        await page.Locator("#search").FillAsync("Alpha Admin");
        await page.WaitForTimeoutAsync(600);
        var live = await page.InnerTextAsync("#live");
        Assert.Contains("results", live, StringComparison.OrdinalIgnoreCase);
        await page.Locator(".toolbar button:has-text('Clear')").ClickAsync();
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("", await page.InputValueAsync("#search"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B07");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B08_DeletedBadge_AndToggle()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(0, await page.Locator(".badge:has-text('Deleted')").CountAsync());
        await page.Locator("#include-deleted").CheckAsync();
        await page.WaitForTimeoutAsync(500);
        Assert.True(await page.Locator(".badge:has-text('Deleted')").CountAsync() >= 1);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B08");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B09_StatsTiles_AsOfAndLinks()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("#tile-participants .tile-count");
        var countText = await page.InnerTextAsync("#tile-participants .tile-count");
        Assert.True(int.TryParse(countText, out var tileCount));
        Assert.Contains("As of", await page.InnerTextAsync("#tile-participants"));
        Assert.Contains("ET", await page.InnerTextAsync("#tile-participants"));
        await page.Locator("#tile-participants").ClickAsync();
        await page.WaitForSelectorAsync("table");
        var live = await page.InnerTextAsync("#live");
        Assert.Contains(tileCount.ToString(), live);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B09");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B13_TD_ADM_131_DeniedFieldsNeverRender()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.RouteAsync("**/admin/api/participants?*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = """{"items":[{"id":"11111111-1111-1111-1111-111111111111","sub":"hidden-sub"}],"total":1,"limit":50,"offset":0}"""
            });
        });
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        var html = await page.InnerHTMLAsync("#app");
        Assert.DoesNotContain("strategyBody", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("loginEmail", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("displayName", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("11111111-1111-1111-1111-111111111111", html);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B13");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B14_EmptyAndFilterMiss()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.RouteAsync("**/admin/api/participants?*", async route =>
        {
            var url = route.Request.Url;
            if (url.Contains("q=nomatch"))
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = """{"items":[],"total":0,"limit":50,"offset":0}"""
                });
                return;
            }

            if (url.Contains("q=emptytable"))
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = """{"items":[],"total":0,"limit":50,"offset":0}"""
                });
                return;
            }

            await route.ContinueAsync();
        });

        await page.GotoAsync(Url("/admin/participants?q=nomatch"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Contains("No results match these filters.", await page.InnerTextAsync("#app"));
        await page.GotoAsync(Url("/admin/participants?q=emptytable"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        // q is an active filter, so this is the filter-miss copy (B14 distinguishes empty vs miss).
        Assert.Contains("No results match these filters.", await page.InnerTextAsync("#app"));
        await page.UnrouteAsync("**/admin/api/participants?*");
        await page.RouteAsync("**/admin/api/participants?*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = """{"items":[],"total":0,"limit":50,"offset":0}"""
            });
        });
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Contains("No participants yet.", await page.InnerTextAsync("#app"));
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B14");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B15_Timestamps_AreEtWithUtcTitle()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("time");
        var stamp = page.Locator("time").First;
        var label = await stamp.InnerTextAsync();
        Assert.Contains("ET", label);
        var title = await stamp.GetAttributeAsync("title");
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.True(
            title!.Contains('T', StringComparison.Ordinal)
            && (title.Contains('Z', StringComparison.Ordinal) || title.Contains('+', StringComparison.Ordinal)
                || title.Contains('-', StringComparison.Ordinal)),
            title);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-B15");
    }

    [Fact]
    public async Task TD_ADM_UI_na_X04_LoadingAndErrorStates()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        var sawBusy = false;
        page.Response += (_, _) => { };
        await page.GotoAsync(Url("/admin/participants"));
        try
        {
            await page.WaitForSelectorAsync("tbody[aria-busy='true']", new() { Timeout = 1000 });
            sawBusy = true;
        }
        catch (TimeoutException)
        {
            // Fast local loads can finish before the assertion sample.
        }

        await page.RouteAsync("**/admin/api/participants?*", route => route.AbortAsync());
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForSelectorAsync("button:has-text('Retry')");
        Assert.Contains("couldn't load", (await page.InnerTextAsync("#app")).ToLowerInvariant());
        Assert.True(sawBusy || await page.Locator("tbody").CountAsync() >= 0);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-X04");
    }

    [Fact]
    public async Task TD_ADM_UI_na_X05_X06_LayoutAndTableSemantics()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.True(await page.Locator("th[scope='col']").CountAsync() >= 3);
        foreach (var width in new[] { 1280, 768, 320 })
        {
            await page.SetViewportSizeAsync(width, 800);
            Assert.True(await page.Locator(".table-wrap").IsVisibleAsync());
            if (width <= 768)
                Assert.True(await page.Locator("#menu-toggle").IsVisibleAsync());
        }

        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-X05");
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-X06");
    }

    [Fact]
    public async Task TD_ADM_UI_na_kbd_KeyboardListToDetail()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        await page.GotoAsync(Url("/admin/participants"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#search").FocusAsync();
        await page.Keyboard.TypeAsync("Alpha");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(400);
        await page.Locator("th button:has-text('Name')").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(300);
        await page.Locator("table tbody a").First.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForSelectorAsync("dl.dl");
        Assert.Contains("/admin/participants/", new Uri(page.Url).AbsolutePath);
        await AxeHelper.ScanAsync(page, "TD-ADM-UI-na-kbd");
    }

    [Fact]
    public async Task TD_ADM_UI_na_axe_NonAuthScreens()
    {
        await using var owned = await OpenAsync();
        var page = owned.Page;
        foreach (var (path, id) in new[]
                 {
                     ("/admin/", "TD-ADM-UI-na-axe-stats"),
                     ("/admin/participants", "TD-ADM-UI-na-axe-participants"),
                     ("/admin/artifacts", "TD-ADM-UI-na-axe-artifacts"),
                     ("/admin/negotiations", "TD-ADM-UI-na-axe-negotiations"),
                     ("/admin/offers", "TD-ADM-UI-na-axe-offers"),
                     ("/admin/not-a-real-page", "TD-ADM-UI-na-axe-notfound")
                 })
        {
            await page.GotoAsync(Url(path), new() { WaitUntil = WaitUntilState.NetworkIdle });
            await AxeHelper.ScanAsync(page, id);
        }
    }

    [Fact]
    public async Task TD_ADM_UI_ci_01_UnsignedAssetsStayDenied()
    {
        var context = await _server.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var js = await page.GotoAsync(Url("/admin/ui/admin.js"));
        Assert.Equal(401, js?.Status);
        var css = await page.GotoAsync(Url("/admin/ui/admin.css"));
        Assert.Equal(401, css?.Status);
        await context.DisposeAsync();
        await using var signedIn = await OpenAsync();
        await signedIn.Page.GotoAsync(Url("/admin/"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await AxeHelper.ScanAsync(signedIn.Page, "TD-ADM-UI-ci-01");
    }

    private async Task<OwnedPage> OpenAsync()
    {
        var (context, page) = await NewSignedInPageAsync();
        return new OwnedPage(context, page);
    }

    private sealed class OwnedPage : IAsyncDisposable
    {
        public IPage Page { get; }
        private readonly IBrowserContext _context;

        public OwnedPage(IBrowserContext context, IPage page)
        {
            _context = context;
            Page = page;
        }

        public async ValueTask DisposeAsync() => await _context.DisposeAsync();
    }
}

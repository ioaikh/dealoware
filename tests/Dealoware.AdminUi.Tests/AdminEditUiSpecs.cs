using Dealoware.Domain.Negotiations;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace Dealoware.AdminUi.Tests;

[Collection("AdminUiPlaywright")]
public sealed class AdminEditUiSpecs : IAsyncLifetime
{
    private readonly AdminUiHarness _harness = new();

    public Task InitializeAsync() => _harness.InitializeAsync();

    public Task DisposeAsync() => _harness.DisposeAsync();

    [Fact]
    public async Task TD_ADM_UI_na_B10_OnlySa4FieldsAreInputs()
    {
        var prefix = $"b10-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix, openOffers: 1, acceptedOffer: true);
        var session = await _harness.SeedSessionAsync();
        var (context, page) = await _harness.NewPageAsync(session);
        await using var _ = context;

        await page.GotoAsync($"/admin/participants/{graph.PartyA.Id:D}/edit");
        await page.Locator("#displayName").WaitForAsync();
        await Expect(page.Locator("#displayName")).ToBeVisibleAsync();
        await Expect(page.Locator("#isActive")).ToBeVisibleAsync();
        await Expect(page.Locator("#loginEmail-readonly")).ToBeVisibleAsync();
        Assert.Contains("(required)", await page.Locator("label[for='isActive']").InnerTextAsync());

        await page.GotoAsync($"/admin/artifacts/{graph.Artifact.Id:D}/edit");
        await page.Locator("#name").WaitForAsync();
        await Expect(page.Locator("#name")).ToBeVisibleAsync();
        await Expect(page.Locator("#description")).ToBeVisibleAsync();
        await Expect(page.Locator("#ownerSearch")).ToBeVisibleAsync();
        await Expect(page.Locator("#intent-readonly")).ToBeVisibleAsync();
        Assert.Contains("(required)", await page.Locator("label[for='name']").InnerTextAsync());

        await page.GotoAsync($"/admin/negotiations/{graph.OpenNegotiation.Id:D}/edit");
        await page.Locator("#status").WaitForAsync();
        var openOptions = await page.Locator("#status option").AllTextContentsAsync();
        Assert.Contains("Closed", openOptions);
        Assert.Contains("Expired", openOptions);
        Assert.DoesNotContain("Withdrawn", openOptions);
        await Expect(page.Locator("#endsAt")).ToBeEnabledAsync();
        await Expect(page.Locator("#expire-open")).ToBeVisibleAsync();

        await page.GotoAsync($"/admin/negotiations/{graph.ClosedNegotiation.Id:D}/edit");
        await page.Locator("#status-readonly").WaitForAsync();
        await Expect(page.Locator("#status-readonly")).ToHaveTextAsync("Closed");
        await Expect(page.Locator("#expire-open")).ToBeHiddenAsync();

        await page.GotoAsync($"/admin/offers/{graph.Offers.First(o => o.Status == OfferStatus.Open).Id:D}/edit");
        await page.Locator("#status").WaitForAsync();
        var offerOptions = await page.Locator("#status option").AllTextContentsAsync();
        Assert.Contains("Cancelled", offerOptions);
        Assert.DoesNotContain("Withdrawn", offerOptions);
        Assert.DoesNotContain("Accepted", offerOptions);

        await page.GotoAsync($"/admin/offers/{graph.AcceptedOffer!.Id:D}/edit");
        await page.Locator("#amount-readonly").WaitForAsync();
        await Expect(page.Locator("#amount-readonly")).ToBeVisibleAsync();
        await Expect(page.Locator("#terms-readonly")).ToBeVisibleAsync();

        await AdminUiHarness.WriteAxeAndAssertAsync(page, "TD-ADM-UI-na-B10");
    }

    [Fact]
    public async Task TD_ADM_UI_na_expire_DialogCountFocusEscapeAndSuccess()
    {
        var prefix = $"ex-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix, openOffers: 3, acceptedOffer: true);
        var zero = await _harness.SeedGraphAsync(prefix + "z", openOffers: 0);
        var session = await _harness.SeedSessionAsync();
        var (context, page) = await _harness.NewPageAsync(session);
        await using var _ = context;

        await page.GotoAsync($"/admin/negotiations/{graph.OpenNegotiation.Id:D}/edit");
        await page.Locator("#expire-open").WaitForAsync();
        await page.Locator("#expire-open").PressAsync("Enter");
        await page.Locator("#expire-dialog").WaitForAsync();
        await Expect(page.Locator("#expire-title")).ToHaveTextAsync($"Expire negotiation {graph.OpenNegotiation.Id:D}?");
        await Expect(page.Locator("#expire-body")).ToHaveTextAsync("3 open offers will be cancelled.");
        Assert.Equal("dialog", await page.Locator("#expire-dialog").GetAttributeAsync("role"));
        Assert.Equal("true", await page.Locator("#expire-dialog").GetAttributeAsync("aria-modal"));
        await Expect(page.Locator("#expire-cancel")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("#expire-dialog")).ToBeHiddenAsync();

        using (var scope = _harness.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var n = await db.Negotiations.FirstAsync(x => x.Id == graph.OpenNegotiation.Id);
            Assert.Equal(NegotiationStatus.Open, n.Status);
            Assert.Equal(0, await db.AdminAuditLog.CountAsync(a => a.EntityId == graph.OpenNegotiation.Id && a.Action == "Edit"));
        }

        await page.Locator("#expire-open").ClickAsync();
        await page.Locator("#expire-dialog .modal-backdrop").ClickAsync(new()
        {
            Force = true,
            Position = new() { X = 4, Y = 4 }
        });
        await Expect(page.Locator("#expire-dialog")).ToBeHiddenAsync();

        var patches = 0;
        page.Request += (_, req) =>
        {
            if (req.Method == "PATCH")
                patches++;
        };
        await page.Locator("#expire-open").ClickAsync();
        await page.Locator("#expire-confirm").DblClickAsync();
        await page.Locator("#live").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Expect(page.Locator("#live")).ToHaveTextAsync($"Negotiation {graph.OpenNegotiation.Id:D} expired.");
        Assert.Equal(1, patches);

        using (var scope = _harness.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var n = await db.Negotiations.FirstAsync(x => x.Id == graph.OpenNegotiation.Id);
            Assert.Equal(NegotiationStatus.Expired, n.Status);
            var openLeft = await db.Offers.CountAsync(o => o.NegotiationId == n.Id && o.Status == OfferStatus.Open);
            Assert.Equal(0, openLeft);
            var accepted = await db.Offers.FirstAsync(o => o.Id == graph.AcceptedOffer!.Id);
            Assert.Equal(OfferStatus.Accepted, accepted.Status);
        }

        await page.GotoAsync($"/admin/negotiations/{zero.OpenNegotiation.Id:D}/edit");
        await page.Locator("#expire-open").ClickAsync();
        await Expect(page.Locator("#expire-body")).ToHaveTextAsync("No open offers will be cancelled.");
        await page.Keyboard.PressAsync("Escape");

        await page.GotoAsync($"/admin/negotiations/{graph.ClosedNegotiation.Id:D}/edit");
        await page.Locator("#status-readonly").WaitForAsync();
        await Expect(page.Locator("#expire-open")).ToBeHiddenAsync();

        await page.GotoAsync($"/admin/negotiations/{zero.OpenNegotiation.Id:D}/edit");
        await page.RouteAsync("**/admin/api/negotiations/**", async route =>
        {
            if (route.Request.Method == "PATCH")
            {
                await route.FulfillAsync(new() { Status = 500, ContentType = "application/json", Body = """{"error":"BadRequest"}""" });
                return;
            }
            await route.FallbackAsync();
        });
        await page.Locator("#expire-open").ClickAsync();
        await page.Locator("#expire-confirm").ClickAsync();
        await Expect(page.Locator("#expire-error")).ToBeVisibleAsync();
        await Expect(page.Locator("#expire-error")).ToHaveTextAsync("Nothing was changed.");
        await Expect(page.Locator("#expire-retry")).ToBeVisibleAsync();
        await page.UnrouteAsync("**/admin/api/negotiations/**");
        await page.Locator("#expire-retry").ClickAsync();
        await Expect(page.Locator("#live")).ToHaveTextAsync($"Negotiation {zero.OpenNegotiation.Id:D} expired.");

        await AdminUiHarness.WriteAxeAndAssertAsync(page, "TD-ADM-UI-na-expire");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B11_SaveCancelDiscardAndErrors()
    {
        var prefix = $"b11-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix);
        var session = await _harness.SeedSessionAsync();
        var (context, page) = await _harness.NewPageAsync(session);
        await using var _ = context;

        await page.GotoAsync($"/admin/participants/{graph.PartyA.Id:D}/edit");
        await page.Locator("#displayName").WaitForAsync();
        await Expect(page.Locator("#save")).ToBeDisabledAsync();
        await page.Locator("#displayName").FillAsync($"{prefix}-edited");

        var patches = 0;
        page.Request += (_, req) =>
        {
            if (req.Method == "PATCH")
                patches++;
        };
        await page.Locator("#save").DblClickAsync();
        await Expect(page.Locator("#live")).ToHaveTextAsync("Changes saved.");
        Assert.Equal(1, patches);

        await page.Locator("#displayName").FillAsync("temp");
        await page.Locator("#cancel").ClickAsync();
        await Expect(page.Locator("#discard-dialog")).ToBeVisibleAsync();
        await page.Locator("#discard-keep").ClickAsync();
        await Expect(page.Locator("#displayName")).ToHaveValueAsync("temp");
        await page.Locator("#cancel").ClickAsync();
        await page.Locator("#discard-confirm").ClickAsync();
        await Expect(page.Locator("#discard-dialog")).ToBeHiddenAsync();

        await page.GotoAsync($"/admin/artifacts/{graph.Artifact.Id:D}/edit");
        await page.Locator("#edit-form").WaitForAsync();
        await page.Locator("#name").WaitForAsync();
        await page.Locator("#name").FillAsync("");
        await page.Locator("#description").FillAsync(new string('d', 4097));
        await page.Locator("#save").ClickAsync();
        await Expect(page.Locator("#error-summary")).ToBeVisibleAsync();
        await Expect(page.Locator("#error-summary")).ToBeFocusedAsync();
        Assert.Contains("Name is required", await page.Locator("#error-summary").InnerTextAsync());

        await AdminUiHarness.WriteAxeAndAssertAsync(page, "TD-ADM-UI-na-B11");
    }

    [Fact]
    public async Task TD_ADM_UI_na_B12_ConflictKeepsTypedValues()
    {
        var prefix = $"b12-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix);
        var session = await _harness.SeedSessionAsync();
        var (contextA, pageA) = await _harness.NewPageAsync(session);
        await using var _ = contextA;

        await pageA.GotoAsync($"/admin/participants/{graph.PartyA.Id:D}/edit");
        await pageA.Locator("#displayName").WaitForAsync();

        using (var scope = _harness.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = await db.Participants.FirstAsync(p => p.Id == graph.PartyA.Id);
            row.UpdateDisplayName($"{prefix}-other");
            row.MarkEdited();
            await db.SaveChangesAsync();
        }

        await pageA.Locator("#displayName").FillAsync($"{prefix}-mine");
        await pageA.Locator("#save").ClickAsync();
        await Expect(pageA.Locator("#conflict")).ToBeVisibleAsync();
        await Expect(pageA.Locator("#conflict")).ToContainTextAsync("This record changed since you opened it. Reload to see the latest version, then reapply your changes.");
        await Expect(pageA.Locator("#displayName")).ToHaveValueAsync($"{prefix}-mine");
        await pageA.Locator("#reload").ClickAsync();
        await Expect(pageA.Locator("#typed-values")).ToContainTextAsync($"{prefix}-mine");

        await AdminUiHarness.WriteAxeAndAssertAsync(pageA, "TD-ADM-UI-na-B12");
    }

    [Fact]
    public async Task TD_ADM_130_EditSmoke_LocalAdminPaths()
    {
        var prefix = $"s130-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix);
        var session = await _harness.SeedSessionAsync();
        var (context, page) = await _harness.NewPageAsync(session);
        await using var _ = context;

        await page.GotoAsync($"/admin/participants/{graph.PartyA.Id:D}/edit");
        await page.Locator("#displayName").WaitForAsync();
        await page.Locator("#displayName").FillAsync($"{prefix}-smoke");
        await page.Locator("#save").ClickAsync();
        await Expect(page.Locator("#live")).ToHaveTextAsync("Changes saved.");
        Assert.StartsWith("/admin/", new Uri(page.Url).AbsolutePath);
        await AdminUiHarness.WriteAxeAndAssertAsync(page, "TD-ADM-130");
    }

    [Fact]
    public async Task TD_ADM_131_UnauthenticatedEditPage_NoEntityData()
    {
        var prefix = $"s131-{Guid.NewGuid():N}"[..10];
        var graph = await _harness.SeedGraphAsync(prefix);
        var (context, page) = await _harness.NewPageAsync(sessionId: null);
        await using var _ = context;
        await page.GotoAsync($"/admin/participants/{graph.PartyA.Id:D}/edit");
        var body = await page.ContentAsync();
        Assert.DoesNotContain(graph.PartyA.DisplayName!, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unauthorized", body);
        await AdminUiHarness.WriteAxeAndAssertAsync(page, "TD-ADM-131");
    }

    [Fact(Skip = "TD-ADM-UI-na-B13 waits on Step 7 list UI (lists, stats). Active hold — not PASS.")]
    public void TD_ADM_UI_na_B13_SkippedUntilStep7ListUi()
    {
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}

[CollectionDefinition("AdminUiPlaywright")]
public sealed class AdminUiPlaywrightCollection : ICollectionFixture<AdminUiHarnessMarker>;

public sealed class AdminUiHarnessMarker;

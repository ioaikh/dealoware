using System.Reflection;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// The denial test host: a CoreOwner set through test config, the test-only
/// exception probe switched on (it is compiled into Debug builds only), and
/// its own PostgreSQL database when DB_HOST is set.
/// </summary>
public sealed class DenialWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_denial";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        builder.UseSetting(AdminTestExceptionEndpoint.ConfigKey, "true");
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// T08a: the route table (<see cref="AdminRouteTable"/>) held against the
/// app's own endpoints, and every row of it through the denial checks of its
/// class (T08 spec, "The denial checks"). Each test names the test design
/// cases (TD-ADM-*) it covers.
/// </summary>
[Collection("WebAppTests")]
public partial class AdminRouteDenialTests : IClassFixture<DenialWebApplicationFactory>
{
#if DEBUG
    private const bool DebugBuild = true;
#else
    private const bool DebugBuild = false;
#endif

    private readonly DenialWebApplicationFactory _factory;

    public AdminRouteDenialTests(DenialWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- The route table is every /admin route, both ways (UI-na-X01 step 1) ----------

    [Fact]
    public void TdAdmUiNaX01_TheRouteTable_IsEveryAdminRouteTheAppMaps_AndNothingElse()
    {
        var mapped = MappedEndpoints()
            .Where(e => IsAdminPattern(e.Pattern))
            .Select(e => $"{e.Method} {e.Pattern}")
            .ToHashSet(StringComparer.Ordinal);
        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in AdminRouteTable.Routes.Where(MappedInThisBuild))
        {
            foreach (var method in row.Methods)
                Assert.True(listed.Add($"{method} {row.Pattern}"), $"Listed twice: {method} {row.Pattern}");
        }

        var withoutRow = mapped.Except(listed).Order(StringComparer.Ordinal).ToList();
        var withoutRoute = listed.Except(mapped).Order(StringComparer.Ordinal).ToList();
        Assert.True(
            withoutRow.Count == 0 && withoutRoute.Count == 0,
            "The route table and the app differ.\n"
            + "Mapped by the app, with no row in AdminRouteTable (add one):\n  " + string.Join("\n  ", withoutRow)
            + "\nA row in AdminRouteTable that the app does not map:\n  " + string.Join("\n  ", withoutRoute));
    }

    // The build catches a route without a row: the test-only probe is one, and leaving it out shows up.
    [Fact]
    public void TdAdmUiNaX01_ARouteLeftOutOfTheTable_IsFound()
    {
        var mapped = MappedEndpoints().Where(e => IsAdminPattern(e.Pattern)).ToList();
        Assert.Contains(mapped, e => e.Pattern == AdminCsrfProbeEndpoint.Path && e.Method == "POST");
        var listedWithoutProbe = AdminRouteTable.Routes
            .Where(MappedInThisBuild)
            .Where(r => r.Pattern != AdminCsrfProbeEndpoint.Path)
            .SelectMany(r => r.Methods.Select(m => $"{m} {r.Pattern}"))
            .ToHashSet(StringComparer.Ordinal);
        var missing = mapped.Select(e => $"{e.Method} {e.Pattern}").Where(k => !listedWithoutProbe.Contains(k)).ToList();
        Assert.Equal([$"POST {AdminCsrfProbeEndpoint.Path}"], missing);
    }

    // Every page a browser opens is one Spec X01 screen, and every screen is a page or says why it has no address.
    [Fact]
    public void TdAdmUiNaX01_EveryPageIsOneSpecScreen_AndEveryScreenIsAPageOrSaysWhyNot()
    {
        foreach (var row in AdminRouteTable.Routes)
        {
            var opened = row.IsPage && row.Methods.Contains("GET");
            if (opened)
            {
                Assert.True(row.Screen is not null, $"A page with no Spec screen: {row}");
                Assert.True(AdminRouteTable.SpecScreens.ContainsKey(row.Screen!), $"Not a Spec X01 screen: {row.Screen} ({row})");
                Assert.Null(AdminRouteTable.SpecScreens[row.Screen!]);
            }
            else
            {
                Assert.True(row.Screen is null, $"Only a page a browser opens is a screen: {row}");
            }
        }

        foreach (var (screen, reason) in AdminRouteTable.SpecScreens)
        {
            var rows = AdminRouteTable.Routes.Count(r => r.Screen == screen);
            if (reason is null)
                Assert.True(rows > 0, $"Spec screen {screen} has no route.");
            else
                Assert.True(rows == 0, $"Spec screen {screen} says it has no route of its own, but has one.");
        }

        // S-B6 is the four detail pages; every other screen is one address.
        foreach (var group in AdminRouteTable.Routes.Where(r => r.Screen is not null).GroupBy(r => r.Screen))
            Assert.Equal(group.Key == "S-B6" ? 4 : 1, group.Count());
    }

    // The signed-out and sign-in-in-progress rows are exactly the gate's exemption list, and nothing skips the anti-forgery check.
    [Fact]
    public void TdAdm004_TheExemptionList_IsExactlyTheSignedOutAndSignInInProgressRows()
    {
        static string Key(string method, string path) => $"{method.ToUpperInvariant()} {path}";

        var exempt = AdminSignedOutExemptions.Rules
            .Select(r => (Key(r.Method.Method, r.Path), r.Token))
            .ToList();
        var rows = AdminRouteTable.Routes
            .Where(r => r.Class is AdminRouteClass.SignedOut or AdminRouteClass.SignInInProgress)
            .SelectMany(r => r.Methods.Select(m => (Key(m, r.Pattern), r.Class == AdminRouteClass.SignedOut
                ? AdminSignedOutExemptions.TokenKind.None
                : AdminSignedOutExemptions.TokenKind.PendingSignIn)))
            .ToList();

        Assert.Equal(exempt.Order(), rows.Order());

        var allowList = (HashSet<string>)typeof(AdminSessionMiddleware)
            .GetField("AntiForgeryTokenAllowList", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        Assert.Empty(allowList);
    }

    // ---- helpers: the app's endpoints ----------------------------------------------

    private sealed record MappedEndpoint(string Pattern, string Method);

    private IReadOnlyList<MappedEndpoint> MappedEndpoints()
    {
        var source = _factory.Services.GetRequiredService<EndpointDataSource>();
        var list = new List<MappedEndpoint>();
        foreach (var endpoint in source.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = endpoint.RoutePattern.RawText ?? string.Empty;
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
            if (methods is null || methods.Count == 0)
            {
                list.Add(new MappedEndpoint(pattern, "*"));
                continue;
            }

            list.AddRange(methods.Select(m => new MappedEndpoint(pattern, m.ToUpperInvariant())));
        }

        return list;
    }

    private static bool IsAdminPattern(string pattern)
        => pattern.Equals("/admin", StringComparison.OrdinalIgnoreCase)
           || pattern.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase);

    private static bool MappedInThisBuild(AdminRoute row) => !row.DebugBuildOnly || DebugBuild;
}

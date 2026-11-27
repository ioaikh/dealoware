using System.Reflection;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// The denial test host: a CoreOwner set through test config, the test-only
/// exception probe switched on (it is compiled into Debug builds only). The
/// denial checks need no PostgreSQL: they run on SQLite only.
/// </summary>
public sealed class DenialWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    /// <summary>An engine access token, issued once for this host (registration is rate limited).</summary>
    public string? EngineToken { get; set; }

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

    // The static folders of the table are the app's only static-file mounts: no third one can serve files under /admin.
    [Fact]
    public void TdAdm003_TheTwoStaticFolders_AreTheOnlyStaticFileMounts()
    {
        var api = Path.Combine(RepoRoot(), "src", "Dealoware.Api");
        var sources = Directory.EnumerateFiles(api, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToDictionary(f => Path.GetRelativePath(api, f), File.ReadAllText);

        int Count(string call) => sources.Values.Sum(text => System.Text.RegularExpressions.Regex.Matches(text, call).Count);

        // Every way ASP.NET Core serves files from disk, counted across the whole API project.
        Assert.Equal(0, Count(@"\bMapStaticAssets\s*\("));
        Assert.Equal(0, Count(@"\bUseFileServer\s*\("));
        Assert.Equal(0, Count(@"\bUseDirectoryBrowser\s*\("));
        Assert.Equal(0, Count(@"\bMapFallbackToFile\s*\("));
        Assert.Equal(0, Count(@"\bResults\.File\s*\(|\bTypedResults\.(Physical)?File\s*\(|\bResults\.PhysicalFile\s*\("));
        Assert.Equal(2, Count(@"\bUseStaticFiles\s*\("));
        Assert.Equal(1, Count(@"\bPhysicalFileProvider\s*\("));

        var program = sources["Program.cs"].Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(program, @"\bUseStaticFiles\s*\(").Count);

        // One: wwwroot, before the gate, only outside /admin or under /admin/auth (the signed-out folder).
        var signedOut = AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedOut);
        Assert.Equal(AdminSignedOutExemptions.AuthStaticPrefix, signedOut.UrlPrefix);
        Assert.Equal(Path.Combine("wwwroot", "admin", "auth"), signedOut.Folder);
        Assert.Contains(
            "app.UseWhen(\n"
            + "    context => !context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix)\n"
            + "               || context.Request.Path.StartsWithSegments(AdminSignedOutExemptions.AuthStaticPrefix),\n"
            + "    branch =>\n"
            + "    {\n"
            + "        branch.UseDefaultFiles();\n"
            + "        branch.UseStaticFiles();\n"
            + "    });",
            program,
            StringComparison.Ordinal);
        // wwwroot/admin holds the signed-out folder and nothing else.
        var webAdmin = Path.Combine(api, "wwwroot", "admin");
        Assert.Equal(["auth"], Directory.GetDirectories(webAdmin).Select(Path.GetFileName));
        Assert.Empty(Directory.GetFiles(webAdmin));

        // Two: the signed-in folder, after the gate.
        var signedIn = AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedIn);
        Assert.Equal(AdminSignedInPage.AssetPrefix, signedIn.UrlPrefix);
        Assert.Equal(AdminSignedInPage.AssetFolder, signedIn.Folder);
        Assert.Contains(
            "app.UseStaticFiles(new StaticFileOptions\n"
            + "{\n"
            + "    RequestPath = AdminSignedInPage.AssetPrefix,\n"
            + "    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(\n"
            + "        Path.Combine(app.Environment.ContentRootPath, AdminSignedInPage.AssetFolder))\n"
            + "});",
            program,
            StringComparison.Ordinal);
        var gate = program.IndexOf("app.UseMiddleware<AdminSessionMiddleware>();", StringComparison.Ordinal);
        Assert.True(gate > 0
                    && program.IndexOf("app.UseWhen(", StringComparison.Ordinal) < gate
                    && program.IndexOf("RequestPath = AdminSignedInPage.AssetPrefix", StringComparison.Ordinal) > gate);
    }

    // Live: no file the app has on disk is served under /admin without a session, but those of the signed-out folder.
    [Fact]
    public async Task TdAdm003_NoOtherFileOnDisk_IsServedUnderAdmin_WithoutASession()
    {
        var api = Path.Combine(RepoRoot(), "src", "Dealoware.Api");
        var webRoot = Path.Combine(api, "wwwroot");
        var signedOutFolder = Path.Combine(api, AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedOut).Folder);

        // Every file of the web root and of the content root's own folders, asked for under /admin by its
        // path from either root: a file served there without a session would be a mount no row lists.
        var asked = new List<string>();
        foreach (var file in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories))
        {
            if (file.StartsWith(signedOutFolder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue;
            asked.Add("/admin/" + Path.GetRelativePath(webRoot, file).Replace('\\', '/'));
        }

        foreach (var file in Directory.EnumerateFiles(api, "*", SearchOption.TopDirectoryOnly)
                     .Concat(Directory.EnumerateFiles(Path.Combine(api, AdminSignedInPage.AssetFolder))))
        {
            asked.Add("/admin/" + Path.GetRelativePath(api, file).Replace('\\', '/'));
        }

        Assert.NotEmpty(asked);
        foreach (var path in asked)
        {
            var reply = await SendAsync("GET", path);
            // The gate's two refusals: a page path goes to sign-in, anything else is the 401. Never the file.
            var refused = (reply.Status == 401 && reply.Body == UnauthorizedBody)
                          || (reply.Status == 302 && reply.Location == AdminSignInPages.SessionEndedLocation && reply.Body.Length == 0);
            Assert.True(refused, $"GET {path} without a session: {reply}");
        }
    }

    // The browser walk opens every signed-in page of the table, signed out, and nothing else (TD-ADM-131).
    [Fact]
    public void TdAdm131_TheSignedOutBrowserWalk_OpensEverySignedInPageOfTheTable()
    {
        var spec = File.ReadAllText(Path.Combine(RepoRoot(), "e2e", "console", "signed-out-walk.spec.ts"));
        var block = System.Text.RegularExpressions.Regex.Match(
            spec, @"const SIGNED_IN_PAGES = \[(?<list>[^\]]*)\];", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(block.Success, "signed-out-walk.spec.ts has no SIGNED_IN_PAGES list.");
        var walked = System.Text.RegularExpressions.Regex.Matches(block.Groups["list"].Value, "'([^']+)'")
            .Select(m => m.Groups[1].Value)
            .ToList();
        var pages = AdminRouteTable.Routes
            .Where(r => r.Class == AdminRouteClass.SignedIn && r.IsPage && r.Methods.Contains("GET"))
            .Select(r => r.Pattern)
            .ToList();

        Assert.Equal(pages.Order(StringComparer.Ordinal), walked.Order(StringComparer.Ordinal));
        Assert.Equal(walked.Count, walked.Distinct().Count());
    }

    // ---- helpers: the app's endpoints ----------------------------------------------

    private sealed record MappedEndpoint(string Pattern, string Method);

    private IReadOnlyList<MappedEndpoint> MappedEndpoints()
    {
        var source = _factory.Services.GetRequiredService<EndpointDataSource>();
        var list = new List<MappedEndpoint>();
        foreach (var endpoint in source.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');
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
    {
        var path = "/" + pattern.TrimStart('/');
        return path.Equals("/admin", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MappedInThisBuild(AdminRoute row) => !row.DebugBuildOnly || DebugBuild;
}

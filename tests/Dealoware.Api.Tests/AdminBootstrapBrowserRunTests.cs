using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// Pins on the browser run for the first-time setup page (e2e/bootstrap): it
/// stays on this machine, needs no privilege, keeps the loopback proxy guard,
/// and uses the stand-in Turnstile verifier only with the app in Development.
/// The browser tests themselves are started with e2e/bootstrap/run.sh.
/// </summary>
public class AdminBootstrapBrowserRunTests
{
    [Fact]
    public void BrowserRun_StaysOnThisMachine_AndUsesTheStandInVerifierOnlyInDevelopment()
    {
        var dir = Path.Combine(RepoRoot(), "e2e", "bootstrap");
        var runner = File.ReadAllText(Path.Combine(dir, "run.sh"));
        Assert.DoesNotContain("\r", runner, StringComparison.Ordinal);
        var code = string.Join(
            "\n",
            runner.Split('\n').Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal)));

        Assert.DoesNotContain("sudo", code, StringComparison.Ordinal);
        Assert.DoesNotContain("sysctl", code, StringComparison.Ordinal);
        Assert.DoesNotContain("setcap", code, StringComparison.Ordinal);
        Assert.DoesNotContain("/etc/hosts", code, StringComparison.Ordinal);
        Assert.DoesNotContain("0.0.0.0", code, StringComparison.Ordinal);
        Assert.DoesNotContain("host-resolver-rules", code, StringComparison.Ordinal);
        Assert.Contains("export ASPNETCORE_ENVIRONMENT=Development", code, StringComparison.Ordinal);
        Assert.Contains("export ASPNETCORE_URLS=\"https://127.0.0.1:${API_PORT}\"", code, StringComparison.Ordinal);
        Assert.Contains(
            "export Admin__Turnstile__VerifyUrl=\"http://127.0.0.1:${VERIFY_PORT}/siteverify\"",
            code,
            StringComparison.Ordinal);
        Assert.Contains("ThreadingHTTPServer((\"127.0.0.1\", port), Handler)", code, StringComparison.Ordinal);
        Assert.Contains("export BOOTSTRAP_E2E_PROXY=\"http://127.0.0.1:${PROXY_PORT}\"", code, StringComparison.Ordinal);
        Assert.Contains("playwright.config.cjs", code, StringComparison.Ordinal);
        foreach (Match port in Regex.Matches(code, @"_PORT=(\d+)"))
        {
            Assert.True(int.Parse(port.Groups[1].Value) >= 1024, "run.sh must not bind a port below 1024");
        }

        // No ports shared with the reset-confirm run, so both can be started side by side.
        var resetRunner = File.ReadAllText(Path.Combine(RepoRoot(), "e2e", "reset-confirm", "run.sh"));
        foreach (Match port in Regex.Matches(code, @"_PORT=(\d+)"))
        {
            Assert.DoesNotContain("PORT=" + port.Groups[1].Value, resetRunner, StringComparison.Ordinal);
        }

        // The run's proxy guard is the reset-confirm one: loopback only.
        var config = File.ReadAllText(Path.Combine(dir, "playwright.config.cjs"));
        Assert.Contains("require(\"../reset-confirm/require-proxy.cjs\")", config, StringComparison.Ordinal);
        Assert.Contains("guard(requireResetConfirmProxy, \"BOOTSTRAP_E2E_PROXY\")", config, StringComparison.Ordinal);
        Assert.Contains("guard(requireResetConfirmBaseUrl, \"BOOTSTRAP_E2E_BASE_URL\")", config, StringComparison.Ordinal);
        // Declared once, then passed to the shared settings and to the browser project.
        Assert.Single(Regex.Matches(config, "const proxy = "));
        Assert.Equal(2, Regex.Matches(config, @"(?m)^\s+proxy,\r?$").Count);

        // The spec answers Cloudflare itself and never opens its own browser or drops the proxy.
        var spec = File.ReadAllText(Path.Combine(dir, "bootstrap.spec.ts"));
        Assert.Contains("page.route((url) => url.hostname === WIDGET_HOST", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("chromium.launch", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("launchPersistentContext", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("newContext(", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("proxy:", spec, StringComparison.Ordinal);
        Assert.Contains("test.use({ javaScriptEnabled: false })", spec, StringComparison.Ordinal);
        // Passwords are made up at run time, never written down.
        Assert.Contains("const GOOD_PASSWORD = crypto.randomBytes(16).toString('hex');", spec, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserRun_HasItsOwnPackage_WithPlaywrightAtTheSharedPin()
    {
        // e2e/package.json feeds the Admin UI CI workflow, so this run keeps its
        // own manifest. Playwright must stay at the same exact version in both.
        var shared = DevDependencies(Path.Combine(RepoRoot(), "e2e", "package.json"));
        var own = DevDependencies(Path.Combine(RepoRoot(), "e2e", "bootstrap", "package.json"));

        Assert.Equal(shared["@playwright/test"], own["@playwright/test"]);
        Assert.Equal(new[] { "@axe-core/playwright", "@playwright/test" }, own.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.All(own.Values, version => Assert.Matches(@"^\d+\.\d+\.\d+$", version));
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "e2e", "bootstrap", "package-lock.json")));
    }

    private static Dictionary<string, string> DevDependencies(string packageJson)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(packageJson));
        return doc.RootElement.GetProperty("devDependencies")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}

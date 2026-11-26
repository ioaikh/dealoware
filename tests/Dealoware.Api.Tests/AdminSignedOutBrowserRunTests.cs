using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// Pins on the browser runs for the signed-out admin pages (e2e/bootstrap for
/// the first-time setup page, e2e/sign-in for the three sign-in pages) and on
/// the runner they share (e2e/signed-out): a run stays on this machine, needs
/// no privilege, keeps the loopback proxy guard, and uses the stand-in
/// Turnstile verifier only with the app in Development.
/// The browser tests themselves are started with each suite's run.sh.
/// </summary>
public class AdminSignedOutBrowserRunTests
{
    private static readonly string[] Suites = ["bootstrap", "sign-in"];

    private static readonly string[] NeverInARunner =
    [
        "sudo", "sysctl", "setcap", "/etc/hosts", "0.0.0.0", "host-resolver-rules"
    ];

    [Fact]
    public void SharedRunner_StaysOnThisMachine_AndUsesTheStandInVerifierOnlyInDevelopment()
    {
        var code = ShellCode(Path.Combine(RepoRoot(), "e2e", "signed-out", "harness.sh"));

        Assert.All(NeverInARunner, banned => Assert.DoesNotContain(banned, code, StringComparison.Ordinal));
        Assert.Contains("export ASPNETCORE_ENVIRONMENT=Development", code, StringComparison.Ordinal);
        Assert.Contains("export ASPNETCORE_URLS=\"https://127.0.0.1:${API_PORT}\"", code, StringComparison.Ordinal);
        Assert.Contains(
            "export Admin__Turnstile__VerifyUrl=\"http://127.0.0.1:${VERIFY_PORT}/siteverify\"",
            code,
            StringComparison.Ordinal);
        Assert.Contains("ThreadingHTTPServer((\"127.0.0.1\", port), Handler)", code, StringComparison.Ordinal);
        Assert.Contains("listen = (\"127.0.0.1\", listen_port)", code, StringComparison.Ordinal);
        Assert.Contains("target = (\"127.0.0.1\", target_port)", code, StringComparison.Ordinal);
        Assert.Contains("export \"${SUITE_ENV}_PROXY=http://127.0.0.1:${PROXY_PORT}\"", code, StringComparison.Ordinal);
        Assert.Contains("export \"${SUITE_ENV}_BASE_URL=https://admin.core.dealoware.com\"", code, StringComparison.Ordinal);
        Assert.Contains("npx playwright test -c playwright.config.cjs \"$@\"", code, StringComparison.Ordinal);
        // The ports are the suite's to name; the shared runner holds none of its own.
        Assert.Empty(Regex.Matches(code, @"_PORT=\d"));
        // It refuses to run half-configured.
        foreach (var required in new[] { "SUITE", "SUITE_ENV", "API_PORT", "PROXY_PORT", "VERIFY_PORT" })
        {
            Assert.Contains($": \"${{{required}:?the suite run.sh must set {required}}}\"", code, StringComparison.Ordinal);
        }

        // Keys, the Turnstile secret and the token the verifier accepts are made up for each run.
        foreach (var made in new[] { "HMAC_KEY", "TOTP_KEY", "RECOVERY_KEY", "TURNSTILE_SECRET", "TURNSTILE_PASS_TOKEN" })
        {
            Assert.Matches($@"(?m)^\s*{made}=""\$\(openssl rand ", code);
        }
    }

    [Theory]
    [InlineData("bootstrap", "BOOTSTRAP_E2E", "bootstrap.spec.ts")]
    [InlineData("sign-in", "SIGN_IN_E2E", "sign-in.spec.ts")]
    public void SuiteRunner_NamesItsPortsAndPrefix_AndLeavesTheRestToTheSharedRunner(
        string suite,
        string envPrefix,
        string specFile)
    {
        var dir = Path.Combine(RepoRoot(), "e2e", suite);
        var code = ShellCode(Path.Combine(dir, "run.sh"));

        Assert.All(NeverInARunner, banned => Assert.DoesNotContain(banned, code, StringComparison.Ordinal));
        Assert.Contains($"\nSUITE={suite}\n", code, StringComparison.Ordinal);
        Assert.Contains($"\nSUITE_ENV={envPrefix}\n", code, StringComparison.Ordinal);
        Assert.Contains("source \"$(dirname \"$0\")/../signed-out/harness.sh\"", code, StringComparison.Ordinal);
        Assert.Contains("\nstart_app_behind_proxy\n", code, StringComparison.Ordinal);
        Assert.EndsWith("run_playwright \"$@\"", code.TrimEnd(), StringComparison.Ordinal);
        // How the app is started is the shared runner's business alone.
        Assert.DoesNotContain("ASPNETCORE_", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Admin__", code, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet ", code, StringComparison.Ordinal);
        Assert.DoesNotContain("npx ", code, StringComparison.Ordinal);

        var ports = Regex.Matches(code, @"(?m)^(API|PROXY|VERIFY)_PORT=(\d+)$")
            .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));
        Assert.Equal(new[] { "API", "PROXY", "VERIFY" }, ports.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.Equal(3, ports.Values.Distinct().Count());
        Assert.All(ports.Values, port => Assert.True(port >= 1024, "a run must not bind a port below 1024"));

        // No ports shared with the reset-confirm run or with another suite, so all can be started side by side.
        var others = new List<string> { File.ReadAllText(Path.Combine(RepoRoot(), "e2e", "reset-confirm", "run.sh")) };
        others.AddRange(Suites.Where(s => s != suite).Select(s => File.ReadAllText(Path.Combine(RepoRoot(), "e2e", s, "run.sh"))));
        foreach (var port in ports.Values)
        {
            Assert.All(others, other => Assert.DoesNotContain("PORT=" + port, other, StringComparison.Ordinal));
        }

        // The suite's Playwright configuration is the shared one, under the suite's own prefix.
        var config = File.ReadAllText(Path.Combine(dir, "playwright.config.cjs"));
        Assert.Contains("require(\"../signed-out/playwright.base.cjs\")", config, StringComparison.Ordinal);
        Assert.Contains(
            $"module.exports = signedOutConfig(require(\"@playwright/test\"), \"{envPrefix}\");",
            config,
            StringComparison.Ordinal);
        Assert.DoesNotContain("proxy", config, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("defineConfig", config, StringComparison.Ordinal);

        // The spec answers Cloudflare with the shared stand-in and never opens its own browser or drops the proxy.
        var spec = File.ReadAllText(Path.Combine(dir, specFile));
        Assert.Contains("} from '../signed-out/cloudflare';", spec, StringComparison.Ordinal);
        Assert.Contains("return answerCloudflare(page, { token: PASS_TOKEN, ...setup });", spec, StringComparison.Ordinal);
        Assert.Contains($"process.env.{envPrefix}_TURNSTILE_TOKEN", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("chromium.launch", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("launchPersistentContext", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("newContext(", spec, StringComparison.Ordinal);
        Assert.DoesNotContain("proxy:", spec, StringComparison.Ordinal);
        Assert.Contains("test.use({ javaScriptEnabled: false })", spec, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedPlaywrightConfiguration_KeepsTheLoopbackProxyGuard()
    {
        // The proxy guard is the reset-confirm one: loopback only, a port from 1024 up.
        var config = File.ReadAllText(Path.Combine(RepoRoot(), "e2e", "signed-out", "playwright.base.cjs"));
        Assert.Contains("require(\"../reset-confirm/require-proxy.cjs\")", config, StringComparison.Ordinal);
        Assert.Contains("guard(requireResetConfirmProxy, envPrefix + \"_PROXY\")", config, StringComparison.Ordinal);
        Assert.Contains("guard(requireResetConfirmBaseUrl, envPrefix + \"_BASE_URL\")", config, StringComparison.Ordinal);
        // Declared once, then passed to the shared settings and to the browser project.
        Assert.Single(Regex.Matches(config, "const proxy = "));
        Assert.Equal(2, Regex.Matches(config, @"(?m)^\s+proxy,\r?$").Count);
        Assert.Contains("workers: 1,", config, StringComparison.Ordinal);
        Assert.Contains("trace: \"off\",", config, StringComparison.Ordinal);
        Assert.Contains("video: \"off\",", config, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedBrowserHelpers_AnswerCloudflareThemselves_AndNeedNoPackageOfTheirOwn()
    {
        var shared = Path.Combine(RepoRoot(), "e2e", "signed-out");
        var cloudflare = File.ReadAllText(Path.Combine(shared, "cloudflare.ts"));
        Assert.Contains("page.route((url) => url.hostname === WIDGET_HOST", cloudflare, StringComparison.Ordinal);
        Assert.Contains("export const WIDGET_HOST = 'challenges.cloudflare.com';", cloudflare, StringComparison.Ordinal);
        // The stand-in refuses to install over something already at window.turnstile.
        Assert.Contains("if (window.turnstile) {", cloudflare, StringComparison.Ordinal);
        Assert.Contains("window.__e2eWidgetRefused = true;", cloudflare, StringComparison.Ordinal);

        // Each suite installs its own packages. A file both suites load may import only
        // Node's own modules at run time: anything else would be loaded twice, once per suite.
        Assert.False(File.Exists(Path.Combine(shared, "package.json")));
        foreach (var file in new[] { "cloudflare.ts", "axe.ts" })
        {
            var imports = Regex.Matches(File.ReadAllText(Path.Combine(shared, file)), @"(?m)^import (?<what>.+) from '(?<from>[^']+)';\r?$");
            Assert.NotEmpty(imports);
            Assert.All(imports, import => Assert.True(
                import.Groups["what"].Value.StartsWith("type ", StringComparison.Ordinal)
                || import.Groups["from"].Value.StartsWith("node:", StringComparison.Ordinal),
                $"{file} imports {import.Groups["from"].Value} at run time"));
        }

        var baseConfig = File.ReadAllText(Path.Combine(shared, "playwright.base.cjs"));
        Assert.Equal(
            new[] { "../reset-confirm/require-proxy.cjs" },
            Regex.Matches(baseConfig, @"require\(""([^""]+)""\)").Select(m => m.Groups[1].Value).ToArray());
    }

    [Theory]
    [InlineData("bootstrap")]
    [InlineData("sign-in")]
    public void Suite_HasItsOwnPackage_WithPlaywrightAtTheSharedPin(string suite)
    {
        // e2e/package.json feeds the Admin UI CI workflow, so each run keeps its
        // own manifest. Playwright must stay at the same exact version in all.
        var shared = DevDependencies(Path.Combine(RepoRoot(), "e2e", "package.json"));
        var own = DevDependencies(Path.Combine(RepoRoot(), "e2e", suite, "package.json"));

        Assert.Equal(shared["@playwright/test"], own["@playwright/test"]);
        Assert.Equal(new[] { "@axe-core/playwright", "@playwright/test" }, own.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.All(own.Values, version => Assert.Matches(@"^\d+\.\d+\.\d+$", version));
        Assert.Equal(DevDependencies(Path.Combine(RepoRoot(), "e2e", "bootstrap", "package.json")), own);

        // The lockfile is committed and names the same two packages at the same versions.
        var lockPath = Path.Combine(RepoRoot(), "e2e", suite, "package-lock.json");
        Assert.True(File.Exists(lockPath));
        using var lockFile = JsonDocument.Parse(File.ReadAllText(lockPath));
        var locked = lockFile.RootElement.GetProperty("packages");
        foreach (var (name, version) in own)
        {
            Assert.Equal(version, locked.GetProperty("node_modules/" + name).GetProperty("version").GetString());
        }
    }

    [Fact]
    public void NoPasswordOrKeyIsWrittenDown_TheyAreMadeUpAtRunTime()
    {
        var bootstrap = File.ReadAllText(Path.Combine(RepoRoot(), "e2e", "bootstrap", "bootstrap.spec.ts"));
        Assert.Contains("const GOOD_PASSWORD = crypto.randomBytes(16).toString('hex');", bootstrap, StringComparison.Ordinal);

        // The sign-in suite seeds its owner itself: a password, an authenticator key and recovery
        // codes, all random, known only to the test process and the run's throwaway database.
        var signIn = Path.Combine(RepoRoot(), "e2e", "sign-in");
        var owner = File.ReadAllText(Path.Combine(signIn, "owner.ts"));
        Assert.Contains("export const OWNER_PASSWORD = crypto.randomBytes(16).toString('hex');", owner, StringComparison.Ordinal);
        Assert.Contains("const secret = state.enrolled ? crypto.randomBytes(20) : null;", owner, StringComparison.Ordinal);
        Assert.Contains("crypto.randomInt(RECOVERY_ALPHABET.length)", owner, StringComparison.Ordinal);
        Assert.Contains("const PASSWORD_ROUNDS = 220_000;", owner, StringComparison.Ordinal);
        Assert.Equal(220_000, Dealoware.Infrastructure.Admin.AdminPasswordHasher.IterationCount);
        // The keys come from the runner's environment; none is spelled out, and nothing is logged.
        Assert.Contains("process.env.SIGN_IN_E2E_TOTP_KEY", owner, StringComparison.Ordinal);
        Assert.Contains("process.env.SIGN_IN_E2E_RECOVERY_KEY", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("console.", owner, StringComparison.Ordinal);
        Assert.DoesNotContain("writeFile", owner, StringComparison.Ordinal);

        var runner = ShellCode(Path.Combine(signIn, "run.sh"));
        Assert.Contains("export SIGN_IN_E2E_TOTP_KEY=\"$TOTP_KEY\"", runner, StringComparison.Ordinal);
        Assert.Contains("export SIGN_IN_E2E_RECOVERY_KEY=\"$RECOVERY_KEY\"", runner, StringComparison.Ordinal);
        Assert.Contains("export SIGN_IN_E2E_DB=\"$DB\"", runner, StringComparison.Ordinal);

        // The seeding script only writes the rows it is handed.
        var seed = File.ReadAllText(Path.Combine(signIn, "seed_owner.py"));
        Assert.Contains("state = json.load(sys.stdin)", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("print(", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("hashlib", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("import os", seed, StringComparison.Ordinal);
    }

    /// <summary>A shell script without its comment lines. It must have Unix line endings, or bash cannot run it.</summary>
    private static string ShellCode(string path)
    {
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
        return string.Join(
            "\n",
            text.Split('\n').Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal)));
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

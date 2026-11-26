using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// Pins on the browser runs for the signed-out admin pages (e2e/bootstrap for
/// the first-time setup page, e2e/sign-in for the three sign-in pages,
/// e2e/enrol for the two authenticator set-up pages) and on the runner they
/// share (e2e/signed-out): a run stays on this machine, needs
/// no privilege, keeps the loopback proxy guard, and uses the stand-in
/// Turnstile verifier only with the app in Development.
/// The browser tests themselves are started with each suite's run.sh, or all
/// of them with e2e/run-signed-out.sh, which is what the Admin UI CI workflow
/// runs; that entry point and the workflow are pinned here too.
/// </summary>
public class AdminSignedOutBrowserRunTests
{
    private static readonly string[] Suites = ["bootstrap", "sign-in", "enrol"];

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
        // Three engines by default, one Playwright run each; fewer only when named, and only these three.
        Assert.Contains("ENGINES=\"${E2E_ENGINES:-chromium firefox webkit}\"", code, StringComparison.Ordinal);
        Assert.Contains("chromium|firefox|webkit) ;;", code, StringComparison.Ordinal);
        Assert.Contains(
            "if ! npx playwright test -c playwright.config.cjs --project=\"$engine\" \"$@\"; then",
            code,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(code, "npx playwright test "));
        // A suite that uses up what it is given puts it back before each engine.
        Assert.Contains("if declare -F seed_suite >/dev/null; then", code, StringComparison.Ordinal);
        // A failure on one engine does not stop the others, and fails the run at the end.
        Assert.Contains("failed=\"$failed $engine\"", code, StringComparison.Ordinal);
        Assert.Contains("return 1", code, StringComparison.Ordinal);
        // The run's folder is marked as its own, for the script that empties tables.
        Assert.Contains("RUN_MARKER=\".dealoware-e2e-run\"", code, StringComparison.Ordinal);
        Assert.Contains("printf 'dealoware e2e run\\n' >\"$WORKDIR/$RUN_MARKER\"", code, StringComparison.Ordinal);
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
    [InlineData("enrol", "ENROL_E2E", "enrol.spec.ts")]
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
        // Declared once, then passed to the shared settings and to every browser project.
        Assert.Single(Regex.Matches(config, "const proxy = "));
        Assert.Equal(2, Regex.Matches(config, @"(?m)^\s+proxy,\r?$").Count);

        // One project per engine, all built the same way, so none can run without the proxy.
        Assert.Contains("chromium: \"Desktop Chrome\",", config, StringComparison.Ordinal);
        Assert.Contains("firefox: \"Desktop Firefox\",", config, StringComparison.Ordinal);
        Assert.Contains("webkit: \"Desktop Safari\",", config, StringComparison.Ordinal);
        Assert.Contains("projects: Object.keys(ENGINES).map((engine) => ({", config, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(config, @"\bprojects:"));
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

    [Fact]
    public void OneEntryPoint_FindsEverySuiteOnTheSharedRunner_AndFailsClosed()
    {
        var e2e = Path.Combine(RepoRoot(), "e2e");
        var code = ShellCode(Path.Combine(e2e, "run-signed-out.sh"));

        Assert.All(NeverInARunner, banned => Assert.DoesNotContain(banned, code, StringComparison.Ordinal));
        // It looks for suites and names none: a suite is a folder whose run.sh sources the shared runner.
        Assert.Contains("for run in \"$E2E\"/*/run.sh; do", code, StringComparison.Ordinal);
        Assert.Contains(
            "if grep -Eq '^[[:space:]]*(source|\\.)[[:space:]].*/signed-out/harness\\.sh' \"$run\"; then",
            code,
            StringComparison.Ordinal);
        Assert.All(Suites, suite => Assert.DoesNotContain(suite, code, StringComparison.Ordinal));
        var found = SuitesOnTheSharedRunner();
        Assert.All(Suites, suite => Assert.Contains(suite, found));

        // No suite found is a failure, not an empty pass.
        Assert.Matches(@"if \[\[ ""\$\{#SUITES\[@\]\}"" -eq 0 \]\]; then\n +echo [^\n]+ >&2\n +exit 1\n", code);
        // A suite that fails fails the run, and the suites after it still run.
        Assert.Contains(
            "if ! E2E_REPORT_DIR=\"$REPORTS/$suite\" bash \"$E2E/$suite/run.sh\" \"$@\"; then",
            code,
            StringComparison.Ordinal);
        Assert.DoesNotContain("break", code, StringComparison.Ordinal);
        // SKIP is not PASS: each engine's run must have executed a test, by Playwright's own report.
        Assert.Contains("ASSERT_EXECUTED=\"$E2E/admin/ci/assert-executed.js\"", code, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(e2e, "admin", "ci", "assert-executed.js")));
        Assert.Contains("export E2E_ENGINES=\"${E2E_ENGINES:-chromium firefox webkit}\"", code, StringComparison.Ordinal);
        Assert.Contains("for engine in $E2E_ENGINES; do", code, StringComparison.Ordinal);
        Assert.Contains(
            "if counts=\"$(node \"$ASSERT_EXECUTED\" \"$REPORTS/$suite/$engine.json\")\"; then",
            code,
            StringComparison.Ordinal);
        Assert.Matches(@"if \[\[ -n ""\$why"" \]\]; then\n +FAILED=1\n", code);
        Assert.Matches(@"if \[\[ ""\$FAILED"" -ne 0 \]\]; then\n +echo [^\n]+ >&2\n +exit 1\n", code);
        Assert.DoesNotContain("|| true", code, StringComparison.Ordinal);

        // The shared runner writes those reports when it is asked to, one for each engine.
        var runner = ShellCode(Path.Combine(e2e, "signed-out", "harness.sh"));
        Assert.Contains("set -- --reporter=list,json \"$@\"", runner, StringComparison.Ordinal);
        Assert.Contains(
            "export PLAYWRIGHT_JSON_OUTPUT_FILE=\"${E2E_REPORT_DIR}/${engine}.json\"",
            runner,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EveryBrowserSpec_BelongsToASuiteTheEntryPointFinds_ExceptTheOlderConfirmPageRun()
    {
        var e2e = Path.Combine(RepoRoot(), "e2e");
        var found = SuitesOnTheSharedRunner();

        // A spec in a folder the entry point does not find would never run in Admin UI CI.
        // e2e/reset-confirm is the one known case: it has its own, older run script.
        var withSpecs = Directory.GetDirectories(e2e)
            .Where(dir => Directory.GetFiles(dir, "*.spec.ts").Length > 0)
            .Select(dir => Path.GetFileName(dir))
            .ToArray();
        Assert.NotEmpty(withSpecs);
        Assert.All(withSpecs, folder => Assert.True(
            found.Contains(folder) || folder == "reset-confirm",
            $"e2e/{folder} holds a spec that Admin UI CI does not run"));

        // The workflow installs the browsers once, from the Playwright that e2e/package.json pins,
        // and the suites run one after the other on one machine: so every suite found, also one
        // added later, is at that version and has ports of its own.
        var pinned = DevDependencies(Path.Combine(e2e, "package.json"))["@playwright/test"];
        var ports = new Dictionary<int, string>();
        foreach (var suite in found)
        {
            Assert.Equal(pinned, DevDependencies(Path.Combine(e2e, suite, "package.json"))["@playwright/test"]);
            Assert.True(File.Exists(Path.Combine(e2e, suite, "package-lock.json")));
            var named = Regex.Matches(File.ReadAllText(Path.Combine(e2e, suite, "run.sh")), @"(?m)^(?:API|PROXY|VERIFY)_PORT=(\d+)$");
            Assert.Equal(3, named.Count);
            foreach (Match port in named)
            {
                Assert.True(
                    ports.TryAdd(int.Parse(port.Groups[1].Value), suite),
                    $"e2e/{suite} names a port another suite uses: {port.Value}");
            }
        }
    }

    [Fact]
    public void AdminUiCi_RunsTheEntryPoint_OncePerEngine_OnPullRequestsThatTouchWhatThePagesAreBuiltFrom()
    {
        var workflow = AdminUiWorkflow();

        // Pull requests only, and only those that touch the application, the suites or the workflow.
        Assert.Contains("\non:\n  pull_request:\n    paths:\n", workflow, StringComparison.Ordinal);
        var paths = Regex.Match(workflow, @"\n    paths:\n((?:      - '[^'\n]+'\n)+)").Groups[1].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('-').Trim().Trim('\''))
            .ToArray();
        Assert.Equal(new[] { "src/**", "e2e/**", ".github/workflows/admin-ui-ci.yml" }, paths);
        Assert.DoesNotContain("paths-ignore", workflow, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "Admin")));
        Assert.True(Directory.Exists(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "wwwroot", "admin")));

        // One job for each engine, side by side, each with a time limit; one failing does not stop the others.
        Assert.Contains("      fail-fast: false\n", workflow, StringComparison.Ordinal);
        Assert.Contains("        engine: [chromium, firefox, webkit]\n", workflow, StringComparison.Ordinal);
        Assert.Contains("      E2E_ENGINES: ${{ matrix.engine }}\n", workflow, StringComparison.Ordinal);
        Assert.Matches(@"\n    timeout-minutes: \d+\n", workflow);

        // The browser with its system packages, the application built once, then every suite.
        var browser = workflow.IndexOf("run: npx playwright install --with-deps \"$E2E_ENGINES\"\n", StringComparison.Ordinal);
        var build = workflow.IndexOf("run: dotnet build src/Dealoware.Api/Dealoware.Api.csproj\n", StringComparison.Ordinal);
        var run = workflow.IndexOf("run: bash e2e/run-signed-out.sh --forbid-only\n", StringComparison.Ordinal);
        Assert.True(browser > 0 && build > browser && run > build, "browser install, build and run must come in this order");
        Assert.Single(Regex.Matches(workflow, "run-signed-out"));
        Assert.Single(Regex.Matches(workflow, "dotnet build "));
        // What the build step builds is what the shared runner starts.
        Assert.Contains(
            "dotnet run --project \"$ROOT/src/Dealoware.Api/Dealoware.Api.csproj\" --no-launch-profile",
            ShellCode(Path.Combine(RepoRoot(), "e2e", "signed-out", "harness.sh")),
            StringComparison.Ordinal);

        // The script that tells a run which executed nothing from a pass is proven in the job itself.
        var control = workflow.IndexOf(
            "          node admin/ci/capture-fixtures.js\n          node --test admin/ci/assert-executed.control.js\n",
            StringComparison.Ordinal);
        Assert.True(control > 0 && control < run);
    }

    [Fact]
    public void AdminUiCi_KeepsWhatAPublicRepositoryNeeds()
    {
        var workflow = AdminUiWorkflow();

        // A read-only token, no credentials left in the checkout, never the base repository's context.
        Assert.Contains("\npermissions:\n  contents: read\n", workflow, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(workflow, @"(?m)^\s*permissions:"));
        Assert.DoesNotContain(": write", workflow, StringComparison.Ordinal);
        Assert.Contains("          persist-credentials: false\n", workflow, StringComparison.Ordinal);

        // Every action at a full commit id.
        var actions = Regex.Matches(workflow, @"(?m)^\s*(?:- )?uses: (\S+)").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(new[] { "actions/checkout", "actions/setup-node" }, actions.Select(a => a.Split('@')[0]).ToArray());
        Assert.All(actions, action => Assert.Matches(@"^[\w./-]+@[0-9a-f]{40}$", action));

        // Nothing stored is read, nothing is uploaded, and nothing can turn a red run green.
        foreach (var banned in new[]
                 {
                     "secrets.", "upload-artifact", "continue-on-error", "|| true", "if: always()",
                     "pull_request_target", "ALLOW_LIVE_ADMIN_CORE", "id-token"
                 })
        {
            Assert.DoesNotContain(banned, workflow, StringComparison.Ordinal);
        }

        // The loopback proxy guard's own checks run before any browser does.
        var guard = workflow.IndexOf("run: node e2e/reset-confirm/require-proxy.check.cjs\n", StringComparison.Ordinal);
        Assert.True(guard > 0 && guard < workflow.IndexOf("run: bash e2e/run-signed-out.sh", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("bootstrap")]
    [InlineData("sign-in")]
    [InlineData("enrol")]
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

        // It empties tables, so it takes one database only: e2e.db in the folder the runner
        // marked as a run's own. The check comes before the file is opened.
        Assert.Contains("RUN_DATABASE = \"e2e.db\"", seed, StringComparison.Ordinal);
        Assert.Contains("RUN_MARKER = \".dealoware-e2e-run\"", seed, StringComparison.Ordinal);
        Assert.Contains("RUN_MARKER_TEXT = \"dealoware e2e run\"", seed, StringComparison.Ordinal);
        Assert.Contains("database = a_runs_own_database(state.get(\"db\"))", seed, StringComparison.Ordinal);
        Assert.Contains("con = sqlite3.connect(database, timeout=15)", seed, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(seed, @"sqlite3\.connect\("));
        Assert.True(
            seed.IndexOf("database = a_runs_own_database(", StringComparison.Ordinal)
            < seed.IndexOf("sqlite3.connect(", StringComparison.Ordinal));
        Assert.Contains("DB=\"$WORKDIR/e2e.db\"", ShellCode(Path.Combine(RepoRoot(), "e2e", "signed-out", "harness.sh")), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSetUpSuite_SeedsItsOwnerLikeTheSignInSuite_AndWritesNoKeyOfItsOwn()
    {
        // It uses the sign-in suite's seeding module, so it hands the same things over under the
        // same names. The authenticator key and the recovery codes are made by the app, and the
        // spec reads them off the pages as an owner would.
        var enrol = Path.Combine(RepoRoot(), "e2e", "enrol");
        var runner = ShellCode(Path.Combine(enrol, "run.sh"));
        var signInRunner = ShellCode(Path.Combine(RepoRoot(), "e2e", "sign-in", "run.sh"));
        foreach (var handed in new[]
                 {
                     "DB=\"$DB\"", "PYTHON=\"$PY\"", "OWNER=\"$OWNER\"", "TOTP_KEY=\"$TOTP_KEY\"", "RECOVERY_KEY=\"$RECOVERY_KEY\""
                 })
        {
            Assert.Contains("export SIGN_IN_E2E_" + handed, signInRunner, StringComparison.Ordinal);
            Assert.Contains("export SIGN_IN_E2E_" + handed, runner, StringComparison.Ordinal);
        }

        var spec = File.ReadAllText(Path.Combine(enrol, "enrol.spec.ts"));
        Assert.Contains("} from '../sign-in/owner';", spec, StringComparison.Ordinal);
        Assert.Contains("owner = seedOwner({ enrolled: false });", spec, StringComparison.Ordinal);
        // It prints nothing, and writes no file but its screenshots (the axe reports are the shared check's).
        Assert.DoesNotMatch(@"console\.\w+\(", spec);
        Assert.DoesNotContain("writeFile", spec, StringComparison.Ordinal);

        // The QR code on the page is read back by the suite's own reader, which needs no package at all.
        var reader = File.ReadAllText(Path.Combine(enrol, "qr-reader.ts"));
        Assert.Empty(Regex.Matches(reader, @"(?m)^import "));
        Assert.DoesNotContain("require(", reader, StringComparison.Ordinal);
        Assert.Contains("} from './qr-reader';", spec, StringComparison.Ordinal);
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

    /// <summary>The suite folders the entry point finds: those whose run.sh sources the shared runner.</summary>
    private static string[] SuitesOnTheSharedRunner()
        => Directory.GetDirectories(Path.Combine(RepoRoot(), "e2e"))
            .Where(dir => File.Exists(Path.Combine(dir, "run.sh"))
                && Regex.IsMatch(
                    File.ReadAllText(Path.Combine(dir, "run.sh")),
                    @"(?m)^[ \t]*(source|\.)[ \t].*/signed-out/harness\.sh"))
            .Select(dir => Path.GetFileName(dir))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The Admin UI CI workflow without its comment lines, with Unix line endings.</summary>
    private static string AdminUiWorkflow()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "admin-ui-ci.yml"));
        return string.Join(
            "\n",
            text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal)));
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

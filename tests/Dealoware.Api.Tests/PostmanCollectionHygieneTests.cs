using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security Note 2: committed Postman collections must not ship secrets or a live Core API default.
/// Environment files (T-B-02) may set the live Core baseUrl but must keep secret slots empty.
/// One matcher treats a slot as secret when type is "secret", the name contains key/token/secret/password
/// (case-insensitive), or the name ends in Sub or Id.
/// </summary>
public class PostmanCollectionHygieneTests
{
    private const string LocalBaseUrl = "http://localhost:5287";
    private const string LiveCoreBaseUrl = "https://api.core.dealoware.com";
    private const string SanctionedLiveEnvRelativePath =
        "postman/Dealoware-api-core.postman_environment.json";

    private static readonly HashSet<string> AllowedLiveOrLocalBaseUrls = new(StringComparer.Ordinal)
    {
        LocalBaseUrl,
        LiveCoreBaseUrl
    };

    private static readonly HashSet<string> SkipDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", "test-results", ".vs", "artifacts"
    };

    private static readonly Regex SetsBaseUrl = new(
        @"(?:collectionVariables|environment|globals|variables)\.set\(\s*['""]baseUrl['""]",
        RegexOptions.Compiled);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }

    /// <summary>
    /// Shared secret matcher for collections and environments.
    /// Covers type:secret (e.g. a slot named password), names containing key/token/secret/password
    /// (e.g. refreshToken), and names ending in Sub or Id.
    /// </summary>
    internal static bool IsSecretSlot(string? key, string? type)
    {
        if (string.Equals(type, "secret", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrEmpty(key))
            return false;
        return key.Contains("key", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("Sub", StringComparison.Ordinal)
            || key.EndsWith("Id", StringComparison.Ordinal);
    }

    internal static IEnumerable<string> EnumeratePostmanFiles(string root, string searchPattern)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var file in Directory.GetFiles(dir, searchPattern))
                yield return file;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                if (SkipDirectoryNames.Contains(Path.GetFileName(sub)))
                    continue;
                stack.Push(sub);
            }
        }
    }

    [Fact]
    public void CommittedPostmanCollections_HaveLocalhostBaseUrl_AndEmptySecretSlots()
    {
        var files = EnumeratePostmanFiles(RepoRoot(), "*.postman_collection.json").ToList();
        Assert.NotEmpty(files);
        Assert.Empty(CollectCollectionFailures(files));
    }

    [Fact]
    public void CommittedPostmanEnvironments_HaveAllowedBaseUrl_AndEmptySecretSlots()
    {
        var root = RepoRoot();
        var sanctioned = Path.Combine(root, "postman", "Dealoware-api-core.postman_environment.json");
        Assert.True(File.Exists(sanctioned), $"{SanctionedLiveEnvRelativePath} must exist");

        var files = EnumeratePostmanFiles(root, "*.postman_environment.json").ToList();
        Assert.Contains(files, f => Path.GetFullPath(f) == Path.GetFullPath(sanctioned));
        Assert.Empty(CollectEnvironmentFailures(files, root));
    }

    [Fact]
    public void SecretMatcher_TreatsTypeSecretPassword_FilledRefreshToken_AndNamePatternsAsSecret()
    {
        Assert.True(IsSecretSlot("password", "secret"));
        Assert.True(IsSecretSlot("refreshToken", "string"));
        Assert.True(IsSecretSlot("buyerApiKey", "secret"));
        Assert.False(IsSecretSlot("baseUrl", "default"));
        Assert.False(IsSecretSlot("baseUrl", "string"));
    }

    [Fact]
    public void HygieneScan_FailsTypeSecretPasswordSlot_InEnvironment()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-password.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": "password", "value": "hunter2", "type": "secret" }
              ]
            }
            """);

        var failures = CollectEnvironmentFailures(
            EnumeratePostmanFiles(temp.Root.FullName, "*.postman_environment.json"),
            temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FailsFilledKey_InNestedEnvironmentFile()
    {
        using var temp = new TempPostmanDir();
        var nested = Path.Combine(temp.PostmanDir, "live");
        Directory.CreateDirectory(nested);
        File.WriteAllText(
            Path.Combine(nested, "X.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": "buyerApiKey", "value": "filled", "type": "secret" }
              ]
            }
            """);

        var files = EnumeratePostmanFiles(temp.Root.FullName, "*.postman_environment.json").ToList();
        Assert.Contains(files, f => f.Contains($"{Path.DirectorySeparatorChar}live{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("buyerApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsFilledRefreshToken_InCollection()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-refresh.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "refreshToken", "value": "filled", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(
            EnumeratePostmanFiles(temp.Root.FullName, "*.postman_collection.json"));
        Assert.Contains(failures, f => f.Contains("refreshToken", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsHardcodedNonLocalhostRequestUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-host.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "Live health",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "https://api.core.dealoware.com/health", "host": ["api.core.dealoware.com"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(
            EnumeratePostmanFiles(temp.Root.FullName, "*.postman_collection.json"));
        Assert.Contains(failures, f => f.Contains("hard-codes host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FindsCollectionOutsidePostmanDirectory()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "sneaky.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "string" }
              ]
            }
            """);

        var files = EnumeratePostmanFiles(temp.Root.FullName, "*.postman_collection.json").ToList();
        Assert.Contains(files, f => f.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("sneaky.postman_collection.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsWhenCollectionScriptSetsBaseUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-script.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "pm.collectionVariables.set(\"baseUrl\", \"https://api.core.dealoware.com\");" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(
            EnumeratePostmanFiles(temp.Root.FullName, "*.postman_collection.json"));
        Assert.Contains(failures, f => f.Contains("sets baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsEnvironmentWithoutValuesArray()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-novalues.postman_environment.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" }
              ]
            }
            """);

        var failures = CollectEnvironmentFailures(
            EnumeratePostmanFiles(temp.Root.FullName, "*.postman_environment.json"),
            temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("must have a values array", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_AllowsLiveBaseUrl_OnlyOnSanctionedEnvFile()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "Dealoware-api-core.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default" },
                { "key": "sellerApiKey", "value": "", "type": "secret" }
              ]
            }
            """);
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "copy.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default" }
              ]
            }
            """);

        var files = EnumeratePostmanFiles(temp.Root.FullName, "*.postman_environment.json").ToList();
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.DoesNotContain(failures, f => f.Contains("Dealoware-api-core.postman_environment.json", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("copy.postman_environment.json", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://api.core.dealoware.com")]
    [InlineData("http://localhost:5287@api.core.dealoware.com")]
    [InlineData("http://localhost.evil.test")]
    [InlineData("http://127.0.0.1.evil.test")]
    [InlineData("HTTP://localhost:5287")]
    [InlineData("http://LOCALHOST:5287")]
    [InlineData("http://localhost:5287#@api.core.dealoware.com")]
    [InlineData("http://localhost:5287\n")]
    [InlineData("http://localhost:5287 ")]
    public async Task RunNewmanScript_RefusesNonLocalBaseUrl_WithoutNetwork(string baseUrl)
    {
        var result = await RunNewmanGuardAsync(baseUrl, curlExitsZero: true);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("refused", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dealoware-api-core.postman_environment.json", result.Output, StringComparison.Ordinal);
        Assert.False(result.NetworkCalled, $"guard must refuse {baseUrl} before curl/npx/npm");
    }

    [Fact]
    public async Task RunNewmanScript_AllowsLocalhost5287_AndReachesCurl()
    {
        var result = await RunNewmanGuardAsync("http://localhost:5287", curlExitsZero: false);
        Assert.DoesNotContain("refused", result.Output, StringComparison.Ordinal);
        Assert.True(result.NetworkCalled, "http://localhost:5287 must pass the guard and reach curl");
    }

    [Fact]
    public async Task RunNewmanScript_AllowsLoopbackWithTrailingSlash_AndReachesCurl()
    {
        var result = await RunNewmanGuardAsync("http://127.0.0.1:5287/", curlExitsZero: false);
        Assert.DoesNotContain("refused", result.Output, StringComparison.Ordinal);
        Assert.True(result.NetworkCalled, "http://127.0.0.1:5287/ must pass the guard and reach curl");
    }

    [Theory]
    [InlineData("--live")]
    [InlineData("-e")]
    public async Task RunNewmanScript_UnknownArgument_WritesStderrAndExitsNonZero(string extraArg)
    {
        var result = await RunNewmanGuardAsync(
            "http://localhost:5287",
            curlExitsZero: true,
            extraArg,
            "postman/Dealoware-api-core.postman_environment.json");
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("unknown argument", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.NetworkCalled, "unknown arguments must exit before curl/npx/npm");
    }

    private static List<string> CollectCollectionFailures(IEnumerable<string> files)
    {
        var failures = new List<string>();
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var label = Path.GetFileName(file);
            if (!doc.RootElement.TryGetProperty("variable", out var variables)
                || variables.ValueKind != JsonValueKind.Array)
            {
                failures.Add($"{label} must have a variable array");
            }
            else
            {
                var seen = ReadSlots(variables);
                if (!seen.TryGetValue("baseUrl", out var baseUrl) || baseUrl.Value != LocalBaseUrl)
                    failures.Add($"{label} baseUrl must be {LocalBaseUrl}");

                CollectSecretFailures(label, seen, failures);
            }

            CollectItemUrlFailures(label, doc.RootElement, failures);
            CollectScriptBaseUrlFailures(label, doc.RootElement, failures);
        }

        return failures;
    }

    private static List<string> CollectEnvironmentFailures(IEnumerable<string> files, string scanRoot)
    {
        var failures = new List<string>();
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var label = file;
            if (!doc.RootElement.TryGetProperty("values", out var values)
                || values.ValueKind != JsonValueKind.Array)
            {
                failures.Add($"{label} must have a values array");
            }

            var seen = ReadEnvironmentSlots(doc.RootElement);
            var allowed = IsSanctionedLiveEnv(file, scanRoot)
                ? AllowedLiveOrLocalBaseUrls
                : new HashSet<string>(StringComparer.Ordinal) { LocalBaseUrl };

            if (seen.TryGetValue("baseUrl", out var baseUrl))
            {
                if (baseUrl.HasValue && (baseUrl.Value is null || !allowed.Contains(baseUrl.Value)))
                    failures.Add($"{label} baseUrl value must be {LocalBaseUrl}" +
                                 (allowed.Contains(LiveCoreBaseUrl) ? $" or {LiveCoreBaseUrl}" : "") +
                                 $", got '{baseUrl.Value}'");
                if (baseUrl.HasCurrentValue && (baseUrl.CurrentValue is null || !allowed.Contains(baseUrl.CurrentValue)))
                    failures.Add($"{label} baseUrl currentValue must be {LocalBaseUrl}" +
                                 (allowed.Contains(LiveCoreBaseUrl) ? $" or {LiveCoreBaseUrl}" : "") +
                                 $", got '{baseUrl.CurrentValue}'");
            }

            CollectSecretFailures(label, seen, failures);
        }

        return failures;
    }

    private static bool IsSanctionedLiveEnv(string file, string scanRoot)
    {
        var expected = Path.GetFullPath(Path.Combine(scanRoot, SanctionedLiveEnvRelativePath));
        return Path.GetFullPath(file) == expected;
    }

    private static void CollectSecretFailures(
        string label,
        Dictionary<string, Slot> seen,
        List<string> failures)
    {
        foreach (var (key, slot) in seen)
        {
            if (!IsSecretSlot(slot.Key, slot.Type))
                continue;
            if (!slot.HasValue || slot.Value != "")
                failures.Add($"{label} {key} value must be empty");
            if (slot.HasCurrentValue && slot.CurrentValue != "")
                failures.Add($"{label} {key} currentValue must be empty");
        }
    }

    private static void CollectItemUrlFailures(string label, JsonElement el, List<string> failures)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectItemUrlFailures(label, child, failures);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;
        if (el.TryGetProperty("request", out var request) && request.ValueKind == JsonValueKind.Object)
            CollectRequestUrlFailures(label, request, failures);
        if (el.TryGetProperty("item", out var item))
            CollectItemUrlFailures(label, item, failures);
    }

    private static void CollectRequestUrlFailures(string label, JsonElement request, List<string> failures)
    {
        if (!request.TryGetProperty("url", out var url))
            return;
        if (url.ValueKind == JsonValueKind.String)
        {
            CheckUrlString(label, url.GetString(), failures);
            return;
        }

        if (url.ValueKind != JsonValueKind.Object)
            return;
        if (url.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.String)
            CheckUrlString(label, raw.GetString(), failures);
        if (url.TryGetProperty("host", out var host) && host.ValueKind == JsonValueKind.Array)
        {
            var parts = host.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "")
                .ToArray();
            if (parts.Length == 1 && parts[0] == "{{baseUrl}}")
                return;
            var joined = string.Join(".", parts);
            CheckUrlString(label, joined.Contains("://", StringComparison.Ordinal) ? joined : "http://" + joined, failures);
        }
    }

    private static void CheckUrlString(string label, string? raw, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("{{baseUrl}}", StringComparison.Ordinal))
            return;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return;
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.Ordinal))
        {
            return;
        }

        failures.Add($"{label} request URL hard-codes host '{uri.Host}' instead of {{{{baseUrl}}}}");
    }

    private static void CollectScriptBaseUrlFailures(string label, JsonElement el, List<string> failures)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("exec", out var exec) && exec.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in exec.EnumerateArray())
                {
                    var s = line.ValueKind == JsonValueKind.String ? line.GetString() : null;
                    if (s is not null && SetsBaseUrl.IsMatch(s))
                    {
                        failures.Add($"{label} collection script sets baseUrl");
                        return;
                    }
                }
            }

            foreach (var prop in el.EnumerateObject())
                CollectScriptBaseUrlFailures(label, prop.Value, failures);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectScriptBaseUrlFailures(label, child, failures);
        }
    }

    private static Dictionary<string, Slot> ReadEnvironmentSlots(JsonElement root)
    {
        var seen = new Dictionary<string, Slot>(StringComparer.Ordinal);
        foreach (var propertyName in new[] { "values", "variable" })
        {
            if (!root.TryGetProperty(propertyName, out var variables)
                || variables.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var (key, slot) in ReadSlots(variables))
                seen[key] = slot;
        }

        return seen;
    }

    private static Dictionary<string, Slot> ReadSlots(JsonElement variables)
    {
        var seen = new Dictionary<string, Slot>(StringComparer.Ordinal);
        foreach (var variable in variables.EnumerateArray())
        {
            var key = variable.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
            if (string.IsNullOrEmpty(key))
                continue;

            var type = variable.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            var hasValue = variable.TryGetProperty("value", out var valueEl);
            var value = hasValue ? valueEl.GetString() : null;
            var hasCurrentValue = variable.TryGetProperty("currentValue", out var currentEl);
            var currentValue = hasCurrentValue ? currentEl.GetString() : null;
            seen[key] = new Slot(key, type, value, hasValue, currentValue, hasCurrentValue);
        }

        return seen;
    }

    private static async Task<(int ExitCode, string Output, bool NetworkCalled)> RunNewmanGuardAsync(
        string baseUrl,
        bool curlExitsZero,
        params string[] extraArgs)
    {
        var script = Path.Combine(RepoRoot(), "scripts", "run-newman.sh");
        Assert.True(File.Exists(script), "scripts/run-newman.sh must exist");

        var temp = Directory.CreateTempSubdirectory("run-newman-guard-");
        try
        {
            var copy = Path.Combine(temp.FullName, "run-newman.sh");
            File.Copy(script, copy);

            var stubDir = Path.Combine(temp.FullName, "bin");
            Directory.CreateDirectory(stubDir);
            var marker = Path.Combine(temp.FullName, "network-called");
            WriteExecutableStub(Path.Combine(stubDir, "curl"), marker, curlExitsZero ? 0 : 1);
            WriteExecutableStub(Path.Combine(stubDir, "npx"), marker, 1);
            WriteExecutableStub(Path.Combine(stubDir, "npm"), marker, 1);

            var quotedArgs = string.Join(" ", extraArgs.Select(a => $"\"{a.Replace("\"", "\\\"")}\""));
            var start = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = quotedArgs.Length == 0 ? $"\"{copy}\"" : $"\"{copy}\" {quotedArgs}",
                WorkingDirectory = temp.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.Environment["BASE_URL"] = baseUrl;
            start.Environment["PATH"] = stubDir + Path.PathSeparator + start.Environment["PATH"];

            using var process = Process.Start(start);
            Assert.NotNull(process);
            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            Assert.True(process.WaitForExit(10_000), "run-newman.sh should finish promptly");

            var output = await stdoutTask + await stderrTask;
            return (process.ExitCode, output, File.Exists(marker));
        }
        finally
        {
            Directory.Delete(temp.FullName, recursive: true);
        }
    }

    private static void WriteExecutableStub(string path, string marker, int exitCode)
    {
        File.WriteAllText(path, $"#!/bin/sh\nprintf called > '{marker}'\nexit {exitCode}\n");
        var chmod = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
            Arguments = $"+x \"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(chmod);
        Assert.NotNull(process);
        Assert.True(process!.WaitForExit(5_000));
        Assert.Equal(0, process.ExitCode);
    }

    private readonly record struct Slot(
        string Key,
        string? Type,
        string? Value,
        bool HasValue,
        string? CurrentValue,
        bool HasCurrentValue);

    private sealed class TempPostmanDir : IDisposable
    {
        public TempPostmanDir()
        {
            Root = Directory.CreateTempSubdirectory("postman-hygiene-");
            PostmanDir = Path.Combine(Root.FullName, "postman");
            Directory.CreateDirectory(PostmanDir);
        }

        public DirectoryInfo Root { get; }
        public string PostmanDir { get; }

        public void Dispose()
        {
            Directory.Delete(Root.FullName, recursive: true);
        }
    }
}

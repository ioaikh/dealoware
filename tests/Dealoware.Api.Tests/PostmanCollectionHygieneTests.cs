using System.Diagnostics;
using System.Text.Json;

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

    private static readonly HashSet<string> AllowedEnvironmentBaseUrls = new(StringComparer.Ordinal)
    {
        LocalBaseUrl,
        LiveCoreBaseUrl
    };

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

    [Fact]
    public void CommittedPostmanCollections_HaveLocalhostBaseUrl_AndEmptySecretSlots()
    {
        var postmanDir = Path.Combine(RepoRoot(), "postman");
        var files = Directory.GetFiles(postmanDir, "*.postman_collection.json", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        Assert.Empty(CollectCollectionFailures(files));
    }

    [Fact]
    public void CommittedPostmanEnvironments_HaveAllowedBaseUrl_AndEmptySecretSlots()
    {
        var postmanDir = Path.Combine(RepoRoot(), "postman");
        Assert.True(Directory.Exists(postmanDir), "postman/ must exist");
        Assert.True(
            File.Exists(Path.Combine(postmanDir, "Dealoware-api-core.postman_environment.json")),
            "postman/Dealoware-api-core.postman_environment.json must exist");

        var files = Directory.GetFiles(postmanDir, "*.postman_environment.json", SearchOption.AllDirectories);
        Assert.Empty(CollectEnvironmentFailures(files));
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
            Directory.GetFiles(temp.PostmanDir, "*.postman_environment.json", SearchOption.AllDirectories));
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

        var files = Directory.GetFiles(temp.PostmanDir, "*.postman_environment.json", SearchOption.AllDirectories);
        Assert.Contains(files, f => f.Contains($"{Path.DirectorySeparatorChar}live{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var failures = CollectEnvironmentFailures(files);
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
            Directory.GetFiles(temp.PostmanDir, "*.postman_collection.json", SearchOption.AllDirectories));
        Assert.Contains(failures, f => f.Contains("refreshToken", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://api.core.dealoware.com")]
    [InlineData("http://localhost:5287@api.core.dealoware.com")]
    [InlineData("http://localhost.evil.test")]
    public async Task RunNewmanScript_RefusesNonLocalBaseUrl_WithoutNetwork(string baseUrl)
    {
        var result = await RunNewmanGuardAsync(baseUrl, curlExitsZero: true);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("refused", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dealoware-api-core.postman_environment.json", result.Output, StringComparison.Ordinal);
        Assert.False(result.NetworkCalled, $"guard must refuse {baseUrl} before curl/npx");
    }

    [Fact]
    public async Task RunNewmanScript_AllowsLocalhost5287_AndReachesCurl()
    {
        var result = await RunNewmanGuardAsync("http://localhost:5287", curlExitsZero: false);
        Assert.DoesNotContain("refused", result.Output, StringComparison.Ordinal);
        Assert.True(result.NetworkCalled, "http://localhost:5287 must pass the guard and reach curl");
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
                continue;
            }

            var seen = ReadSlots(variables);
            if (!seen.TryGetValue("baseUrl", out var baseUrl) || baseUrl.Value != LocalBaseUrl)
                failures.Add($"{label} baseUrl must be {LocalBaseUrl}");

            CollectSecretFailures(label, seen, failures);
        }

        return failures;
    }

    private static List<string> CollectEnvironmentFailures(IEnumerable<string> files)
    {
        var failures = new List<string>();
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var label = file;
            var seen = ReadEnvironmentSlots(doc.RootElement);

            if (seen.TryGetValue("baseUrl", out var baseUrl))
            {
                if (baseUrl.HasValue && (baseUrl.Value is null || !AllowedEnvironmentBaseUrls.Contains(baseUrl.Value)))
                    failures.Add($"{label} baseUrl value must be {LocalBaseUrl} or {LiveCoreBaseUrl}, got '{baseUrl.Value}'");
                if (baseUrl.HasCurrentValue && (baseUrl.CurrentValue is null || !AllowedEnvironmentBaseUrls.Contains(baseUrl.CurrentValue)))
                    failures.Add($"{label} baseUrl currentValue must be {LocalBaseUrl} or {LiveCoreBaseUrl}, got '{baseUrl.CurrentValue}'");
            }

            CollectSecretFailures(label, seen, failures);
        }

        return failures;
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
        bool curlExitsZero)
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

            var start = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"\"{copy}\"",
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

using System.Text.Json;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security Note 2: committed Postman collections must not ship secrets or a live Core API default.
/// Environment files (T-B-02), if present, may set the live Core baseUrl but must keep key/token/Sub/Id empty.
/// </summary>
public class PostmanCollectionHygieneTests
{
    private const string LocalBaseUrl = "http://localhost:5287";
    private const string LiveCoreBaseUrl = "https://api.core.dealoware.com";

    private static readonly string[] NamedEmptyVariables =
    [
        "sellerApiKey",
        "buyerApiKey",
        "observerApiKey",
        "authDemoApiKey",
        "accessToken"
    ];

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

    [Fact]
    public void CommittedPostmanCollections_HaveLocalhostBaseUrl_AndEmptyKeyTokenIdVariables()
    {
        var postmanDir = Path.Combine(RepoRoot(), "postman");
        var files = Directory.GetFiles(postmanDir, "*.postman_collection.json");
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            Assert.True(
                doc.RootElement.TryGetProperty("variable", out var variables)
                && variables.ValueKind == JsonValueKind.Array,
                $"{Path.GetFileName(file)} must have a variable array");

            var seen = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var variable in variables.EnumerateArray())
            {
                var key = variable.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
                if (string.IsNullOrEmpty(key))
                    continue;
                var value = variable.TryGetProperty("value", out var valueEl) ? valueEl.GetString() : null;
                seen[key] = value;
            }

            Assert.True(seen.ContainsKey("baseUrl"), $"{Path.GetFileName(file)} must define baseUrl");
            Assert.Equal(LocalBaseUrl, seen["baseUrl"]);

            foreach (var name in NamedEmptyVariables)
            {
                if (!seen.TryGetValue(name, out var value))
                    continue;
                Assert.Equal("", value);
            }

            foreach (var (key, value) in seen)
            {
                if (key.EndsWith("ApiKey", StringComparison.Ordinal)
                    || key.EndsWith("Sub", StringComparison.Ordinal)
                    || key.EndsWith("Id", StringComparison.Ordinal)
                    || string.Equals(key, "accessToken", StringComparison.Ordinal))
                {
                    Assert.Equal("", value);
                }
            }
        }
    }

    [Fact]
    public void CommittedPostmanEnvironments_IfPresent_HaveAllowedBaseUrl_AndEmptyKeyTokenIdVariables()
    {
        var postmanDir = Path.Combine(RepoRoot(), "postman");
        if (!Directory.Exists(postmanDir))
            return;

        var files = Directory.GetFiles(postmanDir, "*.postman_environment.json");
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var seen = ReadEnvironmentVariables(doc.RootElement);
            var label = Path.GetFileName(file);

            if (seen.TryGetValue("baseUrl", out var baseUrl))
            {
                AssertAllowedEnvironmentBaseUrl(label, "value", baseUrl.HasValue, baseUrl.Value);
                AssertAllowedEnvironmentBaseUrl(label, "currentValue", baseUrl.HasCurrentValue, baseUrl.CurrentValue);
            }

            foreach (var name in NamedEmptyVariables)
            {
                if (!seen.TryGetValue(name, out var entry))
                    continue;
                AssertEmptySlot(label, name, entry);
            }

            foreach (var (key, entry) in seen)
            {
                if (!IsKeyTokenSubOrId(key))
                    continue;
                AssertEmptySlot(label, key, entry);
            }
        }
    }

    private static bool IsKeyTokenSubOrId(string key) =>
        key.Contains("key", StringComparison.OrdinalIgnoreCase)
        || key.Contains("token", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith("Sub", StringComparison.Ordinal)
        || key.EndsWith("Id", StringComparison.Ordinal);

    private static Dictionary<string, EnvironmentVariable> ReadEnvironmentVariables(JsonElement root)
    {
        var seen = new Dictionary<string, EnvironmentVariable>(StringComparer.Ordinal);
        foreach (var propertyName in new[] { "values", "variable" })
        {
            if (!root.TryGetProperty(propertyName, out var variables)
                || variables.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var variable in variables.EnumerateArray())
            {
                var key = variable.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
                if (string.IsNullOrEmpty(key))
                    continue;

                var hasValue = variable.TryGetProperty("value", out var valueEl);
                var value = hasValue ? valueEl.GetString() : null;
                var hasCurrentValue = variable.TryGetProperty("currentValue", out var currentEl);
                var currentValue = hasCurrentValue ? currentEl.GetString() : null;
                seen[key] = new EnvironmentVariable(value, hasValue, currentValue, hasCurrentValue);
            }
        }

        return seen;
    }

    private static void AssertAllowedEnvironmentBaseUrl(string file, string field, bool present, string? value)
    {
        if (!present)
            return;

        Assert.True(
            value is not null && AllowedEnvironmentBaseUrls.Contains(value),
            $"{file} baseUrl {field} must be {LocalBaseUrl} or {LiveCoreBaseUrl}, got '{value}'");
    }

    private static void AssertEmptySlot(string file, string key, EnvironmentVariable entry)
    {
        Assert.True(entry.HasValue, $"{file} {key} must have a value");
        Assert.Equal("", entry.Value);
        if (entry.HasCurrentValue)
            Assert.Equal("", entry.CurrentValue);
    }

    private readonly record struct EnvironmentVariable(
        string? Value,
        bool HasValue,
        string? CurrentValue,
        bool HasCurrentValue);
}

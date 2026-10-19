using System.Text.Json;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security Note 2: committed Postman collections must not ship secrets or a live Core API default.
/// </summary>
public class PostmanCollectionHygieneTests
{
    private static readonly string[] NamedEmptyVariables =
    [
        "sellerApiKey",
        "buyerApiKey",
        "observerApiKey",
        "authDemoApiKey",
        "accessToken"
    ];

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
            Assert.Equal("http://localhost:5287", seen["baseUrl"]);

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
}

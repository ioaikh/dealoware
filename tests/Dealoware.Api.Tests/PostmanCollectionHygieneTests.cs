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

    private static readonly Regex DealowareHost = new(@"dealoware\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BareIp = new(
        @"(?<![A-Za-z0-9.])(?<ip>(?:\d{1,3}\.){3}\d{1,3})(?![A-Za-z0-9.])",
        RegexOptions.Compiled);

    private static readonly HashSet<string> SkipDirectoryNamesAnyDepth = new(StringComparer.Ordinal)
    {
        "bin", "obj", "node_modules", ".git", ".vs"
    };

    private static readonly HashSet<string> SkipDirectoryNamesRepoRootOnly = new(StringComparer.Ordinal)
    {
        "artifacts", "test-results"
    };

    private static readonly HashSet<string> AllowedHostArray = new(StringComparer.Ordinal)
    {
        "{{baseUrl}}",
        "localhost",
        "127.0.0.1"
    };

    private static readonly Regex AllowedBaseUrlTemplate =
        new(@"^\{\{baseUrl\}\}(/|$)", RegexOptions.Compiled);

    private static readonly Regex AllowedLiteralOrigin =
        new(@"^http://(localhost|127\.0\.0\.1)(:[0-9]+)?(/|$)", RegexOptions.Compiled);

    private static readonly Regex HttpUrl = new(@"https?://[^\s""'\\]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NoSchemeHost = new(
        @"^[A-Za-z0-9.-]+\.[A-Za-z]{2,}(:[0-9]+)?(/|$)",
        RegexOptions.Compiled);

    private static readonly Regex SetsBaseUrl = new(
        @"\.set\(\s*(?:['""`]baseUrl['""`]|['""`]base['""`]\s*\+\s*['""`]Url['""`])",
        RegexOptions.Compiled);

    private static readonly Regex TemplateToken = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    private static readonly Regex NumericPort = new(@"^[0-9]{1,5}$", RegexOptions.Compiled);

    private static readonly Regex PmBracketAccess = new(@"pm\[|pm\.[A-Za-z0-9_]+\[", RegexOptions.Compiled);

    private static readonly Regex SetCall = new(@"\.set\s*\(", RegexOptions.Compiled);

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

    private static bool ShouldSkipDirectory(string scanRoot, string directory)
    {
        var name = Path.GetFileName(directory);
        if (SkipDirectoryNamesAnyDepth.Contains(name))
            return true;
        if (!SkipDirectoryNamesRepoRootOnly.Contains(name))
            return false;
        var parent = Path.GetDirectoryName(Path.GetFullPath(directory));
        return parent is not null
               && string.Equals(Path.GetFullPath(parent), scanRoot, StringComparison.Ordinal);
    }

    internal static IEnumerable<string> EnumeratePostmanFiles(string root, string searchPattern)
    {
        var scanRoot = Path.GetFullPath(root);
        var stack = new Stack<string>();
        stack.Push(scanRoot);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var file in Directory.GetFiles(dir, searchPattern))
                yield return file;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                if (ShouldSkipDirectory(scanRoot, sub))
                    continue;
                stack.Push(sub);
            }
        }
    }

    internal static void DiscoverPostmanFiles(string root, out List<string> collections, out List<string> environments)
    {
        collections = [];
        environments = [];
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(List<string> list, string file)
        {
            var full = Path.GetFullPath(file);
            if (seen.Add(full))
                list.Add(full);
        }

        foreach (var file in EnumeratePostmanFiles(root, "*.postman_collection.json"))
            Add(collections, file);
        foreach (var file in EnumeratePostmanFiles(root, "*.postman_environment.json"))
            Add(environments, file);

        foreach (var file in EnumeratePostmanFiles(root, "*.json"))
        {
            var full = Path.GetFullPath(file);
            if (seen.Contains(full))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    continue;
                if (LooksLikeCollection(doc.RootElement))
                    Add(collections, file);
                else if (LooksLikeEnvironment(doc.RootElement))
                    Add(environments, file);
            }
            catch (JsonException)
            {
                // not Postman JSON
            }
        }
    }

    private static bool LooksLikeCollection(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;
        if (!root.TryGetProperty("info", out var info) || info.ValueKind != JsonValueKind.Object)
            return false;
        if (info.TryGetProperty("_postman_id", out _))
            return true;
        return info.TryGetProperty("schema", out var schema)
               && schema.ValueKind == JsonValueKind.String
               && (schema.GetString() ?? "").Contains("getpostman.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeEnvironment(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;
        if (root.TryGetProperty("_postman_variable_scope", out _))
            return true;
        var hasValues = root.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array;
        var hasName = root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String;
        return hasValues && hasName;
    }

    [Fact]
    public void CommittedPostmanCollections_HaveLocalhostBaseUrl_AndEmptySecretSlots()
    {
        DiscoverPostmanFiles(RepoRoot(), out var collections, out _);
        Assert.NotEmpty(collections);
        Assert.Empty(CollectCollectionFailures(collections));
    }

    [Fact]
    public void CommittedPostmanEnvironments_HaveAllowedBaseUrl_AndEmptySecretSlots()
    {
        var root = RepoRoot();
        var sanctioned = Path.Combine(root, "postman", "Dealoware-api-core.postman_environment.json");
        Assert.True(File.Exists(sanctioned), $"{SanctionedLiveEnvRelativePath} must exist");

        DiscoverPostmanFiles(root, out _, out var environments);
        Assert.Contains(environments, f => Path.GetFullPath(f) == Path.GetFullPath(sanctioned));
        Assert.Empty(CollectEnvironmentFailures(environments, root));
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
                { "key": "password", "value": "not-a-real-secret", "type": "secret" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
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

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
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

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        var failures = CollectCollectionFailures(files);
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

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsB1_NoSchemeLiveHostRequestUrl()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "b1.postman_collection.json",
            raw: "api.core.dealoware.com/health",
            host: """["api.core.dealoware.com"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsB2_LiveUrlVariableUsedAsHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b2.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "liveUrl", "value": "https://api.core.dealoware.com", "type": "string" }
              ],
              "item": [
                {
                  "name": "Via liveUrl",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{liveUrl}}/health", "host": ["{{liveUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("liveUrl", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsB3_SendRequestToLiveHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b3.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "pm.sendRequest('https://api.core.dealoware.com/health', function(){});" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsB4_ScriptRewritesPmRequestUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b4.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "pm.request.url = 'https://api.core.dealoware.com/health';" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("pm.request.url", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsBaseUrlUserinfoPrefix()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "userinfo.postman_collection.json",
            raw: "{{baseUrl}}@api.core.dealoware.com/health",
            host: """["{{baseUrl}}"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsOtherVarHoldingLiveHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "othervar.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "otherVar", "value": "https://api.core.dealoware.com", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("otherVar", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsOtherVarHoldingLiveHost_InEnvironment()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "othervar.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": "otherVar", "value": "https://api.core.dealoware.com", "type": "default" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("otherVar", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsSplitLineAndConcatenatedBaseUrlSet()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "split-set.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [
                      "pm.collectionVariables.set(",
                      "  'base'+'Url',",
                      "  'https://api.core.dealoware.com'",
                      ");"
                    ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sets baseUrl", StringComparison.Ordinal));
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

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("sneaky.postman_collection.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FindsLiveHostCollection_UnderSrcApplicationArtifacts()
    {
        using var temp = new TempPostmanDir();
        var plantedDir = Path.Combine(temp.Root.FullName, "src", "Dealoware.Application", "Artifacts");
        Directory.CreateDirectory(plantedDir);
        WriteLiveHostCollection(plantedDir, "src-artifacts.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.Contains(
            $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}Dealoware.Application{Path.DirectorySeparatorChar}Artifacts{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FindsLiveHostCollection_UnderNestedArtifactsFolder()
    {
        using var temp = new TempPostmanDir();
        var plantedDir = Path.Combine(temp.Root.FullName, "docs", "artifacts");
        Directory.CreateDirectory(plantedDir);
        WriteLiveHostCollection(plantedDir, "nested-artifacts.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.Contains(
            $"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FindsDisguisedPostmanJson_ByTopLevelMarkers()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "hidden.json"),
            """
            {
              "info": { "_postman_id": "hidden-collection", "name": "hidden" },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": { "method": "GET", "url": { "raw": "api.core.dealoware.com/health", "host": ["api.core.dealoware.com"] } }
                }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.EndsWith($"{Path.DirectorySeparatorChar}hidden.json", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
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

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sets baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsLiteralSecretInAuthorizationHeader()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "mut-header.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "header": [ { "key": "Authorization", "value": "ApiKey not-a-real-secret" } ],
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("literal secret", StringComparison.OrdinalIgnoreCase));
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

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
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

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.DoesNotContain(failures, f => f.Contains($"{Path.DirectorySeparatorChar}Dealoware-api-core.postman_environment.json", StringComparison.Ordinal)
                                            && f.Contains("non-local host", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("copy.postman_environment.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsStringRequestUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "string-request.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                { "name": "x", "request": "https://api.core.dealoware.com/health" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsStringHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "string-host.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": "api.core.dealoware.com" }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlObjectWithoutRaw()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "no-raw.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "host": ["{{baseUrl}}"], "path": ["health"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsExecString()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "exec-string.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": "pm.sendRequest('https://api.core.dealoware.com/health', function(){});"
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsScriptString()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "script-string.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": "pm.sendRequest('https://api.core.dealoware.com/health', function(){});"
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsScriptArray()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "script-array.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": [ "pm.sendRequest('https://api.core.dealoware.com/health', function(){});" ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsBracketAccessSendRequestAndRequestUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "bracket.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [
                      "pm['sendRequest']('https://api.core.dealoware.com/health', function(){});",
                      "pm.request['url'] = 'https://api.core.dealoware.com/health';"
                    ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("request.url", StringComparison.Ordinal) || f.Contains("pm.request.url", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsFolderVariableHoldingLiveHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "folder-var.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "variable": [
                    { "key": "otherVar", "value": "https://api.core.dealoware.com", "type": "string" }
                  ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("otherVar", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsProxyLiveHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "proxy.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] },
                    "proxy": { "host": "api.core.dealoware.com", "port": 443 }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("non-local host", StringComparison.Ordinal) || f.Contains("dealoware.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FailsCollectionLevelAuthLiteral()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "collection-auth.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "auth": {
                "type": "apikey",
                "apikey": [ { "key": "value", "value": "not-a-real-secret", "type": "string" } ]
              }
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("literal secret", StringComparison.OrdinalIgnoreCase) && f.Contains("auth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FailsFolderLevelAuthLiteral()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "folder-auth.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "auth": {
                    "type": "apikey",
                    "apikey": [ { "key": "value", "value": "not-a-real-secret", "type": "string" } ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("literal secret", StringComparison.OrdinalIgnoreCase) && f.Contains("auth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FailsUnparseableRequestUrl()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "unparseable.postman_collection.json",
            raw: "::::not-a-url",
            host: """["{{baseUrl}}"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsLiveHostArrayWithAllowedRaw()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "live-host-array.postman_collection.json",
            raw: "{{baseUrl}}/health",
            host: """["api.core.dealoware.com"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsHttpsTextInEventScript()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "script-https.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "console.log('https://api.core.dealoware.com/health');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("non-local host", StringComparison.Ordinal) || f.Contains("dealoware.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FailsRequestLevelAuthLiteral()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "request-auth.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] },
                    "auth": {
                      "type": "apikey",
                      "apikey": [ { "key": "value", "value": "not-a-real-secret", "type": "string" } ]
                    }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("literal secret", StringComparison.OrdinalIgnoreCase) && f.Contains("auth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HygieneScan_FindsDisguisedJson_ByInfoSchemaGetpostman()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "schema-only.json"),
            """
            {
              "info": {
                "name": "hidden",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                { "name": "x", "request": "https://api.core.dealoware.com/health" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.EndsWith($"{Path.DirectorySeparatorChar}schema-only.json", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FindsDisguisedEnv_ByValuesAndName()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "named-values.json"),
            """
            {
              "name": "sneaky-env",
              "values": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        Assert.Contains(files, f => f.EndsWith($"{Path.DirectorySeparatorChar}named-values.json", StringComparison.Ordinal));
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("named-values.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_ReadsNonStringVariableValuesWithoutCrashing()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "non-string-vars.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "retries", "value": 3, "type": "string" },
                { "key": "enabled", "value": true, "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.DoesNotContain(failures, f => f.Contains("retries", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains("enabled", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_AllowsEnvBaseUrlLoopback5287()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "loopback.postman_environment.json"),
            """
            {
              "name": "local-loopback",
              "values": [
                { "key": "baseUrl", "value": "http://127.0.0.1:5287", "type": "default" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.DoesNotContain(failures, f => f.Contains("loopback.postman_environment.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlPortAtHost()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "port-at-host.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "protocol": "http",
            "host": ["localhost"],
            "port": "@api.core.dealoware.com",
            "path": ["health"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlPortVariableAtHost()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "port-var-at-host.postman_collection.json",
            extraVars: """{ "key": "p", "value": "@api.core.dealoware.com", "type": "string" }""",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "protocol": "http",
            "host": ["localhost"],
            "port": "{{p}}",
            "path": ["health"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlAuthUserinfo()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "url-auth.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "path": ["health"],
            "auth": { "user": "u", "password": "p" }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("auth", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlProtocolHttps()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "protocol-https.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "protocol": "https",
            "host": ["localhost"],
            "path": ["health"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlPathUnknownToken()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "path-token.postman_collection.json",
            extraVars: """{ "key": "redir", "value": "//api.core.dealoware.com", "type": "string" }""",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "path": ["{{redir}}"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUrlQueryAtHost()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "query-at-host.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "path": ["health"],
            "query": [{ "key": "next", "value": "@api.core.dealoware.com" }]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsFolderVariablePortAtHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "folder-port.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "variable": [
                    { "key": "p", "value": "@api.core.dealoware.com", "type": "string" }
                  ],
                  "item": [
                    {
                      "name": "x",
                      "request": {
                        "method": "GET",
                        "url": {
                          "raw": "{{baseUrl}}/health",
                          "protocol": "http",
                          "host": ["localhost"],
                          "port": "{{p}}",
                          "path": ["health"]
                        }
                      }
                    }
                  ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsEnvironmentSetComputedKey()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "set-computed.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "var k = 'baseUrl'; pm.environment.set(k, 'https://api.core.dealoware.com');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sets baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsTemplateLiteralSetKey()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "set-template.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "pm.collectionVariables.set(`baseUrl`, 'https://api.core.dealoware.com');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sets baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsEvalAtob()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "eval-atob.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "eval(atob('cG0uc2VuZFJlcXVlc3QoKTs='));" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("eval(", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("atob(", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsFunctionConstructor()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "function-ctor.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "Function('return pm')().sendRequest('https://api.core.dealoware.com/health');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("Function(", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsRequire()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "require.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "require('http').get('https://api.core.dealoware.com/health');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("require(", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsConcatenatedSendRequestBracket()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "send-concat.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": {
                    "exec": [ "pm['send'+'Request']('https://api.core.dealoware.com/health', function(){});" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("pm[", StringComparison.Ordinal) || f.Contains("sendRequest", StringComparison.Ordinal));
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

    [Fact]
    public async Task RunNewmanScript_StubInterceptsUsrBinCurl()
    {
        var result = await RunNewmanGuardAsync(
            "http://localhost:5287",
            curlExitsZero: false,
            useUsrBinCurl: true);
        Assert.DoesNotContain("refused", result.Output, StringComparison.Ordinal);
        Assert.True(result.NetworkCalled, "direct /usr/bin/curl must hit the stub");
    }

    [Fact]
    public async Task RunNewmanScript_StubInterceptsBinCurl()
    {
        var result = await RunNewmanGuardAsync(
            "http://localhost:5287",
            curlExitsZero: false,
            useBinCurl: true);
        Assert.DoesNotContain("refused", result.Output, StringComparison.Ordinal);
        Assert.True(result.NetworkCalled, "direct /bin/curl must hit the stub");
    }

    [Theory]
    [InlineData("--live")]
    [InlineData("-e")]
    public async Task RunNewmanScript_UnknownArgument_WritesStderrAndExitsNonZero(string extraArg)
    {
        var result = await RunNewmanGuardAsync(
            "http://localhost:5287",
            curlExitsZero: true,
            extraArgs: [extraArg, "postman/Dealoware-api-core.postman_environment.json"]);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("unknown argument", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.NetworkCalled, "unknown arguments must exit before curl/npx/npm");
    }

    private static List<string> DiscoverCollections(TempPostmanDir temp)
    {
        DiscoverPostmanFiles(temp.Root.FullName, out var collections, out _);
        return collections;
    }

    private static void WriteMinimalCollection(TempPostmanDir temp, string name, string raw, string host)
    {
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, name),
            $$"""
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{raw}}", "host": {{host}} }
                  }
                }
              ]
            }
            """);
    }

    private static void WriteUrlPartsCollection(TempPostmanDir temp, string name, string extraVars, string urlParts)
    {
        var vars = extraVars.Length == 0
            ? """{ "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }"""
            : """{ "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }, """ + extraVars;
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, name),
            $$"""
            {
              "variable": [ {{vars}} ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { {{urlParts}} }
                  }
                }
              ]
            }
            """);
    }

    private static void WriteLiveHostCollection(string directory, string name)
    {
        File.WriteAllText(
            Path.Combine(directory, name),
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
    }

    private static List<string> CollectCollectionFailures(IEnumerable<string> files)
    {
        var failures = new List<string>();
        var seenMessages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var label = Path.GetFileName(file);
            if (!doc.RootElement.TryGetProperty("variable", out var variables)
                || variables.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} must have a variable array");
            }
            else
            {
                var seen = ReadSlots(variables);
                if (!seen.TryGetValue("baseUrl", out var baseUrl) || baseUrl.Value != LocalBaseUrl)
                    AddFailure(failures, seenMessages, $"{label} baseUrl must be {LocalBaseUrl}");

                CollectSecretFailures(label, seen, failures, seenMessages);
                CollectNonLocalHostSlotFailures(label, seen, failures, seenMessages, sanctionedLiveBaseUrl: false);
            }

            var committedKeys = CollectCommittedKeys(doc.RootElement);
            CollectAllVariableSlotFailures(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: false, skipRootVariable: true);
            CollectRequestShapeFailures(label, doc.RootElement, failures, seenMessages, committedKeys);
            CollectAuthAndHeaderFailures(label, doc.RootElement, failures, seenMessages);
            WalkEveryString(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: false, committedKeys);
        }

        return failures;
    }

    private static List<string> CollectEnvironmentFailures(IEnumerable<string> files, string scanRoot)
    {
        var failures = new List<string>();
        var seenMessages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var label = file;
            var sanctioned = IsSanctionedLiveEnv(file, scanRoot);
            if (!doc.RootElement.TryGetProperty("values", out var values)
                || values.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} must have a values array");
            }

            var seen = ReadEnvironmentSlots(doc.RootElement);
            if (seen.TryGetValue("baseUrl", out var baseUrl))
            {
                if (baseUrl.HasValue && !IsAllowedEnvBaseUrl(baseUrl.Value, sanctioned))
                    AddFailure(failures, seenMessages, $"{label} baseUrl value must be a local allowlist origin" +
                                 (sanctioned ? $" or {LiveCoreBaseUrl}" : "") +
                                 $", got '{baseUrl.Value}'");
                if (baseUrl.HasCurrentValue && !IsAllowedEnvBaseUrl(baseUrl.CurrentValue, sanctioned))
                    AddFailure(failures, seenMessages, $"{label} baseUrl currentValue must be a local allowlist origin" +
                                 (sanctioned ? $" or {LiveCoreBaseUrl}" : "") +
                                 $", got '{baseUrl.CurrentValue}'");
            }

            CollectSecretFailures(label, seen, failures, seenMessages);
            CollectNonLocalHostSlotFailures(label, seen, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned);
            var committedKeys = CollectCommittedKeys(doc.RootElement);
            CollectAllVariableSlotFailures(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned, skipRootVariable: false);
            CollectRequestShapeFailures(label, doc.RootElement, failures, seenMessages, committedKeys);
            CollectAuthAndHeaderFailures(label, doc.RootElement, failures, seenMessages);
            WalkEveryString(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned, committedKeys);
        }

        return failures;
    }

    private static bool IsSanctionedLiveEnv(string file, string scanRoot)
    {
        var expected = Path.GetFullPath(Path.Combine(scanRoot, SanctionedLiveEnvRelativePath));
        return Path.GetFullPath(file) == expected;
    }

    private static void AddFailure(List<string> failures, HashSet<string> seen, string message)
    {
        if (seen.Add(message))
            failures.Add(message);
    }

    private static bool IsAllowedEnvBaseUrl(string? value, bool sanctioned)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        var trimmed = value.Trim();
        if (AllowedLiteralOrigin.IsMatch(trimmed))
            return true;
        return sanctioned && trimmed == LiveCoreBaseUrl;
    }

    private static void CollectSecretFailures(
        string label,
        Dictionary<string, Slot> seen,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        foreach (var (key, slot) in seen)
        {
            if (!IsSecretSlot(slot.Key, slot.Type))
                continue;
            if (!slot.HasValue || slot.Value != "")
                AddFailure(failures, seenMessages, $"{label} {key} value must be empty");
            if (slot.HasCurrentValue && slot.CurrentValue != "")
                AddFailure(failures, seenMessages, $"{label} {key} currentValue must be empty");
        }
    }

    private static void CollectNonLocalHostSlotFailures(
        string label,
        Dictionary<string, Slot> seen,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl)
    {
        foreach (var (key, slot) in seen)
        {
            if (sanctionedLiveBaseUrl && key == "baseUrl")
                continue;
            if (slot.HasValue && HoldsNonLocalHost(slot.Value))
                AddFailure(failures, seenMessages, $"{label} {key} value holds a non-local host");
            if (slot.HasCurrentValue && HoldsNonLocalHost(slot.CurrentValue))
                AddFailure(failures, seenMessages, $"{label} {key} currentValue holds a non-local host");
        }
    }

    private static bool HoldsNonLocalHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (HoldsForbiddenHost(value))
            return true;
        var trimmed = value.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal))
            return false;
        if (AllowedLiteralOrigin.IsMatch(trimmed) || AllowedBaseUrlTemplate.IsMatch(trimmed))
            return false;
        return NoSchemeHost.IsMatch(trimmed);
    }

    private static bool HoldsForbiddenHost(string text)
    {
        if (DealowareHost.IsMatch(text))
            return true;
        foreach (Match match in BareIp.Matches(text))
        {
            if (match.Groups["ip"].Value != "127.0.0.1")
                return true;
        }

        return false;
    }

    private static HashSet<string> CollectCommittedKeys(JsonElement root)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        CollectVariableKeys(root, keys);
        keys.Remove("baseUrl");
        return keys;
    }

    private static void CollectVariableKeys(JsonElement el, HashSet<string> keys)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectVariableKeys(child, keys);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var name in new[] { "variable", "values" })
        {
            if (!el.TryGetProperty(name, out var vars) || vars.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var (key, _) in ReadSlots(vars))
                keys.Add(key);
        }

        foreach (var prop in el.EnumerateObject())
            CollectVariableKeys(prop.Value, keys);
    }

    private static void CollectAllVariableSlotFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl,
        bool skipRootVariable,
        string path = "")
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectAllVariableSlotFailures(label, child, failures, seenMessages, sanctionedLiveBaseUrl, skipRootVariable: false, path);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in el.EnumerateObject())
        {
            var childPath = path.Length == 0 ? prop.Name : path + "." + prop.Name;
            if ((prop.Name is "variable" or "values") && prop.Value.ValueKind == JsonValueKind.Array)
            {
                var skip = skipRootVariable && path.Length == 0 && prop.Name == "variable";
                if (!skip)
                {
                    var slots = ReadSlots(prop.Value);
                    CollectSecretFailures(label, slots, failures, seenMessages);
                    CollectNonLocalHostSlotFailures(label, slots, failures, seenMessages, sanctionedLiveBaseUrl);
                }
            }

            CollectAllVariableSlotFailures(label, prop.Value, failures, seenMessages, sanctionedLiveBaseUrl, skipRootVariable: false, childPath);
        }
    }

    private static Dictionary<string, string> ReadVariableMap(JsonElement el)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (el.ValueKind != JsonValueKind.Object)
            return map;
        if (!el.TryGetProperty("variable", out var vars) || vars.ValueKind != JsonValueKind.Array)
            return map;
        foreach (var (key, slot) in ReadSlots(vars))
        {
            if (slot.HasValue && slot.Value is not null)
                map[key] = slot.Value;
            if (slot.HasCurrentValue && slot.CurrentValue is not null)
                map[key] = slot.CurrentValue;
        }

        return map;
    }

    private static Dictionary<string, string> MergeVars(Dictionary<string, string> parent, Dictionary<string, string> child)
    {
        var merged = new Dictionary<string, string>(parent, StringComparer.Ordinal);
        foreach (var (key, value) in child)
            merged[key] = value;
        return merged;
    }

    private static string ResolveVars(string text, Dictionary<string, string> vars)
    {
        return TemplateToken.Replace(text, match =>
        {
            var name = match.Groups[1].Value;
            if (name == "baseUrl")
                return match.Value;
            return vars.TryGetValue(name, out var value) ? value : match.Value;
        });
    }

    private static bool HasForbiddenTokenOrAt(string text)
    {
        if (text.Contains('@'))
            return true;
        foreach (Match match in TemplateToken.Matches(text))
        {
            if (match.Groups[1].Value != "baseUrl")
                return true;
        }

        return false;
    }

    private static void CollectRequestShapeFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        HashSet<string> committedKeys,
        Dictionary<string, string>? vars = null)
    {
        vars ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectRequestShapeFailures(label, child, failures, seenMessages, committedKeys, vars);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        var local = MergeVars(vars, ReadVariableMap(el));
        if (el.TryGetProperty("request", out var request))
            InspectRequest(label, request, local, committedKeys, failures, seenMessages);
        if (el.TryGetProperty("item", out var item))
            CollectRequestShapeFailures(label, item, failures, seenMessages, committedKeys, local);
    }

    private static void InspectRequest(
        string label,
        JsonElement request,
        Dictionary<string, string> vars,
        HashSet<string> committedKeys,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        const string urlMsg = "request URL must match {{baseUrl}}(/|$) or http://localhost/127.0.0.1";
        if (request.ValueKind == JsonValueKind.String)
        {
            CheckRequestUrlString(label, request.GetString(), failures, seenMessages, urlMsg);
            return;
        }

        if (request.ValueKind != JsonValueKind.Object)
        {
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
            return;
        }

        if (!request.TryGetProperty("url", out var url))
            return;
        CollectRequestUrlFailures(label, url, vars, committedKeys, failures, seenMessages);
    }

    private static void CollectRequestUrlFailures(
        string label,
        JsonElement url,
        Dictionary<string, string> vars,
        HashSet<string> committedKeys,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        const string urlMsg = "request URL must match {{baseUrl}}(/|$) or http://localhost/127.0.0.1";
        const string hostMsg = "host array must be [{{baseUrl}}] or localhost";

        if (url.ValueKind == JsonValueKind.String)
        {
            CheckRequestUrlString(label, url.GetString(), failures, seenMessages, urlMsg);
            return;
        }

        if (url.ValueKind != JsonValueKind.Object)
        {
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
            return;
        }

        if (url.TryGetProperty("auth", out _))
            AddFailure(failures, seenMessages, $"{label} request URL must not contain auth");

        if (url.TryGetProperty("protocol", out var protocolEl))
        {
            var protocol = ResolveVars(ElementAsString(protocolEl) ?? "", vars);
            if (!string.Equals(protocol, "http", StringComparison.Ordinal))
                AddFailure(failures, seenMessages, $"{label} {urlMsg}");
        }

        if (url.TryGetProperty("port", out var portEl))
        {
            var port = ResolveVars(ElementAsString(portEl) ?? "", vars);
            if (!NumericPort.IsMatch(port))
                AddFailure(failures, seenMessages, $"{label} {urlMsg}");
        }

        if (url.TryGetProperty("host", out var host))
        {
            if (host.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} {hostMsg}");
            }
            else
            {
                var parts = host.EnumerateArray()
                    .Select(e => ResolveVars(ElementAsString(e) ?? "", vars))
                    .ToArray();
                if (parts.Length != 1 || !AllowedHostArray.Contains(parts[0]))
                    AddFailure(failures, seenMessages, $"{label} {hostMsg}");
            }
        }

        if (url.TryGetProperty("path", out var pathEl) && PathOrQueryForbidden(pathEl, vars, committedKeys))
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
        if (url.TryGetProperty("query", out var queryEl) && PathOrQueryForbidden(queryEl, vars, committedKeys))
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");

        if (!url.TryGetProperty("raw", out var raw) || raw.ValueKind != JsonValueKind.String
            || !IsAllowedRequestUrl(raw.GetString()))
        {
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
        }

        var rebuilt = RebuildUrl(url, vars);
        if (!IsAllowedRequestUrl(rebuilt))
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
    }

    private static void CheckRequestUrlString(
        string label,
        string? raw,
        List<string> failures,
        HashSet<string> seenMessages,
        string urlMsg)
    {
        if (!IsAllowedRequestUrl(raw))
            AddFailure(failures, seenMessages, $"{label} {urlMsg}");
    }

    private static bool PathOrQueryForbidden(
        JsonElement el,
        Dictionary<string, string> vars,
        HashSet<string> committedKeys)
    {
        if (el.ValueKind == JsonValueKind.String)
            return PathPartForbidden(el.GetString() ?? "", vars, committedKeys);
        if (el.ValueKind != JsonValueKind.Array)
            return true;
        foreach (var child in el.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.String)
            {
                if (PathPartForbidden(child.GetString() ?? "", vars, committedKeys))
                    return true;
            }
            else if (child.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "key", "value" })
                {
                    if (!child.TryGetProperty(name, out var part))
                        continue;
                    if (PathPartForbidden(ElementAsString(part) ?? "", vars, committedKeys))
                        return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static bool PathPartForbidden(string text, Dictionary<string, string> vars, HashSet<string> committedKeys)
    {
        if (text.Contains('@'))
            return true;
        var resolved = ResolveVars(text, vars);
        if (resolved.Contains('@') || HoldsForbiddenHost(resolved))
            return true;
        foreach (Match match in TemplateToken.Matches(text))
        {
            var name = match.Groups[1].Value;
            if (name == "baseUrl")
                continue;
            if (!committedKeys.Contains(name))
                return true;
        }

        return false;
    }

    private static string RebuildUrl(JsonElement url, Dictionary<string, string> vars)
    {
        string HostJoined()
        {
            if (!url.TryGetProperty("host", out var host) || host.ValueKind != JsonValueKind.Array)
                return "";
            return string.Join(".", host.EnumerateArray().Select(e => ResolveVars(ElementAsString(e) ?? "", vars)));
        }

        string PathJoined()
        {
            if (!url.TryGetProperty("path", out var path))
                return "";
            if (path.ValueKind == JsonValueKind.String)
            {
                var s = path.GetString() ?? "";
                return s.StartsWith('/') ? s : "/" + s;
            }

            if (path.ValueKind != JsonValueKind.Array)
                return "";
            var parts = path.EnumerateArray().Select(e => ElementAsString(e) ?? "").ToArray();
            if (parts.Length == 0)
                return "";
            return "/" + string.Join("/", parts);
        }

        string QueryJoined()
        {
            if (!url.TryGetProperty("query", out var query) || query.ValueKind != JsonValueKind.Array)
                return "";
            var parts = new List<string>();
            foreach (var child in query.EnumerateArray())
            {
                if (child.ValueKind == JsonValueKind.String)
                {
                    parts.Add(child.GetString() ?? "");
                    continue;
                }

                if (child.ValueKind != JsonValueKind.Object)
                    continue;
                var key = child.TryGetProperty("key", out var k) ? ElementAsString(k) ?? "" : "";
                var value = child.TryGetProperty("value", out var v) ? ElementAsString(v) ?? "" : "";
                parts.Add($"{key}={value}");
            }

            return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
        }

        var host = HostJoined();
        var path = PathJoined();
        var query = QueryJoined();
        if (host == "{{baseUrl}}")
            return "{{baseUrl}}" + path + query;

        var protocol = "http";
        if (url.TryGetProperty("protocol", out var protocolEl))
            protocol = ResolveVars(ElementAsString(protocolEl) ?? "http", vars);
        var rebuilt = protocol + "://" + host;
        if (url.TryGetProperty("port", out var portEl))
            rebuilt += ":" + ResolveVars(ElementAsString(portEl) ?? "", vars);
        return rebuilt + path + query;
    }

    private static bool IsAllowedRequestUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Trim();
        return AllowedBaseUrlTemplate.IsMatch(trimmed) || AllowedLiteralOrigin.IsMatch(trimmed);
    }

    private static void WalkEveryString(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl,
        HashSet<string>? committedKeys = null,
        string path = "")
    {
        committedKeys ??= CollectCommittedKeys(el);
        if (path is "info.description" or "info.schema")
            return;

        if (el.ValueKind == JsonValueKind.String)
        {
            CheckEveryString(label, el.GetString() ?? "", path, failures, seenMessages, sanctionedLiveBaseUrl, committedKeys);
            return;
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                WalkEveryString(label, child, failures, seenMessages, sanctionedLiveBaseUrl, committedKeys, path);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        var skipLiveSlot = sanctionedLiveBaseUrl && IsBaseUrlSlot(el);
        foreach (var prop in el.EnumerateObject())
        {
            if (skipLiveSlot && prop.Name is "value" or "currentValue")
                continue;
            var childPath = path.Length == 0 ? prop.Name : path + "." + prop.Name;
            if (prop.Name is "exec" or "script")
                CheckScriptNode(label, prop.Value, failures, seenMessages, committedKeys);
            WalkEveryString(label, prop.Value, failures, seenMessages, sanctionedLiveBaseUrl, committedKeys, childPath);
        }
    }

    private static void CheckScriptNode(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        HashSet<string> committedKeys)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            CheckScriptPatterns(label, el.GetString() ?? "", failures, seenMessages, committedKeys);
            return;
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            var joined = string.Join("\n", el.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : ""));
            CheckScriptPatterns(label, joined, failures, seenMessages, committedKeys);
        }
    }

    private static void CheckEveryString(
        string label,
        string text,
        string path,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl,
        HashSet<string> committedKeys)
    {
        _ = sanctionedLiveBaseUrl;
        if (HoldsForbiddenHost(text))
            AddFailure(failures, seenMessages, $"{label} contains a non-local host at {path}");
        CheckScriptPatterns(label, text, failures, seenMessages, committedKeys);
    }

    private static void CheckScriptPatterns(
        string label,
        string text,
        List<string> failures,
        HashSet<string> seenMessages,
        HashSet<string> committedKeys)
    {
        if (string.IsNullOrEmpty(text))
            return;
        if (text.Contains("eval(", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains eval(");
        if (text.Contains("Function(", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains Function(");
        if (text.Contains("atob(", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains atob(");
        if (text.Contains("require(", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains require(");
        if (PmBracketAccess.IsMatch(text))
            AddFailure(failures, seenMessages, $"{label} collection script contains pm[ or pm.*[");
        if (text.Contains("sendRequest", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains sendRequest");
        if (text.Contains("request.url", StringComparison.Ordinal)
            || text.Contains("request['url']", StringComparison.Ordinal)
            || text.Contains("request[\"url\"]", StringComparison.Ordinal))
            AddFailure(failures, seenMessages, $"{label} collection script contains pm.request.url");
        CheckSetCalls(label, text, failures, seenMessages, committedKeys);
    }

    private static void CheckSetCalls(
        string label,
        string text,
        List<string> failures,
        HashSet<string> seenMessages,
        HashSet<string> committedKeys)
    {
        foreach (Match match in SetCall.Matches(text))
        {
            var j = match.Index + match.Length;
            while (j < text.Length && char.IsWhiteSpace(text[j]))
                j++;
            if (j >= text.Length)
            {
                AddFailure(failures, seenMessages, $"{label} collection script sets baseUrl");
                continue;
            }

            var quote = text[j];
            if (quote is not ('\'' or '"'))
            {
                AddFailure(failures, seenMessages, $"{label} collection script sets baseUrl");
                continue;
            }

            var end = text.IndexOf(quote, j + 1);
            if (end < 0)
            {
                AddFailure(failures, seenMessages, $"{label} collection script sets baseUrl");
                continue;
            }

            var key = text.Substring(j + 1, end - (j + 1));
            if (key == "baseUrl" || !committedKeys.Contains(key))
                AddFailure(failures, seenMessages, $"{label} collection script sets baseUrl");
        }
    }

    private static bool IsBaseUrlSlot(JsonElement el)
    {
        return el.TryGetProperty("key", out var key)
               && key.ValueKind == JsonValueKind.String
               && key.GetString() == "baseUrl";
    }

    private static void CollectAuthAndHeaderFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectAuthAndHeaderFailures(label, child, failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "header" && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var header in prop.Value.EnumerateArray())
                {
                    if (header.ValueKind != JsonValueKind.Object)
                        continue;
                    var key = header.TryGetProperty("key", out var keyEl) ? ElementAsString(keyEl) : null;
                    var value = header.TryGetProperty("value", out var valueEl) ? ElementAsString(valueEl) : null;
                    if (IsCredentialHeader(key) && !IsTemplateOrEmpty(value))
                        AddFailure(failures, seenMessages, $"{label} literal secret in header '{key}'");
                }
            }
            else if (prop.Name == "auth")
            {
                CollectAuthLiteralValues(label, prop.Value, failures, seenMessages);
            }
            else
            {
                CollectAuthAndHeaderFailures(label, prop.Value, failures, seenMessages);
            }
        }
    }

    private static void CollectAuthLiteralValues(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectAuthLiteralValues(label, child, failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "value")
            {
                var value = ElementAsString(prop.Value);
                if (!IsTemplateOrEmpty(value))
                    AddFailure(failures, seenMessages, $"{label} literal secret in auth block");
            }
            else
            {
                CollectAuthLiteralValues(label, prop.Value, failures, seenMessages);
            }
        }
    }

    private static bool IsCredentialHeader(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return false;
        return key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
               || IsSecretSlot(key, null);
    }

    private static bool IsTemplateOrEmpty(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;
        var rest = Regex.Replace(value, @"\{\{[^}]+\}\}", "");
        rest = Regex.Replace(rest, @"^(ApiKey|Bearer|Basic)\s*", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(rest);
    }

    private static string? ElementAsString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        _ => null
    };

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
            if (variable.ValueKind != JsonValueKind.Object)
                continue;
            if (!variable.TryGetProperty("key", out var keyEl) || keyEl.ValueKind != JsonValueKind.String)
                continue;
            var key = keyEl.GetString();
            if (string.IsNullOrEmpty(key))
                continue;

            var type = variable.TryGetProperty("type", out var typeEl) ? ElementAsString(typeEl) : null;
            var hasValue = variable.TryGetProperty("value", out var valueEl);
            var value = hasValue ? ElementAsString(valueEl) : null;
            var hasCurrentValue = variable.TryGetProperty("currentValue", out var currentEl);
            var currentValue = hasCurrentValue ? ElementAsString(currentEl) : null;
            seen[key] = new Slot(key, type, value, hasValue, currentValue, hasCurrentValue);
        }

        return seen;
    }

    private static async Task<(int ExitCode, string Output, bool NetworkCalled)> RunNewmanGuardAsync(
        string baseUrl,
        bool curlExitsZero,
        bool useUsrBinCurl = false,
        bool useBinCurl = false,
        params string[] extraArgs)
    {
        var script = Path.Combine(RepoRoot(), "scripts", "run-newman.sh");
        Assert.True(File.Exists(script), "scripts/run-newman.sh must exist");

        var temp = Directory.CreateTempSubdirectory("run-newman-guard-");
        try
        {
            var copy = Path.Combine(temp.FullName, "run-newman.sh");
            var text = File.ReadAllText(script);
            if (useUsrBinCurl)
                text = Regex.Replace(text, @"(?<![/\w])curl\b", "/usr/bin/curl");
            if (useBinCurl)
                text = Regex.Replace(text, @"(?<![/\w])curl\b", "/bin/curl");

            var stubDir = Path.Combine(temp.FullName, "bin");
            Directory.CreateDirectory(stubDir);
            var marker = Path.Combine(temp.FullName, "network-called");
            var stubCurl = Path.Combine(stubDir, "curl");
            WriteExecutableStub(stubCurl, marker, curlExitsZero ? 0 : 1);
            WriteExecutableStub(Path.Combine(stubDir, "npx"), marker, 1);
            WriteExecutableStub(Path.Combine(stubDir, "npm"), marker, 1);

            const string curlToken = "___DEALOWARE_CURL_STUB___";
            text = text.Replace("/usr/bin/curl", curlToken, StringComparison.Ordinal);
            text = text.Replace("/bin/curl", curlToken, StringComparison.Ordinal);
            text = Regex.Replace(text, @"(?<![/\w])curl\b", curlToken);
            text = text.Replace(curlToken, stubCurl, StringComparison.Ordinal);
            File.WriteAllText(copy, text);

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

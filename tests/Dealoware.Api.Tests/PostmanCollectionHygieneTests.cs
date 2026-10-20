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
        @"\.set\(\s*(?:['""]baseUrl['""]|['""]base['""]\s*\+\s*['""]Url['""])",
        RegexOptions.Compiled);

    private static readonly Regex TemplateToken = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled);

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
                if (doc.RootElement.TryGetProperty("info", out var info)
                    && info.ValueKind == JsonValueKind.Object
                    && info.TryGetProperty("_postman_id", out _))
                {
                    Add(collections, file);
                }
                else if (doc.RootElement.TryGetProperty("_postman_variable_scope", out _))
                {
                    Add(environments, file);
                }
            }
            catch (JsonException)
            {
                // not Postman JSON
            }
        }
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
                CollectNonLocalHostSlotFailures(label, seen, failures, sanctionedLiveBaseUrl: false);
            }

            CollectItemUrlFailures(label, doc.RootElement, failures);
            CollectScriptFailures(label, doc.RootElement, failures);
            CollectHttpsHostFailures(label, doc.RootElement, failures, sanctionedLiveBaseUrl: false);
            CollectLiteralSecretHeaderFailures(label, doc.RootElement, failures);
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
            var sanctioned = IsSanctionedLiveEnv(file, scanRoot);
            if (!doc.RootElement.TryGetProperty("values", out var values)
                || values.ValueKind != JsonValueKind.Array)
            {
                failures.Add($"{label} must have a values array");
            }

            var seen = ReadEnvironmentSlots(doc.RootElement);
            var allowed = sanctioned
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
            CollectNonLocalHostSlotFailures(label, seen, failures, sanctionedLiveBaseUrl: sanctioned);
            CollectHttpsHostFailures(label, doc.RootElement, failures, sanctionedLiveBaseUrl: sanctioned);
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

    private static void CollectNonLocalHostSlotFailures(
        string label,
        Dictionary<string, Slot> seen,
        List<string> failures,
        bool sanctionedLiveBaseUrl)
    {
        foreach (var (key, slot) in seen)
        {
            if (sanctionedLiveBaseUrl && key == "baseUrl")
                continue;
            if (slot.HasValue && HoldsNonLocalHost(slot.Value))
                failures.Add($"{label} {key} value holds a non-local host");
            if (slot.HasCurrentValue && HoldsNonLocalHost(slot.CurrentValue))
                failures.Add($"{label} {key} currentValue holds a non-local host");
        }
    }

    private static bool HoldsNonLocalHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (HasNonLocalHttpUrl(value))
            return true;
        var trimmed = value.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal))
            return false;
        if (AllowedLiteralOrigin.IsMatch(trimmed) || AllowedBaseUrlTemplate.IsMatch(trimmed))
            return false;
        return NoSchemeHost.IsMatch(trimmed);
    }

    private static bool HasNonLocalHttpUrl(string text)
    {
        foreach (Match match in HttpUrl.Matches(text))
        {
            var candidate = match.Value.TrimEnd('.', ',', ')', ']');
            if (AllowedLiteralOrigin.IsMatch(candidate))
                continue;
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
                && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.Ordinal)))
            {
                continue;
            }

            return true;
        }

        return false;
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
            if (!IsAllowedRequestUrl(url.GetString()))
                failures.Add($"{label} request URL must match {{{{baseUrl}}}}(/|$) or http://localhost/127.0.0.1");
            return;
        }

        if (url.ValueKind != JsonValueKind.Object)
            return;
        if (url.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.String)
        {
            if (!IsAllowedRequestUrl(raw.GetString()))
                failures.Add($"{label} request URL must match {{{{baseUrl}}}}(/|$) or http://localhost/127.0.0.1");
        }

        if (url.TryGetProperty("host", out var host) && host.ValueKind == JsonValueKind.Array)
        {
            var parts = host.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "")
                .ToArray();
            if (parts.Length != 1 || !AllowedHostArray.Contains(parts[0]))
                failures.Add($"{label} host array must be [{{{{baseUrl}}}}] or localhost");
        }
    }

    private static bool IsAllowedRequestUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Trim();
        return AllowedBaseUrlTemplate.IsMatch(trimmed) || AllowedLiteralOrigin.IsMatch(trimmed);
    }

    private static void CollectScriptFailures(string label, JsonElement el, List<string> failures)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("exec", out var exec) && exec.ValueKind == JsonValueKind.Array)
            {
                var text = string.Join("\n", exec.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : ""));
                // F3 + N2: the baseUrl-setting check also flags pm.request.url and pm.sendRequest (item 4).
                if (text.Contains("sendRequest(", StringComparison.Ordinal)
                    || text.Contains("pm.sendRequest", StringComparison.Ordinal))
                    failures.Add($"{label} collection script contains sendRequest(");
                if (text.Contains("pm.request.url", StringComparison.Ordinal))
                    failures.Add($"{label} collection script contains pm.request.url");
                if (SetsBaseUrl.IsMatch(text)
                    || text.Contains("sendRequest(", StringComparison.Ordinal)
                    || text.Contains("pm.sendRequest", StringComparison.Ordinal)
                    || text.Contains("pm.request.url", StringComparison.Ordinal))
                    failures.Add($"{label} collection script sets baseUrl");
            }

            foreach (var prop in el.EnumerateObject())
                CollectScriptFailures(label, prop.Value, failures);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectScriptFailures(label, child, failures);
        }
    }

    private static void CollectHttpsHostFailures(
        string label,
        JsonElement el,
        List<string> failures,
        bool sanctionedLiveBaseUrl,
        string path = "")
    {
        if (path is "info.description" or "info.schema")
            return;

        if (el.ValueKind == JsonValueKind.String)
        {
            if (HasNonLocalHttpUrl(el.GetString() ?? ""))
                failures.Add($"{label} contains http(s) to a non-local host at {path}");
            return;
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectHttpsHostFailures(label, child, failures, sanctionedLiveBaseUrl, path);
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
            CollectHttpsHostFailures(label, prop.Value, failures, sanctionedLiveBaseUrl, childPath);
        }
    }

    private static bool IsBaseUrlSlot(JsonElement el)
    {
        return el.TryGetProperty("key", out var key)
               && key.ValueKind == JsonValueKind.String
               && key.GetString() == "baseUrl";
    }

    private static void CollectLiteralSecretHeaderFailures(string label, JsonElement el, List<string> failures)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectLiteralSecretHeaderFailures(label, child, failures);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        if (el.TryGetProperty("request", out var request) && request.ValueKind == JsonValueKind.Object)
        {
            if (request.TryGetProperty("header", out var headers) && headers.ValueKind == JsonValueKind.Array)
            {
                foreach (var header in headers.EnumerateArray())
                {
                    var key = header.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
                    var value = header.TryGetProperty("value", out var valueEl) ? valueEl.GetString() : null;
                    if (IsCredentialHeader(key) && !IsTemplateOrEmpty(value))
                        failures.Add($"{label} literal secret in header '{key}'");
                }
            }

            if (request.TryGetProperty("auth", out var auth) && auth.ValueKind == JsonValueKind.Object)
                CollectAuthLiteralValues(label, auth, failures);
        }

        if (el.TryGetProperty("item", out var item))
            CollectLiteralSecretHeaderFailures(label, item, failures);
    }

    private static void CollectAuthLiteralValues(string label, JsonElement el, List<string> failures)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectAuthLiteralValues(label, child, failures);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "value" && prop.Value.ValueKind == JsonValueKind.String
                && !IsTemplateOrEmpty(prop.Value.GetString()))
            {
                failures.Add($"{label} literal secret in auth block");
            }
            else
            {
                CollectAuthLiteralValues(label, prop.Value, failures);
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
        var rest = TemplateToken.Replace(value, "");
        rest = Regex.Replace(rest, @"^(ApiKey|Bearer|Basic)\s*", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(rest);
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
        bool useUsrBinCurl = false,
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

            var stubDir = Path.Combine(temp.FullName, "bin");
            Directory.CreateDirectory(stubDir);
            var marker = Path.Combine(temp.FullName, "network-called");
            var stubCurl = Path.Combine(stubDir, "curl");
            WriteExecutableStub(stubCurl, marker, curlExitsZero ? 0 : 1);
            WriteExecutableStub(Path.Combine(stubDir, "npx"), marker, 1);
            WriteExecutableStub(Path.Combine(stubDir, "npm"), marker, 1);

            text = text.Replace("/usr/bin/curl", stubCurl, StringComparison.Ordinal);
            text = Regex.Replace(text, @"(?<![/\w])curl\b", stubCurl);
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

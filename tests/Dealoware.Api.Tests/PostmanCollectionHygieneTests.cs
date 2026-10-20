using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    private const string UrlAllowMsg = "request URL must match {{baseUrl}}(/|$) or http://localhost/127.0.0.1";
    private const string HostAllowMsg = "host array must be [{{baseUrl}}]";
    private const string UrlTokenMsg = "request URL contains a disallowed {{token}}";
    private const string UrlAuthMsg = "request URL must not contain auth";
    private const string ProxyMsg = "proxy configuration is not allowed";
    private const string HostHeaderMsg = "Host header is not allowed";
    private const string ScriptHashMsg = "collection script hash is not on the allowlist";
    private const string ScriptAliasMsg = "collection script aliases pm";
    private const string DangerousPrefixMsg = "value must not start with @ or //";
    private const string VariableKeyMsg = "variable key must be a non-empty string";
    private const string EventMustBeArrayMsg = "event must be an array";
    private const string EventEntryObjectMsg = "event entry must be an object";
    private const string EventOnRequestMsg = "event on request is not allowed";
    private const string ScriptSrcMsg = "script src is not allowed";
    private const string ScriptExecShapeMsg = "script exec must be a string or array of strings";
    private const string DuplicateJsonPropertyMsg = "duplicate JSON property";
    private const string CollectionSchemaMsg =
        "info.schema must be the v2.x getpostman.com collection schema";
    private const string V1KeyMsg = "Postman v1 key is not allowed";
    private const string LegacyScriptKeyMsg = "preRequestScript, tests, and events keys are not allowed";
    private const string BackslashPathMsg = "path segment must not contain a backslash";
    private const string ForbiddenLocationMsg = "Postman file is not allowed under bin/obj/test-results";

    private static readonly HashSet<string> V1CollectionShapeKeys = new(StringComparer.Ordinal)
    {
        "requests", "order", "folders", "folders_order"
    };

    private static readonly HashSet<string> ForbiddenV1CollectionKeys = new(StringComparer.Ordinal)
    {
        "requests", "order", "folders", "folders_order",
        "name", "id", "timestamp", "owner", "remoteLink", "public", "synced"
    };

    private static readonly HashSet<string> ForbiddenLegacyScriptKeys = new(StringComparer.Ordinal)
    {
        "preRequestScript", "tests", "events"
    };

    private static readonly Regex AllowedCollectionSchema = new(
        @"^https://schema\.getpostman\.com/json/collection/v2\.\d+\.\d+/collection\.json$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> AllowedUrlPathTokens = new(StringComparer.Ordinal)
    {
        "artifactId", "negotiationId", "offerId", "counterOfferId"
    };

    private static readonly Regex AllowedBaseUrlTemplate =
        new(@"^\{\{baseUrl\}\}(/|$)", RegexOptions.Compiled);

    private static readonly Regex AllowedLiteralOrigin =
        new(@"^http://(localhost|127\.0\.0\.1)(:[0-9]+)?(/|$)", RegexOptions.Compiled);

    private static readonly Regex HttpOrProtocolRelativeUrl = new(
        @"(?:https?:)?//(?<authority>[^/?#\s""'\\]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NoSchemeHost = new(
        @"^[A-Za-z0-9.-]+\.[A-Za-z]{2,}(:[0-9]+)?(/|$)",
        RegexOptions.Compiled);

    private static readonly Regex TemplateToken = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    private static readonly Regex NumericPort = new(@"^[0-9]{1,5}$", RegexOptions.Compiled);

    private static readonly Regex PmBracketAccess = new(@"pm\[|pm\.[A-Za-z0-9_.]+\[", RegexOptions.Compiled);

    private static readonly Regex PmAlias = new(
        @"[A-Za-z_$]\w*=pm(?![A-Za-z0-9_$.])",
        RegexOptions.Compiled);

    private static readonly ScriptAllowlistEntry[] ScriptAllowlist =
    [
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "00-Health-Check / Health Check (No Auth)", "test", "cd048fda3e1fea44bd48f8dc965bc6d51dbabc64f0a6f45368a1063127e4dbc6"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "01-Bootstrap / Register Seller", "test", "3aa63011b5c03f1b5c3512bc154a595cb740e09f6fabe51f8d984d90f760a243"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "01-Bootstrap / Register Buyer", "test", "1168e6098e28b912617536a020d43cf69e3b52f094df7693d22cad7eb5058d8e"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "01-Bootstrap / Create Artifact (Seller)", "test", "7907787b426fa1fb16d4aba3a4bfec9faa4b8d3b2c1303dd05ee06684a8cdcf8"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "02-Create-Negotiation / Create Negotiation (Seller initiates)", "test", "e21b994b6a4ab5f8c6e85c79dadf88ee48a100763b6775c35119378edd4d1589"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "02-Create-Negotiation / Counterparty GETs Negotiation (Buyer)", "test", "7cbacb3886f2dd7071b0f0295830daa5dd8b65745704ad9e8788e922a38160b3"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Grok Connector - Initiate (Seller)", "test", "317e54ce47fa952e68d3277b830f2a674ac51f8027db1245f68b2d4e86f5360f"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Grok Connector - Initiate (Buyer)", "test", "dcf426f5ad54e7724f63187fabcbe85318084388460b988f0ddc1f4c019a6a15"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Muse Connector - Get Status (Seller)", "test", "ee80a002c3c79d9a24f230a199928da4822134834b0bbfd462a18880f959c829"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Muse Connector - List Offers (Buyer)", "test", "5e2ca9ae2c58d05eb81be2e75148e86098f9bd83239aea871c1c363edd1e4f07"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Dot Connector - View (Seller)", "test", "87408b38539635971e49e6a6e5653e98f5c56e8fc5fcb1758b49c1a78cbaad98"),
        new("postman/Dealoware-Inbound-Connectors.postman_collection.json", "03-Inbound-Connectors / Dot Connector - Interact (Buyer)", "test", "67c1e7b6c3bb9cb70d32d0eda0c803a6194a39bfdeff4ade655a51c00a647b84"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "(collection)", "test", "f38c4ada74284f82d05996798a5db1f1238c24b72ef6e9471fc491d09a186e8f"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "00-Health / Health Check (No Auth)", "test", "df300d936dcb93709f0cf49ef62073076c95c3513ab1ce62ca64826a24a37cec"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "01-Auth / Register Seller", "test", "7d831cb268f32e848e494fbe5ba0ea6b2461cb602ff009da306524b3acd214c5"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "01-Auth / Register Buyer", "test", "c7c540a66b3b5dca813fa3d8adfdc31a7d99187ce2307ee4c2c74fd797beb7b7"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "01-Auth / Issue JWT (store accessToken)", "test", "2e6bc0b91c1b3d2f9ea560064d9b57c9987a1ee503095ca2dc902b9bd310bb7b"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "02-Artifact / Create Sell Artifact (Seller)", "test", "de13c79762810385e31bb596977f696df7d5a02873ef9dd48249ac7ada8cdc3b"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "02-Artifact / Get Artifact by Id (Seller)", "test", "4f0ca36465ea2ca1b785cb481f5de52615836d61893fc4dffef6be9630907aab"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "02-Artifact / List Own Artifacts (Seller)", "test", "9d508483f2445352bc847661c563a99305ec0cf1d29505bbf00ac589406f9878"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "03-Negotiation / Create Negotiation (Seller initiates)", "test", "1e0f53387cd70973ec9cc7881ac7a5e2888b090d4d6c269d2dc8ef261512c9cd"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "03-Negotiation / List Negotiations (Seller Bearer)", "test", "00719ad5b0e87d14b2c08d990fa8ca98b81a25405e1693663b9bb25ef95c3675"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "03-Negotiation / Get Negotiation by Id (Seller)", "test", "4f3a43417f7324ce8c7d442fb870cc01320b4d83600ec40e99082446a09e415e"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "04-Offer / Seller Places Offer", "test", "4c722e31476858d697102199f80e86827cc443d5a5f0a77af1e088e2194e5ee8"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "04-Offer / List Offers (Seller)", "test", "7a30fdfd5c0b013f0fa57e6a65d448cb6306dad4ead6f6a9fe2864c8edeff0b0"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "04-Offer / Get Offer by Id (Buyer)", "test", "dda01f16ce4f7df85b0ae6622dfca11695de778c855eba65dcc927713ebb5011"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "05-Counter / Buyer Counters", "test", "c5f0cd66c9104371a4fff89484137be5f3057287d5ad866adf3938cf82e173d0"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "06-Accept-or-Decline / Seller Accepts Counter", "test", "216227d06e0bf4076f4a0d85caf2b9ff60c9167f6809b1f0737122048b447adc"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "06-Accept-or-Decline / View Final Negotiation State", "test", "bfa731dcf20363cadd05df684882f02d247511001944839cb08cb585548dfcdc"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "06-Accept-or-Decline / Decline setup: Create Fresh Negotiation", "test", "c58a9e9df9cef03ff884ef56ca31ace4c3f399e55ba19df5878844966fc7c5b6"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "06-Accept-or-Decline / Decline setup: Seller Places Offer", "test", "a5f094ad43980cf3463e1b796dcde9f290b9def7a9a5c4f223ea7ca3a2e2496c"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "06-Accept-or-Decline / Buyer Declines Offer", "test", "9be199f8722ea466160333b0dbce4ac2c36754b63d594c7ecf417e6d07a368da"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "07-Close / Close setup: Create Fresh Negotiation", "test", "fb3fb2c87bb60ebc55c7583ef7adfe6380f77fc660a6deb39e1d8bd3a82a9b22"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "07-Close / Close setup: Place Offer (to be cancelled)", "test", "a5f094ad43980cf3463e1b796dcde9f290b9def7a9a5c4f223ea7ca3a2e2496c"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "07-Close / Close Negotiation", "test", "0f07139ee7e1dba684d9ae7d4721de0777c23b06a1aadf4ddd6490b987621da4"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Non-Complementary Intents → 400", "test", "8b5bf27ade811526031a5b91e79f508012ba949385620433b63d4b45d6397b0b"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Register Observer (Third Party)", "test", "6482ba9bf1d98588f4d98d66fec61a08ad77c0a4ca568d21d0aec23e99a24647"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Non-party setup: Create Negotiation", "test", "fb3fb2c87bb60ebc55c7583ef7adfe6380f77fc660a6deb39e1d8bd3a82a9b22"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Non-Party GET → 404", "test", "4b8f119fe2a802e01698abae77ceddbe2a158e700f223d3a8f5ba9b99009255e"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Duplicate-offer setup: Create Negotiation", "test", "fb3fb2c87bb60ebc55c7583ef7adfe6380f77fc660a6deb39e1d8bd3a82a9b22"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / First Offer (Success)", "test", "a5f094ad43980cf3463e1b796dcde9f290b9def7a9a5c4f223ea7ca3a2e2496c"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Second Offer Same Side → 400", "test", "e95a3866fae7a7810f0c526284517597fa0e135598fdaa7379f818c7a2a60a34"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / After-close setup: Create Negotiation", "test", "fb3fb2c87bb60ebc55c7583ef7adfe6380f77fc660a6deb39e1d8bd3a82a9b22"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / After-close setup: Close Negotiation", "test", "007448ccc47a3fa13f47d2f3a6045e0347f1f82d07ccdc26bebc4cff1fd70db9"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Place Offer After Close → 409", "test", "3ccabb6d888d5f359d63d9504a0c2300b160850e6e394ce7e7b642d391d84883"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "08-Negative-Cases / Close Again → 409", "test", "32deaa2526a2144c7ac9ce512de612c7d208607b60f327491810dffb183179fb"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "09-Expiration / Create Expired Negotiation", "test", "fb3fb2c87bb60ebc55c7583ef7adfe6380f77fc660a6deb39e1d8bd3a82a9b22"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "09-Expiration / Place Offer on Expired → 409", "test", "9b5a0ecd8e23a66bafdc5ae38c5a2e98613f98d2da07a99853c82a816a960654"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "09-Expiration / GET Expired (Still Works)", "test", "e6ffc24d2b6a1d10d90e316aa4e1e44590374842ef5114f1bd8a37c4637ded0d"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "10-Other-Intents / Provide ↔ Consume Negotiation", "test", "04a9e64a020dea469238791957f18b56bf11e9f9e825536131530d581fbe7251"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "10-Other-Intents / Rent ↔ Rent Out Negotiation", "test", "04aa24e88fd4b822f3ee557fdd9998f49877bc8d123ed0cae747ee4e2e63923d"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "11-Optional-Rotate-and-Revoke / Register Auth-Demo Participant", "test", "918a0214b4259a47d36a2e3543bbbfca5676a72941bcf09ca9b5c9f6606c1b71"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "11-Optional-Rotate-and-Revoke / Rotate API Key (optional)", "test", "8a8fe9cde6dc4c50e70ef2a0aae540087321cd191ee2c19d409bddc380357b72"),
        new("postman/Dealoware-PoC-Negotiations.postman_collection.json", "11-Optional-Rotate-and-Revoke / Revoke API Key (optional)", "test", "d9fec2deb61dd827fbdf259d4c486d16f809bc5542910ac8271d591d2328abcd"),
    ];

    private static readonly Dictionary<(string File, string Item, string Listen), string> ScriptAllowlistByIdentity =
        ScriptAllowlist.ToDictionary(e => (e.File, e.Item, e.Listen), e => e.Sha256);

    private static string RepoRoot() => ResolveRepoRoot(AppContext.BaseDirectory);

    internal static string ResolveRepoRoot(string startPath)
    {
        DirectoryInfo? dir;
        var full = Path.GetFullPath(startPath);
        if (File.Exists(full) && !Directory.Exists(full))
        {
            var parent = Path.GetDirectoryName(full);
            dir = parent is null ? null : new DirectoryInfo(parent);
        }
        else
        {
            dir = new DirectoryInfo(full);
        }

        string? outermostSln = null;
        string? outermostSlnWithGit = null;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            {
                outermostSln = dir.FullName;
                var git = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(git) || File.Exists(git))
                    outermostSlnWithGit = dir.FullName;
            }

            dir = dir.Parent;
        }

        return outermostSlnWithGit
               ?? outermostSln
               ?? throw new InvalidOperationException("Could not find Dealoware.sln");
    }

    private static string ToPosixRelative(string root, string file)
    {
        var rel = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(file));
        if (RelativePathHasBackslashSegment(root, file))
            return rel;
        return rel.Replace('\\', '/');
    }

    private static bool RelativePathHasBackslashSegment(string root, string file)
    {
        var rel = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(file));
        foreach (var segment in rel.Split(Path.DirectorySeparatorChar))
        {
            if (segment.Contains('\\'))
                return true;
        }

        return false;
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
        var suffix = searchPattern.StartsWith('*') ? searchPattern[1..] : searchPattern;
        foreach (var file in EnumerateFilesSkipping(root))
        {
            if (file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                yield return file;
        }
    }

    private static IEnumerable<string> EnumerateFilesSkipping(string root)
    {
        var scanRoot = Path.GetFullPath(root);
        var stack = new Stack<string>();
        stack.Push(scanRoot);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var file in Directory.GetFiles(dir))
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

        foreach (var file in EnumerateFilesSkipping(root))
        {
            if (file.EndsWith(".postman_collection.json", StringComparison.OrdinalIgnoreCase))
            {
                Add(collections, file);
                continue;
            }

            if (file.EndsWith(".postman_environment.json", StringComparison.OrdinalIgnoreCase))
            {
                Add(environments, file);
                continue;
            }

            if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            var full = Path.GetFullPath(file);
            if (seen.Contains(full))
                continue;
            if (TryClassifyPostmanJson(file, out var isCollection, out var isEnvironment))
            {
                if (isCollection)
                    Add(collections, file);
                else if (isEnvironment)
                    Add(environments, file);
            }
        }
    }

    private static bool TryClassifyPostmanJson(string file, out bool isCollection, out bool isEnvironment)
    {
        isCollection = false;
        isEnvironment = false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (LooksLikeCollection(doc.RootElement))
            {
                isCollection = true;
                return true;
            }

            if (LooksLikeEnvironment(doc.RootElement))
            {
                isEnvironment = true;
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool LooksLikeCollection(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;
        foreach (var key in V1CollectionShapeKeys)
        {
            if (root.TryGetProperty(key, out _))
                return true;
        }

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
        Assert.Empty(CollectForbiddenLocationFailures(RepoRoot()));
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
    public void ScriptAllowlist_HasNoStaleEntries()
    {
        DiscoverPostmanFiles(RepoRoot(), out var collections, out _);
        var actual = new HashSet<ScriptAllowlistEntry>(CollectCommittedScriptEntries(collections));
        var expected = new HashSet<ScriptAllowlistEntry>(ScriptAllowlist);
        Assert.Equal(expected, actual);
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
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default", "enabled": true },
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
                    "url": { "raw": "{{baseUrl}}/health", "host": "evil.example.test" }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
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
                    "proxy": { "host": "evil.example.test", "port": 443 }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ProxyMsg, StringComparison.Ordinal));
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
            host: """["evil.example.test"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
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
                    "exec": [ "console.log('https://evil.example.test/health');" ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("http(s) or // to a non-local host", StringComparison.Ordinal));
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
    public void HygieneScan_FailsUrlPathDisallowedToken()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "path-token.postman_collection.json",
            extraVars: """{ "key": "redir", "value": "health", "type": "string" }""",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "path": ["{{redir}}"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(UrlTokenMsg, StringComparison.Ordinal));
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

    [Fact]
    public void HygieneScan_FailsRequestLevelVariableHoldingLiveHost()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "f06-request-var.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "variable": [
                    { "key": "otherVar", "value": "https://evil.example.test", "type": "string" }
                  ],
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("otherVar", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsHostHeader()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "f07-host-header.postman_collection.json"),
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
                    "header": [ { "key": "Host", "value": "evil.example.test" } ],
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(HostHeaderMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsFolderLevelEventSendRequest()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "folder-event.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "event": [
                    {
                      "listen": "prerequest",
                      "script": {
                        "exec": [ "pm.sendRequest('https://evil.example.test/health', function(){});" ]
                      }
                    }
                  ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("sendRequest", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsLiteralAtInPath()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "path-at.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "path": ["health@evil.example.test"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(UrlAllowMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P01_FailsHostTokenRegardlessOfSavedValue()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p01-host-token.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "h", "value": "localhost", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "event": [
                    {
                      "listen": "prerequest",
                      "script": {
                        "exec": [ "pm.collectionVariables.set('h','api.core.deal'+'oware.com');" ]
                      }
                    }
                  ],
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{h}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P02_FailsPortTokenRegardlessOfSavedValue()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p02-port-token.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "p", "value": "5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "event": [
                    {
                      "listen": "prerequest",
                      "script": {
                        "exec": [ "pm.collectionVariables.set('p','5287@evil.example.test');" ]
                      }
                    }
                  ],
                  "request": {
                    "method": "GET",
                    "url": {
                      "raw": "{{baseUrl}}/health",
                      "host": ["{{baseUrl}}"],
                      "port": "{{p}}",
                      "path": ["health"]
                    }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(UrlTokenMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P03_FailsEvalWithWhitespace()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p03-eval-space.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "eval ('1+1');" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("eval(", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P04_FailsPmAliasAndConcatenatedSendRequest()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p04-pm-alias.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "x=pm; x['send'+'Request']('https://evil.example.test', function(){});" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ScriptAliasMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P05_FailsPmBracketWithWhitespace()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p05-pm-bracket-space.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "pm ['sendRequest']('https://evil.example.test', function(){});" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("pm[ or pm.*[", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P06_FailsSplitPmRequestUrl()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p06-split-request-url.postman_collection.json"),
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
                      "pm.request",
                      ".url = 'https://evil.example.test/health';"
                    ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("pm.request.url", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P07_FailsSetWithWhitespace()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p07-set-space.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "pm.collectionVariables. set('h', 'evil.example.test');" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(".set", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P08_FailsAnyProxyConfig()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p08-proxy.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "proxy": { "host": "evil.example.test", "port": 8080 },
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ProxyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P09_FailsHttpsInVariable()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p09-var-https.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "note", "value": "https://evil.example.test", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("note", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P10_FailsHttpsInDescriptionAndHeader()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p10-desc-header.postman_collection.json"),
            """
            {
              "info": {
                "_postman_id": "p10",
                "name": "p10",
                "description": "see https://evil.example.test"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "header": [ { "key": "X-Link", "value": "https://evil.example.test" } ],
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("http(s) or // to a non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P11_FailsBracketSet()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p11-bracket-set.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "var e = pm.collectionVariables; e['set']('h', 'evil.example.test');" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(".set", StringComparison.Ordinal) || f.Contains("['set']", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_P12_FailsVariableValueAtOrSlashSlashPrefix()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "p12-prefix.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "atHost", "value": "@evil.example.test", "type": "string" },
                { "key": "slashHost", "value": "//evil.example.test", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("atHost", StringComparison.Ordinal) && f.Contains(DangerousPrefixMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("slashHost", StringComparison.Ordinal) && f.Contains(DangerousPrefixMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsSetTimeoutString()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "settimeout.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "setTimeout('pm.sendRequest(1)', 0);" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("setTimeout", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_T1_FailsUnlistedScriptOnHashRuleAlone()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "t1-hash-only.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "void 0;" ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        var hashFailures = failures.Where(f => f.Contains(ScriptHashMsg, StringComparison.Ordinal)).ToList();
        Assert.Single(hashFailures);
        Assert.Equal("t1-hash-only.postman_collection.json " + ScriptHashMsg, hashFailures[0]);
        Assert.DoesNotContain("eval(", hashFailures[0]);
        Assert.DoesNotContain("sendRequest", hashFailures[0]);
        Assert.DoesNotContain(".set", hashFailures[0]);
        Assert.DoesNotContain("Function(", hashFailures[0]);
        Assert.DoesNotContain("aliases pm", hashFailures[0]);
    }

    [Fact]
    public void HygieneScan_T2_FailsNumericPortAtHostAllowlist()
    {
        using var temp = new TempPostmanDir();
        WriteUrlPartsCollection(
            temp,
            "t2-port.postman_collection.json",
            extraVars: "",
            urlParts: """
            "raw": "{{baseUrl}}/health",
            "host": ["{{baseUrl}}"],
            "port": "@evil.example.test",
            "path": ["health"]
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(UrlAllowMsg, StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(UrlTokenMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_T3_FailsAnyAtInUrlAuthorityEvenWhenHostAfterAtIsLocal()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "t3-at-authority.postman_collection.json"),
            """
            {
              "info": {
                "_postman_id": "t3-at",
                "name": "t3-at",
                "description": "see http://evil.example.test@localhost"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("http(s) or // to a non-local host", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(UrlAllowMsg, StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N01_FailsAllowlistedHashOnWrongItem()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n01-wrong-item.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "copied",
                  "event": [
                    {
                      "listen": "test",
                      "script": {
                        "exec": [
                          "pm.test(\"Status code is 200\", function () {",
                          "    pm.response.to.have.status(200);",
                          "});",
                          "",
                          "if (pm.response.code !== 200) { return; }",
                          "",
                          "pm.test(\"Response has status ok\", function () {",
                          "    var jsonData = pm.response.json();",
                          "    pm.expect(jsonData.status).to.eql(\"ok\");",
                          "});"
                        ]
                      }
                    }
                  ],
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ScriptHashMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUserinfoAuthorityInDescription()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "userinfo-desc.postman_collection.json"),
            """
            {
              "info": {
                "_postman_id": "userinfo-desc",
                "name": "userinfo-desc",
                "description": "see http://localhost@evil.example.test"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("http(s) or // to a non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUserinfoAuthorityWithPortInVariable()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "userinfo-var.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "note", "value": "http://localhost:5287@evil.example.test", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("note", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsUserinfoAuthority_InNonSanctionedEnv()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "userinfo.postman_environment.json"),
            """
            {
              "name": "userinfo-env",
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": "note", "value": "http://localhost@evil.example.test", "type": "default" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("http(s) or // to a non-local host", StringComparison.Ordinal)
                                       || (f.Contains("note", StringComparison.Ordinal) && f.Contains("non-local host", StringComparison.Ordinal)));
    }

    [Fact]
    public void HygieneScan_FailsDuplicateBaseUrl_InCollectionRoot()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "dup-root.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "baseUrl", "value": "http://127.0.0.1:5287", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("duplicate baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsDuplicateVariable_InFolder()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "dup-folder.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "variable": [
                    { "key": "n", "value": "1", "type": "string" },
                    { "key": "n", "value": "2", "type": "string" }
                  ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("duplicate variable 'n'", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsDuplicateKey_InEnvironmentValues()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "dup-env.postman_environment.json"),
            """
            {
              "name": "dup-env",
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": "baseUrl", "value": "http://127.0.0.1:5287", "type": "default" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("duplicate baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_FailsLiveEnv_WhenBaseUrlNotExactlyOneEnabledPinned()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "Dealoware-api-core.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default", "enabled": true },
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default", "enabled": true }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("exactly one baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_T4_FailsLiveEnvWhenBaseUrlDisabled()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "Dealoware-api-core.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "default", "enabled": false }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("baseUrl must be enabled", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains("exactly one baseUrl", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains($"baseUrl value must be {LiveCoreBaseUrl}", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_T4_FailsLiveEnvWhenBaseUrlNotPinnedValue()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "Dealoware-api-core.postman_environment.json"),
            """
            {
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default", "enabled": true }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains($"baseUrl value must be {LiveCoreBaseUrl}", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains("baseUrl must be enabled", StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains("exactly one baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N21_FailsNonStringExecElement()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n21-exec.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": [ "void 0;", 1 ] }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("exec element is not a string", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N15_FailsHostHeaderWithTrailingSpace()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n15-host-space.postman_collection.json"),
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
                    "header": [ { "key": "Host ", "value": "evil.example.test" } ],
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(HostHeaderMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N14_FailsFormdataAndFileSrc()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n14-formdata.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "POST",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] },
                    "body": {
                      "mode": "formdata",
                      "formdata": [ { "key": "f", "type": "file", "src": "/tmp/x" } ]
                    }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains("formdata is not allowed", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("file src is not allowed", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://api.core.dealoware.com")]
    [InlineData("http://localhost:5287@api.core.dealoware.com")]
    [InlineData("http://localhost.evil.example.test")]
    [InlineData("http://127.0.0.1.evil.example.test")]
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

    [Fact]
    public void HygieneScan_K1_FailsArrayKeyAtCollectionRoot()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "k1-array-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": ["baseUrl"], "value": "api.core.dealoware.com" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_K1b_FailsIntegerIpVariableValue()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "k1b-integer-ip.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": ["baseUrl"], "value": 3405803783 }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_K1d_FailsDottedIpVariableValue()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "k1d-dotted-ip.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": ["baseUrl"], "value": "203.0.113.7" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("non-local host", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_K1f_FailsArrayKeyInLiveEnv()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "Dealoware-api-core.postman_environment.json"),
            """
            {
              "values": [
                { "key": ["baseUrl"], "value": "https://api.core.dealoware.com", "type": "default", "enabled": true }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("exactly one baseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsArrayKeyAtFolderLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-folder-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "variable": [
                    { "key": ["baseUrl"], "value": "api.core.dealoware.com" }
                  ]
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsArrayKeyAtItemLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-item-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "variable": [
                    { "key": ["baseUrl"], "value": "api.core.dealoware.com" }
                  ],
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsArrayKeyAtRequestLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-request-key.postman_collection.json"),
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
                    "variable": [
                      { "key": ["baseUrl"], "value": "api.core.dealoware.com" }
                    ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsArrayKeyAtEnvLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-env-key.postman_environment.json"),
            """
            {
              "name": "b1-env",
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" },
                { "key": ["baseUrl"], "value": "api.core.dealoware.com" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsNumericKey()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-numeric-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": 1, "value": "api.core.dealoware.com" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsMissingKey()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-missing-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "value": "api.core.dealoware.com" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_B1_FailsEmptyKey()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "b1-empty-key.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" },
                { "key": "", "value": "api.core.dealoware.com" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(VariableKeyMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N1_FP1_AllowsLocalhostHostArrayWithMatchingRaw()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "fp1-localhost-host.postman_collection.json",
            raw: "http://localhost:5287/health",
            host: """["localhost"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.DoesNotContain(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(UrlAllowMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N1_FP2_AllowsLoopbackHostArrayWithMatchingRaw()
    {
        using var temp = new TempPostmanDir();
        WriteMinimalCollection(
            temp,
            "fp2-loopback-host.postman_collection.json",
            raw: "http://127.0.0.1:5287/health",
            host: """["127.0.0.1"]""");

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.DoesNotContain(failures, f => f.Contains(HostAllowMsg, StringComparison.Ordinal));
        Assert.DoesNotContain(failures, f => f.Contains(UrlAllowMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N2_FailsScriptSrc()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n2-script-src.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "src": "https://evil.example.test/x.js" }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ScriptSrcMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(ScriptExecShapeMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N2_FailsExecNull()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n2-exec-null.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": null }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ScriptExecShapeMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N2_FailsNonArrayNonStringExec()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n2-exec-object.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": [
                {
                  "listen": "prerequest",
                  "script": { "exec": { "code": "void 0;" } }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(ScriptExecShapeMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N2_FailsEventOnRequestObject()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n2-request-event.postman_collection.json"),
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
                    "event": [
                      {
                        "listen": "prerequest",
                        "script": { "exec": [ "void 0;" ] }
                      }
                    ]
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventOnRequestMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N3_FailsEventObjectAtCollectionLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n3-event-object-collection.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": {
                "listen": "prerequest",
                "script": { "exec": [ "void 0;" ] }
              }
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventMustBeArrayMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(ScriptHashMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N3_FailsEventObjectAtFolderLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n3-event-object-folder.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "event": {
                    "listen": "prerequest",
                    "script": { "exec": [ "void 0;" ] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventMustBeArrayMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N3_FailsEventObjectAtItemLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n3-event-object-item.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "event": {
                    "listen": "prerequest",
                    "script": { "exec": [ "void 0;" ] }
                  },
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventMustBeArrayMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N3_FailsEventObjectAtRequestLevel()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n3-event-object-request.postman_collection.json"),
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
                    "event": {
                      "listen": "prerequest",
                      "script": { "exec": [ "void 0;" ] }
                    }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventMustBeArrayMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(EventOnRequestMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N3_FailsEventObjectConcatScriptOnAllowlist()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n3-event-object-concat.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "event": {
                "listen": "prerequest",
                "script": { "exec": [ "var u='http://'+'host';" ] }
              }
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(EventMustBeArrayMsg, StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(ScriptHashMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N4_FailsDuplicateJsonProperty_FirstValueBenign()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n4-dup-first-benign.postman_collection.json"),
            """
            {
              "info": { "name": "benign", "_postman_id": "n4a" },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "info": { "name": "evil", "_postman_id": "n4a-evil" }
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(DuplicateJsonPropertyMsg, StringComparison.Ordinal) && f.Contains("'info'", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_N4_FailsDuplicateJsonProperty_FirstValueEvil()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "n4-dup-first-evil.postman_collection.json"),
            """
            {
              "variable": [
                { "key": "baseUrl", "value": "https://api.core.dealoware.com", "type": "string" }
              ],
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp));
        Assert.Contains(failures, f => f.Contains(DuplicateJsonPropertyMsg, StringComparison.Ordinal) && f.Contains("'variable'", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_DocsQaN2_SkipsRootArtifactsDirectory()
    {
        using var temp = new TempPostmanDir();
        var planted = Path.Combine(temp.Root.FullName, "artifacts");
        Directory.CreateDirectory(planted);
        WriteLiveHostCollection(planted, "root-artifacts.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.DoesNotContain(files, f => f.Contains(
            $"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}root-artifacts.postman_collection.json",
            StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_DocsQaN2_SkipsRootTestResultsDirectory()
    {
        using var temp = new TempPostmanDir();
        var planted = Path.Combine(temp.Root.FullName, "test-results");
        Directory.CreateDirectory(planted);
        WriteLiveHostCollection(planted, "root-test-results.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.DoesNotContain(files, f => f.Contains(
            $"{Path.DirectorySeparatorChar}test-results{Path.DirectorySeparatorChar}root-test-results.postman_collection.json",
            StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_DocsQaN2_FindsLiveHostCollection_UnderSrcDomainArtifacts()
    {
        using var temp = new TempPostmanDir();
        var plantedDir = Path.Combine(temp.Root.FullName, "src", "Dealoware.Domain", "Artifacts");
        Directory.CreateDirectory(plantedDir);
        WriteLiveHostCollection(plantedDir, "src-domain-artifacts.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.Contains(
            $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}Dealoware.Domain{Path.DirectorySeparatorChar}Artifacts{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R51_FailsPoCPlusV1NameOrderRequests_UserinfoUrl()
    {
        using var temp = new TempPostmanDir();
        var pocPath = Path.Combine(RepoRoot(), "postman", "Dealoware-PoC-Negotiations.postman_collection.json");
        var node = JsonNode.Parse(File.ReadAllText(pocPath))!.AsObject();
        node["name"] = "Dealoware PoC — Negotiations";
        node["order"] = new JsonArray("injected");
        node["requests"] = new JsonArray(new JsonObject
        {
            ["id"] = "injected",
            ["name"] = "injected",
            ["method"] = "GET",
            ["url"] = "{{baseUrl}}@evil.example.test/health"
        });
        var outPath = Path.Combine(temp.PostmanDir, "poc-plus-v1.postman_collection.json");
        File.WriteAllText(outPath, node.ToJsonString());

        var failures = CollectCollectionFailures([outPath], scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'name'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'order'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'requests'", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R51_FailsV1OnlyCollection_ByFilename()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "v1-only.postman_collection.json"),
            """
            {
              "id": "v1-only",
              "name": "v1-only",
              "order": ["r1"],
              "requests": [
                { "id": "r1", "name": "x", "method": "GET", "url": "{{baseUrl}}@evil.example.test/health" }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'requests'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'order'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(CollectionSchemaMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R51_FailsV1OnlyCollection_DisguisedJson()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "v1-only.json"),
            """
            {
              "id": "v1-disguised",
              "name": "v1-disguised",
              "folders": [],
              "folders_order": [],
              "order": ["r1"],
              "requests": [
                { "id": "r1", "name": "x", "method": "GET", "url": "{{baseUrl}}@evil.example.test/health" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.EndsWith($"{Path.DirectorySeparatorChar}v1-only.json", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files, scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'folders'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'folders_order'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains("'requests'", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains(CollectionSchemaMsg, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("requests", """[ { "id": "r1", "name": "x", "method": "GET", "url": "{{baseUrl}}@evil.example.test/health" } ]""")]
    [InlineData("order", """["r1"]""")]
    [InlineData("folders", "[]")]
    [InlineData("folders_order", "[]")]
    [InlineData("name", "\"v1-name\"")]
    [InlineData("id", "\"v1-id\"")]
    [InlineData("timestamp", "0")]
    [InlineData("owner", "\"v1-owner\"")]
    [InlineData("remoteLink", "\"http://example.test\"")]
    [InlineData("public", "false")]
    [InlineData("synced", "false")]
    public void HygieneScan_R51_FailsEachTopLevelV1Key(string v1Key, string jsonValue)
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, $"v1-{v1Key}.postman_collection.json"),
            """
            {
              "info": {
                "name": "v2-plus-v1",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ],
              "__V1_KEY__": __V1_VALUE__
            }
            """.Replace("__V1_KEY__", v1Key, StringComparison.Ordinal)
              .Replace("__V1_VALUE__", jsonValue, StringComparison.Ordinal));

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(V1KeyMsg, StringComparison.Ordinal) && f.Contains($"'{v1Key}'", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R51_FailsMissingCollectionSchema()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "missing-schema.postman_collection.json"),
            """
            {
              "info": { "name": "no-schema", "_postman_id": "missing-schema" },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(CollectionSchemaMsg, StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R51_FailsNonV2GetpostmanSchema()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, "wrong-schema.postman_collection.json"),
            """
            {
              "info": {
                "name": "wrong-schema",
                "schema": "https://schema.getpostman.com/json/collection/v1.0.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(CollectionSchemaMsg, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("preRequestScript")]
    [InlineData("tests")]
    [InlineData("events")]
    public void HygieneScan_R51_FailsLegacyScriptKeyAtCollectionRoot(string key)
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, $"legacy-{key}.postman_collection.json"),
            """
            {
              "info": {
                "name": "legacy-root",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ],
              "__LEGACY_KEY__": []
            }
            """.Replace("__LEGACY_KEY__", key, StringComparison.Ordinal));

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(LegacyScriptKeyMsg, StringComparison.Ordinal) && f.Contains($"'{key}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("preRequestScript")]
    [InlineData("tests")]
    [InlineData("events")]
    public void HygieneScan_R51_FailsLegacyScriptKeyNestedInCollection(string key)
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, $"legacy-nested-{key}.postman_collection.json"),
            """
            {
              "info": {
                "name": "legacy-nested",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "folder",
                  "item": [
                    {
                      "name": "x",
                      "__LEGACY_KEY__": "pm.test('x', function () {});",
                      "request": {
                        "method": "GET",
                        "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                      }
                    }
                  ]
                }
              ]
            }
            """.Replace("__LEGACY_KEY__", key, StringComparison.Ordinal));

        var failures = CollectCollectionFailures(DiscoverCollections(temp), scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(LegacyScriptKeyMsg, StringComparison.Ordinal) && f.Contains($"'{key}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("preRequestScript")]
    [InlineData("tests")]
    [InlineData("events")]
    public void HygieneScan_R51_FailsLegacyScriptKeyInEnvironment(string key)
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, $"legacy-{key}.postman_environment.json"),
            $$"""
            {
              "name": "legacy-env",
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default" }
              ],
              "{{key}}": []
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(LegacyScriptKeyMsg, StringComparison.Ordinal) && f.Contains($"'{key}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("preRequestScript")]
    [InlineData("tests")]
    [InlineData("events")]
    public void HygieneScan_R51_FailsLegacyScriptKeyNestedInEnvironment(string key)
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(
            Path.Combine(temp.PostmanDir, $"legacy-nested-{key}.postman_environment.json"),
            $$"""
            {
              "name": "legacy-env-nested",
              "values": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "default", "{{key}}": "x" }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out _, out var files);
        var failures = CollectEnvironmentFailures(files, temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(LegacyScriptKeyMsg, StringComparison.Ordinal) && f.Contains($"'{key}'", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveRepoRoot_PrefersOutermostSlnWithGit_OverDecoyTestsSln()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(Path.Combine(temp.Root.FullName, "Dealoware.sln"), "");
        File.WriteAllText(Path.Combine(temp.Root.FullName, ".git"), "gitdir: /tmp/fake-gitdir");
        var testsDir = Path.Combine(temp.Root.FullName, "tests");
        Directory.CreateDirectory(Path.Combine(testsDir, "postman"));
        Directory.CreateDirectory(Path.Combine(testsDir, "scripts"));
        File.WriteAllText(Path.Combine(testsDir, "Dealoware.sln"), "");
        var start = Path.Combine(testsDir, "Dealoware.Api.Tests", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(start);

        var resolved = ResolveRepoRoot(start);
        Assert.Equal(Path.GetFullPath(temp.Root.FullName), Path.GetFullPath(resolved));
    }

    [Fact]
    public void ResolveRepoRoot_TakesOutermostSln_WhenGitIsAbsent()
    {
        using var temp = new TempPostmanDir();
        File.WriteAllText(Path.Combine(temp.Root.FullName, "Dealoware.sln"), "");
        var testsDir = Path.Combine(temp.Root.FullName, "tests");
        Directory.CreateDirectory(testsDir);
        File.WriteAllText(Path.Combine(testsDir, "Dealoware.sln"), "");
        var start = Path.Combine(testsDir, "inner");
        Directory.CreateDirectory(start);

        var resolved = ResolveRepoRoot(start);
        Assert.Equal(Path.GetFullPath(temp.Root.FullName), Path.GetFullPath(resolved));
    }

    [Fact]
    public void HygieneScan_R53_FailsBackslashInPathSegment()
    {
        using var temp = new TempPostmanDir();
        var attackName = "postman\\Dealoware-PoC-Negotiations.postman_collection.json";
        var attackPath = Path.Combine(temp.Root.FullName, attackName);
        File.WriteAllText(
            attackPath,
            """
            {
              "info": {
                "name": "backslash-identity",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "{{baseUrl}}/health", "host": ["{{baseUrl}}"] }
                  }
                }
              ]
            }
            """);

        Assert.True(RelativePathHasBackslashSegment(temp.Root.FullName, attackPath));
        var identity = ToPosixRelative(temp.Root.FullName, attackPath);
        Assert.Contains("\\", identity, StringComparison.Ordinal);
        Assert.NotEqual("postman/Dealoware-PoC-Negotiations.postman_collection.json", identity);
        var failures = CollectCollectionFailures([attackPath], scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains(BackslashPathMsg, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    public void HygieneScan_R54_FailsPostmanShapedFileUnderBinOrObj(string trap)
    {
        using var temp = new TempPostmanDir();
        var plantedDir = Path.Combine(temp.Root.FullName, "src", "Dealoware.Api", trap);
        Directory.CreateDirectory(plantedDir);
        WriteLiveHostCollection(plantedDir, "planted.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var discovered, out _);
        Assert.DoesNotContain(discovered, f => f.Contains(
            $"{Path.DirectorySeparatorChar}{trap}{Path.DirectorySeparatorChar}planted.postman_collection.json",
            StringComparison.Ordinal));

        var locationFailures = CollectForbiddenLocationFailures(temp.Root.FullName);
        Assert.Contains(locationFailures, f => f.Contains(ForbiddenLocationMsg, StringComparison.Ordinal)
            && f.Contains("planted.postman_collection.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R54_FailsPostmanShapedFileUnderRootTestResults()
    {
        using var temp = new TempPostmanDir();
        var planted = Path.Combine(temp.Root.FullName, "test-results");
        Directory.CreateDirectory(planted);
        WriteLiveHostCollection(planted, "root-test-results.postman_collection.json");

        DiscoverPostmanFiles(temp.Root.FullName, out var discovered, out _);
        Assert.DoesNotContain(discovered, f => f.Contains(
            $"{Path.DirectorySeparatorChar}test-results{Path.DirectorySeparatorChar}root-test-results.postman_collection.json",
            StringComparison.Ordinal));

        var locationFailures = CollectForbiddenLocationFailures(temp.Root.FullName);
        Assert.Contains(locationFailures, f => f.Contains(ForbiddenLocationMsg, StringComparison.Ordinal)
            && f.Contains("root-test-results.postman_collection.json", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R54_DiscoversUppercaseJsonByShape()
    {
        using var temp = new TempPostmanDir();
        var docs = Path.Combine(temp.Root.FullName, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(
            Path.Combine(docs, "hidden.JSON"),
            """
            {
              "info": {
                "name": "uppercase-json",
                "_postman_id": "uppercase-json",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
              "variable": [
                { "key": "baseUrl", "value": "http://localhost:5287", "type": "string" }
              ],
              "item": [
                {
                  "name": "x",
                  "request": {
                    "method": "GET",
                    "url": { "raw": "https://api.core.dealoware.com/health", "host": ["api.core.dealoware.com"] }
                  }
                }
              ]
            }
            """);

        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.EndsWith($"{Path.DirectorySeparatorChar}hidden.JSON", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files, scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
    }

    [Fact]
    public void HygieneScan_R54_DiscoversUppercasePostmanCollectionExtension()
    {
        using var temp = new TempPostmanDir();
        WriteLiveHostCollection(temp.PostmanDir, "planted.postman_collection.JSON");
        DiscoverPostmanFiles(temp.Root.FullName, out var files, out _);
        Assert.Contains(files, f => f.EndsWith("planted.postman_collection.JSON", StringComparison.Ordinal));
        var failures = CollectCollectionFailures(files, scanRoot: temp.Root.FullName);
        Assert.Contains(failures, f => f.Contains("request URL", StringComparison.Ordinal));
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
              "info": {
                "name": "minimal",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
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
              "info": {
                "name": "url-parts",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
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
              "info": {
                "name": "live-host-helper",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
              },
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

    private static List<string> CollectCollectionFailures(IEnumerable<string> files, string? scanRoot = null)
    {
        var failures = new List<string>();
        var seenMessages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var label = Path.GetFileName(file);
            CollectDuplicateJsonPropertyFailures(label, json, failures, seenMessages);
            using var doc = JsonDocument.Parse(json);
            var rootForPath = scanRoot ?? RepoRoot();
            if (RelativePathHasBackslashSegment(rootForPath, file))
                AddFailure(failures, seenMessages, $"{label} {BackslashPathMsg}");
            CollectV1TopLevelKeyFailures(label, doc.RootElement, failures, seenMessages);
            CollectCollectionSchemaFailures(label, doc.RootElement, failures, seenMessages);
            CollectForbiddenLegacyScriptKeyFailures(label, doc.RootElement, failures, seenMessages);
            if (!doc.RootElement.TryGetProperty("variable", out var variables)
                || variables.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} must have a variable array");
            }
            else
            {
                CollectDuplicateKeyFailures(label, variables, failures, seenMessages, sanctionedLiveBaseUrl: false);
                var seen = ReadSlots(variables);
                if (!seen.TryGetValue("baseUrl", out var baseUrl) || baseUrl.Value != LocalBaseUrl)
                    AddFailure(failures, seenMessages, $"{label} baseUrl must be {LocalBaseUrl}");

                CollectSecretFailures(label, seen, failures, seenMessages);
                CollectNonLocalHostSlotFailures(label, seen, failures, seenMessages, sanctionedLiveBaseUrl: false);
            }

            CollectAllVariableSlotFailures(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: false, skipRootVariable: true);
            CollectRequestShapeFailures(label, doc.RootElement, failures, seenMessages);
            CollectAuthAndHeaderFailures(label, doc.RootElement, failures, seenMessages);
            CollectProxyFailures(label, doc.RootElement, failures, seenMessages);
            CollectFormDataAndFileSrcFailures(label, doc.RootElement, failures, seenMessages);
            var identity = ToPosixRelative(scanRoot ?? RepoRoot(), file);
            CollectEventScriptFailures(identity, label, doc.RootElement, failures, seenMessages);
            WalkEveryString(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: false);
        }

        return failures;
    }

    private static List<string> CollectEnvironmentFailures(IEnumerable<string> files, string scanRoot)
    {
        var failures = new List<string>();
        var seenMessages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var label = file;
            CollectDuplicateJsonPropertyFailures(label, json, failures, seenMessages);
            using var doc = JsonDocument.Parse(json);
            if (RelativePathHasBackslashSegment(scanRoot, file))
                AddFailure(failures, seenMessages, $"{label} {BackslashPathMsg}");
            CollectForbiddenLegacyScriptKeyFailures(label, doc.RootElement, failures, seenMessages);
            var sanctioned = IsSanctionedLiveEnv(file, scanRoot);
            if (!doc.RootElement.TryGetProperty("values", out var values)
                || values.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} must have a values array");
            }
            else
            {
                CollectDuplicateKeyFailures(label, values, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned);
                if (sanctioned)
                    CollectLiveEnvBaseUrlConstraints(label, values, failures, seenMessages);
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
            CollectAllVariableSlotFailures(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned, skipRootVariable: false);
            CollectRequestShapeFailures(label, doc.RootElement, failures, seenMessages);
            CollectAuthAndHeaderFailures(label, doc.RootElement, failures, seenMessages);
            CollectProxyFailures(label, doc.RootElement, failures, seenMessages);
            CollectFormDataAndFileSrcFailures(label, doc.RootElement, failures, seenMessages);
            CollectEventScriptFailures(ToPosixRelative(scanRoot, file), label, doc.RootElement, failures, seenMessages);
            WalkEveryString(label, doc.RootElement, failures, seenMessages, sanctionedLiveBaseUrl: sanctioned);
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
            if (slot.HasValue && HasDangerousValuePrefix(slot.Value))
                AddFailure(failures, seenMessages, $"{label} {key} {DangerousPrefixMsg}");
            if (slot.HasCurrentValue && HasDangerousValuePrefix(slot.CurrentValue))
                AddFailure(failures, seenMessages, $"{label} {key} currentValue {DangerousPrefixMsg}");
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
        if (HasNonLocalHttpOrProtocolRelativeUrl(value))
            return true;
        var trimmed = value.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal))
            return false;
        if (AllowedLiteralOrigin.IsMatch(trimmed) || AllowedBaseUrlTemplate.IsMatch(trimmed))
            return false;
        if (HasNonLoopbackBareIp(trimmed))
            return true;
        if (IsNonLoopbackIntegerIpv4(trimmed))
            return true;
        return NoSchemeHost.IsMatch(trimmed);
    }

    private static bool IsNonLoopbackIntegerIpv4(string text)
    {
        if (text.Length is < 8 or > 10)
            return false;
        foreach (var c in text)
        {
            if (c is < '0' or > '9')
                return false;
        }

        if (!ulong.TryParse(text, out var n) || n > 0xFFFFFFFFUL)
            return false;
        return n != 2130706433UL;
    }

    private static bool HasDangerousValuePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var trimmed = value.Trim();
        return trimmed.StartsWith('@') || trimmed.StartsWith("//", StringComparison.Ordinal);
    }

    private static bool HasNonLoopbackBareIp(string text)
    {
        foreach (Match match in BareIp.Matches(text))
        {
            if (match.Groups["ip"].Value != "127.0.0.1")
                return true;
        }

        return false;
    }

    private static bool HasNonLocalHttpOrProtocolRelativeUrl(string text)
    {
        foreach (Match match in HttpOrProtocolRelativeUrl.Matches(text))
        {
            var authority = match.Groups["authority"].Value.TrimEnd('.', ',', ')', ']', '`', ';', '\'', '"');
            var at = authority.LastIndexOf('@');
            if (at >= 0)
                return true;
            var host = HostFromAuthority(authority);
            if (IsLocalHttpHost(host))
                continue;
            return true;
        }

        return false;
    }

    private static string HostFromAuthority(string authority)
    {
        var hostPort = authority;
        var at = authority.LastIndexOf('@');
        if (at >= 0)
            hostPort = authority[(at + 1)..];
        var colon = hostPort.LastIndexOf(':');
        if (colon >= 0 && int.TryParse(hostPort[(colon + 1)..], out _))
            return hostPort[..colon];
        return hostPort;
    }

    private static bool IsLocalHttpHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || host.Equals("127.0.0.1", StringComparison.Ordinal);
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
                    CollectDuplicateKeyFailures(label, prop.Value, failures, seenMessages, sanctionedLiveBaseUrl);
                    var slots = ReadSlots(prop.Value);
                    CollectSecretFailures(label, slots, failures, seenMessages);
                    CollectNonLocalHostSlotFailures(label, slots, failures, seenMessages, sanctionedLiveBaseUrl);
                }
            }

            CollectAllVariableSlotFailures(label, prop.Value, failures, seenMessages, sanctionedLiveBaseUrl, skipRootVariable: false, childPath);
        }
    }

    private static void CollectRequestShapeFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectRequestShapeFailures(label, child, failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        if (el.TryGetProperty("request", out var request))
            InspectRequest(label, request, failures, seenMessages);
        if (el.TryGetProperty("item", out var item))
            CollectRequestShapeFailures(label, item, failures, seenMessages);
    }

    private static void InspectRequest(
        string label,
        JsonElement request,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (request.ValueKind == JsonValueKind.String)
        {
            CheckRequestUrlString(label, request.GetString(), failures, seenMessages);
            return;
        }

        if (request.ValueKind != JsonValueKind.Object)
        {
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
            return;
        }

        if (!request.TryGetProperty("url", out var url))
            return;
        CollectRequestUrlFailures(label, url, failures, seenMessages);
    }

    private static void CollectRequestUrlFailures(
        string label,
        JsonElement url,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (url.ValueKind == JsonValueKind.String)
        {
            CheckRequestUrlString(label, url.GetString(), failures, seenMessages);
            return;
        }

        if (url.ValueKind != JsonValueKind.Object)
        {
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
            return;
        }

        if (url.TryGetProperty("auth", out _))
            AddFailure(failures, seenMessages, $"{label} {UrlAuthMsg}");

        if (url.TryGetProperty("protocol", out var protocolEl))
        {
            var protocol = ElementAsString(protocolEl) ?? "";
            if (HasAnyTemplateToken(protocol))
                AddFailure(failures, seenMessages, $"{label} {UrlTokenMsg}");
            else if (!string.Equals(protocol, "http", StringComparison.Ordinal))
                AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
        }

        if (url.TryGetProperty("port", out var portEl))
        {
            var port = ElementAsString(portEl) ?? "";
            if (HasAnyTemplateToken(port))
                AddFailure(failures, seenMessages, $"{label} {UrlTokenMsg}");
            else if (!NumericPort.IsMatch(port))
                AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
        }

        string? rawText = null;
        if (url.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.String)
            rawText = raw.GetString();

        if (url.TryGetProperty("host", out var host))
        {
            if (host.ValueKind != JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} {HostAllowMsg}");
            }
            else
            {
                var parts = host.EnumerateArray().Select(e => ElementAsString(e) ?? "").ToArray();
                if (!IsAllowedHostArray(parts, rawText))
                    AddFailure(failures, seenMessages, $"{label} {HostAllowMsg}");
            }
        }

        if (url.TryGetProperty("path", out var pathEl))
            CollectPathOrQueryTokenFailures(label, pathEl, failures, seenMessages);
        if (url.TryGetProperty("query", out var queryEl))
            CollectPathOrQueryTokenFailures(label, queryEl, failures, seenMessages);

        if (rawText is null)
        {
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
        }
        else
        {
            CheckRequestUrlString(label, rawText, failures, seenMessages);
        }

        // Rebuild protocol/host/port/path/query only. Auth is rejected above and is not part of the rebuild.
        var rebuilt = RebuildUrl(url);
        if (!IsAllowedRequestUrl(rebuilt))
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
    }

    private static void CheckRequestUrlString(
        string label,
        string? raw,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (!IsAllowedRequestUrl(raw))
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
        if (raw is not null && RawHasDisallowedTokens(raw))
            AddFailure(failures, seenMessages, $"{label} {UrlTokenMsg}");
    }

    private static bool RawHasDisallowedTokens(string raw)
    {
        foreach (Match match in TemplateToken.Matches(raw))
        {
            var name = match.Groups[1].Value;
            if (name == "baseUrl")
            {
                if (match.Index != 0)
                    return true;
                continue;
            }

            if (!AllowedUrlPathTokens.Contains(name))
                return true;
        }

        return false;
    }

    private static bool HasAnyTemplateToken(string text) => TemplateToken.IsMatch(text);

    private static void CollectPathOrQueryTokenFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            CheckPathOrQueryPart(label, el.GetString() ?? "", failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
            return;
        }

        foreach (var child in el.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.String)
            {
                CheckPathOrQueryPart(label, child.GetString() ?? "", failures, seenMessages);
            }
            else if (child.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "key", "value" })
                {
                    if (!child.TryGetProperty(name, out var part))
                        continue;
                    CheckPathOrQueryPart(label, ElementAsString(part) ?? "", failures, seenMessages);
                }
            }
            else
            {
                AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
            }
        }
    }

    private static void CheckPathOrQueryPart(
        string label,
        string text,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (text.Contains('@'))
            AddFailure(failures, seenMessages, $"{label} {UrlAllowMsg}");
        foreach (Match match in TemplateToken.Matches(text))
        {
            var name = match.Groups[1].Value;
            if (!AllowedUrlPathTokens.Contains(name))
                AddFailure(failures, seenMessages, $"{label} {UrlTokenMsg}");
        }
    }

    private static string RebuildUrl(JsonElement url)
    {
        string HostJoined()
        {
            if (!url.TryGetProperty("host", out var host) || host.ValueKind != JsonValueKind.Array)
                return "";
            return string.Join(".", host.EnumerateArray().Select(e => ElementAsString(e) ?? ""));
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
            protocol = ElementAsString(protocolEl) ?? "http";
        var rebuilt = protocol + "://" + host;
        if (url.TryGetProperty("port", out var portEl))
            rebuilt += ":" + (ElementAsString(portEl) ?? "");
        return rebuilt + path + query;
    }

    private static bool IsAllowedRequestUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Trim();
        return AllowedBaseUrlTemplate.IsMatch(trimmed) || AllowedLiteralOrigin.IsMatch(trimmed);
    }

    private static bool IsAllowedHostArray(string[] parts, string? raw)
    {
        if (parts.Length != 1)
            return false;
        if (parts[0] == "{{baseUrl}}")
            return true;
        if (raw is null)
            return false;
        var trimmed = raw.Trim();
        if (!AllowedLiteralOrigin.IsMatch(trimmed))
            return false;
        if (parts[0] == "localhost")
            return trimmed.StartsWith("http://localhost", StringComparison.Ordinal);
        if (parts[0] == "127.0.0.1")
            return trimmed.StartsWith("http://127.0.0.1", StringComparison.Ordinal);
        return false;
    }

    private static void WalkEveryString(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl,
        string path = "")
    {
        if (path == "info.schema")
            return;

        if (el.ValueKind == JsonValueKind.String)
        {
            CheckEveryString(label, el.GetString() ?? "", path, failures, seenMessages);
            return;
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                WalkEveryString(label, child, failures, seenMessages, sanctionedLiveBaseUrl, path);
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
            WalkEveryString(label, prop.Value, failures, seenMessages, sanctionedLiveBaseUrl, childPath);
        }
    }

    private static void CheckEveryString(
        string label,
        string text,
        string path,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (HasNonLocalHttpOrProtocolRelativeUrl(text))
            AddFailure(failures, seenMessages, $"{label} contains http(s) or // to a non-local host at {path}");
    }

    private static void CheckScript(
        string label,
        string fileName,
        string item,
        string listen,
        string text,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        var hash = HashScript(text);
        if (ScriptAllowlistByIdentity.TryGetValue((fileName, item, listen), out var expected)
            && expected == hash)
        {
            return;
        }

        var details = SecondLayerDetails(NormalizeScriptForTokens(text));
        var msg = $"{label} {ScriptHashMsg}";
        if (details.Count > 0)
            msg += " (" + string.Join("; ", details) + ")";
        AddFailure(failures, seenMessages, msg);
    }

    private static List<string> SecondLayerDetails(string normalized)
    {
        var details = new List<string>();
        if (string.IsNullOrEmpty(normalized))
            return details;
        if (PmAlias.IsMatch(normalized))
            details.Add(ScriptAliasMsg);
        if (normalized.Contains("eval(", StringComparison.Ordinal))
            details.Add("collection script contains eval(");
        if (normalized.Contains("Function(", StringComparison.Ordinal))
            details.Add("collection script contains Function(");
        if (normalized.Contains("atob(", StringComparison.Ordinal))
            details.Add("collection script contains atob(");
        if (normalized.Contains("require(", StringComparison.Ordinal))
            details.Add("collection script contains require(");
        if (normalized.Contains("setTimeout", StringComparison.Ordinal))
            details.Add("collection script contains setTimeout");
        if (PmBracketAccess.IsMatch(normalized))
            details.Add("collection script contains pm[ or pm.*[");
        if (normalized.Contains("sendRequest", StringComparison.Ordinal))
            details.Add("collection script contains sendRequest");
        if (normalized.Contains("request.url", StringComparison.Ordinal)
            || normalized.Contains("request['url']", StringComparison.Ordinal)
            || normalized.Contains("request[\"url\"]", StringComparison.Ordinal))
            details.Add("collection script contains pm.request.url");
        if (normalized.Contains(".set(", StringComparison.Ordinal)
            || normalized.Contains("['set']", StringComparison.Ordinal)
            || normalized.Contains("[\"set\"]", StringComparison.Ordinal))
        {
            details.Add("collection script contains .set");
            details.Add("collection script sets baseUrl");
        }

        return details;
    }

    private static string HashScript(string text)
    {
        var normalized = text.TrimEnd();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    private static string NormalizeScriptForTokens(string text)
        => Regex.Replace(StripJsComments(text), @"\s+", "");

    private static string StripJsComments(string text)
    {
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is '"' or '\'' or '`')
            {
                var quote = c;
                sb.Append(c);
                i++;
                while (i < text.Length)
                {
                    var ch = text[i];
                    sb.Append(ch);
                    if (ch == '\\' && i + 1 < text.Length)
                    {
                        sb.Append(text[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (ch == quote)
                    {
                        i++;
                        break;
                    }

                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < text.Length)
            {
                if (text[i + 1] == '/')
                {
                    i += 2;
                    while (i < text.Length && text[i] != '\n')
                        i++;
                    continue;
                }

                if (text[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                        i++;
                    i = Math.Min(text.Length, i + 2);
                    continue;
                }
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
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
                    var headerName = key?.Trim();
                    if (string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase))
                        AddFailure(failures, seenMessages, $"{label} {HostHeaderMsg}");
                    if (IsCredentialHeader(headerName) && !IsTemplateOrEmpty(value))
                        AddFailure(failures, seenMessages, $"{label} literal secret in header '{headerName}'");
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

    private static void CollectProxyFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectProxyFailures(label, child, failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "proxy"
                && prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.String or JsonValueKind.Array)
            {
                AddFailure(failures, seenMessages, $"{label} {ProxyMsg}");
            }

            CollectProxyFailures(label, prop.Value, failures, seenMessages);
        }
    }

    private static List<ScriptAllowlistEntry> CollectCommittedScriptEntries(IEnumerable<string> files)
    {
        var found = new List<ScriptAllowlistEntry>();
        var root = RepoRoot();
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            CollectScriptEntries(ToPosixRelative(root, file), doc.RootElement, [], found, label: null, failures: null, seenMessages: null);
        }

        return found;
    }

    private static void CollectEventScriptFailures(
        string fileIdentity,
        string label,
        JsonElement root,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        CollectScriptEntries(fileIdentity, root, [], [], label, failures, seenMessages);
    }

    private static void CollectScriptEntries(
        string fileIdentity,
        JsonElement el,
        List<string> path,
        List<ScriptAllowlistEntry> found,
        string? label,
        List<string>? failures,
        HashSet<string>? seenMessages,
        bool insideRequest = false)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectScriptEntries(fileIdentity, child, path, found, label, failures, seenMessages, insideRequest);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        var localPath = path;
        if (el.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
        {
            var name = nameEl.GetString();
            if (!string.IsNullOrEmpty(name))
                localPath = [.. path, name];
        }

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "event")
            {
                InspectEventProperty(
                    fileIdentity,
                    prop.Value,
                    localPath,
                    found,
                    label,
                    failures,
                    seenMessages,
                    onRequest: insideRequest);
            }

            var childInsideRequest = insideRequest || prop.Name == "request";
            CollectScriptEntries(
                fileIdentity,
                prop.Value,
                localPath,
                found,
                label,
                failures,
                seenMessages,
                childInsideRequest);
        }
    }

    private static void InspectEventProperty(
        string fileIdentity,
        JsonElement events,
        List<string> localPath,
        List<ScriptAllowlistEntry> found,
        string? label,
        List<string>? failures,
        HashSet<string>? seenMessages,
        bool onRequest)
    {
        void Fail(string msg)
        {
            if (failures is not null && seenMessages is not null && label is not null)
                AddFailure(failures, seenMessages, $"{label} {msg}");
        }

        if (onRequest)
            Fail(EventOnRequestMsg);

        if (events.ValueKind == JsonValueKind.Object)
        {
            Fail(EventMustBeArrayMsg);
            InspectOneEvent(fileIdentity, events, localPath, found, label, failures, seenMessages);
            return;
        }

        if (events.ValueKind != JsonValueKind.Array)
        {
            Fail(EventMustBeArrayMsg);
            return;
        }

        foreach (var ev in events.EnumerateArray())
        {
            if (ev.ValueKind != JsonValueKind.Object)
            {
                Fail(EventEntryObjectMsg);
                continue;
            }

            InspectOneEvent(fileIdentity, ev, localPath, found, label, failures, seenMessages);
        }
    }

    private static void InspectOneEvent(
        string fileIdentity,
        JsonElement ev,
        List<string> localPath,
        List<ScriptAllowlistEntry> found,
        string? label,
        List<string>? failures,
        HashSet<string>? seenMessages)
    {
        void Fail(string msg)
        {
            if (failures is not null && seenMessages is not null && label is not null)
                AddFailure(failures, seenMessages, $"{label} {msg}");
        }

        var listen = ev.TryGetProperty("listen", out var listenEl) && listenEl.ValueKind == JsonValueKind.String
            ? listenEl.GetString() ?? "?"
            : "?";
        if (!ev.TryGetProperty("script", out var script))
            return;

        if (script.ValueKind == JsonValueKind.Object
            && script.TryGetProperty("src", out var src)
            && src.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            Fail(ScriptSrcMsg);
        }

        var item = localPath.Count == 0 ? "(collection)" : string.Join(" / ", localPath);
        if (!TryGetScriptExec(script, out var joined, out var nonStringExec, out var execShapeBad))
        {
            if (execShapeBad)
                Fail(ScriptExecShapeMsg);
            return;
        }

        if (nonStringExec)
        {
            Fail("collection script exec element is not a string");
            return;
        }

        found.Add(new ScriptAllowlistEntry(fileIdentity, item, listen, HashScript(joined)));
        if (failures is not null && seenMessages is not null && label is not null)
            CheckScript(label, fileIdentity, item, listen, joined, failures, seenMessages);
    }

    private static bool TryGetScriptExec(
        JsonElement script,
        out string text,
        out bool nonStringExec,
        out bool execShapeBad)
    {
        text = "";
        nonStringExec = false;
        execShapeBad = false;
        JsonElement exec = default;
        var hasExec = false;
        if (script.ValueKind == JsonValueKind.String)
        {
            text = script.GetString() ?? "";
            return true;
        }

        if (script.ValueKind == JsonValueKind.Array)
        {
            exec = script;
            hasExec = true;
        }
        else if (script.ValueKind == JsonValueKind.Object
                 && script.TryGetProperty("exec", out exec))
        {
            hasExec = true;
        }

        if (!hasExec)
        {
            execShapeBad = true;
            return false;
        }

        if (exec.ValueKind == JsonValueKind.String)
        {
            text = exec.GetString() ?? "";
            return true;
        }

        if (exec.ValueKind != JsonValueKind.Array)
        {
            execShapeBad = true;
            return false;
        }

        var lines = new List<string>();
        foreach (var e in exec.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.String)
            {
                nonStringExec = true;
                continue;
            }

            lines.Add(e.GetString() ?? "");
        }

        text = string.Join("\n", lines);
        return true;
    }

    private static void CollectV1TopLevelKeyFailures(
        string label,
        JsonElement root,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return;
        foreach (var prop in root.EnumerateObject())
        {
            if (ForbiddenV1CollectionKeys.Contains(prop.Name))
                AddFailure(failures, seenMessages, $"{label} {V1KeyMsg} '{prop.Name}'");
        }
    }

    private static void CollectCollectionSchemaFailures(
        string label,
        JsonElement root,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (!root.TryGetProperty("info", out var info) || info.ValueKind != JsonValueKind.Object)
        {
            AddFailure(failures, seenMessages, $"{label} {CollectionSchemaMsg}");
            return;
        }

        if (!info.TryGetProperty("schema", out var schema)
            || schema.ValueKind != JsonValueKind.String
            || !AllowedCollectionSchema.IsMatch(schema.GetString() ?? ""))
        {
            AddFailure(failures, seenMessages, $"{label} {CollectionSchemaMsg}");
        }
    }

    private static void CollectForbiddenLegacyScriptKeyFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectForbiddenLegacyScriptKeyFailures(label, child, failures, seenMessages);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in el.EnumerateObject())
        {
            if (ForbiddenLegacyScriptKeys.Contains(prop.Name))
                AddFailure(failures, seenMessages, $"{label} {LegacyScriptKeyMsg} '{prop.Name}'");
            CollectForbiddenLegacyScriptKeyFailures(label, prop.Value, failures, seenMessages);
        }
    }

    // R5-4: discovery still skips bin/obj (any depth) and root test-results so build
    // output is not URL-scanned. Those dirs are trap-scanned instead: a Postman-shaped
    // file there fails closed, tracked or not. Shape matching uses .json ignore-case.
    internal static List<string> CollectForbiddenLocationFailures(string root)
    {
        var failures = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var scanRoot = Path.GetFullPath(root);

        void Consider(string file)
        {
            if (!LooksLikePostmanShapedFile(file))
                return;
            var rel = ToPosixRelative(scanRoot, file);
            var msg = $"{rel} {ForbiddenLocationMsg}";
            if (seen.Add(msg))
                failures.Add(msg);
        }

        void ScanTrap(string dir)
        {
            foreach (var file in Directory.GetFiles(dir))
                Consider(file);
            foreach (var sub in Directory.GetDirectories(dir))
                ScanTrap(sub);
        }

        var stack = new Stack<string>();
        stack.Push(scanRoot);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var sub in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(sub);
                if (name is "node_modules" or ".git" or ".vs")
                    continue;
                if (name is "bin" or "obj")
                {
                    ScanTrap(sub);
                    continue;
                }

                var parent = Path.GetDirectoryName(Path.GetFullPath(sub));
                if (name == "test-results"
                    && parent is not null
                    && string.Equals(Path.GetFullPath(parent), scanRoot, StringComparison.Ordinal))
                {
                    ScanTrap(sub);
                    continue;
                }

                stack.Push(sub);
            }
        }

        return failures;
    }

    private static bool LooksLikePostmanShapedFile(string file)
    {
        if (file.EndsWith(".postman_collection.json", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".postman_environment.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return false;
        return TryClassifyPostmanJson(file, out _, out _);
    }

    private static void CollectDuplicateJsonPropertyFailures(
        string label,
        string json,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            var stack = new Stack<HashSet<string>>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        stack.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.EndObject:
                        if (stack.Count > 0)
                            stack.Pop();
                        break;
                    case JsonTokenType.PropertyName:
                        if (stack.Count == 0)
                            break;
                        var name = reader.GetString() ?? "";
                        if (!stack.Peek().Add(name))
                            AddFailure(failures, seenMessages, $"{label} {DuplicateJsonPropertyMsg} '{name}'");
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // invalid JSON is not scanned as a Postman file
        }
    }

    private static void CollectDuplicateKeyFailures(
        string label,
        JsonElement variables,
        List<string> failures,
        HashSet<string> seenMessages,
        bool sanctionedLiveBaseUrl = false)
    {
        _ = sanctionedLiveBaseUrl;
        if (variables.ValueKind != JsonValueKind.Array)
            return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.ValueKind != JsonValueKind.Object)
            {
                AddFailure(failures, seenMessages, $"{label} variable entry must be an object");
                continue;
            }

            if (!TryGetNonEmptyStringKey(variable, out var key))
            {
                AddFailure(failures, seenMessages, $"{label} {VariableKeyMsg}");
                CollectInvalidKeyHostFailures(label, variable, failures, seenMessages);
                continue;
            }

            if (!seen.Add(key))
            {
                if (key == "baseUrl")
                    AddFailure(failures, seenMessages, $"{label} duplicate baseUrl");
                else
                    AddFailure(failures, seenMessages, $"{label} duplicate variable '{key}'");
            }
        }
    }

    private static bool TryGetNonEmptyStringKey(JsonElement variable, out string key)
    {
        key = "";
        if (!variable.TryGetProperty("key", out var keyEl) || keyEl.ValueKind != JsonValueKind.String)
            return false;
        var value = keyEl.GetString();
        if (string.IsNullOrEmpty(value))
            return false;
        key = value;
        return true;
    }

    private static void CollectInvalidKeyHostFailures(
        string label,
        JsonElement variable,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        const string displayKey = "(invalid-key)";
        if (variable.TryGetProperty("value", out var valueEl))
        {
            var value = ElementAsString(valueEl);
            if (HasDangerousValuePrefix(value))
                AddFailure(failures, seenMessages, $"{label} {displayKey} {DangerousPrefixMsg}");
            if (HoldsNonLocalHost(value))
                AddFailure(failures, seenMessages, $"{label} {displayKey} value holds a non-local host");
        }

        if (variable.TryGetProperty("currentValue", out var currentEl))
        {
            var current = ElementAsString(currentEl);
            if (HasDangerousValuePrefix(current))
                AddFailure(failures, seenMessages, $"{label} {displayKey} currentValue {DangerousPrefixMsg}");
            if (HoldsNonLocalHost(current))
                AddFailure(failures, seenMessages, $"{label} {displayKey} currentValue holds a non-local host");
        }
    }

    private static void CollectLiveEnvBaseUrlConstraints(
        string label,
        JsonElement values,
        List<string> failures,
        HashSet<string> seenMessages)
    {
        var count = 0;
        JsonElement? entry = null;
        foreach (var variable in values.EnumerateArray())
        {
            if (variable.ValueKind != JsonValueKind.Object)
            {
                AddFailure(failures, seenMessages, $"{label} variable entry must be an object");
                continue;
            }

            if (!TryGetNonEmptyStringKey(variable, out var key))
            {
                AddFailure(failures, seenMessages, $"{label} {VariableKeyMsg}");
                CollectInvalidKeyHostFailures(label, variable, failures, seenMessages);
                continue;
            }

            if (key != "baseUrl")
                continue;
            count++;
            entry = variable;
        }

        if (count != 1)
        {
            AddFailure(failures, seenMessages, $"{label} must have exactly one baseUrl");
            return;
        }

        var slot = entry!.Value;
        if (!slot.TryGetProperty("enabled", out var enabledEl) || enabledEl.ValueKind != JsonValueKind.True)
            AddFailure(failures, seenMessages, $"{label} baseUrl must be enabled");
        var value = slot.TryGetProperty("value", out var valueEl) ? ElementAsString(valueEl) : null;
        if (value != LiveCoreBaseUrl)
            AddFailure(failures, seenMessages, $"{label} baseUrl value must be {LiveCoreBaseUrl}");
    }

    private static void CollectFormDataAndFileSrcFailures(
        string label,
        JsonElement el,
        List<string> failures,
        HashSet<string> seenMessages,
        string path = "")
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                CollectFormDataAndFileSrcFailures(label, child, failures, seenMessages, path);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in el.EnumerateObject())
        {
            var childPath = path.Length == 0 ? prop.Name : path + "." + prop.Name;
            if (prop.Name == "formdata")
                AddFailure(failures, seenMessages, $"{label} formdata is not allowed");
            if (prop.Name == "mode" && prop.Value.ValueKind == JsonValueKind.String)
            {
                var mode = prop.Value.GetString();
                if (string.Equals(mode, "formdata", StringComparison.OrdinalIgnoreCase))
                    AddFailure(failures, seenMessages, $"{label} formdata is not allowed");
                if (string.Equals(mode, "file", StringComparison.OrdinalIgnoreCase))
                    AddFailure(failures, seenMessages, $"{label} file src is not allowed");
            }

            if (prop.Name == "src"
                && (path.Contains(".file", StringComparison.Ordinal)
                    || path.Contains(".formdata", StringComparison.Ordinal)
                    || path.EndsWith("file", StringComparison.Ordinal)
                    || path.EndsWith("formdata", StringComparison.Ordinal)))
            {
                AddFailure(failures, seenMessages, $"{label} file src is not allowed");
            }

            CollectFormDataAndFileSrcFailures(label, prop.Value, failures, seenMessages, childPath);
        }
    }

    private readonly record struct ScriptAllowlistEntry(string File, string Item, string Listen, string Sha256);

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

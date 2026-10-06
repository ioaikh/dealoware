using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Domain.Admin;

/// <summary>
/// FieldPolicy-only audit snapshots. Secrets are never serialized.
/// Bodies over 4 KiB are truncated with original length and SHA-256.
/// Forbidden-key stripping also runs on the write path (Truncate / recorder).
/// </summary>
public static class AdminAuditSnapshots
{
    public const int MaxSnapshotChars = 4096;
    public const int TruncateKeepChars = 4000;

    /// <summary>
    /// Stored when the input is not valid JSON or truncation would break JSON.
    /// Raw caller input is never persisted in that case.
    /// </summary>
    public const string FailClosedPlaceholder = """{"_invalid":true}""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Normalized fragments. A key is stripped when its normalized form
    /// contains any of these, or equals <c>code</c> / <c>key</c> exactly.
    /// </summary>
    private static readonly string[] ForbiddenFragments =
    [
        "password",
        "passwd",
        "pwd",
        "secret",
        "token",
        "jwt",
        "bearer",
        "authorization",
        "cookie",
        "session",
        "apikey",
        "hmac",
        "totp",
        "otp",
        "recovery",
        "salt",
        "hash",
        "turnstile",
        "credential",
        "privatekey",
        "signature"
    ];

    /// <summary>
    /// Serializes FieldPolicy-readable fields only. Forbidden secret keys are dropped.
    /// </summary>
    public static string FromAllowedFields(
        IFieldPolicy policy,
        FieldPrincipal principal,
        IReadOnlyDictionary<FieldClass, object?> values,
        FieldResourceContext? resourceContext = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(values);

        var context = resourceContext ?? new FieldResourceContext();
        var allowed = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (field, value) in values)
        {
            if (IsForbiddenKey(field.Name))
                continue;
            if (!policy.Evaluate(principal, field, FieldAction.Read, context))
                continue;
            allowed[field.Name] = value;
        }

        return Truncate(JsonSerializer.Serialize(allowed, JsonOptions));
    }

    /// <summary>
    /// Serializes a caller-supplied dictionary after stripping secret keys.
    /// Use when FieldPolicy has already selected the fields.
    /// </summary>
    public static string FromAllowedDictionary(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var allowed = values
            .Where(kv => !IsForbiddenKey(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        return Truncate(JsonSerializer.Serialize(allowed, JsonOptions));
    }

    /// <summary>
    /// Write-path sanitize: strip forbidden keys at any depth, then truncate.
    /// Invalid JSON and truncation that breaks JSON store a fixed placeholder.
    /// </summary>
    public static string Truncate(string? snapshot)
    {
        if (snapshot is null)
            return string.Empty;

        var stripped = StripForbiddenKeys(snapshot) ?? FailClosedPlaceholder;
        if (stripped.Length <= MaxSnapshotChars)
            return stripped;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stripped)));
        var keep = Math.Min(TruncateKeepChars, stripped.Length);
        var candidate = stripped[..keep]
                        + $"... [TRUNCATED, originalLength={stripped.Length}, sha256={hash}]";
        if (IsValidJson(candidate))
            return candidate;

        return $"{{\"_invalid\":true,\"originalLength\":{stripped.Length},\"sha256\":\"{hash}\"}}";
    }

    /// <summary>
    /// Recursively drops forbidden keys (normalized substring match) from JSON
    /// objects and arrays, including JSON embedded in string values.
    /// Non-JSON input is replaced with <see cref="FailClosedPlaceholder"/>.
    /// </summary>
    public static string? StripForbiddenKeys(string? snapshot)
    {
        if (snapshot is null)
            return null;

        try
        {
            var node = JsonNode.Parse(snapshot);
            var cleaned = SanitizeNode(node);
            return cleaned?.ToJsonString() ?? FailClosedPlaceholder;
        }
        catch (JsonException)
        {
            return FailClosedPlaceholder;
        }
    }

    public static bool IsForbiddenKey(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var normalized = NormalizeKey(name);
        if (normalized.Length == 0)
            return true;
        if (normalized is "code" or "key")
            return true;

        foreach (var fragment in ForbiddenFragments)
        {
            if (normalized.Contains(fragment, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal static string NormalizeKey(string key)
    {
        var trimmed = key.Trim();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (ch is '_' or '-' or '.' || char.IsWhiteSpace(ch))
                continue;
            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static JsonNode? SanitizeNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (IsForbiddenKey(key))
                    {
                        obj.Remove(key);
                        continue;
                    }

                    var cleaned = SanitizeNode(obj[key]);
                    if (!ReferenceEquals(cleaned, obj[key]))
                        obj[key] = cleaned;
                }

                return obj;

            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    var cleaned = SanitizeNode(array[i]);
                    if (!ReferenceEquals(cleaned, array[i]))
                        array[i] = cleaned;
                }

                return array;

            case JsonValue value when value.TryGetValue<string>(out var text):
                return SanitizeStringValue(text);

            default:
                return node;
        }
    }

    private static JsonNode? SanitizeStringValue(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
            return JsonValue.Create(text);

        try
        {
            var embedded = JsonNode.Parse(trimmed);
            return SanitizeNode(embedded);
        }
        catch (JsonException)
        {
            return JsonValue.Create("[redacted]");
        }
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            JsonNode.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

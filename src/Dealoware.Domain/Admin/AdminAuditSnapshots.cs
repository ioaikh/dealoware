using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Property names that must never be stored on an audit row. Match is
    /// case-insensitive and applies at any JSON depth, including arrays.
    /// </summary>
    public static readonly IReadOnlySet<string> ForbiddenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newPassword",
        "currentPassword",
        "passwordHash",
        "token",
        "accessToken",
        "refreshToken",
        "resetToken",
        "bootstrapToken",
        "pendingToken",
        "secret",
        "totpSecret",
        "totp",
        "code",
        "recoveryCode",
        "recoveryCodes",
        "otp",
        "apiKey",
        "key",
        "hmacKey",
        "authorization",
        "cookie",
        "setCookie",
        "session"
    };

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
            if (ForbiddenKeys.Contains(field.Name))
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
            .Where(kv => !ForbiddenKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        return Truncate(JsonSerializer.Serialize(allowed, JsonOptions));
    }

    /// <summary>
    /// Write-path sanitize: strip forbidden keys at any depth, then truncate.
    /// </summary>
    public static string Truncate(string? snapshot)
    {
        if (snapshot is null)
            return string.Empty;

        var stripped = StripForbiddenKeys(snapshot);
        if (stripped.Length <= MaxSnapshotChars)
            return stripped;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stripped)));
        var keep = Math.Min(TruncateKeepChars, stripped.Length);
        return stripped[..keep]
               + $"... [TRUNCATED, originalLength={stripped.Length}, sha256={hash}]";
    }

    /// <summary>
    /// Recursively drops forbidden keys (case-insensitive) from JSON objects
    /// and from objects nested in arrays. Non-JSON that names a forbidden
    /// property is replaced with an empty object so the raw secret cannot persist.
    /// </summary>
    public static string StripForbiddenKeys(string? snapshot)
    {
        if (string.IsNullOrEmpty(snapshot))
            return snapshot ?? string.Empty;

        try
        {
            var node = JsonNode.Parse(snapshot);
            StripNode(node);
            return node?.ToJsonString() ?? string.Empty;
        }
        catch (JsonException)
        {
            return ContainsForbiddenPropertyName(snapshot) ? "{}" : snapshot;
        }
    }

    public static bool IsForbiddenKey(string? name)
        => !string.IsNullOrEmpty(name) && ForbiddenKeys.Contains(name);

    private static void StripNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (ForbiddenKeys.Contains(key))
                    {
                        obj.Remove(key);
                        continue;
                    }

                    StripNode(obj[key]);
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                    StripNode(item);
                break;
        }
    }

    private static bool ContainsForbiddenPropertyName(string text)
    {
        foreach (var key in ForbiddenKeys)
        {
            if (ForbiddenPropertyName.IsMatch(text, key))
                return true;
        }

        return false;
    }

    private static class ForbiddenPropertyName
    {
        public static bool IsMatch(string text, string key)
            => Regex.IsMatch(
                text,
                "\"" + Regex.Escape(key) + "\"\\s*:",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

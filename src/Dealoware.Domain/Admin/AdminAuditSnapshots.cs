using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Domain.Admin;

/// <summary>
/// FieldPolicy-only audit snapshots. Secrets are never serialized.
/// Bodies over 4 KiB are truncated with original length and SHA-256.
/// </summary>
public static class AdminAuditSnapshots
{
    public const int MaxSnapshotChars = 4096;
    public const int TruncateKeepChars = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "totp",
        "totpSecret",
        "recovery",
        "recoveryCode",
        "recoveryCodes",
        "hmac",
        "hmacKey",
        "secret",
        "apiKey",
        "turnstile",
        "turnstileToken"
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

    public static string Truncate(string? snapshot)
    {
        if (snapshot is null)
            return string.Empty;
        if (snapshot.Length <= MaxSnapshotChars)
            return snapshot;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        var keep = Math.Min(TruncateKeepChars, snapshot.Length);
        return snapshot[..keep]
               + $"... [TRUNCATED, originalLength={snapshot.Length}, sha256={hash}]";
    }
}

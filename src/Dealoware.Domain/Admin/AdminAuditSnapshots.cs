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
    public const int MaxInputUtf8Bytes = 64 * 1024;
    public const int MaxDepth = 8;

    /// <summary>
    /// Stored when the input is not valid JSON, exceeds size/depth limits,
    /// or truncation would break JSON. Raw caller input is never persisted.
    /// </summary>
    public const string FailClosedPlaceholder = """{"_invalid":true}""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        MaxDepth = MaxDepth
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
    /// Write-path sanitize: size/depth caps, strip forbidden keys, then truncate.
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
    /// Oversize, over-deep, or non-JSON input becomes <see cref="FailClosedPlaceholder"/>.
    /// </summary>
    public static string? StripForbiddenKeys(string? snapshot)
    {
        if (snapshot is null)
            return null;
        if (ExceedsInputSize(snapshot))
            return FailClosedPlaceholder;

        try
        {
            var node = JsonNode.Parse(snapshot, documentOptions: ParseOptions);
            var state = new SanitizeState();
            var cleaned = SanitizeNode(node, depth: 0, state);
            if (state.DepthExceeded)
                return FailClosedPlaceholder;
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
        var nfkc = key.Normalize(NormalizationForm.FormKC);
        var trimmed = nfkc.Trim();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (ch is '_' or '-' or '.' || char.IsWhiteSpace(ch))
                continue;
            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static bool ExceedsInputSize(string snapshot)
        => snapshot.Length > MaxInputUtf8Bytes
           || Encoding.UTF8.GetByteCount(snapshot) > MaxInputUtf8Bytes;

    private static JsonNode? SanitizeNode(JsonNode? node, int depth, SanitizeState state)
    {
        if (state.DepthExceeded || node is null)
            return node;

        switch (node)
        {
            case JsonObject obj:
                return SanitizeObject(obj, depth, state);

            case JsonArray array:
                return SanitizeArray(array, depth, state);

            case JsonValue value when value.TryGetValue<string>(out var text):
                return SanitizeStringValue(text, depth, state);

            default:
                return node;
        }
    }

    private static JsonNode SanitizeObject(JsonObject obj, int depth, SanitizeState state)
    {
        if (!TryEnter(depth, state))
            return obj;

        var next = depth + 1;
        foreach (var key in obj.Select(p => p.Key).ToList())
        {
            if (state.DepthExceeded)
                break;
            if (IsForbiddenKey(key))
            {
                obj.Remove(key);
                continue;
            }

            var cleaned = SanitizeNode(obj[key], next, state);
            if (!ReferenceEquals(cleaned, obj[key]))
                obj[key] = cleaned;
        }

        return obj;
    }

    private static JsonNode SanitizeArray(JsonArray array, int depth, SanitizeState state)
    {
        if (!TryEnter(depth, state))
            return array;

        var next = depth + 1;
        for (var i = 0; i < array.Count; i++)
        {
            if (state.DepthExceeded)
                break;

            var cleaned = SanitizeNode(array[i], next, state);
            if (!ReferenceEquals(cleaned, array[i]))
                array[i] = cleaned;
        }

        return array;
    }

    private static JsonNode? SanitizeStringValue(string text, int depth, SanitizeState state)
    {
        if (text.Length > MaxInputUtf8Bytes)
            return JsonValue.Create("[redacted]");

        var trimmed = text.Trim();
        if (LooksLikeSecretValue(trimmed))
            return JsonValue.Create("[redacted]");

        if (trimmed.Length > 0
            && trimmed[0] is '{' or '['
            && TryMatchBalanced(trimmed, 0, out var wholeEnd)
            && SkipWhiteSpace(trimmed.AsSpan(), wholeEnd) == trimmed.Length)
        {
            var promoted = TrySanitizeJsonSlice(trimmed, depth, state, promote: true);
            if (state.DepthExceeded)
                return JsonValue.Create("[redacted]");
            if (promoted is JsonNode node)
                return node;
            return JsonValue.Create("[redacted]");
        }

        var rewritten = RewriteFreeText(text, depth, state);
        if (state.DepthExceeded)
            return JsonValue.Create("[redacted]");
        return JsonValue.Create(rewritten);
    }

    private static bool TryEnter(int depth, SanitizeState state)
    {
        if (depth + 1 <= MaxDepth)
            return true;
        state.DepthExceeded = true;
        return false;
    }

    /// <summary>
    /// Finds <c>{...}</c> / <c>[...]</c> substrings, parses and strips each,
    /// or redacts the span if it is not JSON. Then redacts
    /// <c>forbidden-key [=: ] value</c> assignments. Linear, no backtracking regex.
    /// </summary>
    private static string RewriteFreeText(string text, int depth, SanitizeState state)
    {
        if (text.Length > MaxInputUtf8Bytes)
            return "[redacted]";

        var builder = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length && !state.DepthExceeded)
        {
            if (text[i] is '{' or '[')
            {
                if (!TryMatchBalanced(text, i, out var end))
                {
                    builder.Append("[redacted]");
                    break;
                }

                var slice = text[i..end];
                var cleaned = TrySanitizeJsonSlice(slice, depth, state, promote: false);
                builder.Append(cleaned as string ?? "[redacted]");
                i = end;
                continue;
            }

            builder.Append(text[i]);
            i++;
        }

        if (state.DepthExceeded)
            return "[redacted]";
        return RedactForbiddenAssignments(builder.ToString());
    }

    private static object? TrySanitizeJsonSlice(string slice, int depth, SanitizeState state, bool promote)
    {
        if (!TryEnter(depth, state))
            return promote ? null : "[redacted]";

        var embedDepth = depth + 1;
        var remaining = MaxDepth - embedDepth;
        if (remaining < 1)
        {
            state.DepthExceeded = true;
            return promote ? null : "[redacted]";
        }

        try
        {
            var embedded = JsonNode.Parse(
                slice,
                documentOptions: new JsonDocumentOptions { MaxDepth = remaining });
            var cleaned = SanitizeNode(embedded, embedDepth, state);
            if (state.DepthExceeded)
                return promote ? null : "[redacted]";
            if (promote)
                return cleaned;
            return cleaned?.ToJsonString() ?? "[redacted]";
        }
        catch (JsonException ex) when (IsMaxDepthException(ex))
        {
            state.DepthExceeded = true;
            return promote ? null : "[redacted]";
        }
        catch (JsonException)
        {
            return promote ? null : "[redacted]";
        }
    }

    private static bool TryMatchBalanced(string text, int start, out int end)
    {
        end = start;
        if (start >= text.Length || text[start] is not ('{' or '['))
            return false;

        Span<char> stack = stackalloc char[MaxDepth + 1];
        var top = 0;
        stack[top++] = text[start] == '{' ? '}' : ']';
        var inString = false;
        var quote = '\0';
        var limit = Math.Min(text.Length, start + MaxInputUtf8Bytes);

        for (var i = start + 1; i < limit; i++)
        {
            var ch = text[i];
            if (inString)
            {
                if (ch == '\\' && i + 1 < limit)
                {
                    i++;
                    continue;
                }

                if (ch == quote)
                    inString = false;
                continue;
            }

            if (ch is '"' or '\'')
            {
                inString = true;
                quote = ch;
                continue;
            }

            if (ch is '{' or '[')
            {
                if (top >= stack.Length)
                    return false;
                stack[top++] = ch == '{' ? '}' : ']';
                continue;
            }

            if (ch is not ('}' or ']'))
                continue;
            if (top == 0 || stack[top - 1] != ch)
                return false;
            top--;
            if (top != 0)
                continue;
            end = i + 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Linear scan (no backtracking). Quotes never stop the scan: an unclosed
    /// quote is a normal character, and quoted prose is still searched for
    /// assignments. Separators are NFKC-normalized and %3D/%3A are decoded.
    /// </summary>
    private static string RedactForbiddenAssignments(string text)
    {
        if (text.Length > MaxInputUtf8Bytes)
            return "[redacted]";

        var span = text.AsSpan();
        var builder = new StringBuilder(text.Length);
        var i = 0;
        var copyFrom = 0;
        while (i < span.Length)
        {
            if (!IsBareNameStart(span[i]))
            {
                i++;
                continue;
            }

            var nameStart = i;
            i++;
            while (i < span.Length && IsBareNameContinue(span[i]))
                i++;
            var nameEnd = i;

            var look = nameEnd;
            if (look < span.Length && span[look] is '"' or '\'')
                look++;
            look = SkipWhiteSpace(span, look);

            if (!TryReadSeparator(span, look, out var afterSep)
                || !IsForbiddenKey(span[nameStart..nameEnd].ToString()))
                continue;

            var valueEnd = ConsumeAssignmentValue(span, afterSep);
            var redactFrom = nameStart;
            if (redactFrom > 0 && span[redactFrom - 1] is '"' or '\'')
                redactFrom--;
            builder.Append(text, copyFrom, redactFrom - copyFrom);
            builder.Append("[redacted]");
            copyFrom = valueEnd;
            i = valueEnd;
        }

        builder.Append(text, copyFrom, text.Length - copyFrom);
        return builder.ToString();
    }

    /// <summary>
    /// True when the next token is '=', ':', their NFKC equivalents, or
    /// percent-encoded <c>%3D</c>/<c>%3A</c> (any case).
    /// </summary>
    private static bool TryReadSeparator(ReadOnlySpan<char> span, int index, out int after)
    {
        after = index;
        if (index >= span.Length)
            return false;

        if (index + 2 < span.Length
            && span[index] == '%'
            && char.ToUpperInvariant(span[index + 1]) == '3'
            && char.ToUpperInvariant(span[index + 2]) is 'D' or 'A')
        {
            after = index + 3;
            return true;
        }

        var nfkc = span[index].ToString().Normalize(NormalizationForm.FormKC);
        if (nfkc is "=" or ":")
        {
            after = index + 1;
            return true;
        }

        return false;
    }

    private static int ConsumeAssignmentValue(ReadOnlySpan<char> span, int index)
    {
        index = SkipWhiteSpace(span, index);
        if (index >= span.Length)
            return index;
        if (span[index] is '"' or '\'')
        {
            var quote = span[index++];
            while (index < span.Length)
            {
                if (span[index] == '\\' && index + 1 < span.Length)
                {
                    index += 2;
                    continue;
                }

                if (span[index] == quote)
                    return index + 1;
                index++;
            }

            return index;
        }

        while (index < span.Length
               && !char.IsWhiteSpace(span[index])
               && span[index] is not (',' or ';' or '}' or ']'))
        {
            index++;
        }

        return index;
    }

    private static int SkipWhiteSpace(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index]))
            index++;
        return index;
    }

    private static bool IsBareNameStart(char ch)
        => char.IsLetter(ch) || ch == '_';

    private static bool IsBareNameContinue(char ch)
        => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.';

    private static bool LooksLikeSecretValue(string text)
        => text.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
           || LooksLikeJwt(text);

    private static bool LooksLikeJwt(string text)
    {
        var first = text.IndexOf('.');
        if (first <= 0)
            return false;
        var second = text.IndexOf('.', first + 1);
        if (second <= first + 1 || second == text.Length - 1)
            return false;
        if (text.IndexOf('.', second + 1) >= 0)
            return false;
        return IsBase64Url(text.AsSpan(0, first))
               && IsBase64Url(text.AsSpan(first + 1, second - first - 1))
               && IsBase64Url(text.AsSpan(second + 1));
    }

    private static bool IsBase64Url(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return false;
        foreach (var ch in span)
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
                return false;
        }

        return true;
    }

    private static bool IsMaxDepthException(JsonException ex)
        => ex.Message.Contains("depth", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("MaxDepth", StringComparison.Ordinal);

    private static bool IsValidJson(string text)
    {
        try
        {
            JsonNode.Parse(text, documentOptions: ParseOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class SanitizeState
    {
        public bool DepthExceeded;
    }
}

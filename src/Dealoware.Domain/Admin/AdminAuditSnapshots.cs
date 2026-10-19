using System.Globalization;
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
        "signature",
        "passphrase",
        "accesskey",
        "verifier"
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
        catch (Exception)
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
        if (ContainsNonAsciiLetter(normalized))
            return true;
        if (normalized is "code" or "codes" or "key" or "pass" or "pin" or "auth")
            return true;
        if (normalized.EndsWith("code", StringComparison.Ordinal))
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
        var folded = key.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(folded.Length);
        foreach (var rune in folded.EnumerateRunes())
        {
            if (IsIgnorableRune(rune)
                || rune.Value is '_' or '-' or '.'
                || Rune.IsWhiteSpace(rune))
                continue;
            if (rune.Utf16SequenceLength == 1)
            {
                builder.Append(char.ToLowerInvariant(MapLookalike((char)rune.Value)));
                continue;
            }

            builder.Append(rune);
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
        catch (Exception)
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
    /// The same pass also redacts Bearer/Basic/Digest/Token values and JWT-shaped words.
    /// </summary>
    private static string RedactForbiddenAssignments(string text)
    {
        if (text.Length > MaxInputUtf8Bytes)
            return "[redacted]";

        var span = text.AsSpan();
        var builder = new StringBuilder(text.Length);
        var i = 0;
        var copyFrom = 0;
        while (i < text.Length)
        {
            if (!IsWordStart(text, i))
            {
                i++;
                continue;
            }

            var nameStart = i;
            i += RuneLength(text, i);
            while (i < text.Length && IsWordContinue(text, i))
                i += RuneLength(text, i);
            var nameEnd = i;
            var word = span[nameStart..nameEnd];

            if (IsAuthScheme(word))
            {
                var afterScheme = SkipWhiteSpace(span, nameEnd);
                if (afterScheme > nameEnd
                    && afterScheme < span.Length
                    && !TryReadSeparator(span, afterScheme, out _))
                {
                    if (IsRedactedPlaceholder(span, afterScheme))
                    {
                        i = afterScheme + 10;
                        continue;
                    }

                    var lineEnd = ConsumeToEndOfLine(span, afterScheme);
                    if (lineEnd > afterScheme)
                    {
                        builder.Append(text, copyFrom, afterScheme - copyFrom);
                        builder.Append("[redacted]");
                        copyFrom = lineEnd;
                        i = lineEnd;
                        continue;
                    }
                }
            }

            if (TryMatchJwtPrefix(word, out var jwtLen))
            {
                var jwtEnd = nameStart + jwtLen;
                builder.Append(text, copyFrom, nameStart - copyFrom);
                builder.Append("[redacted]");
                copyFrom = jwtEnd;
                i = jwtEnd;
                continue;
            }

            var look = nameEnd;
            if (look < span.Length && span[look] is '"' or '\'')
                look++;
            look = SkipWhiteSpace(span, look);

            if (!TryReadSeparator(span, look, out var afterSep)
                || !IsForbiddenKey(word.ToString()))
                continue;

            var valueEnd = ConsumeAssignmentValue(span, afterSep, nameStart);
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
    /// True when the next token is '=', ':', '=>', '->', their NFKC equivalents, or
    /// percent-encoded <c>%3D</c>/<c>%3A</c> (any case).
    /// </summary>
    private static bool TryReadSeparator(ReadOnlySpan<char> span, int index, out int after)
    {
        after = index;
        if (index >= span.Length)
            return false;

        if (index + 1 < span.Length)
        {
            if (span[index] == '=' && span[index + 1] == '>')
            {
                after = index + 2;
                return true;
            }

            if (span[index] == '-' && span[index + 1] == '>')
            {
                after = index + 2;
                return true;
            }
        }

        if (index + 2 < span.Length
            && span[index] == '%'
            && char.ToUpperInvariant(span[index + 1]) == '3'
            && char.ToUpperInvariant(span[index + 2]) is 'D' or 'A')
        {
            after = index + 3;
            return true;
        }

        if (IsLookalikeColon(span[index]))
        {
            after = index + 1;
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

    private static int ConsumeAssignmentValue(ReadOnlySpan<char> span, int index, int lineContextStart)
    {
        index = SkipWhiteSpace(span, index);
        if (index >= span.Length)
            return index;
        if (span[index] is '"' or '\'')
        {
            var quote = span[index++];
            var innerStart = index;
            while (index < span.Length)
            {
                if (span[index] == '\\' && index + 1 < span.Length)
                {
                    index += 2;
                    continue;
                }

                if (span[index] == quote)
                {
                    if (index + 1 < span.Length && span[index + 1] == quote)
                    {
                        index += 2;
                        continue;
                    }

                    var after = index + 1;
                    if (index == innerStart)
                        return ConsumeUnquotedToLineEnd(span, after, lineContextStart);
                    return after;
                }

                index++;
            }

            return index;
        }

        return ConsumeUnquotedToLineEnd(span, index, lineContextStart);
    }

    private static int ConsumeUnquotedToLineEnd(ReadOnlySpan<char> span, int index, int lineContextStart)
    {
        while (index < span.Length && span[index] is not ('\n' or '\r'))
        {
            if (span[index] == '&' && IsInsideUrlQuery(span, lineContextStart))
                break;
            index++;
        }

        return index;
    }

    private static int ConsumeToEndOfLine(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && span[index] is not ('\n' or '\r'))
            index++;
        return index;
    }

    /// <summary>
    /// True only when <paramref name="keyStart"/> sits in the same URL-query token
    /// as a preceding <c>?</c> (no whitespace between them).
    /// </summary>
    private static bool IsInsideUrlQuery(ReadOnlySpan<char> span, int keyStart)
    {
        for (var i = keyStart - 1; i >= 0; i--)
        {
            if (span[i] is '\n' or '\r' || char.IsWhiteSpace(span[i]))
                return false;
            if (span[i] == '?')
                return true;
        }

        return false;
    }

    private static int SkipWhiteSpace(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index]))
            index++;
        return index;
    }

    private static bool IsWordStart(string text, int index)
    {
        var rune = Rune.GetRuneAt(text, index);
        return Rune.IsLetter(rune) || rune.Value == '_';
    }

    private static bool IsWordContinue(string text, int index)
    {
        if (text[index] == '-' && index + 1 < text.Length && text[index + 1] == '>')
            return false;
        if (IsLookalikeColon(text[index]))
            return false;
        var rune = Rune.GetRuneAt(text, index);
        return Rune.IsLetterOrDigit(rune)
               || IsIgnorableRune(rune)
               || rune.Value is '_' or '-' or '.';
    }

    private static bool IsLookalikeColon(char ch)
        => ch is '\uA789' or '\u02D0' or '\u2236';

    private static int RuneLength(string text, int index)
        => Rune.GetRuneAt(text, index).Utf16SequenceLength;

    private static bool IsAuthScheme(ReadOnlySpan<char> word)
        => word.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
           || word.Equals("Basic", StringComparison.OrdinalIgnoreCase)
           || word.Equals("Digest", StringComparison.OrdinalIgnoreCase)
           || word.Equals("Token", StringComparison.OrdinalIgnoreCase);

    private static bool IsRedactedPlaceholder(ReadOnlySpan<char> span, int index)
        => index + 10 <= span.Length
           && span.Slice(index, 10).Equals("[redacted]", StringComparison.Ordinal);

    private static bool IsIgnorableKeyChar(char ch)
    {
        var category = char.GetUnicodeCategory(ch);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format;
    }

    private static bool IsIgnorableRune(Rune rune)
    {
        if (rune.Utf16SequenceLength == 1)
            return IsIgnorableKeyChar((char)rune.Value);
        var category = Rune.GetUnicodeCategory(rune);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format;
    }

    private static bool ContainsNonAsciiLetter(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsLetter(rune) && rune.Value > 0x7F)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Maps Cyrillic and Greek letters that impersonate Latin identifiers.
    /// Accents are already stripped by NFKD + combining-mark removal.
    /// </summary>
    private static char MapLookalike(char ch) => ch switch
    {
        'А' or 'Α' => 'A',
        'а' or 'α' => 'a',
        'В' or 'Β' => 'B',
        'в' or 'β' => 'b',
        'Е' or 'Ε' => 'E',
        'е' or 'ε' => 'e',
        'К' or 'Κ' => 'K',
        'к' or 'κ' => 'k',
        'М' or 'Μ' => 'M',
        'м' or 'μ' => 'm',
        'Н' or 'Η' => 'H',
        'н' or 'η' => 'h',
        'О' or 'Ο' => 'O',
        'о' or 'ο' => 'o',
        'Р' or 'Ρ' => 'P',
        'р' or 'ρ' => 'p',
        'С' => 'C',
        'с' => 'c',
        'Т' or 'Τ' => 'T',
        'т' or 'τ' => 't',
        'У' or 'Υ' => 'Y',
        'у' or 'υ' => 'y',
        'Х' or 'Χ' => 'X',
        'х' or 'χ' => 'x',
        'І' or 'Ι' => 'I',
        'і' or 'ι' => 'i',
        'Ј' => 'J',
        'ј' => 'j',
        'Ѕ' => 'S',
        'ѕ' => 's',
        'Ν' => 'N',
        'ν' => 'n',
        'Ζ' => 'Z',
        'ζ' => 'z',
        '0' => 'o',
        '1' => 'l',
        _ => ch
    };

    private static bool LooksLikeSecretValue(string text)
        => LooksLikeJwt(text);

    /// <summary>
    /// Sentence punctuation only. A trailing <c>.</c> is an empty JWT segment
    /// (unsecured compact form) and must not be stripped before matching.
    /// </summary>
    private static ReadOnlySpan<char> TrimTrailingJwtPunct(ReadOnlySpan<char> text)
    {
        while (text.Length > 0 && text[^1] is '?' or '!' or ':')
            text = text[..^1];
        return text;
    }

    /// <summary>
    /// True when <paramref name="text"/> is a compact JWT (3) or JWE (5) whose
    /// first segment starts with <c>eyJ</c>. Empty 2nd JWE (<c>alg: dir</c>)
    /// and empty last JWT (unsecured) segments are accepted. Extra
    /// <c>.word</c> text after those parts is left unmatched.
    /// </summary>
    private static bool TryMatchJwtPrefix(ReadOnlySpan<char> text, out int matchLength)
    {
        matchLength = 0;
        if (text.Length < 4 || !text.StartsWith("eyJ", StringComparison.Ordinal))
            return false;

        Span<int> ends = stackalloc int[6];
        Span<int> lengths = stackalloc int[6];
        var segments = 0;
        var start = 0;
        for (var i = 0; i <= text.Length && segments < 6; i++)
        {
            if (i < text.Length && text[i] != '.')
                continue;

            var length = i - start;
            if (length > 0 && !IsBase64Url(text.Slice(start, length)))
                break;

            ends[segments] = i;
            lengths[segments] = length;
            segments++;
            start = i + 1;
        }

        if (segments >= 5
            && lengths[0] >= 4
            && lengths[0] > 0
            && lengths[2] > 0
            && lengths[3] > 0
            && lengths[4] > 0)
        {
            matchLength = ends[4];
            return true;
        }

        if (segments >= 3
            && lengths[0] >= 4
            && lengths[0] > 0
            && lengths[1] > 0)
        {
            matchLength = ends[2];
            return true;
        }

        return false;
    }

    private static bool LooksLikeJwt(ReadOnlySpan<char> text)
    {
        text = TrimTrailingJwtPunct(text);
        return TryMatchJwtPrefix(text, out var len) && len == text.Length;
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

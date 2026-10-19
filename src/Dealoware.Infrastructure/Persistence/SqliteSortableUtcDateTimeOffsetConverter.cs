using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Shared SQLite store mapping for <see cref="DateTimeOffset"/>: a fixed-width
/// sortable UTC ISO string (<c>yyyy-MM-ddTHH:mm:ss.fffffffZ</c>).
/// <para>
/// Apply on SQLite only. PostgreSQL keeps <see cref="UtcDateTimeOffsetConverter"/>
/// / timestamptz. Reuse this type wherever SQLite must <c>ORDER BY</c> or range-compare
/// a timestamp column (this PR and later PRs such as #32).
/// </para>
/// <para>
/// Legacy EF Core SQLite TEXT (written by
/// <c>SqliteDateTimeOffsetTypeMapping</c> as
/// <c>yyyy-MM-dd HH:mm:ss.FFFFFFFzzz</c>) is accepted on read: space separator,
/// optional fractional digits (0–7), and any numeric offset (including non-UTC).
/// Values are normalized to UTC with exactly seven fractional digits.
/// </para>
/// </summary>
public sealed class SqliteSortableUtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, string>
{
    /// <summary>Canonical store format: UTC, <c>T</c> separator, seven fractional digits, <c>Z</c>.</summary>
    public const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    /// <summary>
    /// EF Core 10 SQLite default store format
    /// (<c>SqliteDateTimeOffsetTypeMapping.DateTimeOffsetFormatConst</c>).
    /// <c>FFFFFFF</c> omits trailing zeros (and the decimal point when the fraction is zero).
    /// <c>zzz</c> is a numeric offset such as <c>+00:00</c> or <c>+02:00</c>.
    /// </summary>
    public const string LegacyEfFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz";

    public SqliteSortableUtcDateTimeOffsetConverter()
        : base(
            v => v.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture),
            v => Parse(v))
    {
    }

    public static bool IsCanonical(string? value)
        => value is { Length: > 0 }
           && DateTimeOffset.TryParseExact(
               value,
               Format,
               CultureInfo.InvariantCulture,
               DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
               out _);

    public static DateTimeOffset Parse(string value)
    {
        if (TryParse(value, out var parsed))
            return parsed;

        throw new FormatException($"Unsupported DateTimeOffset store value '{value}'.");
    }

    public static bool TryParse(string? value, out DateTimeOffset parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (DateTimeOffset.TryParseExact(
                value,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            return true;
        }

        if (DateTimeOffset.TryParseExact(
                value,
                LegacyExactFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
        {
            parsed = parsed.ToUniversalTime();
            return true;
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            parsed = parsed.ToUniversalTime();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the canonical store string, or <c>null</c> when <paramref name="value"/> is null/empty.
    /// Throws when a non-empty value cannot be parsed (fail closed).
    /// Already-canonical strings are returned unchanged.
    /// </summary>
    public static string? NormalizeStoreValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (IsCanonical(value))
            return value;
        if (!TryParse(value, out var parsed))
            throw new FormatException($"Unsupported DateTimeOffset store value '{value}'.");
        return parsed.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
    }

    private static readonly string[] LegacyExactFormats =
    [
        "yyyy-MM-dd HH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss.fzzz",
        "yyyy-MM-dd HH:mm:ss.ffzzz",
        "yyyy-MM-dd HH:mm:ss.fffzzz",
        "yyyy-MM-dd HH:mm:ss.ffffzzz",
        "yyyy-MM-dd HH:mm:ss.fffffzzz",
        "yyyy-MM-dd HH:mm:ss.ffffffzzz",
        "yyyy-MM-dd HH:mm:ss.fffffffzzz",
        LegacyEfFormat
    ];
}

using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// SQLite-only store mapping: DateTimeOffset as a sortable UTC ISO string (TEXT).
/// Lets EF ORDER BY timestamp columns on SQLite without changing the column type.
/// PostgreSQL keeps <see cref="UtcDateTimeOffsetConverter"/> / timestamptz.
/// </summary>
public sealed class SqliteSortableUtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, string>
{
    public const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    public SqliteSortableUtcDateTimeOffsetConverter()
        : base(
            v => v.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture),
            v => Parse(v))
    {
    }

    public static DateTimeOffset Parse(string value)
    {
        if (DateTimeOffset.TryParseExact(
                value,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var exact))
        {
            return exact;
        }

        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}

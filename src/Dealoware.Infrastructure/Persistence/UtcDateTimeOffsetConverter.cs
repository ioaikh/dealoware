using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Normalizes DateTimeOffset values to UTC (offset zero) on write and read.
/// The instant is unchanged; only the offset representation is.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v.ToUniversalTime())
    {
    }
}

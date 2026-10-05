namespace Dealoware.Domain.Artifacts;

/// <summary>
/// D5: Time period with start and end dates.
/// </summary>
public sealed class TimePeriod
{
    public Guid Id { get; private set; }
    
    public DateTimeOffset Start { get; private set; }
    
    public DateTimeOffset End { get; private set; }

    private TimePeriod() { }

    public static TimePeriod Create(DateTimeOffset start, DateTimeOffset end)
    {
        return new TimePeriod
        {
            Id = Guid.NewGuid(),
            // Persist as UTC: PostgreSQL timestamptz (Npgsql) only accepts offset zero.
            Start = start.ToUniversalTime(),
            End = end.ToUniversalTime()
        };
    }
}

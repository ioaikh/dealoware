namespace Dealoware.Domain.Admin;

/// <summary>
/// One reset-IP throttle event. R8: the 15-minute budget is the count of
/// rows with <see cref="OccurredAt"/> still inside the exclusive window.
/// </summary>
public sealed class AdminResetIpEvent
{
    public Guid Id { get; private set; }

    public string IpHmac { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    private AdminResetIpEvent() { }

    public static AdminResetIpEvent Create(string ipHmac, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            IpHmac = ipHmac,
            OccurredAt = now
        };
}

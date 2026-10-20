using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

public class TestAdminClock : IAdminClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    public void Advance(TimeSpan delta) => UtcNow += delta;
}

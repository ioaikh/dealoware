using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

public sealed class SystemAdminClock : IAdminClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

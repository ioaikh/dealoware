namespace Dealoware.Domain.Admin;

/// <summary>
/// One durable gate row per scope+key. ExpiresAt in the past means not locked.
/// Version is the SC-6 concurrency token for check-and-increment.
/// </summary>
public sealed class AdminAuthLockout
{
    public Guid Id { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string SubjectKey { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public uint Version { get; private set; }

    private AdminAuthLockout()
    {
    }

    public static AdminAuthLockout Create(
        string scope,
        string subjectKey,
        DateTimeOffset startedAt,
        TimeSpan duration)
    {
        var row = CreateGate(scope, subjectKey);
        row.Activate(startedAt, duration);
        return row;
    }

    public static AdminAuthLockout CreateGate(string scope, string subjectKey)
    {
        return new AdminAuthLockout
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            SubjectKey = subjectKey,
            StartedAt = DateTimeOffset.UnixEpoch,
            ExpiresAt = DateTimeOffset.UnixEpoch,
            Version = 0
        };
    }

    public void Activate(DateTimeOffset startedAt, TimeSpan duration)
    {
        StartedAt = startedAt;
        ExpiresAt = startedAt.Add(duration);
    }

    public void BumpVersion() => Version++;

    public bool IsActive(DateTimeOffset now) => now < ExpiresAt;
}

namespace Dealoware.Domain.Admin;

/// <summary>
/// Flat lock or IP throttle. Duration does not extend when further attempts arrive.
/// </summary>
public sealed class AdminAuthLockout
{
    public Guid Id { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string SubjectKey { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private AdminAuthLockout()
    {
    }

    public static AdminAuthLockout Create(
        string scope,
        string subjectKey,
        DateTimeOffset startedAt,
        TimeSpan duration)
    {
        return new AdminAuthLockout
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            SubjectKey = subjectKey,
            StartedAt = startedAt,
            ExpiresAt = startedAt.Add(duration)
        };
    }

    public bool IsActive(DateTimeOffset now) => now < ExpiresAt;
}

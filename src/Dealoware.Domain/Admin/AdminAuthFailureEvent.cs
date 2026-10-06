namespace Dealoware.Domain.Admin;

/// <summary>
/// Short-lived counted auth failure. Account rows store the email; IP rows store raw IP
/// and expire with the 15-minute window. Never written to audit.
/// </summary>
public sealed class AdminAuthFailureEvent
{
    public Guid Id { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string SubjectKey { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    private AdminAuthFailureEvent()
    {
    }

    public static AdminAuthFailureEvent Create(string scope, string subjectKey, DateTimeOffset occurredAt)
    {
        return new AdminAuthFailureEvent
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            SubjectKey = subjectKey,
            OccurredAt = occurredAt
        };
    }
}

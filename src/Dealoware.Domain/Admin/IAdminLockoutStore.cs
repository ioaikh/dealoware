namespace Dealoware.Domain.Admin;

/// <summary>
/// Cross-instance lockout counters and locks. Account keys are emails; IP keys are raw IP.
/// </summary>
public interface IAdminLockoutStore
{
    Task<int> CountFailuresAsync(string scope, string subjectKey, DateTimeOffset windowStartExclusive, CancellationToken ct);

    Task AddFailureAsync(AdminAuthFailureEvent failure, CancellationToken ct);

    Task<AdminAuthLockout?> GetActiveLockoutAsync(string scope, string subjectKey, DateTimeOffset now, CancellationToken ct);

    Task AddLockoutAsync(AdminAuthLockout lockout, CancellationToken ct);

    Task ClearAccountFailuresAsync(string email, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

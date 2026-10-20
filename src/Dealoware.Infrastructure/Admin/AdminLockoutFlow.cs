using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Reserve-before-evaluate helpers. Lock only after a failed attempt;
/// release the reserved slot on success and CAS-clear the gate only when
/// the sliding count is below the limit.
/// </summary>
public static class AdminLockoutFlow
{
    public static Task<FailureIncrementResult> ReserveAsync(
        IAdminLockoutStore lockouts,
        string scope,
        string subjectKey,
        DateTimeOffset now,
        int limit,
        CancellationToken ct)
        => lockouts.TryIncrementFailureAsync(
            scope,
            subjectKey,
            now,
            AdminAuthService.FailureWindow,
            limit,
            AdminAuthService.LockDuration,
            ct);

    public static async Task ConfirmFailureAsync(
        IAdminLockoutStore lockouts,
        FailureIncrementResult reserved,
        string scope,
        string subjectKey,
        DateTimeOffset now,
        int limit,
        Func<Task>? onLockCreated,
        CancellationToken ct)
    {
        if (!reserved.Incremented || reserved.Count < limit)
        {
            return;
        }

        if (await lockouts.TryActivateLockAsync(
                    scope,
                    subjectKey,
                    now,
                    AdminAuthService.FailureWindow,
                    limit,
                    AdminAuthService.LockDuration,
                    ct)
                .ConfigureAwait(false)
            && onLockCreated is not null)
        {
            await onLockCreated().ConfigureAwait(false);
        }
    }

    public static async Task ReleaseSuccessAsync(
        IAdminLockoutStore lockouts,
        FailureIncrementResult reserved,
        string scope,
        string subjectKey,
        DateTimeOffset now,
        int limit,
        CancellationToken ct)
    {
        if (reserved.ReservationId is { } id)
        {
            await lockouts.ReleaseReservationAsync(id, ct).ConfigureAwait(false);
        }

        await lockouts.TryReleaseLockIfBelowLimitAsync(
                scope,
                subjectKey,
                now,
                AdminAuthService.FailureWindow,
                limit,
                ct)
            .ConfigureAwait(false);
    }
}

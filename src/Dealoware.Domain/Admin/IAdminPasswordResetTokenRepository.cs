namespace Dealoware.Domain.Admin;

public interface IAdminPasswordResetTokenRepository
{
    Task AddAsync(AdminPasswordResetToken token, CancellationToken cancellationToken = default);

    Task<AdminPasswordResetToken?> FindUsableByHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the unused, unexpired row with this hash as consumed.
    /// Returns true only when this call won the race.
    /// </summary>
    Task<bool> TryConsumeAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task InvalidateUnusedForEmailAsync(
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments failed 2FA attempts on the unused row while
    /// <see cref="AdminPasswordResetToken.FailedCodeAttempts"/> is under
    /// <see cref="AdminPasswordResetToken.MaxFailedCodeAttempts"/>. Burns the
    /// token on the fifth increment. Returns false when the row is already
    /// consumed or at the cap.
    /// </summary>
    Task<bool> TryRecordFailedCodeAttemptAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

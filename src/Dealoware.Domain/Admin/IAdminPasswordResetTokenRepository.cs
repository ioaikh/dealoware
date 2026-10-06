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
}

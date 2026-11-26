namespace Dealoware.Domain.Admin;

public interface IAdminBootstrapTokenRepository
{
    Task AddAsync(AdminBootstrapToken token, CancellationToken cancellationToken = default);

    Task<AdminBootstrapToken?> FindUsableByHashAsync(
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

    /// <summary>
    /// Burns every unused bootstrap token. Called when a newer link is issued,
    /// when the first password is set, and on the account-wide lock.
    /// </summary>
    Task InvalidateUnusedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

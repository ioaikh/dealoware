namespace Dealoware.Domain.Participants;

/// <summary>
/// Repository for revoked token tracking.
/// </summary>
public interface IRevokedTokenRepository
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task AddAsync(RevokedToken revokedToken, CancellationToken cancellationToken = default);
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

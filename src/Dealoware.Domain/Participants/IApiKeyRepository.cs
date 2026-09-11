namespace Dealoware.Domain.Participants;

/// <summary>
/// Repository for API key credential persistence.
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKeyCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKeyCredential>> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<ApiKeyCredential?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKeyCredential>> GetAllValidAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ApiKeyCredential credential, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

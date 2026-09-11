using Dealoware.Domain.Participants;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly DealowareDbContext _context;

    public ApiKeyRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKeyCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeyCredentials
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKeyCredential>> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeyCredentials
            .Where(c => c.ParticipantId == participantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKeyCredential?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeyCredentials
            .FirstOrDefaultAsync(c => c.KeyPrefix == keyPrefix, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKeyCredential>> GetAllValidAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeyCredentials
            .Where(c => c.RevokedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApiKeyCredential credential, CancellationToken cancellationToken = default)
    {
        await _context.ApiKeyCredentials.AddAsync(credential, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

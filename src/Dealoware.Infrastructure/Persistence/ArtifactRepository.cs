using Dealoware.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

public class ArtifactRepository : IArtifactRepository
{
    private readonly DealowareDbContext _context;

    public ArtifactRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<Artifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Artifacts
            .Include(a => a.Entities)
                .ThenInclude(e => e.Properties)
            .Include(a => a.Values)
            .Include(a => a.TimePeriods)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Artifact?> GetByIdForOwnerAsync(Guid id, string ownerParticipantId, CancellationToken cancellationToken = default)
    {
        return await _context.Artifacts
            .Include(a => a.Entities)
                .ThenInclude(e => e.Properties)
            .Include(a => a.Values)
            .Include(a => a.TimePeriods)
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerParticipantId == ownerParticipantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Artifact>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default)
    {
        var artifacts = await _context.Artifacts
            .Include(a => a.Entities)
                .ThenInclude(e => e.Properties)
            .Include(a => a.Values)
            .Include(a => a.TimePeriods)
            .Where(a => a.OwnerParticipantId == ownerParticipantId)
            .ToListAsync(cancellationToken);
        
        return artifacts.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task AddAsync(Artifact artifact, CancellationToken cancellationToken = default)
    {
        await _context.Artifacts.AddAsync(artifact, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

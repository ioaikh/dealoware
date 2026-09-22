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

    public async Task<IReadOnlyList<Artifact>> SearchDiscoverableAsync(
        string query, 
        string callerParticipantId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<Artifact>();
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();
        
        var matchingArtifactIds = await _context.Artifacts
            .Where(a => 
                EF.Functions.Like(a.Intent.ToLower(), $"%{normalizedQuery}%") ||
                a.Entities.Any(e => 
                    EF.Functions.Like(e.Name.ToLower(), $"%{normalizedQuery}%") ||
                    EF.Functions.Like(e.Description.ToLower(), $"%{normalizedQuery}%")
                ) ||
                a.Entities.Any(e =>
                    e.Properties.Any(p =>
                        EF.Functions.Like(p.Name.ToLower(), $"%{normalizedQuery}%") ||
                        EF.Functions.Like(p.Value.ToLower(), $"%{normalizedQuery}%")
                    )
                )
            )
            .OrderByDescending(a => a.Id)
            .Take(limit)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (matchingArtifactIds.Count == 0)
        {
            return Array.Empty<Artifact>();
        }

        var artifacts = await _context.Artifacts
            .Include(a => a.Entities)
                .ThenInclude(e => e.Properties)
            .Include(a => a.Values)
            .Include(a => a.TimePeriods)
            .Where(a => matchingArtifactIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        return artifacts.OrderByDescending(a => a.CreatedAt).ToList();
    }
}

using Dealoware.Domain.Participants;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <remarks>
/// The engine's participant lookups. A soft-deleted participant (admin
/// delete, Spec §11) is not found, exactly like one that does not exist: it
/// cannot sign in, act, or be named as a counterparty.
/// </remarks>
public class ParticipantRepository : IParticipantRepository
{
    private readonly DealowareDbContext _context;

    public ParticipantRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
    }

    public async Task<Participant?> GetBySubAsync(string sub, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .FirstOrDefaultAsync(p => p.Sub == sub && p.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        await _context.Participants.AddAsync(participant, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

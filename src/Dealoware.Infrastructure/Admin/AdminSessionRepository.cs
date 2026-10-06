using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// EF Core implementation of IAdminSessionRepository.
/// </summary>
public sealed class AdminSessionRepository : IAdminSessionRepository
{
    private readonly DealowareDbContext _context;

    public AdminSessionRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task<AdminSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(AdminSession session, CancellationToken cancellationToken = default)
    {
        await _context.AdminSessions.AddAsync(session, cancellationToken);
    }

    public Task UpdateAsync(AdminSession session, CancellationToken cancellationToken = default)
    {
        _context.AdminSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _context.AdminSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
        if (session is not null)
        {
            _context.AdminSessions.Remove(session);
        }
    }

    public async Task DeleteByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.AdminSessions.ToListAsync(cancellationToken);
        _context.AdminSessions.RemoveRange(
            sessions.Where(s => string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

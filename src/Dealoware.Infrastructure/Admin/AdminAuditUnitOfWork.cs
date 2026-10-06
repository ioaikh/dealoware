using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Same-transaction pairing: mutation + audit inserts commit or roll back together.
/// Later edit and delete PRs call <see cref="IAdminAuditUnitOfWork"/>.
/// </summary>
public sealed class AdminAuditUnitOfWork : IAdminAuditUnitOfWork
{
    private readonly DealowareDbContext _db;
    private readonly IAdminAuditRepository _audit;

    public AdminAuditUnitOfWork(DealowareDbContext db, IAdminAuditRepository audit)
    {
        _db = db;
        _audit = audit;
    }

    public Task ExecutePairedAsync(
        Func<CancellationToken, Task> mutateAsync,
        AdminAuditEntry auditEntry,
        CancellationToken cancellationToken = default)
        => ExecutePairedAsync(mutateAsync, [auditEntry], cancellationToken);

    public async Task ExecutePairedAsync(
        Func<CancellationToken, Task> mutateAsync,
        IReadOnlyList<AdminAuditEntry> auditEntries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutateAsync);
        ArgumentNullException.ThrowIfNull(auditEntries);
        if (auditEntries.Count == 0)
            throw new ArgumentException("At least one audit entry is required.", nameof(auditEntries));

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await mutateAsync(cancellationToken).ConfigureAwait(false);
            foreach (var entry in auditEntries)
            {
                await _audit.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}

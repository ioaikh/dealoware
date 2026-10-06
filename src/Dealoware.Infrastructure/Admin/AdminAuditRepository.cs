using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// EF Core implementation of IAdminAuditRepository.
/// Append-only: provides only Add and paged read, no Update or Delete.
/// </summary>
public sealed class AdminAuditRepository : IAdminAuditRepository
{
    private static readonly HashSet<string> SortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "timestamp",
        "-timestamp",
        "action",
        "-action",
        "actoremail",
        "-actoremail",
        "entitytype",
        "-entitytype"
    };

    private readonly DealowareDbContext _context;

    public AdminAuditRepository(DealowareDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.AdminAuditLog.AddAsync(entry, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListPageAsync(
        AdminAuditListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyFilters(_context.AdminAuditLog.AsNoTracking(), criteria);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var sorted = ApplySort(query, criteria.Sort);
        var items = await sorted
            .Skip(criteria.Offset)
            .Take(criteria.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    private static IQueryable<AdminAuditEntry> ApplyFilters(
        IQueryable<AdminAuditEntry> query,
        AdminAuditListCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.Action))
        {
            var action = criteria.Action;
            query = query.Where(e => e.Action == action);
        }

        if (!string.IsNullOrEmpty(criteria.ActorEmail))
        {
            var actor = criteria.ActorEmail.ToLowerInvariant();
            query = query.Where(e => e.ActorEmail.ToLower().Contains(actor));
        }

        if (!string.IsNullOrEmpty(criteria.EntityType))
        {
            var entityType = criteria.EntityType;
            query = query.Where(e => e.EntityType == entityType);
        }

        if (criteria.EntityId is { } entityId)
        {
            query = query.Where(e => e.EntityId == entityId);
        }

        if (!string.IsNullOrEmpty(criteria.ReasonClass))
        {
            var reason = criteria.ReasonClass;
            query = query.Where(e => e.ReasonClass == reason);
        }

        if (criteria.CorrelationId is { } correlationId)
        {
            query = query.Where(e => e.CorrelationId == correlationId);
        }

        if (criteria.From is { } from)
        {
            query = query.Where(e => e.Timestamp >= from);
        }

        if (criteria.To is { } to)
        {
            query = query.Where(e => e.Timestamp <= to);
        }

        if (!string.IsNullOrEmpty(criteria.Q))
        {
            var q = criteria.Q.ToLowerInvariant();
            query = query.Where(e =>
                e.Action.ToLower().Contains(q)
                || e.ActorEmail.ToLower().Contains(q)
                || (e.EntityType != null && e.EntityType.ToLower().Contains(q)));
        }

        return query;
    }

    private static IQueryable<AdminAuditEntry> ApplySort(IQueryable<AdminAuditEntry> query, string sort)
    {
        var key = string.IsNullOrWhiteSpace(sort) ? "-timestamp" : sort.Trim();
        if (!SortKeys.Contains(key))
            key = "-timestamp";

        return key.ToLowerInvariant() switch
        {
            "timestamp" => query.OrderBy(e => e.Timestamp).ThenBy(e => e.Id),
            "action" => query.OrderBy(e => e.Action).ThenBy(e => e.Id),
            "-action" => query.OrderByDescending(e => e.Action).ThenBy(e => e.Id),
            "actoremail" => query.OrderBy(e => e.ActorEmail).ThenBy(e => e.Id),
            "-actoremail" => query.OrderByDescending(e => e.ActorEmail).ThenBy(e => e.Id),
            "entitytype" => query.OrderBy(e => e.EntityType).ThenBy(e => e.Id),
            "-entitytype" => query.OrderByDescending(e => e.EntityType).ThenBy(e => e.Id),
            _ => query.OrderByDescending(e => e.Timestamp).ThenBy(e => e.Id)
        };
    }
}

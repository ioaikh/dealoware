namespace Dealoware.Domain.Admin;

/// <summary>
/// Same-transaction pairing for admin mutations and their audit rows
/// (Spec §10; SA §3.5). Later edit and delete PRs call this helper:
/// the mutation and every audit insert commit or roll back together.
/// </summary>
public interface IAdminAuditUnitOfWork
{
    Task ExecutePairedAsync(
        Func<CancellationToken, Task> mutateAsync,
        AdminAuditEntry auditEntry,
        CancellationToken cancellationToken = default);

    Task ExecutePairedAsync(
        Func<CancellationToken, Task> mutateAsync,
        IReadOnlyList<AdminAuditEntry> auditEntries,
        CancellationToken cancellationToken = default);
}

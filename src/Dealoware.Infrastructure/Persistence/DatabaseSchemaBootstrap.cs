using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Separates local Development schema bootstrap (EnsureCreated) from the
/// explicit migrate one-shot (MigrateAsync). The long-lived API process must
/// not run migrations; operators inject migrations credentials into DB_* only
/// for the migrate task, never into the long-lived API task.
/// </summary>
public static class DatabaseSchemaBootstrap
{
    /// <summary>
    /// EnsureCreated is for Development (and local SQLite) only.
    /// Non-Development schema evolution goes through <see cref="ApplyMigrationsAsync"/>.
    /// </summary>
    public static bool ShouldEnsureCreatedOnStartup(string? environmentName)
        => string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates the schema via EnsureCreated when running in Development.
    /// No-ops for every other environment so production schema changes use the migrate one-shot.
    /// </summary>
    public static async Task ApplyStartupSchemaAsync(
        DealowareDbContext db,
        string? environmentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (!ShouldEnsureCreatedOnStartup(environmentName))
        {
            return;
        }

        await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies EF Core migrations, including the baseline stamp for EnsureCreated
    /// databases that have no history row. Intended for the migrate one-shot
    /// (<c>dealoware_migrate</c>) only — never the long-lived API
    /// (<c>dealoware_app</c> is SELECT-only on <c>__EFMigrationsHistory</c> and
    /// must not run this).
    /// </summary>
    public static Task ApplyMigrationsAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        return DatabaseMigrationBaseline.ApplyAsync(db, cancellationToken);
    }
}

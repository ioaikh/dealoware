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
    /// Applies EF Core migrations. Intended for the migrate one-shot entrypoint only,
    /// using the same DB_* / connection-string configuration as the API (the operator
    /// supplies migrations-capable credentials for that task).
    /// </summary>
    public static Task ApplyMigrationsAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        return db.Database.MigrateAsync(cancellationToken);
    }
}

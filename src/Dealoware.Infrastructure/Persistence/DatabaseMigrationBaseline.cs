using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// What the migrate one-shot should do before <c>MigrateAsync</c>.
/// Never invoked from the long-lived API process.
/// </summary>
public enum MigrationBaselineAction
{
    /// <summary>Fresh or already recorded — just apply pending migrations.</summary>
    ApplyMigrations,

    /// <summary>
    /// Complete EnsureCreated / bootstrap schema, no baseline history row.
    /// Record the baseline as applied, then apply newer migrations.
    /// </summary>
    StampBaselineThenMigrate,

    /// <summary>Some but not all baseline tables exist.</summary>
    FailClosedPartialSchema,

    /// <summary>Non-empty database that is not the baseline app schema.</summary>
    FailClosedUnknownSchema
}

/// <summary>
/// Decision plus an operator-safe failure message (no hosts, users, or connection strings).
/// </summary>
public sealed record MigrationBaselinePlan(
    MigrationBaselineAction Action,
    string? FailureMessage);

/// <summary>
/// Bridges EnsureCreated / schema-bootstrap databases (no
/// <c>__EFMigrationsHistory</c> row) onto the first real EF migration.
/// CREATE / INSERT on the history table is migrate-login only
/// (<c>dealoware_migrate</c>). The runtime login (<c>dealoware_app</c>) is
/// SELECT-only on that table and must never call this type.
/// </summary>
public static class DatabaseMigrationBaseline
{
    public const string BaselineMigrationId = "20261005000000_Baseline";

    public const string HistoryTableName = "__EFMigrationsHistory";

    public const string PartialSchemaMessage =
        "The database schema is incomplete relative to the baseline (initial) migration. " +
        "The migrate one-shot refuses to stamp history or re-run CREATE TABLE. " +
        "Restore a known complete schema or provision a fresh database.";

    public const string UnknownSchemaMessage =
        "The database has tables that are not the Dealoware baseline schema, " +
        "and the baseline migration is not recorded. " +
        "The migrate one-shot refuses to continue.";

    /// <summary>
    /// Tables created by Development <c>EnsureCreated</c> / schema bootstrap
    /// before any EF migration existed. Frozen for detection; do not replace
    /// with the current model (later migrations such as A7 admin tables are
    /// incremental after this baseline).
    /// </summary>
    public static readonly IReadOnlyList<string> BaselineTableNames =
    [
        "AcceptGrants",
        "ApiKeyCredentials",
        "ArtifactValues",
        "Artifacts",
        "EntityProperties",
        "Negotiations",
        "Offers",
        "ParticipantBudgets",
        "Participants",
        "RevokedTokens",
        "Strategies",
        "SubjectEntities",
        "TimePeriods"
    ];

    /// <summary>
    /// Pure decision over applied history, assembly migrations, and user tables.
    /// </summary>
    public static MigrationBaselinePlan Decide(
        IReadOnlyList<string> assemblyMigrations,
        IReadOnlyCollection<string> appliedMigrations,
        IReadOnlyCollection<string> userTables)
    {
        ArgumentNullException.ThrowIfNull(assemblyMigrations);
        ArgumentNullException.ThrowIfNull(appliedMigrations);
        ArgumentNullException.ThrowIfNull(userTables);

        if (assemblyMigrations.Count == 0)
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.ApplyMigrations, null);
        }

        var baselineId = assemblyMigrations.Contains(BaselineMigrationId, StringComparer.Ordinal)
            ? BaselineMigrationId
            : assemblyMigrations[0];

        if (appliedMigrations.Contains(baselineId, StringComparer.Ordinal))
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.ApplyMigrations, null);
        }

        var tables = new HashSet<string>(userTables, StringComparer.OrdinalIgnoreCase);
        var baseline = new HashSet<string>(BaselineTableNames, StringComparer.OrdinalIgnoreCase);
        var present = BaselineTableNames.Count(t => tables.Contains(t));
        var extras = tables.Count(t => !baseline.Contains(t));

        if (present == 0 && extras == 0)
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.ApplyMigrations, null);
        }

        if (present == BaselineTableNames.Count)
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.StampBaselineThenMigrate, null);
        }

        if (present == 0)
        {
            return new MigrationBaselinePlan(
                MigrationBaselineAction.FailClosedUnknownSchema,
                UnknownSchemaMessage);
        }

        return new MigrationBaselinePlan(
            MigrationBaselineAction.FailClosedPartialSchema,
            PartialSchemaMessage);
    }

    /// <summary>
    /// Migrate-only: stamp the baseline when the existing schema matches, then
    /// apply pending migrations. Must not be called from API startup.
    /// </summary>
    public static async Task ApplyAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var assembly = db.Database.GetMigrations().ToList();
        var applied = await ReadAppliedMigrationsSafeAsync(db, cancellationToken).ConfigureAwait(false);
        var userTables = await ListUserTablesAsync(db, cancellationToken).ConfigureAwait(false);
        var plan = Decide(assembly, applied, userTables);

        switch (plan.Action)
        {
            case MigrationBaselineAction.FailClosedPartialSchema:
            case MigrationBaselineAction.FailClosedUnknownSchema:
                throw new InvalidOperationException(
                    plan.FailureMessage ?? PartialSchemaMessage);

            case MigrationBaselineAction.StampBaselineThenMigrate:
                await StampBaselineAsync(db, ResolveBaselineId(assembly), cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyList<string>> ListUserTablesAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = db.Database.IsNpgsql()
                ? """
                  SELECT tablename
                  FROM pg_catalog.pg_tables
                  WHERE schemaname = current_schema()
                    AND tablename <> '__EFMigrationsHistory'
                  """
                : """
                  SELECT name
                  FROM sqlite_master
                  WHERE type = 'table'
                    AND name NOT LIKE 'sqlite_%'
                    AND name <> '__EFMigrationsHistory'
                  """;

            var tables = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static string ResolveBaselineId(IReadOnlyList<string> assemblyMigrations)
        => assemblyMigrations.Contains(BaselineMigrationId, StringComparer.Ordinal)
            ? BaselineMigrationId
            : assemblyMigrations[0];

    private static async Task<IReadOnlyList<string>> ReadAppliedMigrationsSafeAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await db.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
                .ToList();
        }
        catch (Exception)
        {
            // Missing history table or a SELECT failure — treat as no history.
            // Do not leak driver / connection details.
            return [];
        }
    }

    /// <summary>
    /// CREATE / INSERT on <c>__EFMigrationsHistory</c>. Call only as
    /// <c>dealoware_migrate</c> from the one-shot. Idempotent if the row exists.
    /// </summary>
    private static async Task StampBaselineAsync(
        DealowareDbContext db,
        string baselineId,
        CancellationToken cancellationToken)
    {
        var history = db.GetService<IHistoryRepository>();
        await history.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        if (applied.Contains(baselineId, StringComparer.Ordinal))
        {
            return;
        }

        var sql = history.GetInsertScript(new HistoryRow(baselineId, ProductInfo.GetVersion()));
        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }
}

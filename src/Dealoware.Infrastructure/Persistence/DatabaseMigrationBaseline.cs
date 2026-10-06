using System.Data;
using System.Data.Common;
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
    /// Exact pre-#20 13-table schema, no history. Stamp Baseline only,
    /// then apply newer migrations (including #20) for real.
    /// </summary>
    StampBaselineThenMigrate,

    /// <summary>
    /// Exact current-model schema (15 tables including admin / soft-delete),
    /// no history. Stamp Baseline and #20, then apply any later migrations.
    /// </summary>
    StampCurrentThenMigrate,

    /// <summary>Some but not all baseline tables exist.</summary>
    FailClosedPartialSchema,

    /// <summary>Non-empty database that is not the baseline app schema.</summary>
    FailClosedUnknownSchema,

    /// <summary>Expected table names exist but columns or primary keys do not match.</summary>
    FailClosedSchemaMismatch
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

    public const string AdminTablesMigrationId = "20261006000100_AddAdminTablesAndSoftDelete";

    public const string HistoryTableName = "__EFMigrationsHistory";

    public const string PartialSchemaMessage =
        "The database schema is incomplete relative to the baseline (initial) migration. " +
        "The migrate one-shot refuses to stamp history or re-run CREATE TABLE. " +
        "Restore a known complete schema or provision a fresh database.";

    public const string UnknownSchemaMessage =
        "The database has tables that are not the Dealoware baseline schema, " +
        "and the baseline migration is not recorded. " +
        "The migrate one-shot refuses to continue.";

    public const string SchemaMismatchMessage =
        "The database table names match the baseline, but columns or primary keys " +
        "do not match the frozen baseline model. The migrate one-shot refuses to " +
        "stamp history or re-run CREATE TABLE.";

    /// <summary>
    /// Pre-#20 EnsureCreated tables (no Admin*). Frozen for the 13-table stamp path.
    /// </summary>
    public static readonly IReadOnlyList<string> Pre20TableNames =
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
    /// Current-model EnsureCreated tables (pre-#20 plus A7 admin tables).
    /// Frozen for the 15-table stamp path. Incremental migrations after
    /// <see cref="AdminTablesMigrationId"/> still apply after a successful stamp.
    /// </summary>
    public static readonly IReadOnlyList<string> BaselineTableNames =
    [
        "AcceptGrants",
        "AdminAuditLog",
        "AdminSessions",
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
    /// PostgreSQL uses ordinal (quoted, case-sensitive) table names; SQLite does not.
    /// </summary>
    public static MigrationBaselinePlan Decide(
        IReadOnlyList<string> assemblyMigrations,
        IReadOnlyCollection<string> appliedMigrations,
        IReadOnlyCollection<string> userTables,
        bool postgres = false)
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

        var names = postgres ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var tables = new HashSet<string>(userTables, names);
        var current = new HashSet<string>(BaselineTableNames, names);
        var pre20 = new HashSet<string>(Pre20TableNames, names);

        if (tables.Count == 0)
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.ApplyMigrations, null);
        }

        if (tables.SetEquals(current))
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.StampCurrentThenMigrate, null);
        }

        if (tables.SetEquals(pre20))
        {
            return new MigrationBaselinePlan(MigrationBaselineAction.StampBaselineThenMigrate, null);
        }

        if (tables.Any(t => !current.Contains(t)))
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
        var postgres = db.Database.IsNpgsql();
        var plan = Decide(assembly, applied, userTables, postgres);

        switch (plan.Action)
        {
            case MigrationBaselineAction.FailClosedPartialSchema:
            case MigrationBaselineAction.FailClosedUnknownSchema:
            case MigrationBaselineAction.FailClosedSchemaMismatch:
                throw new InvalidOperationException(
                    plan.FailureMessage ?? PartialSchemaMessage);

            case MigrationBaselineAction.StampBaselineThenMigrate:
                await CompareAndStampAsync(
                        db,
                        assembly,
                        postgres,
                        BaselineSchema.Pre20Columns,
                        BaselineSchema.Pre20PrimaryKeys,
                        stampAdminMigration: false,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case MigrationBaselineAction.StampCurrentThenMigrate:
                await CompareAndStampAsync(
                        db,
                        assembly,
                        postgres,
                        BaselineSchema.Columns,
                        BaselineSchema.PrimaryKeys,
                        stampAdminMigration: true,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
        }

        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<(IReadOnlyList<BaselineLiveColumn> Columns, IReadOnlyList<BaselineLivePrimaryKey> PrimaryKeys)>
        ReadLiveSchemaAsync(DealowareDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return db.Database.IsNpgsql()
                ? await ReadPostgresSchemaAsync(connection, cancellationToken).ConfigureAwait(false)
                : await ReadSqliteSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
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

    private static async Task<(IReadOnlyList<BaselineLiveColumn> Columns, IReadOnlyList<BaselineLivePrimaryKey> PrimaryKeys)>
        ReadSqliteSchemaAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var columns = new List<BaselineLiveColumn>();
        var keys = new List<BaselineLivePrimaryKey>();

        foreach (var table in DatabaseMigrationBaseline.BaselineTableNames)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\")";
            var pk = new SortedDictionary<int, string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var name = reader.GetString(1);
                var storeType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var notNull = reader.GetInt64(3) != 0;
                var pkOrdinal = Convert.ToInt32(reader.GetValue(5));
                columns.Add(new BaselineLiveColumn(table, name, storeType, IsNullable: !notNull));
                if (pkOrdinal > 0)
                {
                    pk[pkOrdinal] = name;
                }
            }

            keys.Add(new BaselineLivePrimaryKey(table, pk.Values.ToList()));
        }

        return (columns, keys);
    }

    private static async Task<(IReadOnlyList<BaselineLiveColumn> Columns, IReadOnlyList<BaselineLivePrimaryKey> PrimaryKeys)>
        ReadPostgresSchemaAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var columns = new List<BaselineLiveColumn>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT table_name, column_name, data_type, is_nullable
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND table_name <> '__EFMigrationsHistory'
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                columns.Add(new BaselineLiveColumn(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    IsNullable: string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase)));
            }
        }

        var keyColumns = new Dictionary<string, SortedDictionary<int, string>>(StringComparer.Ordinal);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT tc.table_name, kcu.column_name, kcu.ordinal_position
                FROM information_schema.table_constraints tc
                INNER JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                WHERE tc.table_schema = current_schema()
                  AND tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_name <> '__EFMigrationsHistory'
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var table = reader.GetString(0);
                if (!keyColumns.TryGetValue(table, out var ordered))
                {
                    ordered = new SortedDictionary<int, string>();
                    keyColumns[table] = ordered;
                }

                ordered[Convert.ToInt32(reader.GetValue(2))] = reader.GetString(1);
            }
        }

        var keys = keyColumns
            .Select(kv => new BaselineLivePrimaryKey(kv.Key, kv.Value.Values.ToList()))
            .ToList();
        return (columns, keys);
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

    private static async Task CompareAndStampAsync(
        DealowareDbContext db,
        IReadOnlyList<string> assemblyMigrations,
        bool postgres,
        IReadOnlyList<BaselineColumnSpec> expectedColumns,
        IReadOnlyList<BaselinePrimaryKeySpec> expectedKeys,
        bool stampAdminMigration,
        CancellationToken cancellationToken)
    {
        var live = await ReadLiveSchemaAsync(db, cancellationToken).ConfigureAwait(false);
        var comparison = BaselineSchema.Compare(
            live.Columns,
            live.PrimaryKeys,
            expectedColumns,
            expectedKeys,
            postgres);
        if (!comparison.Matches)
        {
            throw new InvalidOperationException(SchemaMismatchMessage);
        }

        var history = db.GetService<IHistoryRepository>();
        await history.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .ToList();

        foreach (var migrationId in StampIds(assemblyMigrations, stampAdminMigration))
        {
            if (applied.Contains(migrationId, StringComparer.Ordinal))
            {
                continue;
            }

            var sql = history.GetInsertScript(new HistoryRow(migrationId, ProductInfo.GetVersion()));
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
            applied.Add(migrationId);
        }
    }

    private static IEnumerable<string> StampIds(
        IReadOnlyList<string> assemblyMigrations,
        bool stampAdminMigration)
    {
        yield return ResolveBaselineId(assemblyMigrations);
        if (stampAdminMigration
            && assemblyMigrations.Contains(AdminTablesMigrationId, StringComparer.Ordinal))
        {
            yield return AdminTablesMigrationId;
        }
    }
}

using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Dealoware.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dealoware.Api.Tests;

/// <summary>
/// H3: migrate one-shot baselines EnsureCreated databases that have no
/// __EFMigrationsHistory row. API startup must not stamp or write history.
/// </summary>
public class DatabaseMigrationBaselineTests
{
    private static readonly string[] Empty = [];

    private static IReadOnlyList<string> AssemblyWithBaseline =>
        [DatabaseMigrationBaseline.BaselineMigrationId, DatabaseMigrationBaseline.AdminTablesMigrationId];

    private static readonly string[] FrozenSchemaMigrationIds =
    [
        DatabaseMigrationBaseline.BaselineMigrationId,
        DatabaseMigrationBaseline.AdminTablesMigrationId,
        DatabaseMigrationBaseline.UpdatedAtMigrationId
    ];

    /// <summary>
    /// Stamp set is unchanged (PR #23 frozen + #27 UpdatedAt). Migration 380
    /// applies after stamp via MigrateAsync.
    /// </summary>
    private static readonly string[] AppliedAfterMigrate =
    [
        DatabaseMigrationBaseline.BaselineMigrationId,
        DatabaseMigrationBaseline.AdminTablesMigrationId,
        DatabaseMigrationBaseline.UpdatedAtMigrationId,
        AdminAuditLogAppendOnly.MigrationId
    ];

    [Fact]
    public void Decide_FreshEmpty_AppliesMigrations()
    {
        var plan = DatabaseMigrationBaseline.Decide(AssemblyWithBaseline, Empty, Empty);

        Assert.Equal(MigrationBaselineAction.ApplyMigrations, plan.Action);
        Assert.Null(plan.FailureMessage);
    }

    [Fact]
    public void Decide_CompleteSchemaNoHistory_StampsCurrent()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: DatabaseMigrationBaseline.BaselineTableNames);

        Assert.Equal(MigrationBaselineAction.StampCurrentThenMigrate, plan.Action);
        Assert.Null(plan.FailureMessage);
    }

    [Fact]
    public void Decide_Pre20SchemaNoHistory_StampsBaselineOnly()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: DatabaseMigrationBaseline.Pre20TableNames);

        Assert.Equal(MigrationBaselineAction.StampBaselineThenMigrate, plan.Action);
        Assert.Null(plan.FailureMessage);
    }

    [Fact]
    public void Decide_AlreadyHasBaseline_AppliesMigrations()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: [DatabaseMigrationBaseline.BaselineMigrationId],
            userTables: DatabaseMigrationBaseline.BaselineTableNames);

        Assert.Equal(MigrationBaselineAction.ApplyMigrations, plan.Action);
    }

    [Fact]
    public void Decide_PartialSchema_FailsClosed()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: ["Participants", "Artifacts"]);

        Assert.Equal(MigrationBaselineAction.FailClosedPartialSchema, plan.Action);
        Assert.Equal(DatabaseMigrationBaseline.PartialSchemaMessage, plan.FailureMessage);
        AssertSafe(plan.FailureMessage!);
    }

    [Fact]
    public void Decide_UnknownSchema_FailsClosed()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: ["SomethingElse"]);

        Assert.Equal(MigrationBaselineAction.FailClosedUnknownSchema, plan.Action);
        Assert.Equal(DatabaseMigrationBaseline.UnknownSchemaMessage, plan.FailureMessage);
        AssertSafe(plan.FailureMessage!);
    }

    [Fact]
    public void Decide_NoAssemblyMigrations_AppliesMigrations()
    {
        var plan = DatabaseMigrationBaseline.Decide([], Empty, DatabaseMigrationBaseline.BaselineTableNames);

        Assert.Equal(MigrationBaselineAction.ApplyMigrations, plan.Action);
    }

    [Fact]
    public void Decide_CompleteSchemaPlusExtraTable_FailsClosedUnknown()
    {
        var tables = DatabaseMigrationBaseline.BaselineTableNames.Append("ExtraJunk").ToArray();
        var plan = DatabaseMigrationBaseline.Decide(AssemblyWithBaseline, Empty, tables);

        Assert.Equal(MigrationBaselineAction.FailClosedUnknownSchema, plan.Action);
        Assert.Equal(DatabaseMigrationBaseline.UnknownSchemaMessage, plan.FailureMessage);
        AssertSafe(plan.FailureMessage!);
    }

    [Fact]
    public void Compare_ColumnMismatch_IsColumnMismatch()
    {
        var columns = ExpectedAsLive();
        var displayName = columns.FindIndex(c =>
            c.Table == "Participants" && c.Name == "DisplayName");
        columns[displayName] = columns[displayName] with { IsNullable = false };

        var comparison = BaselineSchema.Compare(columns, ExpectedKeys());

        Assert.False(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.Column, comparison.Kind);
    }

    [Fact]
    public void Compare_PrimaryKeyMismatch_IsPrimaryKeyMismatch()
    {
        var keys = ExpectedKeys();
        var participants = keys.FindIndex(k => k.Table == "Participants");
        keys[participants] = new BaselineLivePrimaryKey("Participants", ["Sub"]);

        var comparison = BaselineSchema.Compare(ExpectedAsLive(), keys);

        Assert.False(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.PrimaryKey, comparison.Kind);
    }

    [Fact]
    public void Compare_FrozenBaseline_MatchesItself()
    {
        var comparison = BaselineSchema.Compare(ExpectedAsLive(), ExpectedKeys());
        Assert.True(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.None, comparison.Kind);
    }

    [Fact]
    public void Compare_Pre20Frozen_MatchesItself()
    {
        var comparison = BaselineSchema.ComparePre20(
            ExpectedAsLive(BaselineSchema.Pre20Columns),
            ExpectedKeys(BaselineSchema.Pre20PrimaryKeys));
        Assert.True(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.None, comparison.Kind);
    }

    [Fact]
    public void Decide_Postgres_LowercaseTables_FailsClosed()
    {
        var tables = DatabaseMigrationBaseline.BaselineTableNames
            .Select(t => t.ToLowerInvariant())
            .ToArray();

        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: tables,
            postgres: true);

        Assert.Equal(MigrationBaselineAction.FailClosedUnknownSchema, plan.Action);
        Assert.Equal(DatabaseMigrationBaseline.UnknownSchemaMessage, plan.FailureMessage);
    }

    [Fact]
    public void Compare_Postgres_LowercaseTables_FailsClosed()
    {
        var columns = ExpectedAsPostgresLive()
            .Select(c => c with { Table = c.Table.ToLowerInvariant() })
            .ToList();
        var keys = ExpectedPostgresKeys()
            .Select(k => k with { Table = k.Table.ToLowerInvariant() })
            .ToList();

        var comparison = BaselineSchema.Compare(columns, keys, postgres: true);

        Assert.False(comparison.Matches);
    }

    [Fact]
    public void Compare_Postgres_TextForGuid_FailsClosed()
    {
        var columns = ExpectedAsPostgresLive();
        var id = columns.FindIndex(c => c.Table == "Participants" && c.Name == "Id");
        columns[id] = columns[id] with { StoreType = "text" };

        var comparison = BaselineSchema.Compare(columns, ExpectedPostgresKeys(), postgres: true);

        Assert.False(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.Column, comparison.Kind);
    }

    [Fact]
    public void Compare_Postgres_TimestampWithoutTimeZone_FailsClosed()
    {
        var columns = ExpectedAsPostgresLive();
        var created = columns.FindIndex(c => c.Table == "Participants" && c.Name == "CreatedAt");
        columns[created] = columns[created] with { StoreType = "timestamp without time zone" };

        var comparison = BaselineSchema.Compare(columns, ExpectedPostgresKeys(), postgres: true);

        Assert.False(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.Column, comparison.Kind);
    }

    [Fact]
    public void Compare_Postgres_FrozenSpec_Matches()
    {
        var comparison = BaselineSchema.Compare(
            ExpectedAsPostgresLive(),
            ExpectedPostgresKeys(),
            postgres: true);

        Assert.True(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.None, comparison.Kind);
    }

    [Fact]
    public void Compare_Postgres_FrozenSpec_MatchesNpgsqlGeneratedModel()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=baseline-drift;Username=x")
            .Options;
        using var db = new DealowareDbContext(options);

        var live = new List<BaselineLiveColumn>();
        var keys = new List<BaselineLivePrimaryKey>();
        foreach (var tableGroup in BaselineSchema.Columns.GroupBy(c => c.Table, StringComparer.Ordinal))
        {
            var entity = db.Model.GetEntityTypes()
                .Single(t => string.Equals(t.GetTableName(), tableGroup.Key, StringComparison.Ordinal));
            var store = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var expected in tableGroup)
            {
                var property = entity.GetProperties()
                    .Single(p => string.Equals(p.GetColumnName(store), expected.Name, StringComparison.Ordinal));
                live.Add(new BaselineLiveColumn(
                    tableGroup.Key,
                    expected.Name,
                    CatalogStoreType(property.GetColumnType()),
                    property.IsNullable));
            }

            var pk = entity.FindPrimaryKey()
                ?? throw new InvalidOperationException(tableGroup.Key);
            keys.Add(new BaselineLivePrimaryKey(
                tableGroup.Key,
                pk.Properties.Select(p => p.GetColumnName(store)!).ToList()));
        }

        var comparison = BaselineSchema.Compare(live, keys, postgres: true);
        Assert.True(comparison.Matches);
        Assert.Equal(BaselineSchemaMismatchKind.None, comparison.Kind);

        var versions = live.Where(c => c.Name == "Version").ToList();
        Assert.Equal(4, versions.Count);
        Assert.All(versions, c => Assert.Equal("bigint", c.StoreType));
        Assert.True(BaselineSchema.StoreTypeMatches(BaselineColumnKind.UInt32, "bigint", postgres: true));
        Assert.False(BaselineSchema.StoreTypeMatches(BaselineColumnKind.Int32, "bigint", postgres: true));
        Assert.True(BaselineSchema.StoreTypeMatches(BaselineColumnKind.UInt32, "INTEGER", postgres: false));
    }

    [Fact]
    public async Task Apply_FreshDatabase_CreatesSchemaAndRecordsBaseline()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Fresh;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        await using (var db = new DealowareDbContext(options))
        {
            Assert.Equal(0, await db.Participants.CountAsync());
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(AppliedAfterMigrate, applied);
            Assert.Equal(DatabaseMigrationBaseline.BaselineMigrationId, applied[0]);
            Assert.Contains(DatabaseMigrationBaseline.UpdatedAtMigrationId, applied);
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
    }

    [Fact]
    public async Task Apply_BootstrappedDatabaseWithoutHistory_StampsBaselineWithoutRecreating()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Bootstrapped;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText =
                """
                INSERT INTO "Participants"
                  ("Id", "Sub", "DisplayName", "LoginEmail", "ContactEmail", "CreatedAt", "IsActive", "DeletedAt", "Version")
                VALUES
                  ('00000000-0000-0000-0000-0000000000b1', 'baseline-keep', 'baseline-keep', NULL, NULL, '2026-01-01T00:00:00+00:00', 1, NULL, 0);
                """;
            await insert.ExecuteNonQueryAsync();
        }

        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        await using (var db = new DealowareDbContext(options))
        {
            Assert.Equal(1, await db.Participants.CountAsync());
            Assert.Equal("baseline-keep", (await db.Participants.SingleAsync()).DisplayName);
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(AppliedAfterMigrate, applied);
            Assert.Contains(DatabaseMigrationBaseline.UpdatedAtMigrationId, applied);
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
    }

    [Fact]
    public async Task Apply_AlreadyMigratedDatabase_IsIdempotent()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_AlreadyMigrated;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            db.Participants.Add(Participant.Create("keep-me"));
            await db.SaveChangesAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            var ex = await Record.ExceptionAsync(() => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));
            Assert.Null(ex);
            Assert.Equal(1, await db.Participants.CountAsync());
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(AppliedAfterMigrate, applied);
            Assert.Contains(DatabaseMigrationBaseline.UpdatedAtMigrationId, applied);
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
    }

    [Fact]
    public async Task Apply_BootstrappedThenMigrateTwice_IsIdempotent()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_StampTwice;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        Assert.DoesNotContain("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(AppliedAfterMigrate, applied);
            Assert.Contains(DatabaseMigrationBaseline.UpdatedAtMigrationId, applied);
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
    }

    [Fact]
    public async Task Apply_EnsureCreatedWithUpdatedAtAndNoHistory_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_CurrentUpdatedAt;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);

        await using var apply = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(apply));

        Assert.Equal(DatabaseMigrationBaseline.SchemaMismatchMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_PartialSchema_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Partial;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE Participants (Id TEXT PRIMARY KEY);";
            await cmd.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Equal(DatabaseMigrationBaseline.PartialSchemaMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_UnknownSchema_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Unknown;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE NotADealowareTable (Id INTEGER);";
            await cmd.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Equal(DatabaseMigrationBaseline.UnknownSchemaMessage, ex.Message);
        AssertSafe(ex.Message);
    }

    [Fact]
    public async Task Apply_ColumnMismatch_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_ColumnMismatch;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "ALTER TABLE Participants DROP COLUMN DisplayName;";
            await cmd.ExecuteNonQueryAsync();
        }

        await using var apply = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(apply));

        Assert.Equal(DatabaseMigrationBaseline.SchemaMismatchMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_PrimaryKeyMismatch_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_PkMismatch;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                DROP TABLE Participants;
                CREATE TABLE Participants (
                  Id TEXT NOT NULL,
                  Sub TEXT NOT NULL,
                  DisplayName TEXT,
                  LoginEmail TEXT,
                  ContactEmail TEXT,
                  CreatedAt TEXT NOT NULL,
                  IsActive INTEGER NOT NULL,
                  DeletedAt TEXT,
                  Version INTEGER NOT NULL
                );
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        await using var apply = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(apply));

        Assert.Equal(DatabaseMigrationBaseline.SchemaMismatchMessage, ex.Message);
        AssertSafe(ex.Message);
    }

    [Fact]
    public async Task Apply_ExtraTable_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_ExtraTable;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE ExtraJunk (Id TEXT);";
            await cmd.ExecuteNonQueryAsync();
        }

        await using var apply = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(apply));

        Assert.Equal(DatabaseMigrationBaseline.UnknownSchemaMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_Pre20Schema_StampsBaselineOnlyThenAppliesAdmin()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Pre20;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateTablesFromSpecAsync(
            connection,
            BaselineSchema.Pre20Columns,
            BaselineSchema.Pre20PrimaryKeys);
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText =
                """
                INSERT INTO "Participants"
                  ("Id", "Sub", "DisplayName", "LoginEmail", "ContactEmail", "CreatedAt", "IsActive")
                VALUES
                  ('00000000-0000-0000-0000-000000000001', 'pre20-keep', 'pre20-keep', NULL, NULL, '2026-01-01T00:00:00+00:00', 1);
                """;
            await insert.ExecuteNonQueryAsync();
        }

        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        await using (var db = new DealowareDbContext(options))
        {
            Assert.Equal(1, await db.Participants.CountAsync());
            Assert.Equal("pre20-keep", (await db.Participants.SingleAsync()).DisplayName);
            Assert.Equal(0, await db.AdminSessions.CountAsync());
            Assert.Equal(0, await db.AdminAuditLog.CountAsync());
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(AppliedAfterMigrate, applied);
            Assert.Contains(DatabaseMigrationBaseline.UpdatedAtMigrationId, applied);
        }

        Assert.Contains("UpdatedAt", await ListSqliteColumnsAsync(connection, "Participants"));
    }

    [Fact]
    public async Task Apply_AdminTablesWithoutSoftDelete_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_AdminNoSoft;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var columns = BaselineSchema.Pre20Columns
            .Concat(BaselineSchema.Columns.Where(c =>
                c.Table is "AdminSessions" or "AdminAuditLog"))
            .ToList();
        var keys = BaselineSchema.Pre20PrimaryKeys
            .Concat(BaselineSchema.PrimaryKeys.Where(k =>
                k.Table is "AdminSessions" or "AdminAuditLog"))
            .ToList();
        await CreateTablesFromSpecAsync(connection, columns, keys);

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Equal(DatabaseMigrationBaseline.SchemaMismatchMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_SoftDeleteWithoutAdminTables_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_SoftNoAdmin;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var columns = BaselineSchema.Pre20Columns
            .Concat(BaselineSchema.Columns.Where(c => c.Name is "DeletedAt" or "Version"))
            .ToList();
        await CreateTablesFromSpecAsync(connection, columns, BaselineSchema.Pre20PrimaryKeys);

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Equal(DatabaseMigrationBaseline.SchemaMismatchMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Apply_Pre20PlusOneAdminTable_FailsClosed()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_OneAdmin;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var columns = BaselineSchema.Pre20Columns
            .Concat(BaselineSchema.Columns.Where(c => c.Table == "AdminSessions"))
            .ToList();
        var keys = BaselineSchema.Pre20PrimaryKeys
            .Concat(BaselineSchema.PrimaryKeys.Where(k => k.Table == "AdminSessions"))
            .ToList();
        await CreateTablesFromSpecAsync(connection, columns, keys);

        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Equal(DatabaseMigrationBaseline.PartialSchemaMessage, ex.Message);
        AssertSafe(ex.Message);
        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyStartupSchemaAsync_Development_DoesNotWriteMigrationsHistory()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_StartupNoHistory;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using var db = new DealowareDbContext(options);
        await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, "Development");

        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FirstAssemblyMigration_IsTheBaselineId()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new DealowareDbContext(options);
        var migrations = db.Database.GetMigrations().ToList();
        Assert.Equal(DatabaseMigrationBaseline.BaselineMigrationId, migrations[0]);
        Assert.Equal(DatabaseMigrationBaseline.AdminTablesMigrationId, migrations[1]);
        Assert.Equal(DatabaseMigrationBaseline.UpdatedAtMigrationId, migrations[2]);
        Assert.Equal(AdminAuditLogAppendOnly.MigrationId, migrations[3]);
    }

    [Fact]
    public void Program_RuntimePath_DoesNotWriteMigrationsHistory()
    {
        var source = ReadRepoFile("src/Dealoware.Api/Program.cs");
        Assert.DoesNotContain("ApplyMigrationsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DatabaseMigrationBaseline", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StampBaseline", source, StringComparison.Ordinal);
        Assert.DoesNotContain(DatabaseMigrationBaseline.HistoryTableName, source, StringComparison.Ordinal);
        Assert.Contains("ApplyStartupSchemaAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseMigrateCommand_IsTheOnlyEntrypointThatAppliesMigrations()
    {
        var source = ReadRepoFile("src/Dealoware.Api/DatabaseMigrateCommand.cs");
        Assert.Contains("ApplyMigrationsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimeLoginPrivilegeGuard", source, StringComparison.Ordinal);
    }

    private static List<BaselineLiveColumn> ExpectedAsLive()
        => ExpectedAsLive(BaselineSchema.Columns);

    private static List<BaselineLiveColumn> ExpectedAsLive(IReadOnlyList<BaselineColumnSpec> columns)
        => columns
            .Select(c => new BaselineLiveColumn(c.Table, c.Name, StoreTypeFor(c.Kind), c.IsNullable))
            .ToList();

    private static List<BaselineLivePrimaryKey> ExpectedKeys()
        => ExpectedKeys(BaselineSchema.PrimaryKeys);

    private static List<BaselineLivePrimaryKey> ExpectedKeys(IReadOnlyList<BaselinePrimaryKeySpec> keys)
        => keys
            .Select(k => new BaselineLivePrimaryKey(k.Table, k.Columns))
            .ToList();

    private static List<BaselineLiveColumn> ExpectedAsPostgresLive()
        => BaselineSchema.Columns
            .Select(c => new BaselineLiveColumn(c.Table, c.Name, PostgresStoreTypeFor(c.Kind), c.IsNullable))
            .ToList();

    private static List<BaselineLivePrimaryKey> ExpectedPostgresKeys()
        => ExpectedKeys();

    private static string StoreTypeFor(BaselineColumnKind kind)
        => kind switch
        {
            BaselineColumnKind.Guid or BaselineColumnKind.String or BaselineColumnKind.DateTimeOffset
                or BaselineColumnKind.Decimal => "TEXT",
            BaselineColumnKind.Boolean or BaselineColumnKind.Int32 or BaselineColumnKind.Int64
                or BaselineColumnKind.UInt32 => "INTEGER",
            _ => "TEXT"
        };

    /// <summary>
    /// Npgsql <c>GetColumnType()</c> includes facets (<c>character varying(256)</c>,
    /// <c>numeric(18,4)</c>). PostgreSQL <c>information_schema.data_type</c>
    /// and the frozen map use the catalog name without facets.
    /// </summary>
    private static string CatalogStoreType(string columnType)
    {
        var t = columnType.Trim().ToLowerInvariant();
        var paren = t.IndexOf('(', StringComparison.Ordinal);
        return paren < 0 ? t : t[..paren];
    }

    private static string PostgresStoreTypeFor(BaselineColumnKind kind)
        => kind switch
        {
            BaselineColumnKind.Guid => "uuid",
            BaselineColumnKind.String => "character varying",
            BaselineColumnKind.DateTimeOffset => "timestamp with time zone",
            BaselineColumnKind.Boolean => "boolean",
            BaselineColumnKind.Int32 => "integer",
            BaselineColumnKind.Int64 => "bigint",
            BaselineColumnKind.UInt32 => "bigint",
            BaselineColumnKind.Decimal => "numeric",
            _ => "text"
        };

    private static Task CreateFrozen15TableSchemaAsync(SqliteConnection connection)
        => CreateTablesFromSpecAsync(connection, BaselineSchema.Columns, BaselineSchema.PrimaryKeys);

    private static async Task<IReadOnlyList<string>> ListSqliteColumnsAsync(
        SqliteConnection connection,
        string table)
    {
        var columns = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(1));
        return columns;
    }

    private static async Task CreateTablesFromSpecAsync(
        SqliteConnection connection,
        IReadOnlyList<BaselineColumnSpec> columns,
        IReadOnlyList<BaselinePrimaryKeySpec> keys)
    {
        foreach (var tableGroup in columns.GroupBy(c => c.Table, StringComparer.Ordinal))
        {
            var pk = keys.Single(k => string.Equals(k.Table, tableGroup.Key, StringComparison.Ordinal));
            var defs = tableGroup
                .Select(c =>
                    $"\"{c.Name}\" {StoreTypeFor(c.Kind)}{(c.IsNullable ? "" : " NOT NULL")}");
            var sql =
                $"CREATE TABLE \"{tableGroup.Key}\" ({string.Join(", ", defs)}, " +
                $"PRIMARY KEY ({string.Join(", ", pk.Columns.Select(n => $"\"{n}\""))}))";
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<IReadOnlyList<string>> ListSqliteTablesAsync(SqliteConnection connection)
    {
        var names = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static void AssertSafe(string message)
    {
        Assert.DoesNotContain("Host=", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("arn:", message, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}

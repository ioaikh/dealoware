using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Tests;

/// <summary>
/// H3: migrate one-shot baselines EnsureCreated databases that have no
/// __EFMigrationsHistory row. API startup must not stamp or write history.
/// </summary>
public class DatabaseMigrationBaselineTests
{
    private static readonly string[] Empty = [];

    private static IReadOnlyList<string> AssemblyWithBaseline =>
        [DatabaseMigrationBaseline.BaselineMigrationId, "20261006000100_AddAdminTablesAndSoftDelete"];

    [Fact]
    public void Decide_FreshEmpty_AppliesMigrations()
    {
        var plan = DatabaseMigrationBaseline.Decide(AssemblyWithBaseline, Empty, Empty);

        Assert.Equal(MigrationBaselineAction.ApplyMigrations, plan.Action);
        Assert.Null(plan.FailureMessage);
    }

    [Fact]
    public void Decide_CompleteSchemaNoHistory_StampsBaseline()
    {
        var plan = DatabaseMigrationBaseline.Decide(
            AssemblyWithBaseline,
            appliedMigrations: Empty,
            userTables: DatabaseMigrationBaseline.BaselineTableNames);

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
            Assert.Contains(DatabaseMigrationBaseline.BaselineMigrationId, applied);
            Assert.Equal(DatabaseMigrationBaseline.BaselineMigrationId, applied[0]);
        }
    }

    [Fact]
    public async Task Apply_BootstrappedDatabaseWithoutHistory_StampsBaselineWithoutRecreating()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_Bootstrapped;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, "Development");
            db.Participants.Add(Participant.Create("baseline-keep"));
            await db.SaveChangesAsync();
        }

        Assert.DoesNotContain(
            DatabaseMigrationBaseline.HistoryTableName,
            await ListSqliteTablesAsync(connection),
            StringComparer.OrdinalIgnoreCase);

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        await using (var db = new DealowareDbContext(options))
        {
            Assert.Equal(1, await db.Participants.CountAsync());
            Assert.Equal("baseline-keep", (await db.Participants.SingleAsync()).DisplayName);
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal([DatabaseMigrationBaseline.BaselineMigrationId], applied);
        }
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
            Assert.Equal([DatabaseMigrationBaseline.BaselineMigrationId], applied);
        }
    }

    [Fact]
    public async Task Apply_BootstrappedThenMigrateTwice_IsIdempotent()
    {
        await using var connection = new SqliteConnection("Data Source=Baseline_StampTwice;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal([DatabaseMigrationBaseline.BaselineMigrationId], applied);
        }
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
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

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
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

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
                  IsActive INTEGER NOT NULL
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
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

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
        => BaselineSchema.Columns
            .Select(c => new BaselineLiveColumn(c.Table, c.Name, StoreTypeFor(c.Kind), c.IsNullable))
            .ToList();

    private static List<BaselineLivePrimaryKey> ExpectedKeys()
        => BaselineSchema.PrimaryKeys
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
            BaselineColumnKind.Boolean or BaselineColumnKind.Int32 or BaselineColumnKind.Int64 => "INTEGER",
            _ => "TEXT"
        };

    private static string PostgresStoreTypeFor(BaselineColumnKind kind)
        => kind switch
        {
            BaselineColumnKind.Guid => "uuid",
            BaselineColumnKind.String => "character varying",
            BaselineColumnKind.DateTimeOffset => "timestamp with time zone",
            BaselineColumnKind.Boolean => "boolean",
            BaselineColumnKind.Int32 => "integer",
            BaselineColumnKind.Int64 => "bigint",
            BaselineColumnKind.Decimal => "numeric",
            _ => "text"
        };

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

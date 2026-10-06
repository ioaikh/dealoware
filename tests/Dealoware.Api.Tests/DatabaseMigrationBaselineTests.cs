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

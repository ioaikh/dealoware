using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Dealoware.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dealoware.Api.Tests;

/// <summary>
/// T04 migration 20261126000500 (UXR-A40 recovery-code banner, TD-ADM-UI-auth-16):
/// a new table only. Rows written by the previous release survive it, the
/// previous release's queries still run after it, it rolls back cleanly, the
/// table is not in the frozen stamp lists, and its rows go with their session.
/// </summary>
public class AdminSessionRecoveryNoticeMigrationTests
{
    private const string OwnerEmail = "owner@example.test";
    private const string Table = "AdminSessionRecoveryNotices";

    [Fact]
    public async Task UiAuth16_Migration500_KeepsPreviousReleaseRowsAndQueries_AndGoesWithItsSession()
    {
        await using var connection = new SqliteConnection($"Data Source=NoticeMig_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        // The previous release: every migration up to and including the step-ups.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(AdminSessionStepUps.MigrationId);
            Assert.DoesNotContain(Table, await ListTablesAsync(connection));
        }

        var now = DateTimeOffset.UtcNow;
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", now);
        session.MarkTotpVerified();
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminSessions.Add(session);
            db.AdminSessionStepUps.Add(AdminSessionStepUp.Create(session.Id, now));
            await db.SaveChangesAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync();
            Assert.Contains(AdminSessionRecoveryNotices.MigrationId, await db.Database.GetAppliedMigrationsAsync());
        }

        Assert.Equal(
            new[] { "CreatedAt", "SessionId" },
            (await ListColumnsAsync(connection, Table)).Order(StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(Table, DatabaseMigrationBaseline.BaselineTableNames);
        Assert.DoesNotContain(Table, DatabaseMigrationBaseline.Pre20TableNames);
        Assert.DoesNotContain(BaselineSchema.Columns, c => c.Table == Table);

        // The previous release's rows and reads, unchanged.
        await using (var db = new DealowareDbContext(options))
        {
            var loaded = await new AdminSessionRepository(db).GetByIdAsync(session.Id);
            Assert.NotNull(loaded);
            Assert.True(loaded.IsValid(now));
            Assert.Equal(1, await db.AdminSessionStepUps.CountAsync());
        }

        // Its rows belong to a session and go with it; one with no session is refused.
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminSessionRecoveryNotices.Add(AdminSessionRecoveryNotice.Create(session.Id, now));
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.AdminSessionRecoveryNotices.CountAsync());
            await db.AdminSessions.Where(s => s.Id == session.Id).ExecuteDeleteAsync();
            Assert.Equal(0, await db.AdminSessionRecoveryNotices.CountAsync());

            db.AdminSessionRecoveryNotices.Add(AdminSessionRecoveryNotice.Create(Guid.NewGuid(), now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // Down drops only the new table.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(AdminSessionStepUps.MigrationId);
        }

        var tables = await ListTablesAsync(connection);
        Assert.DoesNotContain(Table, tables);
        Assert.Contains("AdminSessionStepUps", tables);
        Assert.Contains("AdminSessions", tables);
    }

    [Fact]
    public void UiAuth16_Migration500_IsANewTableOnly()
    {
        var source = ReadRepoFile("src/Dealoware.Infrastructure/Persistence/Migrations/20261126000500_AdminSessionRecoveryNotices.cs");
        Assert.Contains("CreateTable(", source, StringComparison.Ordinal);
        Assert.Contains("onDelete: ReferentialAction.Cascade", source, StringComparison.Ordinal);
        foreach (var change in new[] { "AddColumn", "AlterColumn", "DropColumn", "RenameColumn", ".Sql(" })
        {
            Assert.DoesNotContain(change, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UiAuth16_ANoticeBelongsToASession()
    {
        Assert.Throws<ArgumentException>(() => AdminSessionRecoveryNotice.Create(Guid.Empty, DateTimeOffset.UtcNow));
        var id = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        var notice = AdminSessionRecoveryNotice.Create(id, at);
        Assert.Equal(id, notice.SessionId);
        Assert.Equal(at, notice.CreatedAt);
    }

    private static async Task<IReadOnlyList<string>> ListTablesAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    private static async Task<IReadOnlyList<string>> ListColumnsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(1));
        return names;
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}

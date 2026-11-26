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
/// T04 migration 20261126000400 (S-A12 step-up, TD-ADM-174): a new table only.
/// Rows written by the previous release survive it, the previous release's
/// queries still run after it, it rolls back cleanly, the table is not in the
/// frozen stamp lists, and its rows go with their session.
/// </summary>
public class AdminSessionStepUpMigrationTests
{
    private const string OwnerEmail = "owner@example.test";

    [Fact]
    public async Task TdAdm174_Migration400_KeepsPreviousReleaseRowsAndQueries()
    {
        await using var connection = new SqliteConnection($"Data Source=StepUpMig_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        // The previous release: every migration up to and including bootstrap tokens.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(DatabaseMigrationBaseline.BootstrapTokensMigrationId);
            Assert.DoesNotContain("AdminSessionStepUps", await ListTablesAsync(connection));
        }

        var now = DateTimeOffset.UtcNow;
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", now);
        session.MarkTotpVerified();
        var account = AdminCoreOwnerAccount.Create(OwnerEmail, now);
        account.SetPasswordHash("pbkdf2-sha512$1$AAAA$AAAA");
        account.CompleteEnrollment("cipher-placeholder", now: now);
        var code = AdminRecoveryCode.Create(account.Id, "HASH-PLACEHOLDER", now);
        var audit = AdminAuditEntry.CreateAuthEvent(AdminAuditActions.LoginSuccess, OwnerEmail, "hmac", timestamp: now);
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminSessions.Add(session);
            db.AdminCoreOwnerAccounts.Add(account);
            db.AdminRecoveryCodes.Add(code);
            db.AdminAuditLog.Add(audit);
            await db.SaveChangesAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync();
            Assert.Contains(AdminSessionStepUps.MigrationId, await db.Database.GetAppliedMigrationsAsync());
        }

        Assert.Equal(
            new[] { "CodesIssuedAt", "PendingTotpSecretCipher", "SessionId", "SteppedUpAt" },
            (await ListColumnsAsync(connection, "AdminSessionStepUps")).Order(StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain("AdminSessionStepUps", DatabaseMigrationBaseline.BaselineTableNames);
        Assert.DoesNotContain("AdminSessionStepUps", DatabaseMigrationBaseline.Pre20TableNames);
        Assert.DoesNotContain(BaselineSchema.Columns, c => c.Table == "AdminSessionStepUps");

        // The previous release's columns, exactly, still answer with the same rows.
        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM \"AdminSessions\" WHERE \"Id\" IS NOT NULL AND \"Email\" IS NOT NULL " +
            "AND \"CreatedAt\" IS NOT NULL AND \"LastActivityAt\" IS NOT NULL AND \"AbsoluteExpiresAt\" IS NOT NULL " +
            "AND \"TotpVerified\" = 1 AND \"IpHmac\" = 'testhmac-not-an-ip'"));
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM \"AdminRecoveryCodes\" WHERE \"UsedAt\" IS NULL"));
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM \"AdminAuditLog\""));

        // The previous release's own reads and conditional writes run unchanged.
        await using (var db = new DealowareDbContext(options))
        {
            var sessions = new AdminSessionRepository(db);
            var loaded = await sessions.GetByIdAsync(session.Id);
            Assert.NotNull(loaded);
            Assert.True(loaded.IsValid(now));

            var accounts = new AdminCoreOwnerAccountRepository(db);
            Assert.True((await accounts.GetByEmailAsync(OwnerEmail))!.IsTotpEnrolled);
            Assert.True(await accounts.TryRecordTotpTimestepAsync(account.Id, 42));
            Assert.True(await accounts.TryRedeemRecoveryCodeAsync(account.Id, "HASH-PLACEHOLDER", now));
        }

        // Its rows belong to a session and go with it.
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminSessionStepUps.Add(AdminSessionStepUp.Create(session.Id, now));
            await db.SaveChangesAsync();
            await db.AdminSessions.Where(s => s.Id == session.Id).ExecuteDeleteAsync();
            Assert.Equal(0, await db.AdminSessionStepUps.CountAsync());

            db.AdminSessionStepUps.Add(AdminSessionStepUp.Create(Guid.NewGuid(), now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // Down drops only the new table.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(DatabaseMigrationBaseline.BootstrapTokensMigrationId);
        }

        Assert.DoesNotContain("AdminSessionStepUps", await ListTablesAsync(connection));
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM \"AdminCoreOwnerAccounts\""));
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM \"AdminRecoveryCodes\""));
    }

    [Fact]
    public void TdAdm174_Migration400_IsANewTableOnly()
    {
        var source = ReadRepoFile("src/Dealoware.Infrastructure/Persistence/Migrations/20261126000400_AdminSessionStepUps.cs");
        Assert.Contains("CreateTable(", source, StringComparison.Ordinal);
        Assert.Contains("onDelete: ReferentialAction.Cascade", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AlterColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DropColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RenameColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Sql(", source, StringComparison.Ordinal);
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
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

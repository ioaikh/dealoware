using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security R2: apply the append-only migration on PostgreSQL and prove
/// INSERT works while UPDATE / DELETE / TRUNCATE raise.
/// Runs in the existing PostgreSQL Sec Tests job (name matches SecR / AdminAuditPostgres).
/// </summary>
public class AdminAuditPostgresAppendOnlyTests
{
    [Fact]
    public async Task SecR2_AdminAuditPostgres_InsertWorks_UpdateDeleteTruncateRaise()
    {
        Assert.True(
            IsolatedWebApplicationFactory.TryBuildTestPostgresConnectionString(out var baseCs),
            "PostgreSQL DB_HOST is required for AdminAuditPostgres append-only proof.");

        const string dbName = "dealoware_ci_audit_appendonly";
        var adminCsb = new NpgsqlConnectionStringBuilder(baseCs);
        var testCsb = new NpgsqlConnectionStringBuilder(baseCs) { Database = dbName };

        await using (var admin = new NpgsqlConnection(adminCsb.ConnectionString))
        {
            await admin.OpenAsync();
            await using var exists = admin.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
            exists.Parameters.AddWithValue("n", dbName);
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = $"""CREATE DATABASE "{dbName}" """;
                await create.ExecuteNonQueryAsync();
            }
        }

        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseNpgsql(testCsb.ConnectionString)
            .Options;

        await using (var migrate = new DealowareDbContext(options))
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(migrate);

        Guid entryId;
        await using (var db = new DealowareDbContext(options))
        {
            var entry = AdminAuditEntry.CreateAuthEvent(
                AdminAuditActions.LoginSuccess,
                "io@aiknowhow.com",
                "hmac-not-an-ip");
            db.AdminAuditLog.Add(entry);
            await db.SaveChangesAsync();
            entryId = entry.Id;
            Assert.Equal(1, await db.AdminAuditLog.CountAsync(e => e.Id == entryId));
        }

        await AssertAppendOnly(
            options,
            $"""UPDATE "AdminAuditLog" SET "Action" = 'tamper' WHERE "Id" = '{entryId}'""");
        await AssertAppendOnly(
            options,
            $"""DELETE FROM "AdminAuditLog" WHERE "Id" = '{entryId}'""");
        await AssertAppendOnly(options, """TRUNCATE TABLE "AdminAuditLog" """);

        await using (var db = new DealowareDbContext(options))
        {
            var stored = await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == entryId);
            Assert.Equal(AdminAuditActions.LoginSuccess, stored.Action);
        }
    }

    private static async Task AssertAppendOnly(DbContextOptions<DealowareDbContext> options, string sql)
    {
        await using var db = new DealowareDbContext(options);
        var ex = await Record.ExceptionAsync(() => db.Database.ExecuteSqlRawAsync(sql));
        Assert.NotNull(ex);
        Assert.Contains("AdminAuditLog is append-only", ex.Message, StringComparison.Ordinal);
    }
}

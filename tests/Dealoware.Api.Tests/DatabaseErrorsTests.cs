using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// Only a unique or primary-key violation counts as "someone else wrote this
/// row first". Every other database fault must stay a fault.
/// </summary>
public class DatabaseErrorsTests
{
    [Fact]
    public async Task IsUniqueViolation_Sqlite_TrueForPrimaryKeyAndUniqueIndex_FalseForOtherConstraints()
    {
        await using var connection = new SqliteConnection($"Data Source=DbErrors_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using (var db = new DealowareDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var hash = AdminBootstrapToken.HashRaw(Guid.NewGuid().ToString("N"));

        // Primary key: AdminCredentials.Email.
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminCredentials.Add(AdminCredential.Create("owner@example.test", hash, now));
            await db.SaveChangesAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            db.AdminCredentials.Add(AdminCredential.Create("owner@example.test", hash, now));
            var primaryKey = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.True(DatabaseErrors.IsUniqueViolation(primaryKey));
        }

        // Unique index: AdminBootstrapTokens.TokenHash.
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminBootstrapTokens.Add(AdminBootstrapToken.Create("owner@example.test", hash, now));
            await db.SaveChangesAsync();
            db.AdminBootstrapTokens.Add(AdminBootstrapToken.Create("owner@example.test", hash, now));
            var uniqueIndex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.True(DatabaseErrors.IsUniqueViolation(uniqueIndex));
        }

        // NOT NULL is a constraint fault too, but not "someone else wrote this row".
        await using (var db = new DealowareDbContext(options))
        {
            var notNull = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"AdminCredentials\" (\"Email\", \"PasswordHash\", \"PasswordUpdatedAt\") "
                + "VALUES ('other@example.test', NULL, '2030-01-01T00:00:00.0000000Z')"));
            Assert.False(DatabaseErrors.IsUniqueViolation(notNull));
        }
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, true)]
    [InlineData(PostgresErrorCodes.DeadlockDetected, false)]
    [InlineData(PostgresErrorCodes.SerializationFailure, false)]
    [InlineData(PostgresErrorCodes.NotNullViolation, false)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, false)]
    public void IsUniqueViolation_Postgres_OnlySqlState23505(string sqlState, bool expected)
    {
        var postgres = new PostgresException("fault", "ERROR", "ERROR", sqlState);

        Assert.Equal(expected, DatabaseErrors.IsUniqueViolation(postgres));
        Assert.Equal(expected, DatabaseErrors.IsUniqueViolation(new DbUpdateException("save failed", postgres)));
    }

    [Fact]
    public void IsUniqueViolation_FalseForFaultsThatAreNotFromTheDatabase()
    {
        Assert.False(DatabaseErrors.IsUniqueViolation(new InvalidOperationException("UNIQUE constraint failed")));
        Assert.False(DatabaseErrors.IsUniqueViolation(new DbUpdateException("unique")));
        Assert.False(DatabaseErrors.IsUniqueViolation(new TimeoutException()));
    }
}

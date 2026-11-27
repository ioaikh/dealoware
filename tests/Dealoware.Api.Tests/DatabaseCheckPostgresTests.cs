using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// T09c, <c>dbcheck</c> on a real PostgreSQL 16 (the CI PostgreSQL job; skipped without
/// <c>DB_HOST</c>). Each test builds two new databases with the migrations applied, fills them
/// with the same rows, and runs <see cref="DatabaseCheck"/> on each: the restore test's
/// comparison of the live database with its restored copy. The CI server has no TLS, so these
/// call <see cref="DatabaseCheck"/> directly; the command's arguments and refusals are in
/// <see cref="DatabaseCheckCommandTests"/>.
/// </summary>
public class DatabaseCheckPostgresTests
{
    private const string DatabaseA = "dealoware_t09c_check_a";
    private const string DatabaseB = "dealoware_t09c_check_b";

    private static readonly DateTimeOffset Cutoff = new(2026, 12, 1, 10, 0, 0, TimeSpan.Zero);

    // Values that must never appear in the output.
    private static readonly Guid ParticipantOld = Guid.Parse("0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0");
    private static readonly Guid ParticipantNewest = Guid.Parse("11112222-3333-4444-5555-666677778888");
    private static readonly Guid ParticipantLater = Guid.Parse("99998888-7777-6666-5555-444433332222");
    private static readonly Guid AuditEntry = Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");
    private const string MarkerEmail = "marker.person@example.invalid";
    private const string MarkerName = "Marker Display Name 7731";
    private const string MarkerSub = "marker-sub-4f9a";
    private const string MarkerIpHmac = "marker-ip-hmac-c0ffee";

    [PostgresOnlyFact]
    public async Task DbCheck_AWriteInsideItsTransaction_IsRefused()
    {
        await NewDatabasesAsync();
        await using var connection = Open(DatabaseA);
        var before = await CountAsync(DatabaseA, "Participants");

        await using (var tx = await DatabaseCheck.BeginReadOnlyAsync(connection))
        {
            foreach (var sql in new[]
                     {
                         "INSERT INTO \"Participants\" (\"Id\", \"Sub\", \"CreatedAt\", \"IsActive\") " +
                         "VALUES ('00000000-0000-0000-0000-000000000001', 'write-attempt', now(), true)",
                         "UPDATE \"Participants\" SET \"DisplayName\" = 'changed'",
                         "DELETE FROM \"Participants\"",
                         "CREATE TABLE dbcheck_write_attempt (id int)",
                     })
            {
                await using var save = new NpgsqlCommand("SAVEPOINT attempt", connection, tx);
                await save.ExecuteNonQueryAsync();
                await using var write = new NpgsqlCommand(sql, connection, tx);
                var ex = await Assert.ThrowsAsync<PostgresException>(() => write.ExecuteNonQueryAsync());
                Assert.Equal(PostgresErrorCodes.ReadOnlySqlTransaction, ex.SqlState);
                await using var back = new NpgsqlCommand("ROLLBACK TO SAVEPOINT attempt", connection, tx);
                await back.ExecuteNonQueryAsync();
            }
        }

        Assert.Equal(before, await CountAsync(DatabaseA, "Participants"));

        // The check itself runs in that transaction and leaves the database as it was.
        var result = await DatabaseCheck.RunAsync(connection, Cutoff);
        Assert.Equal(before, await CountAsync(DatabaseA, "Participants"));
        Assert.Equal(2, result.Tables.Single(t => t.Table == "Participants").Rows);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_TwoDatabasesWithEqualData_GiveEqualOutput()
    {
        await NewDatabasesAsync();
        // A row created after the cutoff exists only in A (the live database); the copy,
        // restored to the cutoff, does not have it. The output up to the cutoff is the same.
        await ExecAsync(DatabaseA,
            $"INSERT INTO \"Participants\" (\"Id\", \"Sub\", \"CreatedAt\", \"IsActive\") " +
            $"VALUES ('{ParticipantLater}', 'later-sub', '2026-12-01T10:05:00Z', true)");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        Assert.Equal(a.ToJson(), b.ToJson());
        Assert.Equal(DatabaseCheck.CreatedColumns.Count, a.Tables.Count);
        Assert.Equal("20261126000600_AdminDeleteConfirmTokens", a.Migration);
        var participants = a.Tables.Single(t => t.Table == "Participants");
        Assert.Equal(2, participants.Rows);
        Assert.NotNull(participants.Newest);
        Assert.Equal(1, a.Tables.Single(t => t.Table == "AdminAuditLog").Rows);
        Assert.Null(a.Tables.Single(t => t.Table == "Offers").Newest);

        // The same database read twice gives the same output too.
        Assert.Equal(a.ToJson(), (await CheckAsync(DatabaseA)).ToJson());
    }

    [PostgresOnlyFact]
    public async Task DbCheck_OneChangedField_ChangesTheHash()
    {
        await NewDatabasesAsync();
        await ExecAsync(DatabaseB, $"UPDATE \"Participants\" SET \"IsActive\" = false WHERE \"Id\" = '{ParticipantNewest}'");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        var pa = a.Tables.Single(t => t.Table == "Participants");
        var pb = b.Tables.Single(t => t.Table == "Participants");
        Assert.Equal(pa.Rows, pb.Rows);
        Assert.Equal(pa.Newest!.KeySha256, pb.Newest!.KeySha256);
        Assert.NotEqual(pa.Newest.RowSha256, pb.Newest.RowSha256);
        Assert.NotEqual(a.ToJson(), b.ToJson());

        // Nothing else differs.
        foreach (var (ta, tb) in a.Tables.Zip(b.Tables).Where(p => p.First.Table != "Participants"))
            Assert.Equal(ta, tb);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_Output_HoldsNoColumnValue()
    {
        await NewDatabasesAsync();

        var json = (await CheckAsync(DatabaseA)).ToJson();

        foreach (var value in new[]
                 {
                     ParticipantOld.ToString(), ParticipantNewest.ToString(), AuditEntry.ToString(),
                     ParticipantOld.ToString("N"), ParticipantNewest.ToString("N"), AuditEntry.ToString("N"),
                     MarkerEmail, MarkerName, MarkerSub, MarkerIpHmac, "marker", "2026-11-",
                 })
        {
            Assert.DoesNotContain(value, json, StringComparison.OrdinalIgnoreCase);
        }

        // Every string in it is a fixed word, a table or column name, the cutoff, the
        // migration ID or a SHA-256; every number is a count.
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(["check", "cutoff", "migration", "tables"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal("2026-12-01T10:00:00.0000000Z", doc.RootElement.GetProperty("cutoff").GetString());
        foreach (var table in doc.RootElement.GetProperty("tables").EnumerateArray())
        {
            Assert.Equal(["table", "createdColumn", "rows", "newest"], table.EnumerateObject().Select(p => p.Name));
            Assert.True(DatabaseCheck.CreatedColumns.ContainsKey(table.GetProperty("table").GetString()!));
            var newest = table.GetProperty("newest");
            if (newest.ValueKind == JsonValueKind.Null)
                continue;
            Assert.Equal(["keySha256", "rowSha256"], newest.EnumerateObject().Select(p => p.Name));
            foreach (var hash in newest.EnumerateObject())
                Assert.Matches(new Regex("^[0-9a-f]{64}$"), hash.Value.GetString()!);
        }
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static async Task<DatabaseCheckResult> CheckAsync(string database)
    {
        await using var connection = Open(database);
        return await DatabaseCheck.RunAsync(connection, Cutoff);
    }

    /// <summary>Two new databases with the migrations applied and the same rows in both.</summary>
    private static async Task NewDatabasesAsync()
    {
        foreach (var database in new[] { DatabaseA, DatabaseB })
        {
            await using (var server = Open("postgres"))
            {
                await server.OpenAsync();
                foreach (var sql in new[] { $"DROP DATABASE IF EXISTS {database} WITH (FORCE)", $"CREATE DATABASE {database}" })
                {
                    await using var cmd = new NpgsqlCommand(sql, server);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await using (var connection = Open(database))
            await using (var db = new DealowareDbContext(
                             new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql(connection).Options))
            {
                await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
            }

            await ExecAsync(database,
                "INSERT INTO \"Participants\" (\"Id\", \"Sub\", \"CreatedAt\", \"IsActive\", \"ContactEmail\", \"DisplayName\", \"LoginEmail\") VALUES " +
                $"('{ParticipantOld}', 'old-sub', '2026-11-20T08:00:00Z', true, NULL, NULL, NULL), " +
                $"('{ParticipantNewest}', '{MarkerSub}', '2026-11-30T09:30:00.123456Z', true, '{MarkerEmail}', '{MarkerName}', '{MarkerEmail}')");
            await ExecAsync(database,
                "INSERT INTO \"AdminAuditLog\" (\"Id\", \"Action\", \"ActorEmail\", \"IpHmac\", \"Timestamp\") VALUES " +
                $"('{AuditEntry}', 'participant.edit', '{MarkerEmail}', '{MarkerIpHmac}', '2026-11-30T09:31:00Z')");
        }
    }

    private static NpgsqlConnection Open(string database)
        => new(new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST"),
            Port = Environment.GetEnvironmentVariable("DB_PORT") is { Length: > 0 } p ? int.Parse(p) : 5432,
            Database = database,
            Username = Environment.GetEnvironmentVariable("DB_USERNAME"),
            Password = Environment.GetEnvironmentVariable("DB_PASSWORD"),
            SslMode = SslMode.Disable,
            Pooling = false,
        }.ConnectionString);

    private static async Task ExecAsync(string database, string sql)
    {
        await using var connection = Open(database);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountAsync(string database, string table)
    {
        await using var connection = Open(database);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand($"SELECT count(*) FROM \"{table}\"", connection);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}

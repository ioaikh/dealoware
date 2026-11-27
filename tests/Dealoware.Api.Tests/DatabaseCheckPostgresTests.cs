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
/// comparison of the live database (A, read some minutes after the cutoff) with its copy
/// restored to the cutoff (B). The CI server has no TLS, so these call
/// <see cref="DatabaseCheck"/> directly; the command's arguments and refusals are in
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
    private static readonly Guid Session = Guid.Parse("5e5510e0-0000-4000-8000-00000000c0de");
    private static readonly Guid Amount = Guid.Parse("a0a0a0a0-1111-4222-8333-444455556666");
    private const string MarkerEmail = "marker.person@example.invalid";
    private const string MarkerName = "Marker Display Name 7731";
    private const string MarkerSub = "marker-sub-4f9a";
    private const string MarkerIpHmac = "marker-ip-hmac-c0ffee";
    private const string MarkerAmount = "98765.4321";
    private static readonly Guid Artifact = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid ArtifactLater = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000002");
    private static readonly Guid Orphan = Guid.Parse("0e0e0e0e-0000-4000-8000-000000000003");
    private static readonly string[] Children = ["ArtifactValues", "SubjectEntities", "EntityProperties", "TimePeriods"];

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
        await AddArtifactAsync(DatabaseA, ArtifactLater, "2026-12-01T10:06:00Z", withValue: true);

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        Assert.Equal(a.ToJson(), b.ToJson());
        Assert.Equal(DatabaseCheck.CreatedColumns.Count, a.Tables.Count);
        Assert.Equal("20261126000600_AdminDeleteConfirmTokens", a.Migration);
        var participants = a.Tables.Single(t => t.Table == "Participants");
        Assert.Equal(2, participants.Rows);
        Assert.Equal(2, participants.Known.Count);
        Assert.Equal(1, a.Tables.Single(t => t.Table == "AdminAuditLog").Rows);
        Assert.Empty(a.Tables.Single(t => t.Table == "Offers").Known);
        // numeric is hashed as stored, the same on both sides. The artifact's children are cut
        // through it: the later artifact's children are not counted; the value with no artifact
        // is counted apart.
        var values = a.Tables.Single(t => t.Table == "ArtifactValues");
        Assert.Single(values.Known);
        Assert.Equal(1, values.Rows);
        Assert.Equal(1, values.UncutRows);
        foreach (var child in Children)
            Assert.Equal(1, a.Tables.Single(t => t.Table == child).Rows);
        Assert.Null(a.Tables.Single(t => t.Table == "AdminCredentials").Rows);
        Assert.True(a.Tables.Single(t => t.Table == "AdminSessions").HardDeletes);

        // The same database read twice gives the same output too.
        Assert.Equal(a.ToJson(), (await CheckAsync(DatabaseA)).ToJson());
    }

    [PostgresOnlyFact]
    public async Task DbCheck_OneChangedField_ChangesTheHash()
    {
        await NewDatabasesAsync();
        // A field that differs without any record of a change: what a bad backup looks like.
        await ExecAsync(DatabaseB, $"UPDATE \"Participants\" SET \"IsActive\" = false WHERE \"Id\" = '{ParticipantNewest}'");
        await ExecAsync(DatabaseB, $"UPDATE \"ArtifactValues\" SET \"Amount\" = 98765.4322 WHERE \"Id\" = '{Amount}'");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        foreach (var name in new[] { "Participants", "ArtifactValues" })
        {
            var ta = a.Tables.Single(t => t.Table == name);
            var tb = b.Tables.Single(t => t.Table == name);
            Assert.Equal(ta.Rows, tb.Rows);
            Assert.Equal(ta.Known[0].KeySha256, tb.Known[0].KeySha256);
            Assert.NotEqual(ta.Known[0].RowSha256, tb.Known[0].RowSha256);
        }

        Assert.NotEqual(a.ToJson(), b.ToJson());

        // Nothing else differs.
        foreach (var (ta, tb) in a.Tables.Zip(b.Tables).Where(p => p.First.Table is not "Participants" and not "ArtifactValues"))
            Assert.Equal(ta, tb);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_ARowChangedOnTheLiveDatabaseAfterTheCutoff_IsLeftOutOfItsKnownRecords()
    {
        await NewDatabasesAsync();
        // On A (live), after the cutoff: the newest participant is edited and the session's
        // last activity moves on. The copy B holds both as they were at the cutoff.
        await ExecAsync(DatabaseA,
            $"UPDATE \"Participants\" SET \"DisplayName\" = 'edited later', \"UpdatedAt\" = '2026-12-01T10:07:00Z' WHERE \"Id\" = '{ParticipantNewest}'");
        await ExecAsync(DatabaseA, $"UPDATE \"AdminSessions\" SET \"LastActivityAt\" = '2026-12-01T10:08:00Z' WHERE \"Id\" = '{Session}'");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        var pa = a.Tables.Single(t => t.Table == "Participants");
        var pb = b.Tables.Single(t => t.Table == "Participants");
        Assert.Equal(pa.Rows, pb.Rows);
        Assert.Single(pa.Known);
        Assert.Equal(2, pb.Known.Count);
        // What A still holds as unchanged since the cutoff is the same on B, record by record.
        Assert.All(pa.Known, k => Assert.Contains(k, pb.Known));
        Assert.Empty(a.Tables.Single(t => t.Table == "AdminSessions").Known);
        Assert.Single(b.Tables.Single(t => t.Table == "AdminSessions").Known);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_TheArtifactsChildren_AreCutThroughTheirArtifact()
    {
        await NewDatabasesAsync();
        // A (live): an artifact created after the cutoff, with children, and the old artifact
        // edited after the cutoff. Neither changes the counts up to the cutoff.
        await AddArtifactAsync(DatabaseA, ArtifactLater, "2026-12-01T10:06:00Z", withValue: true);
        await ExecAsync(DatabaseA, $"UPDATE \"Artifacts\" SET \"UpdatedAt\" = '2026-12-01T10:08:00Z', \"Version\" = 1 WHERE \"Id\" = '{Artifact}'");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);
        foreach (var child in Children)
        {
            var ta = a.Tables.Single(t => t.Table == child);
            var tb = b.Tables.Single(t => t.Table == child);
            Assert.Equal("Artifacts.CreatedAt", ta.CreatedColumn);
            Assert.Equal(1, ta.Rows);
            Assert.Equal(ta.Rows, tb.Rows);
            Assert.Equal(ta.UncutRows, tb.UncutRows);
            // The old artifact changed on A after the cutoff: its children leave A's known records.
            Assert.Empty(ta.Known);
            Assert.Single(tb.Known);
        }

        // B (a bad copy): the children are missing. Each count up to the cutoff now differs.
        foreach (var child in new[] { "EntityProperties", "SubjectEntities", "TimePeriods" })
            await ExecAsync(DatabaseB, $"DELETE FROM \"{child}\"");
        await ExecAsync(DatabaseB, "DELETE FROM \"ArtifactValues\" WHERE \"ArtifactId\" IS NOT NULL");
        b = await CheckAsync(DatabaseB);
        foreach (var child in Children)
            Assert.Equal(0, b.Tables.Single(t => t.Table == child).Rows);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_AHardDeleteOnTheLiveDatabase_LowersOnlyThatTablesCount()
    {
        await NewDatabasesAsync();
        await ExecAsync(DatabaseA, $"DELETE FROM \"AdminSessions\" WHERE \"Id\" = '{Session}'");

        var a = await CheckAsync(DatabaseA);
        var b = await CheckAsync(DatabaseB);

        var sa = a.Tables.Single(t => t.Table == "AdminSessions");
        var sb = b.Tables.Single(t => t.Table == "AdminSessions");
        Assert.True(sa.HardDeletes);
        Assert.Equal(0, sa.Rows);
        Assert.Equal(1, sb.Rows);
        foreach (var (ta, tb) in a.Tables.Zip(b.Tables).Where(p => p.First.Table != "AdminSessions"))
            Assert.Equal(ta, tb);
    }

    [PostgresOnlyFact]
    public async Task DbCheck_Output_HoldsNoColumnValue()
    {
        await NewDatabasesAsync();

        var json = (await CheckAsync(DatabaseA)).ToJson();

        foreach (var value in new[]
                 {
                     ParticipantOld.ToString(), ParticipantNewest.ToString(), AuditEntry.ToString(), Session.ToString(), Amount.ToString(),
                     Artifact.ToString(), Orphan.ToString(), "marker-intent", "marker-facts",
                     ParticipantOld.ToString("N"), ParticipantNewest.ToString("N"), AuditEntry.ToString("N"),
                     MarkerEmail, MarkerName, MarkerSub, MarkerIpHmac, MarkerAmount, "marker", "2026-11-",
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
            Assert.Equal(["table", "createdColumn", "changedColumns", "hardDeletes", "rows", "uncutRows", "known"], table.EnumerateObject().Select(p => p.Name));
            var name = table.GetProperty("table").GetString()!;
            Assert.True(DatabaseCheck.CreatedColumns.ContainsKey(name));
            foreach (var column in table.GetProperty("changedColumns").EnumerateArray())
            {
                // Its own change columns, or (through a parent cut) the artifact's.
                var c = column.GetString()!;
                if (c.StartsWith(DatabaseCheck.ParentTable + ".", StringComparison.Ordinal))
                    Assert.Contains(c[(DatabaseCheck.ParentTable.Length + 1)..], DatabaseCheck.ChangedColumns[DatabaseCheck.ParentTable]);
                else
                    Assert.Contains(c, DatabaseCheck.ChangedColumns[name]);
            }
            foreach (var known in table.GetProperty("known").EnumerateArray())
            {
                Assert.Equal(["keySha256", "rowSha256"], known.EnumerateObject().Select(p => p.Name));
                foreach (var hash in known.EnumerateObject())
                    Assert.Matches(new Regex("^[0-9a-f]{64}$"), hash.Value.GetString()!);
            }
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
            await ExecAsync(database,
                "INSERT INTO \"AdminSessions\" (\"Id\", \"Email\", \"CreatedAt\", \"LastActivityAt\", \"AbsoluteExpiresAt\", \"TotpVerified\", \"IpHmac\") VALUES " +
                $"('{Session}', '{MarkerEmail}', '2026-11-30T09:40:00Z', '2026-11-30T09:45:00Z', '2026-11-30T21:40:00Z', true, '{MarkerIpHmac}')");
            await AddArtifactAsync(database, Artifact, "2026-11-30T09:00:00Z", withValue: false);
            await ExecAsync(database,
                "INSERT INTO \"ArtifactValues\" (\"Id\", \"Amount\", \"Currency\", \"ArtifactId\") VALUES " +
                $"('{Amount}', {MarkerAmount}, 'EUR', '{Artifact}'), ('{Orphan}', 1.5, 'USD', NULL)");
        }
    }

    /// <summary>An artifact with one subject entity (with one property) and one time period.</summary>
    private static async Task AddArtifactAsync(string database, Guid id, string createdAt, bool withValue)
    {
        var n = id.ToString("N")[^4..];
        await ExecAsync(database,
            "INSERT INTO \"Artifacts\" (\"Id\", \"OwnerParticipantId\", \"Intent\", \"CreatedAt\", \"Locations\") VALUES " +
            $"('{id}', 'owner-{n}', 'marker-intent', '{createdAt}', '[]')");
        var entity = Derived(id, 1);
        await ExecAsync(database,
            "INSERT INTO \"SubjectEntities\" (\"Id\", \"Name\", \"Description\", \"ArtifactId\", \"Facts\") VALUES " +
            $"('{entity}', 'entity-{n}', 'd', '{id}', 'marker-facts')");
        await ExecAsync(database,
            "INSERT INTO \"EntityProperties\" (\"Id\", \"Name\", \"Type\", \"Value\", \"SubjectEntityId\") VALUES " +
            $"('{Derived(id, 2)}', 'p', 'string', 'v', '{entity}')");
        await ExecAsync(database,
            "INSERT INTO \"TimePeriods\" (\"Id\", \"Start\", \"End\", \"ArtifactId\") VALUES " +
            $"('{Derived(id, 3)}', '2027-01-01T00:00:00Z', '2027-02-01T00:00:00Z', '{id}')");
        if (withValue)
        {
            await ExecAsync(database,
                "INSERT INTO \"ArtifactValues\" (\"Id\", \"Amount\", \"Currency\", \"ArtifactId\") VALUES " +
                $"('{Derived(id, 4)}', 10, 'EUR', '{id}')");
        }
    }

    /// <summary>The same child ID in both databases, from the artifact's ID.</summary>
    private static Guid Derived(Guid id, int n)
        => new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{id}/{n}")));

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

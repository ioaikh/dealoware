using System.Net;
using System.Security.Cryptography;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// SQLite DateTimeOffset rewrite: old EF TEXT → canonical sortable UTC.
/// </summary>
public class SqliteDateTimeOffsetRewriteTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    [Theory]
    [InlineData("2030-06-01 10:30:00+00:00", "2030-06-01T10:30:00.0000000Z")]
    [InlineData("2030-06-01 10:30:00.123+00:00", "2030-06-01T10:30:00.1230000Z")]
    [InlineData("2030-06-01 10:30:00.0000007+00:00", "2030-06-01T10:30:00.0000007Z")]
    [InlineData("2030-06-01 12:30:00+02:00", "2030-06-01T10:30:00.0000000Z")]
    [InlineData("2030-06-01 08:00:00-05:00", "2030-06-01T13:00:00.0000000Z")]
    [InlineData("2030-06-01 10:30:00.0000000+00:00", "2030-06-01T10:30:00.0000000Z")]
    [InlineData("2030-06-01T10:30:00.0000000Z", "2030-06-01T10:30:00.0000000Z")]
    public void TD_ADM_066_SqliteConverter_OldFormatRoundTrip(string stored, string canonical)
    {
        var converter = new SqliteSortableUtcDateTimeOffsetConverter();
        var parsed = SqliteSortableUtcDateTimeOffsetConverter.Parse(stored);
        var fromProvider = (DateTimeOffset)converter.ConvertFromProvider(stored)!;
        var normalized = SqliteSortableUtcDateTimeOffsetConverter.NormalizeStoreValue(stored);

        Assert.Equal(TimeSpan.Zero, parsed.Offset);
        Assert.Equal(canonical, normalized);
        Assert.Equal(DateTimeOffset.Parse(canonical).UtcDateTime, parsed.UtcDateTime);
        Assert.Equal(parsed, fromProvider);
        Assert.Equal(canonical, SqliteSortableUtcDateTimeOffsetConverter.NormalizeStoreValue(canonical));
    }

    [Fact]
    public void TD_ADM_066_SqliteRewrite_ColumnsMatchModel()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=RewriteCols;Mode=Memory;Cache=Shared")
            .Options;
        using var db = new DealowareDbContext(options);
        var discovered = SqliteDateTimeOffsetRewrite.DiscoverFrom(db.Model);
        Assert.Equal(
            SqliteDateTimeOffsetRewrite.Columns.Select(c => $"{c.Table}.{c.Column}"),
            discovered.Select(c => $"{c.Table}.{c.Column}"));
    }

    [Fact]
    public async Task TD_ADM_066_SqliteRewrite_IsIdempotent()
    {
        await using var connection = new SqliteConnection($"Data Source=RewriteIdem_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await CreateFrozen15TableSchemaAsync(connection);
        await ExecAsync(connection,
            """
            INSERT INTO "Participants"
              ("Id", "Sub", "DisplayName", "LoginEmail", "ContactEmail", "CreatedAt", "IsActive", "DeletedAt", "Version")
            VALUES
              ('00000000-0000-0000-0000-0000000000a1', 'sub-a1', 'idem-a', NULL, NULL, '2030-06-01 23:00:00+00:00', 1, NULL, 0),
              ('00000000-0000-0000-0000-0000000000a2', 'sub-a2', 'idem-b', NULL, NULL, '2030-06-01T01:00:00.0000000Z', 1, NULL, 0);
            INSERT INTO "AdminAuditLog"
              ("Id", "Timestamp", "Action", "ActorEmail", "IpHmac")
            VALUES
              ('00000000-0000-0000-0000-0000000000aa', '2030-06-01 12:30:00.123+00:00', 'login', 'qa@example.com', 'hmac');
            """);

        var first = SqliteDateTimeOffsetRewrite.Apply(connection);
        Assert.True(first >= 2, $"expected at least two rewrites, got {first}");
        var afterFirst = await SnapshotDateTimeTextAsync(connection);
        Assert.All(afterFirst, v =>
            Assert.True(SqliteSortableUtcDateTimeOffsetConverter.IsCanonical(v), v));

        var second = SqliteDateTimeOffsetRewrite.Apply(connection);
        Assert.Equal(0, second);
        var afterSecond = await SnapshotDateTimeTextAsync(connection);
        Assert.Equal(afterFirst, afterSecond);
    }

    [Fact]
    public async Task TD_ADM_066_SqliteRewrite_MixedFormat_SortAndRangeAfterMigrate()
    {
        var prefix = $"dto-{Guid.NewGuid():N}";
        await using var factory = new RewriteAdminFactory();
        await CreateFrozen15TableSchemaAsync(factory.Connection);
        await ExecAsync(factory.Connection,
            $"""
            INSERT INTO "Participants"
              ("Id", "Sub", "DisplayName", "LoginEmail", "ContactEmail", "CreatedAt", "IsActive", "DeletedAt", "Version")
            VALUES
              ('{Guid.NewGuid():D}', 'sub-early', '{prefix}-early', NULL, NULL, '2030-06-01T01:00:00.0000000Z', 1, NULL, 0),
              ('{Guid.NewGuid():D}', 'sub-offset', '{prefix}-offset', NULL, NULL, '2030-06-01 08:00:00+02:00', 1, NULL, 0),
              ('{Guid.NewGuid():D}', 'sub-mid', '{prefix}-mid', NULL, NULL, '2030-06-01 12:30:00.123+00:00', 1, NULL, 0),
              ('{Guid.NewGuid():D}', 'sub-late', '{prefix}-late', NULL, NULL, '2030-06-01 23:00:00+00:00', 1, NULL, 0);
            """);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        var created = await ReadColumnAsync(factory.Connection, "Participants", "CreatedAt");
        Assert.Equal(4, created.Count);
        Assert.All(created, v => Assert.True(SqliteSortableUtcDateTimeOffsetConverter.IsCanonical(v), v));

        Guid sessionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var session = AdminSession.Create(CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        var client = factory.CreateClient();
        var asc = await ListNamesAsync(client, sessionId, $"/admin/api/participants?q={prefix}-&sort=created&dir=asc");
        Assert.Equal(["early", "offset", "mid", "late"], asc.Select(Short).ToList());

        var desc = await ListNamesAsync(client, sessionId, $"/admin/api/participants?q={prefix}-&sort=created&dir=desc");
        Assert.Equal(["late", "mid", "offset", "early"], desc.Select(Short).ToList());

        var ranged = await ListNamesAsync(
            client,
            sessionId,
            $"/admin/api/participants?q={prefix}-&sort=created&dir=asc&createdFrom=2030-06-01T10:00:00Z&createdTo=2030-06-01T20:00:00Z");
        Assert.Equal(["mid"], ranged.Select(Short).ToList());

        await ExecAsync(factory.Connection,
            $"""
            UPDATE "Participants" SET "UpdatedAt" = '2030-06-02 22:00:00+00:00' WHERE "DisplayName" = '{prefix}-late';
            UPDATE "Participants" SET "UpdatedAt" = '2030-06-02T03:00:00.0000000Z' WHERE "DisplayName" = '{prefix}-early';
            """);
        Assert.Equal(1, SqliteDateTimeOffsetRewrite.Apply(factory.Connection));

        var updatedDesc = await ListNamesAsync(
            client, sessionId, $"/admin/api/participants?q={prefix}-&sort=updated&dir=desc");
        Assert.Equal("late", Short(updatedDesc[0]));
        Assert.Equal("early", Short(updatedDesc[1]));

        static string Short(string displayName) => displayName.Split('-')[^1];
    }

    private static async Task<List<string>> ListNamesAsync(HttpClient client, Guid sessionId, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await response.Content.ReadAsStringAsync());
        return payload.GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("displayName").GetString()!)
            .ToList();
    }

    private static async Task ExecAsync(SqliteConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ReadColumnAsync(SqliteConnection connection, string table, string column)
    {
        var values = new List<string>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT \"{column}\" FROM \"{table}\" WHERE \"{column}\" IS NOT NULL";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));
        return values;
    }

    private static async Task<List<string>> SnapshotDateTimeTextAsync(SqliteConnection connection)
    {
        var values = new List<string>();
        foreach (var (table, column) in SqliteDateTimeOffsetRewrite.Columns)
        {
            await using var exists = connection.CreateCommand();
            exists.CommandText = $"SELECT 1 FROM pragma_table_info('{table}') WHERE name = '{column}'";
            if (await exists.ExecuteScalarAsync() is null)
                continue;
            values.AddRange((await ReadColumnAsync(connection, table, column)).OrderBy(v => v, StringComparer.Ordinal));
        }

        return values;
    }

    private static async Task CreateFrozen15TableSchemaAsync(SqliteConnection connection)
    {
        foreach (var tableGroup in BaselineSchema.Columns.GroupBy(c => c.Table, StringComparer.Ordinal))
        {
            var pk = BaselineSchema.PrimaryKeys.Single(k => string.Equals(k.Table, tableGroup.Key, StringComparison.Ordinal));
            var defs = tableGroup.Select(c =>
                $"\"{c.Name}\" {StoreTypeFor(c.Kind)}{(c.IsNullable ? "" : " NOT NULL")}");
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $"CREATE TABLE \"{tableGroup.Key}\" ({string.Join(", ", defs)}, " +
                $"PRIMARY KEY ({string.Join(", ", pk.Columns.Select(n => $"\"{n}\""))}))";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string StoreTypeFor(BaselineColumnKind kind)
        => kind switch
        {
            BaselineColumnKind.Guid or BaselineColumnKind.String or BaselineColumnKind.DateTimeOffset
                or BaselineColumnKind.Decimal => "TEXT",
            BaselineColumnKind.Boolean or BaselineColumnKind.Int32 or BaselineColumnKind.Int64
                or BaselineColumnKind.UInt32 => "INTEGER",
            _ => "TEXT"
        };

    private sealed class RewriteAdminFactory : WebApplicationFactory<Program>
    {
        public SqliteConnection Connection { get; }

        public RewriteAdminFactory()
        {
            Connection = new SqliteConnection($"Data Source=RewriteAdmin_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            Connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                IpHasher.KeyEnvironmentVariable,
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services
                             .Where(d => d.ServiceType == typeof(DbContextOptions<DealowareDbContext>)
                                         || d.ServiceType == typeof(DbContextOptions)
                                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<DealowareDbContext>)
                                         || d.ServiceType == typeof(DealowareDbContext))
                             .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<DealowareDbContext>(options => options.UseSqlite(Connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Connection.Dispose();
        }
    }
}

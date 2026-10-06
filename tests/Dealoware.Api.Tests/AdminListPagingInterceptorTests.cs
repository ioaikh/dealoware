using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-065: list queries apply Take in SQL and do not materialize more rows than limit.
/// </summary>
public class AdminListPagingInterceptorTests
{
    private const string AdminHost = "admin.core.dealoware.com";

    [Fact]
    public async Task TD_ADM_065_ListQuery_DoesNotMaterializeMoreRowsThanLimit()
    {
        var interceptor = new ParticipantListRowInterceptor();
        await using var factory = new InterceptingAdminFactory(interceptor);
        var prefix = $"p65i-{Guid.NewGuid():N}";

        Guid sessionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            for (var i = 0; i < 120; i++)
                db.Participants.Add(Participant.Create($"{prefix}-{i:D3}"));
            var session = AdminSession.Create("io@aiknowhow.com", ipHmac: "testhmac-not-an-ip");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        interceptor.Reset();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/api/participants?q={prefix}-&limit=50");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {body}");
        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        Assert.Equal(50, payload.GetProperty("limit").GetInt32());
        Assert.Equal(50, payload.GetProperty("items").GetArrayLength());
        Assert.True(payload.GetProperty("total").GetInt32() >= 120);

        var pageSql = interceptor.Commands.FirstOrDefault(ParticipantListRowInterceptor.IsParticipantPageSelect);
        Assert.False(string.IsNullOrEmpty(pageSql), "Expected a Participants page SELECT with LIMIT.");
        Assert.Contains("LIMIT", pageSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(interceptor.Commands, sql =>
            ParticipantListRowInterceptor.IsParticipantPageSelect(sql)
            && !sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
        Assert.True(interceptor.PageRowCounts.Count > 0);
        Assert.All(interceptor.PageRowCounts, count => Assert.True(count <= 50, $"Page reader returned {count} rows"));
    }

    private sealed class InterceptingAdminFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection;
        private readonly ParticipantListRowInterceptor _interceptor;

        public InterceptingAdminFactory(ParticipantListRowInterceptor interceptor)
        {
            _interceptor = interceptor;
            _connection = new SqliteConnection($"Data Source=Intercept_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            _connection.Open();
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

                services.AddDbContext<DealowareDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                    options.AddInterceptors(_interceptor);
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }
    }
}

/// <summary>
/// Captures list SELECTs and counts rows the SQL actually returns by re-executing
/// the same command. Does not wrap EF's reader (that breaks materialization).
/// </summary>
internal sealed class ParticipantListRowInterceptor : DbCommandInterceptor
{
    public ConcurrentBag<string> Commands { get; } = new();
    public ConcurrentBag<int> PageRowCounts { get; } = new();

    public void Reset()
    {
        while (Commands.TryTake(out _)) { }
        while (PageRowCounts.TryTake(out _)) { }
    }

    public static bool IsParticipantPageSelect(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return false;
        if (sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase))
            return false;
        return sql.Contains("FROM \"Participants\"", StringComparison.OrdinalIgnoreCase)
               || Regex.IsMatch(sql, @"FROM\s+Participants\b", RegexOptions.IgnoreCase);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Observe(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Observe(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Observe(DbCommand command)
    {
        Commands.Add(command.CommandText);
        if (!IsParticipantPageSelect(command.CommandText) || command.Connection is null)
            return;

        using var clone = command.Connection.CreateCommand();
        clone.Transaction = command.Transaction;
        clone.CommandText = command.CommandText;
        foreach (DbParameter parameter in command.Parameters)
        {
            var copy = clone.CreateParameter();
            copy.ParameterName = parameter.ParameterName;
            copy.Value = parameter.Value;
            copy.DbType = parameter.DbType;
            clone.Parameters.Add(copy);
        }

        using var reader = clone.ExecuteReader();
        var count = 0;
        while (reader.Read())
            count++;
        PageRowCounts.Add(count);
    }
}

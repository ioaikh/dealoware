using System.Security.Cryptography;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-100 / TD-ADM-053: auth events persist with keyed HMAC IP only.
/// Auth HTTP endpoints are later PRs; this PR owns the writer they will call.
/// </summary>
public class AdminAuditAuthEventTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public async Task TdAdm100_AllAuthEvents_AreWrittenWithReasonClassAndHmacIp()
    {
        await using var connection = new SqliteConnection("Data Source=AuditAuth_Events;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new IpHasher(NewKey());
        var recorder = new AdminAuditRecorder(hasher);
        var repo = new AdminAuditRepository(db);
        var uow = new AdminAuditUnitOfWork(db, repo);
        const string ip = "203.0.113.44";

        var events = new[]
        {
            recorder.Auth(AdminAuditActions.LoginSuccess, "io@aiknowhow.com", ip),
            recorder.Auth(AdminAuditActions.LoginFailure, "io@aiknowhow.com", ip, AdminAuditActions.ReasonBadPassword),
            recorder.Auth(AdminAuditActions.LoginFailure, "io@aiknowhow.com", ip, AdminAuditActions.ReasonBad2Fa),
            recorder.Auth(AdminAuditActions.LoginFailure, "anonymous", ip, AdminAuditActions.ReasonCaptchaFailed),
            recorder.Auth(AdminAuditActions.LoginFailure, "io@aiknowhow.com", ip, AdminAuditActions.ReasonLocked),
            recorder.Auth(AdminAuditActions.LoginFailure, "io@aiknowhow.com", ip, AdminAuditActions.ReasonRateLimited),
            recorder.Auth(AdminAuditActions.SecondFactorFailed, "io@aiknowhow.com", ip, AdminAuditActions.ReasonBad2Fa),
            recorder.Auth(AdminAuditActions.ResetRequest, "io@aiknowhow.com", ip),
            recorder.Auth(AdminAuditActions.ResetComplete, "io@aiknowhow.com", ip),
            recorder.Auth(AdminAuditActions.TotpEnroll, "io@aiknowhow.com", ip),
            recorder.Auth(AdminAuditActions.TotpChange, "io@aiknowhow.com", ip),
            recorder.Auth(AdminAuditActions.RecoveryCodeUse, "io@aiknowhow.com", ip)
        };

        await uow.ExecutePairedAsync(_ => Task.CompletedTask, events);

        var rows = await db.AdminAuditLog.AsNoTracking().ToListAsync();
        Assert.Equal(events.Length, rows.Count);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.LoginSuccess);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.LoginFailure && r.ReasonClass == AdminAuditActions.ReasonBadPassword);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.LoginFailure && r.ReasonClass == AdminAuditActions.ReasonBad2Fa);
        Assert.Contains(rows, r => r.ReasonClass == AdminAuditActions.ReasonCaptchaFailed);
        Assert.Contains(rows, r => r.ReasonClass == AdminAuditActions.ReasonLocked);
        Assert.Contains(rows, r => r.ReasonClass == AdminAuditActions.ReasonRateLimited);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.SecondFactorFailed);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.ResetRequest);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.ResetComplete);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.TotpEnroll);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.TotpChange);
        Assert.Contains(rows, r => r.Action == AdminAuditActions.RecoveryCodeUse);
        Assert.All(rows, r =>
        {
            Assert.DoesNotContain(ip, r.IpHmac);
            Assert.DoesNotContain("password", r.IpHmac, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(r.IpHmac));
            Assert.Equal(hasher.Hash(ip), r.IpHmac);
        });
    }

    [Fact]
    public async Task TdAdm053_RecordedIp_IsKeyedHmacNotUnkeyedSha()
    {
        var key = NewKey();
        var hasher = new IpHasher(key);
        var recorder = new AdminAuditRecorder(hasher);
        var entry = recorder.Auth(AdminAuditActions.LoginFailure, "anonymous", "192.0.2.15", AdminAuditActions.ReasonBadPassword);
        Assert.NotEqual("192.0.2.15", entry.IpHmac);
        Assert.DoesNotContain("192.0.2.15", entry.IpHmac);
        var unkeyed = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("192.0.2.15")));
        Assert.NotEqual(unkeyed, entry.IpHmac);
        Assert.Equal(hasher.Hash("192.0.2.15"), entry.IpHmac);
    }

    [Fact]
    public async Task TdAdm101_ListPage_FilterSortSkipTakeAndCountRunInSql()
    {
        await using var connection = new SqliteConnection("Data Source=AuditAuth_SqlPage;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var interceptor = new SqlCaptureInterceptor();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new DealowareDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new IpHasher(NewKey());
        var recorder = new AdminAuditRecorder(hasher);
        for (var i = 0; i < 5; i++)
        {
            db.AdminAuditLog.Add(recorder.Auth(
                i % 2 == 0 ? AdminAuditActions.LoginSuccess : AdminAuditActions.LoginFailure,
                "io@aiknowhow.com",
                "198.51.100.20",
                i % 2 == 0 ? null : AdminAuditActions.ReasonBadPassword,
                DateTimeOffset.UtcNow.AddMinutes(-i)));
        }

        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var repo = new AdminAuditRepository(db);
        var (items, total) = await repo.ListPageAsync(new AdminAuditListCriteria
        {
            Offset = 1,
            Limit = 2,
            Sort = "-timestamp",
            Action = AdminAuditActions.LoginFailure
        });

        Assert.Equal(2, items.Count);
        Assert.True(total >= 2);
        Assert.All(items, i => Assert.Equal(AdminAuditActions.LoginFailure, i.Action));
        Assert.Contains(interceptor.Commands, sql =>
            sql.Contains("AdminAuditLog", StringComparison.OrdinalIgnoreCase)
            && sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, sql =>
            sql.Contains("COUNT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(interceptor.Commands, sql =>
            sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class SqlCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }
    }
}

using System.Security.Cryptography;
using Dealoware.Domain.Admin;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-080 / TD-ADM-102: same-transaction pairing and FieldPolicy-only snapshots.
/// Later edit/delete PRs call <see cref="IAdminAuditUnitOfWork"/>.
/// </summary>
public class AdminAuditPairingTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static async Task<(SqliteConnection Connection, DealowareDbContext Db, IIpHasher Hasher)> OpenAsync(string name)
    {
        var connection = new SqliteConnection($"Data Source={name};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        var db = new DealowareDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (connection, db, new IpHasher(NewKey()));
    }

    [Fact]
    public async Task TdAdm102_AuditInsertFail_RollsBackMutation()
    {
        var (connection, db, hasher) = await OpenAsync("AuditPair_Rollback");
        await using var _ = connection;
        await using var __ = db;

        var participant = Participant.Create("before");
        db.Participants.Add(participant);
        await db.SaveChangesAsync();

        var recorder = new AdminAuditRecorder(hasher);
        var uow = new AdminAuditUnitOfWork(db, new ThrowingAdminAuditRepository());
        var audit = recorder.Entity(
            AdminAuditActions.EntityEdit,
            "io@aiknowhow.com",
            "198.51.100.9",
            "Participant",
            participant.Id,
            beforeSnapshot: """{"displayName":"before"}""",
            afterSnapshot: """{"displayName":"after"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecutePairedAsync(
                _ =>
                {
                    participant.UpdateDisplayName("after");
                    return Task.CompletedTask;
                },
                audit));

        await using var verify = new DealowareDbContext(
            new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options);
        Assert.Equal("before", (await verify.Participants.SingleAsync()).DisplayName);
        Assert.Equal(0, await verify.AdminAuditLog.CountAsync());
    }

    [Fact]
    public async Task TdAdm102_PairedEdit_CommitsMutationAndAuditTogether()
    {
        var (connection, db, hasher) = await OpenAsync("AuditPair_Commit");
        await using var _ = connection;
        await using var __ = db;

        var participant = Participant.Create("before");
        db.Participants.Add(participant);
        await db.SaveChangesAsync();

        var recorder = new AdminAuditRecorder(hasher);
        var repo = new AdminAuditRepository(db);
        var uow = new AdminAuditUnitOfWork(db, repo);
        var before = AdminAuditSnapshots.FromAllowedDictionary(new Dictionary<string, object?>
        {
            ["displayName"] = "before"
        });
        var after = AdminAuditSnapshots.FromAllowedDictionary(new Dictionary<string, object?>
        {
            ["displayName"] = "after"
        });
        var audit = recorder.Entity(
            AdminAuditActions.EntityEdit,
            "io@aiknowhow.com",
            "198.51.100.9",
            "Participant",
            participant.Id,
            before,
            after);

        await uow.ExecutePairedAsync(
            _ =>
            {
                participant.UpdateDisplayName("after");
                return Task.CompletedTask;
            },
            audit);

        await using var verify = new DealowareDbContext(
            new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options);
        Assert.Equal("after", (await verify.Participants.SingleAsync()).DisplayName);
        var row = Assert.Single(await verify.AdminAuditLog.ToListAsync());
        Assert.Equal(AdminAuditActions.EntityEdit, row.Action);
        Assert.Equal(participant.Id, row.EntityId);
        Assert.DoesNotContain("198.51.100.9", row.IpHmac);
        Assert.Contains("after", row.AfterSnapshot);
    }

    [Fact]
    public async Task TdAdm080_SnapshotsAreFieldPolicyOnly_NoSecrets()
    {
        var policy = new FieldPolicy();
        var owner = FieldPrincipal.CoreOwner("io@aiknowhow.com");
        var json = AdminAuditSnapshots.FromAllowedFields(
            policy,
            owner,
            new Dictionary<FieldClass, object?>
            {
                [FieldClass.DisplayName] = "Ada",
                [FieldClass.ParticipantActive] = true,
                [FieldClass.StrategyBody] = "must-not-appear",
                [FieldClass.LoginEmail] = "owner-login@example.com"
            });

        Assert.Contains("Ada", json);
        Assert.DoesNotContain("must-not-appear", json);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);

        var stripped = AdminAuditSnapshots.FromAllowedDictionary(new Dictionary<string, object?>
        {
            ["displayName"] = "Ada",
            ["password"] = "must-never-store",
            ["totpSecret"] = "must-never-store",
            ["recoveryCode"] = "must-never-store"
        });
        Assert.Contains("Ada", stripped);
        Assert.DoesNotContain("must-never-store", stripped);
        Assert.DoesNotContain("password", stripped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totp", stripped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recovery", stripped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TdAdm101_OversizedBody_TruncatesWithLengthAndHash()
    {
        var original = new string('z', 5000);
        var truncated = AdminAuditSnapshots.Truncate(original);
        Assert.Contains("TRUNCATED", truncated);
        Assert.Contains("originalLength=5000", truncated);
        Assert.Contains("sha256=", truncated);
        Assert.True(truncated.Length < original.Length + 80);
        Assert.StartsWith(original[..4000], truncated);
    }

    [Fact]
    public async Task TdAdm102_CascadeRows_ShareCorrelationIdInSameTransaction()
    {
        var (connection, db, hasher) = await OpenAsync("AuditPair_Cascade");
        await using var _ = connection;
        await using var __ = db;

        var recorder = new AdminAuditRecorder(hasher);
        var repo = new AdminAuditRepository(db);
        var uow = new AdminAuditUnitOfWork(db, repo);
        var correlation = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var rows = new[]
        {
            recorder.Entity(AdminAuditActions.EntityDelete, "io@aiknowhow.com", "192.0.2.8", "Negotiation", parentId, correlationId: correlation),
            recorder.Entity(AdminAuditActions.EntityDelete, "io@aiknowhow.com", "192.0.2.8", "Offer", childId, correlationId: correlation)
        };

        await uow.ExecutePairedAsync(_ => Task.CompletedTask, rows);

        var stored = await db.AdminAuditLog.AsNoTracking().ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.All(stored, r => Assert.Equal(correlation, r.CorrelationId));
        Assert.All(stored, r => Assert.DoesNotContain("192.0.2.8", r.IpHmac));
    }

    private sealed class ThrowingAdminAuditRepository : IAdminAuditRepository
    {
        public Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit insert failed");

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListPageAsync(
            AdminAuditListCriteria criteria,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<AdminAuditEntry>, int)>(([], 0));
    }
}

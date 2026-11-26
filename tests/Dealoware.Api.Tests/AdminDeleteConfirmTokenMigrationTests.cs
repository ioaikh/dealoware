using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Dealoware.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dealoware.Api.Tests;

/// <summary>
/// T06a migration 20261126000600 (A7 Step 10, the confirm tokens of admin
/// deletes): a new table only. Rows written by the previous release survive
/// it, the previous release's queries still run after it, it rolls back
/// cleanly, the table is not in the frozen stamp lists, and its rows go with
/// their session.
/// </summary>
public class AdminDeleteConfirmTokenMigrationTests
{
    private const string OwnerEmail = "owner@example.test";
    private const string Table = "AdminDeleteConfirmTokens";
    private static readonly string Hash = new('a', 64);

    [Fact]
    public async Task TdAdm090_Migration600_KeepsPreviousReleaseRowsAndQueries_AndGoesWithItsSession()
    {
        await using var connection = new SqliteConnection($"Data Source=DeleteTokenMig_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;

        // The previous release: every migration up to and including the recovery notices.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(AdminSessionRecoveryNotices.MigrationId);
            Assert.DoesNotContain(Table, await ListTablesAsync(connection));
        }

        // Rows of the previous release: a session with its notice and step-up, and the four entities.
        var now = DateTimeOffset.UtcNow;
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", now);
        session.MarkTotpVerified();
        var partyA = Participant.Create("Alpha");
        var partyB = Participant.Create("Beta");
        var artifact = Artifact.Create(partyA.Sub, [SubjectEntity.Create("Widget", "desc")], "sell");
        var (negotiation, _) = Negotiation.Create(artifact.Id, partyA.Sub, partyB.Sub, "sell", "buy");
        var (offer, _) = Offer.Create(negotiation!.Id, partyA.Sub, partyB.Sub, 10m, "usd", "terms");
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminSessions.Add(session);
            db.AdminSessionStepUps.Add(AdminSessionStepUp.Create(session.Id, now));
            db.AdminSessionRecoveryNotices.Add(AdminSessionRecoveryNotice.Create(session.Id, now));
            db.Participants.AddRange(partyA, partyB);
            db.Artifacts.Add(artifact);
            db.Negotiations.Add(negotiation);
            db.Offers.Add(offer!);
            await db.SaveChangesAsync();
        }

        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync();
            Assert.Contains(AdminDeleteConfirmTokens.MigrationId, await db.Database.GetAppliedMigrationsAsync());
        }

        Assert.Equal(
            new[] { "CascadeHash", "ConsumedAt", "CreatedAt", "EntityId", "EntityType", "ExpiresAt", "Id", "SessionId", "TokenHash" },
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
            Assert.Equal(1, await db.AdminSessionRecoveryNotices.CountAsync());
            Assert.Equal(2, await db.Participants.CountAsync(p => p.DeletedAt == null));
            Assert.Equal("Widget", (await db.Artifacts.Include(a => a.Entities).SingleAsync()).Entities.Single().Name);
            Assert.Equal(NegotiationStatus.Open, (await db.Negotiations.SingleAsync(n => n.DeletedAt == null)).Status);
            var storedOffer = await db.Offers.SingleAsync(o => o.DeletedAt == null && o.NegotiationId == negotiation.Id);
            Assert.Equal(10m, storedOffer.Amount);
            Assert.Equal("terms", storedOffer.Terms);
        }

        // Its rows belong to a session and go with it; one with no session is refused.
        await using (var db = new DealowareDbContext(options))
        {
            db.AdminDeleteConfirmTokens.Add(AdminDeleteConfirmToken.Create(Hash, session.Id, "offers", offer!.Id, Hash, now));
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.AdminDeleteConfirmTokens.CountAsync());

            // The token hash is unique.
            db.ChangeTracker.Clear();
            db.AdminDeleteConfirmTokens.Add(AdminDeleteConfirmToken.Create(Hash, session.Id, "offers", offer.Id, Hash, now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            await db.AdminSessions.Where(s => s.Id == session.Id).ExecuteDeleteAsync();
            Assert.Equal(0, await db.AdminDeleteConfirmTokens.CountAsync());

            db.AdminDeleteConfirmTokens.Add(AdminDeleteConfirmToken.Create(new string('b', 64), Guid.NewGuid(), "offers", offer.Id, Hash, now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // Down drops only the new table.
        await using (var db = new DealowareDbContext(options))
        {
            await db.GetService<IMigrator>().MigrateAsync(AdminSessionRecoveryNotices.MigrationId);
        }

        var tables = await ListTablesAsync(connection);
        Assert.DoesNotContain(Table, tables);
        Assert.Contains("AdminSessionRecoveryNotices", tables);
        Assert.Contains("AdminSessions", tables);
        Assert.Contains("Offers", tables);
    }

    [Fact]
    public void TdAdm090_Migration600_IsANewTableOnly()
    {
        var source = ReadRepoFile("src/Dealoware.Infrastructure/Persistence/Migrations/20261126000600_AdminDeleteConfirmTokens.cs");
        Assert.Contains("CreateTable(", source, StringComparison.Ordinal);
        Assert.Contains("onDelete: ReferentialAction.Cascade", source, StringComparison.Ordinal);
        foreach (var change in new[] { "AddColumn", "AlterColumn", "DropColumn", "RenameColumn", ".Sql(" })
        {
            Assert.DoesNotContain(change, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TdAdm090_AToken_IsItsHashOnly_BoundAndFiveMinutes()
    {
        var session = Guid.NewGuid();
        var entity = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var token = AdminDeleteConfirmToken.Create(Hash, session, "offers", entity, Hash, at);

        Assert.Equal(at.AddMinutes(5), token.ExpiresAt);
        Assert.True(token.IsUsableBy(session, "offers", entity, at.AddMinutes(5).AddTicks(-1)));
        Assert.False(token.IsUsableBy(session, "offers", entity, at.AddMinutes(5)));
        Assert.False(token.IsUsableBy(Guid.NewGuid(), "offers", entity, at));
        Assert.False(token.IsUsableBy(session, "negotiations", entity, at));
        Assert.False(token.IsUsableBy(session, "offers", Guid.NewGuid(), at));

        Assert.Throws<ArgumentException>(() => AdminDeleteConfirmToken.Create("raw-token", session, "offers", entity, Hash, at));
        Assert.Throws<ArgumentException>(() => AdminDeleteConfirmToken.Create(Hash, Guid.Empty, "offers", entity, Hash, at));
        Assert.Throws<ArgumentException>(() => AdminDeleteConfirmToken.Create(Hash, session, "offers", Guid.Empty, Hash, at));
        Assert.Throws<ArgumentException>(() => AdminDeleteConfirmToken.Create(Hash, session, "offers", entity, "", at));
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

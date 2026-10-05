using Dealoware.Api;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Tests;

/// <summary>
/// H3: Production/non-Development must not EnsureCreated; migrate is a separate entrypoint.
/// Uses SQLite only — no live Postgres required.
/// </summary>
public class DatabaseSchemaBootstrapTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("development", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Testing", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void ShouldEnsureCreatedOnStartup_OnlyDevelopment(string? environment, bool expected)
    {
        Assert.Equal(expected, DatabaseSchemaBootstrap.ShouldEnsureCreatedOnStartup(environment));
    }

    [Fact]
    public async Task ApplyStartupSchemaAsync_Development_CreatesTables()
    {
        await using var connection = new SqliteConnection("Data Source=EnsureCreated_Dev;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, "Development");
        }

        await using (var db = new DealowareDbContext(options))
        {
            Assert.True(await db.Database.CanConnectAsync());
            // Participants table exists after EnsureCreated
            Assert.Equal(0, await db.Participants.CountAsync());
        }
    }

    [Fact]
    public async Task ApplyStartupSchemaAsync_Production_DoesNotCreateTables()
    {
        await using var connection = new SqliteConnection("Data Source=EnsureCreated_Prod;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new DealowareDbContext(options))
        {
            await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, "Production");
        }

        await using (var db = new DealowareDbContext(options))
        {
            // No EnsureCreated → querying a mapped table must fail (no such table).
            var ex = await Assert.ThrowsAsync<SqliteException>(
                async () => await db.Participants.CountAsync());
            Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ApplyStartupSchemaAsync_Staging_DoesNotCreateTables()
    {
        await using var connection = new SqliteConnection("Data Source=EnsureCreated_Staging;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new DealowareDbContext(options);
        await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, "Staging");

        var ex = await Assert.ThrowsAsync<SqliteException>(
            async () => await db.Participants.CountAsync());
        Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyMigrationsAsync_OnSqlite_CompletesWithoutLivePostgres()
    {
        // With no migration assemblies/files yet, MigrateAsync still succeeds (creates history only).
        await using var connection = new SqliteConnection("Data Source=Migrate_Sqlite;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new DealowareDbContext(options);
        var ex = await Record.ExceptionAsync(
            async () => await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(new[] { "migrate" }, true)]
    [InlineData(new[] { "Migrate" }, true)]
    [InlineData(new[] { "MIGRATE", "--verbose" }, true)]
    [InlineData(new[] { "run" }, false)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--migrate" }, false)]
    public void IsMigrateArgs_DetectsMigrateCommand(string[] args, bool expected)
    {
        Assert.Equal(expected, DatabaseMigrateCommand.IsMigrateArgs(args));
    }

    [Fact]
    public async Task Host_Production_WithValidKey_StartsWithoutEnsureCreated_HealthOk()
    {
        // Existing Production startup path must remain healthy without EnsureCreated.
        using var factory = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}

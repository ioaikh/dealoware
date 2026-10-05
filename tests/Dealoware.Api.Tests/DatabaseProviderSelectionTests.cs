using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// Provider selection (SQLite vs PostgreSQL). Uses an injected environment lookup,
/// so no process environment variables are read or mutated and no live database is needed.
/// </summary>
public class DatabaseProviderSelectionTests
{
    private static Func<string, string?> Env(params (string Key, string? Value)[] values)
    {
        var map = values.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
        return key => map.TryGetValue(key, out var value) ? value : null;
    }

    [Fact]
    public void DbHost_BuildsNpgsqlConnectionString_WithDefaultPortAndSslRequire()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select("Data Source=dealoware.db", env);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal("db.example", csb.Host);
        Assert.Equal(5432, csb.Port);
        Assert.Equal("dealoware", csb.Database);
        Assert.Equal("app_user", csb.Username);
        Assert.Equal("placeholder-password", csb.Password);
        Assert.Equal(SslMode.Require, csb.SslMode);
    }

    [Fact]
    public void DbHost_UsesDbPort_WhenProvided()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "6543"), ("DB_NAME", "dealoware"));

        var selection = DatabaseProviderSelector.Select(null, env);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(6543, csb.Port);
        Assert.Equal(SslMode.Require, csb.SslMode);
    }

    [Fact]
    public void DbHost_InvalidPort_Throws()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "not-a-port"));

        Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Select(null, env));
    }

    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder")]
    [InlineData("host=db.example;Database=dealoware")]
    [InlineData("Database=dealoware;Host=db.example")]
    public void HostConnectionString_SelectsPostgres_Unchanged(string connectionString)
    {
        var selection = DatabaseProviderSelector.Select(connectionString, Env());

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Theory]
    [InlineData("Data Source=dealoware.db")]
    [InlineData("Data Source=TestDb;Mode=Memory;Cache=Shared")]
    public void SqliteConnectionString_StaysSqlite(string connectionString)
    {
        var selection = DatabaseProviderSelector.Select(connectionString, Env());

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyDbHost_IsIgnored(string dbHost)
    {
        var selection = DatabaseProviderSelector.Select("Data Source=dealoware.db", Env(("DB_HOST", dbHost)));

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
    }

    [Fact]
    public void AddInfrastructure_WithDbHost_ConfiguresNpgsqlProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Data Source=dealoware.db", Env(("DB_HOST", "db.example"), ("DB_NAME", "dealoware")));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        Assert.True(db.Database.IsNpgsql());
        Assert.False(db.Database.IsSqlite());
    }

    [Fact]
    public void AddInfrastructure_WithSqliteConnectionString_ConfiguresSqliteProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Data Source=dealoware.db", Env());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        Assert.True(db.Database.IsSqlite());
        Assert.False(db.Database.IsNpgsql());
    }

    [Fact]
    public void StrictPostgres_WithSqliteConnectionString_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Select("Data Source=dealoware.db", Env(), requireStrictPostgres: true));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DB_HOST", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Host=", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StrictPostgres_DevelopmentStyle_AllowsSqlite()
    {
        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db",
            Env(),
            requireStrictPostgres: false);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
    }

    [Fact]
    public void StrictPostgres_WithDbHost_SelectsPostgres()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select("Data Source=dealoware.db", env, requireStrictPostgres: true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
    }

    [Fact]
    public void StrictPostgres_WithHostConnectionString_SelectsPostgres()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder";

        var selection = DatabaseProviderSelector.Select(connectionString, Env(), requireStrictPostgres: true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Fact]
    public void AddInfrastructure_StrictPostgres_WithSqlite_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure("Data Source=dealoware.db", Env(), requireStrictPostgres: true));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("placeholder-password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Host_Production_WithSqliteOnly_FailsAtStartup()
    {
        using var factory = new SqliteOnlyProductionWebApplicationFactory();

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        Assert.Contains(messages, m => m.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, m => m.Contains("DB_HOST", StringComparison.Ordinal));
    }
}

/// <summary>
/// Production host with a valid JWT key but an explicit SQLite connection string and no DB_HOST,
/// so provider selection would choose SQLite and must fail closed.
/// </summary>
file sealed class SqliteOnlyProductionWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.UseSetting(
            JwtSigningKeyValidator.EnvironmentVariableName,
            EnvironmentWebApplicationFactory.TestSigningKey64);
        builder.UseSetting("Jwt:SigningKey", string.Empty);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Data Source=dealoware.db");
    }
}

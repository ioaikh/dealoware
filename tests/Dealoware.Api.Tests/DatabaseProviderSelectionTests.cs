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

    #region Non-strict mode (Development) - allows SQLite and weaker SSL

    [Fact]
    public void NonStrict_DbHost_BuildsNpgsqlConnectionString_WithSslRequire()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db", env, strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal("db.example", csb.Host);
        Assert.Equal(5432, csb.Port);
        Assert.Equal("dealoware", csb.Database);
        Assert.Equal("app_user", csb.Username);
        Assert.Equal("placeholder-password", csb.Password);
        Assert.Equal(SslMode.Require, csb.SslMode);
        Assert.True(string.IsNullOrEmpty(csb.RootCertificate));
    }

    [Fact]
    public void NonStrict_DbHost_UsesDbPort_WhenProvided()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "6543"), ("DB_NAME", "dealoware"));

        var selection = DatabaseProviderSelector.Select(null, env, strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(6543, csb.Port);
        Assert.Equal(SslMode.Require, csb.SslMode);
    }

    [Fact]
    public void NonStrict_HostConnectionString_SelectsPostgres_Unchanged()
    {
        var connectionString = "Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder";
        var selection = DatabaseProviderSelector.Select(connectionString, Env(), strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Fact]
    public void NonStrict_DevelopmentStyle_AllowsSqlite()
    {
        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db",
            Env(),
            strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
    }

    [Theory]
    [InlineData("Data Source=dealoware.db")]
    [InlineData("Data Source=TestDb;Mode=Memory;Cache=Shared")]
    public void NonStrict_SqliteConnectionString_StaysSqlite(string connectionString)
    {
        var selection = DatabaseProviderSelector.Select(connectionString, Env(), strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NonStrict_EmptyDbHost_IsIgnored(string dbHost)
    {
        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db",
            Env(("DB_HOST", dbHost)),
            strictNonDevelopment: false);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
    }

    #endregion

    #region Strict mode (Production) - enforces VerifyFull + RootCertificate + refuses SQLite

    [Fact]
    public void Strict_DbHost_BuildsNpgsqlConnectionString_WithVerifyFullAndRootCert()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db",
            env,
            strictNonDevelopment: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal("db.example", csb.Host);
        Assert.Equal(5432, csb.Port);
        Assert.Equal("dealoware", csb.Database);
        Assert.Equal("app_user", csb.Username);
        Assert.Equal("placeholder-password", csb.Password);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
        Assert.Equal(DatabaseProviderSelector.RdsRootCertificatePath, csb.RootCertificate);
    }

    [Fact]
    public void Strict_DbHost_UsesDbPort_WhenProvided()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "6543"), ("DB_NAME", "dealoware"));

        var selection = DatabaseProviderSelector.Select(
            null, env, strictNonDevelopment: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(6543, csb.Port);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
        Assert.Equal(DatabaseProviderSelector.RdsRootCertificatePath, csb.RootCertificate);
    }

    [Fact]
    public void Strict_HostConnectionString_EnforcesVerifyFullAndRootCert()
    {
        var connectionString = "Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder";
        var selection = DatabaseProviderSelector.Select(
            connectionString, Env(), strictNonDevelopment: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
        Assert.Equal(DatabaseProviderSelector.RdsRootCertificatePath, csb.RootCertificate);
    }

    [Fact]
    public void Strict_MissingBundle_Throws()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(
                null, env, strictNonDevelopment: true,
                fileExistsCheck: _ => false,
                certLoadCheck: _ => true));

        Assert.Contains("missing or unreadable", ex.Message);
        Assert.Contains(DatabaseProviderSelector.RdsRootCertificatePath, ex.Message);
    }

    [Fact]
    public void Strict_EmptyCertBundle_Throws()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(
                null, env, strictNonDevelopment: true,
                fileExistsCheck: _ => true,
                certLoadCheck: _ => false));

        Assert.Contains("no valid certificates", ex.Message);
        Assert.Contains(DatabaseProviderSelector.RdsRootCertificatePath, ex.Message);
    }

    [Theory]
    [InlineData("SSL Mode=Require")]
    [InlineData("SslMode=Require")]
    [InlineData("SSL Mode=Disable")]
    [InlineData("SSL Mode=Allow")]
    [InlineData("SSL Mode=VerifyCA")]
    public void Strict_PathB_WeakSslMode_Throws(string sslModeSetting)
    {
        var connectionString = $"Host=localhost;Database=dealoware;{sslModeSetting}";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(
                connectionString, Env(), strictNonDevelopment: true,
                fileExistsCheck: _ => true,
                certLoadCheck: _ => true));

        Assert.Contains("not permitted outside Development", ex.Message);
    }

    [Fact]
    public void Strict_PathB_VerifyFull_AllowedWithRootCert()
    {
        var connectionString = "Host=localhost;Database=dealoware;SSL Mode=VerifyFull";
        var selection = DatabaseProviderSelector.Select(
            connectionString, Env(), strictNonDevelopment: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
        Assert.Equal(DatabaseProviderSelector.RdsRootCertificatePath, csb.RootCertificate);
    }

    [Theory]
    [InlineData("Trust Server Certificate=true")]
    [InlineData("TrustServerCertificate=true")]
    [InlineData("TrustServerCertificate=True")]
    public void Strict_PathB_TrustServerCertificateTrue_Throws(string trustSetting)
    {
        var connectionString = $"Host=localhost;Database=dealoware;{trustSetting}";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(
                connectionString, Env(), strictNonDevelopment: true,
                fileExistsCheck: _ => true,
                certLoadCheck: _ => true));

        Assert.Contains("Trust Server Certificate=true is not permitted", ex.Message);
    }

    [Theory]
    [InlineData("Trust Server Certificate=false")]
    [InlineData("TrustServerCertificate=false")]
    public void Strict_PathB_TrustServerCertificateFalse_Allowed(string trustSetting)
    {
        var connectionString = $"Host=localhost;Database=dealoware;{trustSetting}";
        var selection = DatabaseProviderSelector.Select(
            connectionString, Env(), strictNonDevelopment: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
    }

    [Fact]
    public void Strict_WithSqliteConnectionString_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Select(
                "Data Source=dealoware.db", Env(), strictNonDevelopment: true,
                fileExistsCheck: _ => true, certLoadCheck: _ => true));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DB_HOST", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Host=", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strict_WithDbHost_SelectsPostgres()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db", env, strictNonDevelopment: true,
            fileExistsCheck: _ => true, certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
    }

    [Fact]
    public void Strict_WithHostConnectionString_SelectsPostgres()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder";

        var selection = DatabaseProviderSelector.Select(
            connectionString, Env(), strictNonDevelopment: true,
            fileExistsCheck: _ => true, certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
    }

    #endregion

    #region Shared behavior (both modes)

    [Fact]
    public void DbHost_InvalidPort_Throws()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "not-a-port"));

        Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(null, env, strictNonDevelopment: false));
    }

    #endregion

    #region Integration with AddInfrastructure

    [Fact]
    public void AddInfrastructure_NonStrict_WithDbHost_ConfiguresNpgsqlProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(
            "Data Source=dealoware.db",
            Env(("DB_HOST", "db.example"), ("DB_NAME", "dealoware")),
            strictNonDevelopment: false);

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
        services.AddInfrastructure(
            "Data Source=dealoware.db",
            Env(),
            strictNonDevelopment: false);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        Assert.True(db.Database.IsSqlite());
        Assert.False(db.Database.IsNpgsql());
    }

    [Fact]
    public void AddInfrastructure_Strict_WithSqlite_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure("Data Source=dealoware.db", Env(), strictNonDevelopment: true));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("placeholder-password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Root certificate path constant

    [Fact]
    public void RdsRootCertificatePath_IsCorrectImagePath()
    {
        Assert.Equal("/app/certs/rds-global-bundle.pem", DatabaseProviderSelector.RdsRootCertificatePath);
    }

    #endregion

    #region Host integration tests

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

    #endregion
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

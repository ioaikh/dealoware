using Dealoware.Infrastructure;
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

    #region Non-strict mode (Development) - keeps existing Require behavior

    [Fact]
    public void NonStrict_DbHost_BuildsNpgsqlConnectionString_WithSslRequire()
    {
        var env = Env(
            ("DB_HOST", "db.example"),
            ("DB_NAME", "dealoware"),
            ("DB_USERNAME", "app_user"),
            ("DB_PASSWORD", "placeholder-password"));

        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db", env, requireVerifiedTls: false);

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

        var selection = DatabaseProviderSelector.Select(null, env, requireVerifiedTls: false);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(6543, csb.Port);
        Assert.Equal(SslMode.Require, csb.SslMode);
    }

    [Fact]
    public void NonStrict_HostConnectionString_SelectsPostgres_Unchanged()
    {
        var connectionString = "Host=localhost;Port=5432;Database=dealoware;Username=app_user;Password=placeholder";
        var selection = DatabaseProviderSelector.Select(connectionString, Env(), requireVerifiedTls: false);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    #endregion

    #region Strict mode (Production) - enforces VerifyFull + RootCertificate

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
            requireVerifiedTls: true,
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
            null, env, requireVerifiedTls: true,
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
            connectionString, Env(), requireVerifiedTls: true,
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
                null, env, requireVerifiedTls: true,
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
                null, env, requireVerifiedTls: true,
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
                connectionString, Env(), requireVerifiedTls: true,
                fileExistsCheck: _ => true,
                certLoadCheck: _ => true));

        Assert.Contains("not permitted outside Development", ex.Message);
    }

    [Fact]
    public void Strict_PathB_VerifyFull_AllowedWithRootCert()
    {
        var connectionString = "Host=localhost;Database=dealoware;SSL Mode=VerifyFull";
        var selection = DatabaseProviderSelector.Select(
            connectionString, Env(), requireVerifiedTls: true,
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
                connectionString, Env(), requireVerifiedTls: true,
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
            connectionString, Env(), requireVerifiedTls: true,
            fileExistsCheck: _ => true,
            certLoadCheck: _ => true);

        Assert.Equal(DatabaseProvider.Postgres, selection.Provider);
        var csb = new NpgsqlConnectionStringBuilder(selection.ConnectionString);
        Assert.Equal(SslMode.VerifyFull, csb.SslMode);
    }

    #endregion

    #region Shared behavior (both modes)

    [Fact]
    public void DbHost_InvalidPort_Throws()
    {
        var env = Env(("DB_HOST", "localhost"), ("DB_PORT", "not-a-port"));

        Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderSelector.Select(null, env, requireVerifiedTls: false));
    }

    [Theory]
    [InlineData("Data Source=dealoware.db")]
    [InlineData("Data Source=TestDb;Mode=Memory;Cache=Shared")]
    public void SqliteConnectionString_StaysSqlite(string connectionString)
    {
        var selection = DatabaseProviderSelector.Select(connectionString, Env(), requireVerifiedTls: true);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
        Assert.Equal(connectionString, selection.ConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyDbHost_IsIgnored(string dbHost)
    {
        var selection = DatabaseProviderSelector.Select(
            "Data Source=dealoware.db",
            Env(("DB_HOST", dbHost)),
            requireVerifiedTls: false);

        Assert.Equal(DatabaseProvider.Sqlite, selection.Provider);
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
            requireVerifiedTls: false);

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
            requireVerifiedTls: false);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        Assert.True(db.Database.IsSqlite());
        Assert.False(db.Database.IsNpgsql());
    }

    #endregion

    #region Root certificate path constant

    [Fact]
    public void RdsRootCertificatePath_IsCorrectImagePath()
    {
        Assert.Equal("/app/certs/rds-global-bundle.pem", DatabaseProviderSelector.RdsRootCertificatePath);
    }

    #endregion
}

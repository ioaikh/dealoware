using System.Data.Common;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Npgsql;

// HostFactoryResolver.HostingListener (DiagnosticListener) deadlocks when
// WebApplicationFactory<Program> CreateHost/Dispose overlap. The password-reset
// collection was a third parallel TestServer; keep one in-process host at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Dealoware.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory with isolated in-memory SQLite database.
/// Each factory instance gets its own database to prevent race conditions
/// when tests run in parallel.
/// </summary>
public class IsolatedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _keeper;
    private readonly string? _sqliteConnectionString;
    private readonly string? _postgresConnectionString;

    /// <summary>Stable per-factory keys so a restarted host can decrypt the same rows.</summary>
    public string TotpKey { get; }

    public string RecoveryHmacKey { get; }

    public string IpHmacKey { get; }

    public CollectingLoggerProvider LogCollector { get; } = new();

    public CountingDbCommandInterceptor CommandCounter { get; } = new();

    public IsolatedWebApplicationFactory()
        : this(
            sqliteConnectionString: null,
            totpKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            recoveryHmacKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ipHmacKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
    {
    }

    /// <param name="sqliteConnectionString">
    /// File-backed store for SC-6 restart tests. Null uses isolated in-memory SQLite,
    /// or the process DB_* PostgreSQL when <c>DB_HOST</c> is set (CI postgres job).
    /// </param>
    internal IsolatedWebApplicationFactory(
        string? sqliteConnectionString,
        string totpKey,
        string recoveryHmacKey,
        string ipHmacKey)
    {
        TotpKey = totpKey;
        RecoveryHmacKey = recoveryHmacKey;
        IpHmacKey = ipHmacKey;

        if (!string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            _sqliteConnectionString = sqliteConnectionString;
        }
        else if (TryBuildTestPostgresConnectionString(out var postgres, PostgresDatabaseName))
        {
            EnsurePostgresDatabase(postgres);
            _postgresConnectionString = postgres;
        }
        else
        {
            var dbName = $"TestDb_{Guid.NewGuid():N}";
            _keeper = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
            _keeper.Open();
            using var pragma = _keeper.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            pragma.ExecuteNonQuery();
            _sqliteConnectionString = _keeper.ConnectionString;
        }
    }

    /// <summary>
    /// Optional dedicated PostgreSQL database so a fixture with its own CoreOwner
    /// config does not share rows with other collections on DB_NAME.
    /// </summary>
    protected virtual string? PostgresDatabaseName => null;

    /// <summary>
    /// CI postgres:16 has no TLS. Build a throwaway SSL Mode=Disable string from DB_*.
    /// </summary>
    internal static bool TryBuildTestPostgresConnectionString(out string connectionString)
        => TryBuildTestPostgresConnectionString(out connectionString, databaseOverride: null);

    internal static bool TryBuildTestPostgresConnectionString(
        out string connectionString,
        string? databaseOverride)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            connectionString = string.Empty;
            return false;
        }

        var port = Environment.GetEnvironmentVariable("DB_PORT");
        if (string.IsNullOrWhiteSpace(port))
            port = "5432";
        var database = string.IsNullOrWhiteSpace(databaseOverride)
            ? Environment.GetEnvironmentVariable("DB_NAME")
            : databaseOverride;
        if (string.IsNullOrWhiteSpace(database))
            database = "dealoware_ci";
        var username = Environment.GetEnvironmentVariable("DB_USERNAME");
        if (string.IsNullOrWhiteSpace(username))
            username = "dealoware_ci";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        connectionString =
            $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable";
        return true;
    }

    private static void EnsurePostgresDatabase(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        if (string.IsNullOrWhiteSpace(database)
            || !Regex.IsMatch(database, "^[a-z][a-z0-9_]*$"))
        {
            throw new InvalidOperationException("PostgreSQL test database name is not a safe identifier.");
        }

        builder.Database = "postgres";
        using var conn = new NpgsqlConnection(builder.ConnectionString);
        conn.Open();
        using (var exists = conn.CreateCommand())
        {
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
            exists.Parameters.AddWithValue("n", database);
            if (exists.ExecuteScalar() is not null)
            {
                return;
            }
        }

        using var create = conn.CreateCommand();
        create.CommandText = "CREATE DATABASE " + database;
        create.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Runtime-generated key so Production test hosts pass IIpHasher fail-closed.
        builder.UseSetting(IpHasher.KeyEnvironmentVariable, IpHmacKey);
        builder.UseSetting(TotpSecretProtector.KeyEnvironmentVariable, TotpKey);
        builder.UseSetting(AdminRecoveryCodeHasher.KeyEnvironmentVariable, RecoveryHmacKey);

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(LogCollector);
        });

        // After the app registers its provider (SQLite or Npgsql), replace it. The
        // default is isolated in-memory SQLite. When DB_HOST is set (postgres CI job)
        // use that server with SSL Mode=Disable. An explicit sqlite path (SC-6) wins.
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
                options.AddInterceptors(CommandCounter);
                if (_postgresConnectionString is not null)
                {
                    options.UseNpgsql(_postgresConnectionString);
                    return;
                }

                options.UseSqlite(_sqliteConnectionString);
                options.AddInterceptors(new SqliteBusyTimeoutInterceptor());
            });

            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IAdminMailSender)
                                     || d.ServiceType == typeof(ITurnstileVerifier)
                                     || d.ServiceType == typeof(IAdminClock)
                                     || d.ServiceType == typeof(FakeTurnstileVerifier)
                                     || d.ServiceType == typeof(TestAdminClock)
                                     || d.ServiceType == typeof(FakeAdminClock)
                                     || d.ServiceType == typeof(RecordingMailSender))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<RecordingMailSender>();
            services.AddSingleton<IAdminMailSender>(sp => sp.GetRequiredService<RecordingMailSender>());
            services.AddSingleton<FakeTurnstileVerifier>();
            services.AddSingleton<ITurnstileVerifier>(sp => sp.GetRequiredService<FakeTurnstileVerifier>());
            services.AddSingleton<FakeAdminClock>();
            services.AddSingleton<TestAdminClock>(sp => sp.GetRequiredService<FakeAdminClock>());
            services.AddSingleton<IAdminClock>(sp => sp.GetRequiredService<FakeAdminClock>());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Origin",
            AdminHostOptions.ProductionAdminOrigin);
    }

    private static void ReplaceSingleton<TService, TImpl>(IServiceCollection services)
        where TService : class
        where TImpl : class, TService
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(TService)).ToList())
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<TImpl>();
        services.AddSingleton<TService>(sp => sp.GetRequiredService<TImpl>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keeper?.Dispose();
        }
    }
}

/// <summary>
/// Collection definition for tests that use IsolatedWebApplicationFactory.
/// All test classes in this collection share the same factory instance
/// and run serially within the collection.
/// Password-reset tests stay in this collection as a class fixture so they
/// are not a third parallel xUnit collection next to WebAppTests and AdminAuthTests.
/// </summary>
[CollectionDefinition("WebAppTests")]
public class WebAppTestCollection : ICollectionFixture<IsolatedWebApplicationFactory>
{
}

[CollectionDefinition("AdminAuthTests")]
public class AdminAuthTestCollection : ICollectionFixture<IsolatedWebApplicationFactory>
{
}

/// <summary>
/// Password-reset suite sets CoreOwner through test config, not appsettings.
/// </summary>
public sealed class PasswordResetWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_pwreset";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>File-backed host used only by SC-6 restart tests. Not a collection fixture.</summary>
public sealed class SharedStoreWebApplicationFactory : IsolatedWebApplicationFactory
{
    public SharedStoreWebApplicationFactory(
        string sqliteConnectionString,
        string totpKey,
        string recoveryHmacKey,
        string ipHmacKey)
        : base(sqliteConnectionString, totpKey, recoveryHmacKey, ipHmacKey)
    {
    }
}

/// <summary>Counts EF commands so owner vs non-owner reset-request DB work can be compared.</summary>
public sealed class CountingDbCommandInterceptor : DbCommandInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Reset() => Volatile.Write(ref _count, 0);

    private void Hit() => Interlocked.Increment(ref _count);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Hit();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Hit();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Hit();
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Hit();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Hit();
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Hit();
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}

/// <summary>Per-connection busy_timeout so parallel redeem/attempt updates wait instead of failing.</summary>
file sealed class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

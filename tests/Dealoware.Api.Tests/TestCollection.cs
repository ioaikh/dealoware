using System.Data.Common;
using System.Security.Cryptography;
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

namespace Dealoware.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory with isolated in-memory SQLite database.
/// Each factory instance gets its own database to prevent race conditions
/// when tests run in parallel. When <c>DB_HOST</c> is set (PR #33 postgres
/// CI job on main), the host uses that PostgreSQL server instead.
/// </summary>
public class IsolatedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _connection;
    private readonly string? _sqliteConnectionString;
    private readonly string? _postgresConnectionString;

    public CollectingLoggerProvider LogCollector { get; } = new();

    public IsolatedWebApplicationFactory()
    {
        if (TryBuildTestPostgresConnectionString(out var postgres))
        {
            _postgresConnectionString = postgres;
            return;
        }

        var dbName = $"TestDb_{Guid.NewGuid():N}";
        _sqliteConnectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_sqliteConnectionString);
        _connection.Open();
        using var busy = _connection.CreateCommand();
        busy.CommandText = "PRAGMA busy_timeout = 5000;";
        busy.ExecuteNonQuery();
    }

    /// <summary>
    /// CI postgres:16 has no TLS. Build a throwaway SSL Mode=Disable string from DB_*.
    /// </summary>
    internal static bool TryBuildTestPostgresConnectionString(out string connectionString)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            connectionString = string.Empty;
            return false;
        }

        var port = Environment.GetEnvironmentVariable("DB_PORT");
        if (string.IsNullOrWhiteSpace(port))
        {
            port = "5432";
        }

        var database = Environment.GetEnvironmentVariable("DB_NAME");
        if (string.IsNullOrWhiteSpace(database))
        {
            database = "dealoware_ci";
        }

        var username = Environment.GetEnvironmentVariable("DB_USERNAME");
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "dealoware_ci";
        }

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        connectionString =
            $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable";
        return true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Runtime-generated key so Production test hosts pass IIpHasher fail-closed.
        builder.UseSetting(
            IpHasher.KeyEnvironmentVariable,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(LogCollector);
        });

        // After the app registers its provider (SQLite or Npgsql), replace it.
        // Default is isolated in-memory SQLite. When DB_HOST is set, use Postgres.
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
                if (_postgresConnectionString is not null)
                {
                    options.UseNpgsql(_postgresConnectionString);
                    return;
                }

                options.UseSqlite(_sqliteConnectionString);
                options.AddInterceptors(new SqliteBusyTimeoutInterceptor());
            });

            ReplaceSingleton<IAdminMailSender, RecordingMailSender>(services);
            ReplaceSingleton<IAdminSecondFactorVerifier, FakeAdminSecondFactorVerifier>(services);
            ReplaceSingleton<IAdminClock, FakeAdminClock>(services);
        });
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
            _connection?.Dispose();
        }
    }
}

/// <summary>
/// Collection definition for tests that use IsolatedWebApplicationFactory.
/// All test classes in this collection share the same factory instance
/// and run serially within the collection.
/// </summary>
[CollectionDefinition("WebAppTests")]
public class WebAppTestCollection : ICollectionFixture<IsolatedWebApplicationFactory>
{
}

file sealed class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetBusyTimeout(connection);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        SetBusyTimeout(connection);
        return Task.CompletedTask;
    }

    private static void SetBusyTimeout(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout = 5000;";
        cmd.ExecuteNonQuery();
    }
}

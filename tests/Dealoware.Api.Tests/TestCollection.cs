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
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory with isolated in-memory SQLite database.
/// Each factory instance gets its own database to prevent race conditions
/// when tests run in parallel.
/// </summary>
public class IsolatedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _keeper;
    private readonly string _sqliteConnectionString;

    /// <summary>Stable per-factory keys so a restarted host can decrypt the same rows.</summary>
    public string TotpKey { get; }

    public string RecoveryHmacKey { get; }

    public string IpHmacKey { get; }

    public IsolatedWebApplicationFactory()
        : this(
            sqliteConnectionString: null,
            totpKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            recoveryHmacKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ipHmacKey: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
    {
    }

    /// <param name="sqliteConnectionString">
    /// File-backed store for SC-6 restart tests. Null uses an isolated in-memory database.
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

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            var dbName = $"TestDb_{Guid.NewGuid():N}";
            _keeper = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
            _keeper.Open();
            using var pragma = _keeper.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            pragma.ExecuteNonQuery();
            _sqliteConnectionString = _keeper.ConnectionString;
        }
        else
        {
            _sqliteConnectionString = sqliteConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Runtime-generated key so Production test hosts pass IIpHasher fail-closed.
        builder.UseSetting(IpHasher.KeyEnvironmentVariable, IpHmacKey);
        builder.UseSetting(TotpSecretProtector.KeyEnvironmentVariable, TotpKey);
        builder.UseSetting(AdminRecoveryCodeHasher.KeyEnvironmentVariable, RecoveryHmacKey);

        // After the app registers its provider (SQLite or Npgsql), replace with this
        // factory's in-memory SQLite so Production Host= selection does not leave Npgsql
        // registered alongside SQLite. Use the shared-memory connection string so each
        // scope can open its own connection (needed for atomic parallel redeem tests).
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
                options.UseSqlite(_sqliteConnectionString);
                options.AddInterceptors(new SqliteBusyTimeoutInterceptor());
            });

            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IAdminMailSender)).ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IAdminMailSender, RecordingMailSender>();
        });
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
/// </summary>
[CollectionDefinition("WebAppTests")]
public class WebAppTestCollection : ICollectionFixture<IsolatedWebApplicationFactory>
{
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

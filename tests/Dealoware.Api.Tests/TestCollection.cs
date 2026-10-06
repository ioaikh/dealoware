using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    private readonly SqliteConnection _connection;
    private readonly string _dbName;

    public IsolatedWebApplicationFactory()
    {
        _dbName = $"TestDb_{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"Data Source={_dbName};Mode=Memory;Cache=Shared");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Runtime-generated key so Production test hosts pass IIpHasher fail-closed.
        builder.UseSetting(
            IpHasher.KeyEnvironmentVariable,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        // Non-Development hosts fail closed without a Turnstile secret. Test-only value.
        builder.UseSetting(
            TurnstileOptions.SecretEnvironmentVariable,
            "test-turnstile-secret-value");

        // After the app registers its provider (SQLite or Npgsql), replace with this
        // factory's in-memory SQLite so Production Host= selection does not leave Npgsql
        // registered alongside SQLite.
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
                options.UseSqlite(_connection);
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
            _connection.Dispose();
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

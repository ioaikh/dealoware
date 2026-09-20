using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DealowareDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DealowareDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
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

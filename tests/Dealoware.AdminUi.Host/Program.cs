using System.Text.Json;
using Dealoware.Api.Tests;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.AdminUi.Host;

/// <summary>
/// Local-only Kestrel wrapper around the admin UI test host (audit API stub).
/// Playwright talks to this process; it never targets admin.core.dealoware.com.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var listen = Environment.GetEnvironmentVariable("ADMIN_UI_URL")
                     ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                     ?? "http://127.0.0.1:4173";

        await using var factory = new PlaywrightAdminUiFactory(listen);
        _ = factory.Services;
        var sessionId = await factory.SeedKestrelSessionAsync();
        var info = new HostInfo(factory.ListenUrl, sessionId.ToString("D"), AdminUiWebApplicationFactory.AdminHost);
        var json = JsonSerializer.Serialize(info);
        var infoPath = Path.Combine(AppContext.BaseDirectory, "admin-ui-host.json");
        await File.WriteAllTextAsync(infoPath, json);
        Console.WriteLine(json);
        Console.WriteLine($"ADMIN_UI_HOST_INFO={infoPath}");
        await Task.Delay(Timeout.Infinite);
    }
}

public sealed record HostInfo(string BaseUrl, string SessionId, string AdminHost);

public sealed class PlaywrightAdminUiFactory : AdminUiWebApplicationFactory
{
    private IHost? _kestrel;

    public PlaywrightAdminUiFactory(string listenUrl)
    {
        ListenUrl = listenUrl;
    }

    public string ListenUrl { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseContentRoot(FindApiContentRoot());
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, ListenUrl);
        var dbPath = Path.Combine(Path.GetTempPath(), $"admin-ui-host-{Guid.NewGuid():N}.db");
        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(DbContextOptions<DealowareDbContext>)
                                     || d.ServiceType == typeof(DbContextOptions)
                                     || d.ServiceType == typeof(DealowareDbContext))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DealowareDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(web =>
        {
            web.UseKestrel();
            web.UseUrls(ListenUrl);
            web.UseContentRoot(FindApiContentRoot());
        });

        _kestrel = builder.Build();
        _kestrel.Start();

        var dummy = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseTestServer();
                web.Configure(_ => { });
            })
            .Build();
        dummy.Start();
        return dummy;
    }

    public async Task<Guid> SeedKestrelSessionAsync()
    {
        if (_kestrel is null)
            throw new InvalidOperationException("Kestrel host is not started.");

        using var scope = _kestrel.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(AdminUiWebApplicationFactory.CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static string FindApiContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Dealoware.Api");
            if (File.Exists(Path.Combine(candidate, "Dealoware.Api.csproj")))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate src/Dealoware.Api for the admin UI host.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrel?.Dispose();
        }

        base.Dispose(disposing);
    }
}

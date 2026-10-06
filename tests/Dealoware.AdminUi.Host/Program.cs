using System.Text.Json;
using Dealoware.Api.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var sessionId = await factory.SeedCoreOwnerSessionAsync();
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
        builder.UseKestrel();
        builder.UseUrls(ListenUrl);
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, ListenUrl);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(web =>
        {
            web.UseKestrel();
            web.UseUrls(ListenUrl);
        });

        _kestrel = builder.Build();
        _kestrel.Start();

        var addresses = _kestrel.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is { Count: > 0 })
        {
            ListenUrlObserved = addresses.First();
        }

        return _kestrel;
    }

    public string ListenUrlObserved { get; private set; } = "";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrel?.Dispose();
        }

        base.Dispose(disposing);
    }
}

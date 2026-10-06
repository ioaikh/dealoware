using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Admin viewer test host: real UI endpoints plus a GET /admin/api/audit stub
/// bound to Step 8 PR #32 @ db22afba. After #32 merges, rebase onto main and
/// drop the stub.
/// </summary>
public class AdminUiWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string AdminHost = "admin.core.dealoware.com";
    public const string CoreOwnerEmail = "io@aiknowhow.com";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminHost:AllowedHosts:0"] = AdminHost,
                ["AdminHost:AllowedHosts:1"] = "127.0.0.1",
                ["AdminHost:AllowedHosts:2"] = "localhost"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, AdminAuditApiStubStartupFilter>();
        });
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return client;
    }

    public async Task<Guid> SeedCoreOwnerSessionAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    public static HttpRequestMessage AdminGet(string path, string host, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = host;
        if (sessionId is { } id)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{AdminSessionCookie.Name}={id:D}");
        }

        return request;
    }
}

[CollectionDefinition("AdminUiWebAppTests")]
public class AdminUiWebAppTestCollection : ICollectionFixture<AdminUiWebApplicationFactory>
{
}

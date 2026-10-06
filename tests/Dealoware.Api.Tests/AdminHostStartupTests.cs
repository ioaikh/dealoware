using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Non-Development startup tests for A11 items 3 and 4.
/// WebApplicationFactory hosts keep the throw (the real Dealoware.Api process exits 1).
/// </summary>
public class AdminHostStartupTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Host_NonDevelopment_EnforceHostValidationFalse_FailsAtStartup(string environment)
    {
        using var factory = new AdminHostConfigWebApplicationFactory(environment, builder =>
            builder.UseSetting("AdminHost:EnforceHostValidation", "false"));

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = ExceptionMessages(ex!);
        Assert.Contains(messages, m => m == AdminHostOptionsValidator.EnforceHostValidationMessage);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Host_NonDevelopment_ExtraAllowedHost_FailsAtStartup(string environment)
    {
        using var factory = new AdminHostConfigWebApplicationFactory(environment, builder =>
            builder.UseSetting("AdminHost:AllowedHosts:1", "evil.example"));

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = ExceptionMessages(ex!);
        Assert.Contains(messages, m => m == AdminHostOptionsValidator.AllowedHostsMessage);
        Assert.DoesNotContain(messages, m => m.Contains("evil.example", StringComparison.Ordinal));
    }

    [Fact]
    public void Host_Production_ReplacementAllowedHost_FailsAtStartup()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Production", builder =>
            builder.UseSetting("AdminHost:AllowedHosts:0", "raw.alb.example"));

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = ExceptionMessages(ex!);
        Assert.Contains(messages, m => m == AdminHostOptionsValidator.AllowedHostsMessage);
    }

    [Fact]
    public async Task Host_Production_ExactAllowlist_Starts()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Host_Development_EnforceHostValidationFalse_Starts()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Development", builder =>
            builder.UseSetting("AdminHost:EnforceHostValidation", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Host_Development_ExtraAllowedHost_Starts()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Development", builder =>
            builder.UseSetting("AdminHost:AllowedHosts:1", "localhost"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Host_Production_EnableTestExceptionEndpointTrue_ThrowRouteReturns404()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Production", builder =>
            builder.UseSetting(AdminTestExceptionEndpoint.ConfigKey, "true"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.Database.EnsureCreated();
        var session = AdminSession.Create("io@aiknowhow.com", ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, AdminTestExceptionEndpoint.Path);
        request.Headers.Host = AdminHostOptions.ProductionAdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session.Id:D}");
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AdminTestExceptionEndpoint.ProbeExceptionMessage, body);
    }

    [Fact]
    public async Task Host_Production_EnableTestExceptionEndpointFalse_ThrowRouteReturns404()
    {
        using var factory = new AdminHostConfigWebApplicationFactory("Production", builder =>
            builder.UseSetting(AdminTestExceptionEndpoint.ConfigKey, "false"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        db.Database.EnsureCreated();
        var session = AdminSession.Create("io@aiknowhow.com", ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, AdminTestExceptionEndpoint.Path);
        request.Headers.Host = AdminHostOptions.ProductionAdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session.Id:D}");
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static List<string> ExceptionMessages(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        if (ex is AggregateException aggregate)
        {
            messages.AddRange(aggregate.Flatten().InnerExceptions.Select(e => e.Message));
        }

        return messages;
    }
}

/// <summary>
/// Isolated test host with an explicit environment and optional AdminHost settings.
/// </summary>
file sealed class AdminHostConfigWebApplicationFactory : IsolatedWebApplicationFactory
{
    private readonly string _environment;
    private readonly Action<IWebHostBuilder>? _configure;

    public AdminHostConfigWebApplicationFactory(string environment, Action<IWebHostBuilder>? configure = null)
    {
        _environment = environment;
        _configure = configure;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(_environment);
        builder.UseSetting(JwtSigningKeyValidator.EnvironmentVariableName, EnvironmentWebApplicationFactory.TestSigningKey64);
        builder.UseSetting("Jwt:SigningKey", string.Empty);
        builder.UseSetting(IpHasher.KeyEnvironmentVariable, EnvironmentWebApplicationFactory.TestIpHmacKey);
        if (!string.Equals(_environment, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=5432;Database=dealoware_test;Username=test;Password=test");
        }

        _configure?.Invoke(builder);
    }
}

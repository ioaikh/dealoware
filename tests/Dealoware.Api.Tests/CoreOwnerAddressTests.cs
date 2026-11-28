using System.Net;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// T59 spec §2.9 (answer 11): the owner address is not in the code or the shipped settings.
/// The engine reads it from configuration the environment supplies (the stack binds
/// <c>CoreOwner__Email</c> from a secret); without it, bootstrap-link refuses and nobody can
/// sign in.
/// </summary>
public class CoreOwnerAddressTests
{
    [Fact]
    public void CoreOwnerOptions_HaveNoBuiltInAddress()
    {
        Assert.Equal(string.Empty, new CoreOwnerOptions().Email);
    }

    [Fact]
    public void ShippedAppSettings_HoldNoOwnerAddress()
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(ShippedAppSettingsPath()));

        if (settings.RootElement.TryGetProperty(CoreOwnerOptions.SectionName, out var section)
            && section.TryGetProperty(nameof(CoreOwnerOptions.Email), out var email))
        {
            Assert.Equal(JsonValueKind.String, email.ValueKind);
            Assert.True(string.IsNullOrWhiteSpace(email.GetString()));
        }
    }

    [Fact]
    public async Task BootstrapLinkCommand_WithTheShippedSettingsAndNoOwnerAddress_Refuses()
    {
        var connectionString = $"Data Source=BootstrapShippedSettings_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();

        // What the one-shot reads in Production when the environment supplies no owner address:
        // the shipped appsettings.json and nothing else for CoreOwner.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(ShippedAppSettingsPath(), optional: false, reloadOnChange: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["environment"] = Environments.Production
            })
            .Build();
        Assert.True(string.IsNullOrWhiteSpace(configuration["CoreOwner:Email"]));

        var mail = new RecordingMailSender();
        var services = new ServiceCollection();
        services.AddLogging();
        AdminBootstrapLinkCommand.AddServices(services, configuration, _ => null, strictNonDevelopment: false);
        services.AddSingleton<IAdminMailSender>(mail);
        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DealowareDbContext>().Database.EnsureCreatedAsync();
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await AdminBootstrapLinkCommand.ExecuteAsync(provider, output, error, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(AdminBootstrapLinkCommand.OwnerNotConfiguredMessage, error.ToString().Trim());
        Assert.Empty(mail.Sent);
        await using var check = provider.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Empty(db.AdminBootstrapTokens.ToList());
        Assert.Empty(db.AdminAuditLog.ToList());
    }

    [Theory]
    [InlineData(null, "Production", true)]
    [InlineData("", "Production", true)]
    [InlineData("  ", "Staging", true)]
    [InlineData("core-owner@example.test", "Production", false)]
    [InlineData(null, "Development", false)]
    public void MissingOwnerWarning_IsGivenOutsideDevelopmentOnly(string? email, string environment, bool warns)
    {
        var warning = CoreOwnerOptions.MissingEmailWarning(email, environment);

        Assert.Equal(warns, warning is not null);
        if (warning is not null)
        {
            Assert.Equal(CoreOwnerOptions.MissingEmailWarningText, warning);
            Assert.Contains("CoreOwner__Email", warning, StringComparison.Ordinal);
            Assert.DoesNotContain("@", warning, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Host_ProductionWithoutAnOwnerAddress_StartsWarnsAndLetsNobodySignIn()
    {
        using var factory = new OwnerConfigWebApplicationFactory(
            Environments.Production,
            builder => builder.UseSetting("CoreOwner:Email", string.Empty));
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var warning = Assert.Single(
            factory.LogCollector.Lines,
            line => line.Contains(CoreOwnerOptions.MissingEmailWarningText, StringComparison.Ordinal));
        Assert.Contains("|Warning|", warning, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var directory = scope.ServiceProvider.GetRequiredService<IAdminCredentialDirectory>();
        Assert.False(directory.IsOwnerEmail(string.Empty));
        Assert.False(directory.IsOwnerEmail(TestCoreOwner.Email));
        Assert.False(await directory.VerifyPasswordAsync(string.Empty, "any-password-at-all", CancellationToken.None));
        var bootstrap = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var result = await bootstrap.IssueLinkAsync(CancellationToken.None);
        Assert.Equal(AdminBootstrapLinkOutcome.OwnerNotConfigured, result.Outcome);
        Assert.False(await result.MailSent);
    }

    [Fact]
    public async Task Host_ProductionWithAnOwnerAddress_DoesNotWarn()
    {
        using var factory = new OwnerConfigWebApplicationFactory(Environments.Production, _ => { });
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.DoesNotContain(
            factory.LogCollector.Lines,
            line => line.Contains(CoreOwnerOptions.MissingEmailWarningText, StringComparison.Ordinal));
    }

    private static string ShippedAppSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return Path.Combine(dir.FullName, "src", "Dealoware.Api", "appsettings.json");
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }
}

/// <summary>Isolated test host with an explicit environment, then the test's own settings.</summary>
file sealed class OwnerConfigWebApplicationFactory : IsolatedWebApplicationFactory
{
    private readonly string _environment;
    private readonly Action<IWebHostBuilder> _configure;

    public OwnerConfigWebApplicationFactory(string environment, Action<IWebHostBuilder> configure)
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
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=5432;Database=dealoware_test;Username=test;Password=test");
        builder.UseSetting(ForwardedHeadersTrust.ConfigurationKey, EnvironmentWebApplicationFactory.TestKnownNetworks);
        _configure(builder);
    }
}

using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Only Development may verify Turnstile against anything but Cloudflare. The
/// local browser tests (e2e/bootstrap) point the verifier at a stand-in; these
/// tests show a deployed host refuses to start that way.
/// WebApplicationFactory hosts keep the throw (the real Dealoware.Api process exits 1).
/// </summary>
public class TurnstileOptionsValidatorTests
{
    private const string StandIn = "http://127.0.0.1:18081/siteverify";

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_OutsideDevelopment_RefusesAnyOtherVerifyUrl(string? environment)
    {
        foreach (var url in new[]
                 {
                     StandIn,
                     "https://challenges.cloudflare.com/turnstile/v0/siteverify/",
                     "https://challenges.cloudflare.com.example/turnstile/v0/siteverify",
                     "http://challenges.cloudflare.com/turnstile/v0/siteverify",
                     " https://challenges.cloudflare.com/turnstile/v0/siteverify",
                     ""
                 })
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                TurnstileOptionsValidator.Validate(new TurnstileOptions { VerifyUrl = url }, environment));
            Assert.Equal(TurnstileOptionsValidator.VerifyUrlMessage, ex.Message);
            Assert.DoesNotContain("127.0.0.1", ex.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Validate_OutsideDevelopment_AcceptsTheCloudflareUrl_AndTheDefault(string environment)
    {
        TurnstileOptionsValidator.Validate(new TurnstileOptions(), environment);
        TurnstileOptionsValidator.Validate(null, environment);
        TurnstileOptionsValidator.Validate(
            new TurnstileOptions { VerifyUrl = TurnstileOptions.DefaultVerifyUrl, SiteKey = "any-public-key" },
            environment);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    public void Validate_Development_AllowsAStandIn(string environment)
    {
        TurnstileOptionsValidator.Validate(new TurnstileOptions { VerifyUrl = StandIn }, environment);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Host_NonDevelopment_StandInVerifyUrl_FailsAtStartup(string environment)
    {
        using var factory = new TurnstileConfigWebApplicationFactory(environment, builder =>
            builder.UseSetting("Admin:Turnstile:VerifyUrl", StandIn));

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        Assert.Contains(ExceptionMessages(ex!), m => m == TurnstileOptionsValidator.VerifyUrlMessage);
    }

    [Fact]
    public async Task Host_Production_DefaultVerifyUrl_AndASiteKey_Starts()
    {
        using var factory = new TurnstileConfigWebApplicationFactory("Production", builder =>
            builder.UseSetting("Admin:Turnstile:SiteKey", "any-public-key"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Host_Development_StandInVerifyUrl_Starts()
    {
        using var factory = new TurnstileConfigWebApplicationFactory("Development", builder =>
            builder.UseSetting("Admin:Turnstile:VerifyUrl", StandIn));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

/// <summary>Isolated test host with an explicit environment and Turnstile settings.</summary>
file sealed class TurnstileConfigWebApplicationFactory : IsolatedWebApplicationFactory
{
    private readonly string _environment;
    private readonly Action<IWebHostBuilder> _configure;

    public TurnstileConfigWebApplicationFactory(string environment, Action<IWebHostBuilder> configure)
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
            builder.UseSetting(
                ForwardedHeadersTrust.ConfigurationKey,
                EnvironmentWebApplicationFactory.TestKnownNetworks);
        }

        _configure(builder);
    }
}

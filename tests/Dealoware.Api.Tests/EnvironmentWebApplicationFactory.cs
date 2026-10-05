using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Isolated (in-memory SQLite) test host that runs under an explicit environment name with an
/// explicit JWT signing key setting. Host settings override process environment variables, so
/// these tests do not depend on (or leak into) the runner's environment. No Postgres or DB_HOST needed.
/// </summary>
public sealed class EnvironmentWebApplicationFactory : IsolatedWebApplicationFactory
{
    /// <summary>Test-only 64-character alphanumeric signing key. Not a real secret.</summary>
    public static readonly string TestSigningKey64 =
        string.Concat(Enumerable.Repeat("TestOnlyNotSecret0123456789", 3))[..64];

    private readonly string _environment;
    private readonly string? _signingKey;

    /// <param name="environment">Host environment name, e.g. Development, Staging, Production.</param>
    /// <param name="signingKey">Signing key setting; null means "no key configured".</param>
    public EnvironmentWebApplicationFactory(string environment, string? signingKey)
    {
        _environment = environment;
        _signingKey = signingKey;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(_environment);
        // Empty string = blank, which strict mode treats as "not set" and overrides any runner env value.
        builder.UseSetting(JwtSigningKeyValidator.EnvironmentVariableName, _signingKey ?? string.Empty);
        builder.UseSetting("Jwt:SigningKey", string.Empty);
        // Non-Development refuses silent SQLite. Supply a Host= connection string so Production/Staging
        // hosts pass provider selection; ConfigureServices still swaps to in-memory SQLite for the test.
        if (!string.Equals(_environment, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=5432;Database=dealoware_test;Username=test;Password=test");
        }
    }
}

using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dealoware.Api.Tests;

public sealed class FakeTurnstileVerifier : ITurnstileVerifier
{
    public const string ValidToken = "ok";

    public bool Unavailable { get; set; }

    public Task<TurnstileVerifyResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (Unavailable)
        {
            return Task.FromResult(new TurnstileVerifyResult(false));
        }

        return Task.FromResult(new TurnstileVerifyResult(
            string.Equals(token, ValidToken, StringComparison.Ordinal)));
    }
}

public sealed class ControllableAdminClock : IAdminClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
}

public sealed class BootstrapWebApplicationFactory : IsolatedWebApplicationFactory
{
    public FakeTurnstileVerifier Turnstile { get; } = new();

    public ControllableAdminClock Clock { get; } = new();

    public RecordingMailSender Mail
        => (RecordingMailSender)Services.GetRequiredService<IAdminMailSender>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITurnstileVerifier>();
            services.RemoveAll<IAdminClock>();
            services.AddSingleton<ITurnstileVerifier>(Turnstile);
            services.AddSingleton<IAdminClock>(Clock);
        });
    }
}

[CollectionDefinition("BootstrapTests")]
public sealed class BootstrapTestCollection : ICollectionFixture<BootstrapWebApplicationFactory>
{
}

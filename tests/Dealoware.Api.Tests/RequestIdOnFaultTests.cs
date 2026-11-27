using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Api.Observability;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Tests;

/// <summary>
/// T09 spec §4.1: every request returns its ID, also when an endpoint throws. On the engine host
/// the request-ID middleware turns the fault into a bare 500 (no body) outside Development; on the
/// admin host the admin's own 500 clears the response, and the ID is set again when the answer
/// starts. Neither answer shows anything of the fault, and the log line has its type, never its
/// message.
/// </summary>
public class RequestIdOnFaultTests
{
    private const string FaultText = "probe-fault-detail-7c1e-NOT-FOR-ANSWERS";

    [Fact]
    public async Task EngineHost_EndpointThrows_Answers500_WithTheId_AndNoDetail()
    {
        var capture = new FormatterCapture();
        using var baseFactory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(capture));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IParticipantRepository>();
                services.AddScoped(_ => ThrowingProxy<IParticipantRepository>.Create());
            });
        });
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/auth/register", new { });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", Assert.Single(response.Headers.GetValues("traceparent")));
        Assert.Equal(string.Empty, body);

        var line = capture.Lines
            .Select(l => JsonDocument.Parse(l).RootElement)
            .Single(e => e.GetProperty("Category").GetString() == typeof(RequestIdMiddleware).FullName);
        Assert.Equal("Error", line.GetProperty("Level").GetString());
        Assert.Contains(typeof(InvalidOperationException).FullName!, line.GetProperty("ExceptionType").GetString());
        Assert.DoesNotContain(FaultText, capture.Text);
    }

    [Fact]
    public async Task AdminHost_EndpointThrows_Answers500_WithTheId_AndNoDetail()
    {
        var options = new ArmedTurnstileOptions();
        using var baseFactory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOptions<TurnstileOptions>>();
            services.AddSingleton<IOptions<TurnstileOptions>>(options);
        }));
        using var client = factory.CreateClient();
        options.Armed = true; // after start-up, which reads the options once

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://admin.core.dealoware.com" + AdminSignedOutExemptions.SignIn);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", Assert.Single(response.Headers.GetValues("traceparent")));
        Assert.DoesNotContain(FaultText, body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    /// <summary>An implementation of <typeparamref name="T"/> whose every call throws.</summary>
    public class ThrowingProxy<T> : DispatchProxy where T : class
    {
        public static T Create() => DispatchProxy.Create<T, ThrowingProxy<T>>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException(FaultText);
    }

    /// <summary>Turnstile options that throw once armed (start-up reads them before that).</summary>
    private sealed class ArmedTurnstileOptions : IOptions<TurnstileOptions>
    {
        private readonly TurnstileOptions _value = new();

        public volatile bool Armed;

        public TurnstileOptions Value => Armed ? throw new InvalidOperationException(FaultText) : _value;
    }
}

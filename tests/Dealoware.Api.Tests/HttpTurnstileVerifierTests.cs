using System.Net;
using System.Text;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Tests;

public class HttpTurnstileVerifierTests
{
    [Fact]
    public async Task FailClosed_LogsErrorCodesOnly()
    {
        const string secret = "unit-test-secret-not-used-in-logs";
        const string token = "siteverify-token-must-not-appear";
        const string remoteIp = "203.0.113.55";
        var logger = new CaptureLogger();
        using var handler = new StubHandler(
            """{"success":false,"error-codes":["invalid-input-response","timeout-or-duplicate"],"hostname":"example.com"}""");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://challenges.cloudflare.com/") };
        var verifier = new HttpTurnstileVerifier(
            client,
            Options.Create(new TurnstileOptions
            {
                VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify"
            }),
            name => name == TurnstileOptions.SecretEnvironmentVariable ? secret : null,
            logger);

        var ok = await verifier.VerifyAsync(token, remoteIp, CancellationToken.None);

        Assert.False(ok);
        Assert.NotNull(logger.Last);
        var rendered = logger.Last.Message + " " + logger.Last.Rendered;
        Assert.Contains("invalid-input-response", rendered, StringComparison.Ordinal);
        Assert.Contains("timeout-or-duplicate", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(token, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(remoteIp, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("hostname", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", rendered, StringComparison.Ordinal);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CaptureLogger : ILogger<HttpTurnstileVerifier>
    {
        public CaptureEntry? Last { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Last = new CaptureEntry(
                formatter(state, exception),
                state!,
                state is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? string.Join('|', pairs.Select(p => $"{p.Key}={p.Value}"))
                    : state?.ToString() ?? string.Empty);
        }
    }

    private sealed record CaptureEntry(string Message, object State, string Rendered);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

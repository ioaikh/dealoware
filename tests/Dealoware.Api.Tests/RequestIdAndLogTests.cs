using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Tests;

/// <summary>
/// The test host for the request ID and the log lines: the isolated host, with every log entry also
/// written through the engine's JSON line formatter (<see cref="FormatterCapture"/>), and one probe
/// path that writes a log line inside a request.
/// </summary>
public sealed class LogCaptureWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string ProbePath = "/__test/log-probe";

    public FormatterCapture Capture { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // Debug and up from every category: stricter than the engine's own levels, so the privacy
        // test also covers what the framework says below Warning.
        builder.ConfigureLogging(logging => logging
            .AddProvider(Capture)
            .AddFilter<FormatterCapture>(category: null, level: LogLevel.Debug));
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, LogProbeStartupFilter>());
    }

    private sealed class LogProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, inner) =>
            {
                if (context.Request.Path == ProbePath)
                {
                    var log = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Dealoware.LogProbe");
                    log.LogWarning("Probe line one");
                    log.LogError("Probe line two");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                await inner();
            });
            next(app);
        };
    }
}

/// <summary>
/// T09 spec §4.1: every request has an ID (the trace ID of a valid incoming W3C traceparent, or a
/// new one), returned in traceparent and written on every log line of the request; log lines are
/// one JSON object each with Time, Level, Category, Message and TraceId.
/// </summary>
public class RequestIdAndLogTests : IClassFixture<LogCaptureWebApplicationFactory>
{
    private const string IncomingTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
    private const string IncomingParent = "00-" + IncomingTraceId + "-00f067aa0ba902b7-01";

    private readonly LogCaptureWebApplicationFactory _factory;

    public RequestIdAndLogTests(LogCaptureWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RequestId_WithoutTraceparent_AnswersANewValidOne()
    {
        using var client = _factory.CreateClient();
        var first = await client.GetAsync("/health");
        var second = await client.GetAsync("/health");

        var a = TraceIdOf(first);
        var b = TraceIdOf(second);
        Assert.NotEqual(a, b);
        Assert.NotEqual(new string('0', 32), a);
    }

    [Fact]
    public async Task RequestId_WithAValidTraceparent_KeepsItsTraceId()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("traceparent", IncomingParent);

        var response = await client.SendAsync(request);

        Assert.Equal(IncomingTraceId, TraceIdOf(response));
    }

    [Theory]
    [InlineData("not-a-traceparent")]
    [InlineData("00-00000000000000000000000000000000-00f067aa0ba902b7-01")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e473G-00f067aa0ba902b7-01")]
    public async Task RequestId_WithAnInvalidTraceparent_AnswersANewOne(string incoming)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("traceparent", incoming);

        var response = await client.SendAsync(request);

        var id = TraceIdOf(response);
        Assert.NotEqual(new string('0', 32), id);
        Assert.DoesNotContain(id, incoming, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestId_IsAnsweredEvenWhenAGateRefusesTheRequest()
    {
        // /admin/ on the engine host is refused by the host gate (404); the ID is still there.
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/admin/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Matches("^[0-9a-f]{32}$", TraceIdOf(response));
    }

    [Fact]
    public async Task LogLines_AreJsonObjects_CarryingTheRequestId()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, LogCaptureWebApplicationFactory.ProbePath);
        request.Headers.TryAddWithoutValidation("traceparent", IncomingParent);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var probeLines = _factory.Capture.Lines
            .Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.GetProperty("Category").GetString() == "Dealoware.LogProbe")
            .ToList();
        Assert.Equal(2, probeLines.Count);
        foreach (var line in probeLines)
        {
            Assert.Equal(IncomingTraceId, line.GetProperty("TraceId").GetString());
            Assert.True(DateTimeOffset.TryParse(line.GetProperty("Time").GetString(), out _));
            Assert.StartsWith("Probe line", line.GetProperty("Message").GetString());
            // Exactly the policy's fields: nothing else (no state, no scopes).
            Assert.Equal(
                new[] { "Time", "Level", "Category", "Message", "TraceId" },
                line.EnumerateObject().Select(p => p.Name).ToArray());
        }

        Assert.Equal(new[] { "Warning", "Error" }, probeLines.Select(l => l.GetProperty("Level").GetString()).ToArray());
    }

    [Fact]
    public void LogLines_OutsideARequest_HaveNoTraceId_AndOneLineEach()
    {
        var capture = new FormatterCapture();
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            capture.CreateLogger("Dealoware.Test").LogInformation("first\nsecond\r\nthird");
        }
        finally
        {
            Activity.Current = previous;
        }

        var line = Assert.Single(capture.Lines);
        var json = JsonDocument.Parse(line).RootElement;
        Assert.False(json.TryGetProperty("TraceId", out _));
        Assert.Equal("first\nsecond\r\nthird", json.GetProperty("Message").GetString());
    }

    [Fact]
    public void TheEngine_WritesItsConsoleLogsWithTheJsonLineFormatter()
    {
        var options = _factory.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
        Assert.Equal(JsonLineConsoleFormatter.FormatterName, options.FormatterName);
        Assert.Contains(
            _factory.Services.GetServices<ConsoleFormatter>(),
            f => f is JsonLineConsoleFormatter);
    }

    private static string TraceIdOf(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("traceparent", out var values), "no traceparent in the answer");
        var value = Assert.Single(values);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", value);
        return value.Substring(3, 32);
    }
}

/// <summary>
/// The log-privacy test (T09 spec §4.1): requests that carry an email address, a token, an API key
/// and a password go through the engine, and an entry that carries them in its scope, its values and
/// its exception goes through the logger. None of them appears in any JSON log line.
/// </summary>
public class LogPrivacyTests : IClassFixture<LogCaptureWebApplicationFactory>
{
    private const string ProbeEmail = "privacy-probe@example.invalid";
    private const string ProbePassword = "Pw-probe-7f3c9e1d-NOT-REAL";
    private const string ProbeToken = "eyJwcm9iZSI6dHJ1ZX0.probe-token-5b2d.NOT-REAL";
    private const string ProbeApiKey = "dk_probe_apikey_91c4_NOT_REAL";

    private static readonly string[] Probes = [ProbeEmail, ProbePassword, ProbeToken, ProbeApiKey];

    private readonly LogCaptureWebApplicationFactory _factory;

    public LogPrivacyTests(LogCaptureWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Requests_WithAnEmailTokenAndPassword_LeaveNoneOfThemInTheLogs()
    {
        _factory.Capture.Clear();
        using var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/auth/token", new { apiKey = ProbeApiKey });
        await client.PostAsJsonAsync($"/auth/register?email={Uri.EscapeDataString(ProbeEmail)}", new { displayName = ProbeEmail });

        using (var profile = new HttpRequestMessage(HttpMethod.Get, "/profile"))
        {
            profile.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ProbeToken);
            await client.SendAsync(profile);
        }

        using (var signIn = new HttpRequestMessage(HttpMethod.Post, "http://admin.core.dealoware.com" + AdminSignedOutExemptions.ApiSignIn))
        {
            signIn.Content = JsonContent.Create(new { email = ProbeEmail, password = ProbePassword, turnstileToken = ProbeToken });
            signIn.Headers.TryAddWithoutValidation("Origin", "https://admin.core.dealoware.com");
            await client.SendAsync(signIn);
        }

        // Something was logged, so the check below is not vacuous.
        Assert.NotEmpty(_factory.Capture.Lines);
        AssertNoProbe(_factory.Capture.Text);
    }

    [Fact]
    public void AnEntry_CarryingThemInScopeValuesAndException_WritesNoneOfThem()
    {
        var capture = new FormatterCapture();
        var scopes = new LoggerExternalScopeProvider();
        capture.SetScopeProvider(scopes);
        var log = capture.CreateLogger("Dealoware.Privacy");

        using (log.BeginScope(new Dictionary<string, object> { ["Email"] = ProbeEmail, ["Authorization"] = "Bearer " + ProbeToken }))
        {
            log.Log(
                LogLevel.Error,
                new EventId(1),
                new Dictionary<string, object> { ["Password"] = ProbePassword, ["ApiKey"] = ProbeApiKey },
                new InvalidOperationException($"failed for {ProbeEmail} with {ProbePassword}"),
                (_, _) => "Sign-in failed");
        }

        var line = Assert.Single(capture.Lines);
        AssertNoProbe(line);
        var json = JsonDocument.Parse(line).RootElement;
        Assert.Equal("Sign-in failed", json.GetProperty("Message").GetString());
        Assert.Equal(typeof(InvalidOperationException).FullName, json.GetProperty("ExceptionType").GetString());
    }

    private static void AssertNoProbe(string text)
    {
        foreach (var probe in Probes)
        {
            foreach (var form in new[] { probe, Uri.EscapeDataString(probe) })
            {
                var leak = text.Split('\n').FirstOrDefault(l => l.Contains(form, StringComparison.OrdinalIgnoreCase));
                Assert.True(leak is null, $"a log line carries a probe value: {leak}");
            }
        }
    }
}

/// <summary>The formatter's two safety nets on a message: query strings and email-shaped text.</summary>
public class LogRedactionTests
{
    [Theory]
    [InlineData("Request starting HTTP/1.1 GET http://localhost/a/b?x=1&y=2 - -", "Request starting HTTP/1.1 GET http://localhost/a/b?<query> - -")]
    [InlineData("GET /auth/register?email=a%40b.example done", "GET /auth/register?<query> done")]
    [InlineData("for someone@example.invalid today", "for <email> today")]
    [InlineData("encoded someone%40example.invalid", "encoded <email>")]
    [InlineData("Is it up? yes", "Is it up? yes")]
    [InlineData("Readiness check failed: TimeoutException", "Readiness check failed: TimeoutException")]
    public void Redact_RemovesQueryStringsAndEmailShapedText(string message, string expected)
        => Assert.Equal(expected, JsonLineConsoleFormatter.Redact(message));
}

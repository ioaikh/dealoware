using System.Collections.Concurrent;
using System.Diagnostics;
using Dealoware.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Dealoware.Api.Tests;

/// <summary>
/// Traces (T09 spec §4.2). Off by default. With <c>OTEL_TRACES_EXPORTER=otlp</c> the engine sends
/// its spans over OTLP/HTTP to the endpoint and with the headers the standard OTEL_EXPORTER_OTLP_*
/// settings name, from its own process: no second container. The receiver here is a local stand-in
/// on the loopback address; nothing leaves the machine.
/// </summary>
public class TracingExportTests
{
    [Fact]
    public void Traces_AreOffByDefault()
    {
        using var factory = new IsolatedWebApplicationFactory();
        _ = factory.Services;

        Assert.Null(factory.Services.GetService<TracerProvider>());
    }

    [Fact]
    public void Traces_StayOffWhenTheExporterIsNone()
    {
        using var factory = new TracingFactory(new Dictionary<string, string?>
        {
            [ObservabilityExtensions.TracesExporterKey] = "none",
        });
        _ = factory.Services;

        Assert.Null(factory.Services.GetService<TracerProvider>());
    }

    [Fact]
    public async Task Traces_WhenSwitchedOn_AreSentOverOtlpHttp_WithTheConfiguredHeader()
    {
        await using var receiver = await OtlpReceiver.StartAsync();
        using var factory = new TracingFactory(new Dictionary<string, string?>
        {
            [ObservabilityExtensions.TracesExporterKey] = "otlp",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = receiver.BaseAddress,
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_HEADERS"] = "Authorization=Bearer test-only-not-a-real-value",
            [ObservabilityExtensions.SampleRatioKey] = "1",
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var provider = factory.Services.GetRequiredService<TracerProvider>();
        Assert.True(provider.ForceFlush(10_000), "the exporter did not flush");

        var post = Assert.Single(receiver.Requests, r => r.Path == "/v1/traces");
        Assert.Equal("POST", post.Method);
        Assert.Equal("application/x-protobuf", post.ContentType);
        Assert.Equal("Bearer test-only-not-a-real-value", post.Authorization);
        Assert.True(post.BodyLength > 0);
    }

    [Fact]
    public void Sampler_SamplesTheRatio_AndRecordsTheRest()
    {
        var none = new ObservabilityExtensions.RatioOrRecordSampler(0);
        var all = new ObservabilityExtensions.RatioOrRecordSampler(1);
        var parameters = new SamplingParameters(default, ActivityTraceId.CreateRandom(), "GET", ActivityKind.Server);

        Assert.Equal(SamplingDecision.RecordOnly, none.ShouldSample(parameters).Decision);
        Assert.Equal(SamplingDecision.RecordAndSample, all.ShouldSample(parameters).Decision);
    }

    [Fact]
    public void AFailedSpan_IsMarkedForExport_EvenWhenNotSampled()
    {
        var processor = new ObservabilityExtensions.ExportFailedSpansProcessor();
        using var failed = new Activity("failed").Start();
        failed.ActivityTraceFlags = ActivityTraceFlags.None;
        failed.SetStatus(ActivityStatusCode.Error);
        using var fine = new Activity("fine").Start();
        fine.ActivityTraceFlags = ActivityTraceFlags.None;

        processor.OnEnd(failed);
        processor.OnEnd(fine);

        Assert.True(failed.Recorded);
        Assert.False(fine.Recorded);
    }

    private sealed class TracingFactory(IDictionary<string, string?> settings) : IsolatedWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }
    }

    /// <summary>A local OTLP/HTTP stand-in on 127.0.0.1 that records what it is sent.</summary>
    private sealed class OtlpReceiver : IAsyncDisposable
    {
        public sealed record Received(string Method, string Path, string? ContentType, string? Authorization, long BodyLength);

        private readonly WebApplication _app;
        private readonly ConcurrentQueue<Received> _requests;

        private OtlpReceiver(WebApplication app, ConcurrentQueue<Received> requests, string baseAddress)
        {
            _app = app;
            _requests = requests;
            BaseAddress = baseAddress;
        }

        public string BaseAddress { get; }

        public IReadOnlyList<Received> Requests => _requests.ToArray();

        public static async Task<OtlpReceiver> StartAsync()
        {
            var requests = new ConcurrentQueue<Received>();
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            app.Run(async context =>
            {
                using var body = new MemoryStream();
                await context.Request.Body.CopyToAsync(body);
                requests.Enqueue(new Received(
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    context.Request.ContentType,
                    context.Request.Headers.Authorization.ToString(),
                    body.Length));
                context.Response.StatusCode = StatusCodes.Status200OK;
            });
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            return new OtlpReceiver(app, requests, address);
        }

        public async ValueTask DisposeAsync() => await _app.DisposeAsync();
    }
}

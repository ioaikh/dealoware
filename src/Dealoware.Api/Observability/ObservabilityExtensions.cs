using System.Diagnostics;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Dealoware.Api.Observability;

/// <summary>
/// Logs and traces (T09 spec §4.1 and §4.2).
/// <list type="bullet">
/// <item>Logs: the console only, one JSON object per line (<see cref="JsonLineConsoleFormatter"/>).</item>
/// <item>Traces: the OpenTelemetry SDK, for incoming HTTP requests and database calls. Off unless
/// <c>OTEL_TRACES_EXPORTER</c> is <c>otlp</c>. Then spans go to the OTLP endpoint named by the
/// standard <c>OTEL_EXPORTER_OTLP_*</c> settings (endpoint, protocol, headers). The engine names
/// no tracing service: where the spans go is the deployment's choice.</item>
/// <item>Sampling: <see cref="DefaultSampleRatio"/> of traces (<c>Tracing:SampleRatio</c>), plus
/// every request that fails.</item>
/// </list>
/// </summary>
public static class ObservabilityExtensions
{
    public const string TracesExporterKey = "OTEL_TRACES_EXPORTER";
    public const string OtlpExporterValue = "otlp";
    public const string SampleRatioKey = "Tracing:SampleRatio";
    public const double DefaultSampleRatio = 0.05;
    public const string ServiceNameDefault = "dealoware-api";

    /// <summary>The database driver's own activity source (PostgreSQL).</summary>
    public const string NpgsqlSource = "Npgsql";

    public static WebApplicationBuilder AddDealowareObservability(this WebApplicationBuilder builder)
    {
        // The host's default console logger, switched to JSON lines. Other providers are left as
        // they are (none of them writes to the container's output).
        builder.Logging.AddConsole(options => options.FormatterName = JsonLineConsoleFormatter.FormatterName);
        builder.Logging.AddConsoleFormatter<JsonLineConsoleFormatter, ConsoleFormatterOptions>();

        if (TracesExportOn(builder.Configuration))
        {
            var ratio = builder.Configuration.GetValue(SampleRatioKey, DefaultSampleRatio);
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .SetSampler(new RatioOrRecordSampler(ratio))
                    .AddAspNetCoreInstrumentation()
                    .AddSource(NpgsqlSource)
                    // Before the exporter: a failed request's span is exported even when not sampled.
                    .AddProcessor(new ExportFailedSpansProcessor())
                    .AddOtlpExporter());
        }

        return builder;
    }

    /// <summary>True only when <c>OTEL_TRACES_EXPORTER</c> is <c>otlp</c>. Unset or <c>none</c>: off.</summary>
    public static bool TracesExportOn(IConfiguration configuration)
        => string.Equals(configuration[TracesExporterKey]?.Trim(), OtlpExporterValue, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Records every span (so a failure can still be exported) and marks a share of traces as
    /// sampled, by trace ID, so all spans of a trace agree.
    /// </summary>
    public sealed class RatioOrRecordSampler : Sampler
    {
        private readonly TraceIdRatioBasedSampler _ratio;

        public RatioOrRecordSampler(double ratio)
        {
            _ratio = new TraceIdRatioBasedSampler(Math.Clamp(ratio, 0.0, 1.0));
            Description = $"RatioOrRecord{{{ratio}}}";
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            var sampled = _ratio.ShouldSample(samplingParameters).Decision == SamplingDecision.RecordAndSample;
            return new SamplingResult(sampled ? SamplingDecision.RecordAndSample : SamplingDecision.RecordOnly);
        }
    }

    /// <summary>
    /// A span that ended in error (an HTTP 5xx, an exception) is marked sampled, so the exporter
    /// that runs after this processor sends it.
    /// </summary>
    public sealed class ExportFailedSpansProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data)
        {
            if (data.Status == ActivityStatusCode.Error)
            {
                data.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }
        }
    }
}

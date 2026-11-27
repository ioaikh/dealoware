using Microsoft.Extensions.Logging.Console;

namespace Dealoware.Api.Observability;

/// <summary>
/// Logs (T09 spec §4.1).
/// <list type="bullet">
/// <item>Logs: the console only, one JSON object per line (<see cref="JsonLineConsoleFormatter"/>).</item>
/// </list>
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddDealowareObservability(this WebApplicationBuilder builder)
    {
        // The host's default console logger, switched to JSON lines. Other providers are left as
        // they are (none of them writes to the container's output).
        builder.Logging.AddConsole(options => options.FormatterName = JsonLineConsoleFormatter.FormatterName);
        builder.Logging.AddConsoleFormatter<JsonLineConsoleFormatter, ConsoleFormatterOptions>();

        return builder;
    }

}

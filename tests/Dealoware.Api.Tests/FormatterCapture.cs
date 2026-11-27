using System.Collections.Concurrent;
using Dealoware.Api.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dealoware.Api.Tests;

/// <summary>
/// A logger provider that writes every entry through the engine's own console formatter
/// (<see cref="JsonLineConsoleFormatter"/>), synchronously, into memory. So a test sees exactly the
/// lines the container's output would carry, with the request ID of the request that wrote them.
/// </summary>
public sealed class FormatterCapture : ILoggerProvider, ISupportExternalScope
{
    private readonly JsonLineConsoleFormatter _formatter = new();
    private readonly ConcurrentQueue<string> _lines = new();
    private IExternalScopeProvider? _scopes;

    public IReadOnlyList<string> Lines => _lines.ToArray();

    public string Text => string.Join("\n", _lines);

    public void Clear()
    {
        while (_lines.TryDequeue(out _))
        {
        }
    }

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(FormatterCapture owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => owner._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            using var writer = new StringWriter();
            var entry = new LogEntry<TState>(logLevel, category, eventId, state, exception, formatter);
            owner._formatter.Write(in entry, owner._scopes, writer);
            foreach (var line in writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                owner._lines.Enqueue(line);
            }
        }
    }
}

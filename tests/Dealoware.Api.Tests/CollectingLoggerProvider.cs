using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

public sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines.ToArray();

    public void Clear()
    {
        while (_lines.TryDequeue(out _))
        {
        }
    }

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, _lines);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<string> _lines;

        public CollectingLogger(string category, ConcurrentQueue<string> lines)
        {
            _category = category;
            _lines = lines;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _lines.Enqueue(
                $"{_category}|{logLevel}|{message}|{exception?.GetType().FullName}|{exception?.Message}");
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

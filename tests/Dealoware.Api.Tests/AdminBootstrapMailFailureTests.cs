using System.Collections.Concurrent;
using System.Diagnostics;
using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

/// <summary>
/// N4: bootstrap mail dispatch failure logs exception type and trace ID only.
/// Uses a throwing sender for this case only; the shared factory still uses
/// RecordingMailSender.
/// </summary>
public class AdminBootstrapMailFailureTests
{
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    [Fact]
    public async Task N4_BootstrapMailDispatchFailure_LogsExceptionTypeAndTraceId_Only()
    {
        using var factory = new ThrowingMailWebApplicationFactory();
        using var activity = new Activity("n4-bootstrap-mail");
        activity.Start();
        var traceId = activity.TraceId.ToString();

        using var scope = factory.Services.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
        var issued = await bootstrap.IssueLinkAsync("testhmac");

        Assert.False(issued.Sent);
        Assert.Equal(AdminAuthMessages.InvalidOrExpiredLink, issued.Error);

        var attempted = factory.Sender.Attempted;
        Assert.NotNull(attempted);
        var body = attempted!.TextBody;
        var start = body.IndexOf(AdminMailPagePaths.Origin, StringComparison.Ordinal);
        Assert.True(start >= 0, body);
        var end = body.IndexOfAny(['\r', '\n'], start);
        var link = end < 0 ? body[start..] : body[start..end];
        var uri = new Uri(link);
        Assert.StartsWith("#token=", uri.Fragment, StringComparison.Ordinal);
        var token = Uri.UnescapeDataString(uri.Fragment["#token=".Length..]);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var logs = string.Join('\n', factory.LogSink.Entries);
        Assert.Contains("Bootstrap mail dispatch failed", logs, StringComparison.Ordinal);
        Assert.Contains(typeof(InvalidOperationException).FullName!, logs, StringComparison.Ordinal);
        Assert.Contains(traceId, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(CoreOwnerEmail, logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(attempted.To, logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(link, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("#token=", logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AdminMailPagePaths.Origin, logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(uri.Fragment, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("dispatch failed to=", logs, StringComparison.Ordinal);

        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Empty(db.AdminBootstrapTokens.ToList());
        Assert.Empty(db.AdminAuditLog.ToList());
    }

    [Fact]
    public void N4_BootstrapMailFailure_Source_NeverLogsRecipientLinkOrToken()
    {
        var root = RepoRoot();
        var service = File.ReadAllText(Path.Combine(
            root, "src", "Dealoware.Infrastructure", "Admin", "AdminBootstrapService.cs"));
        Assert.Contains("N4", service, StringComparison.Ordinal);
        Assert.Contains("ExceptionType={ExceptionType}", service, StringComparison.Ordinal);
        Assert.Contains("TraceId={TraceId}", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.Write", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.Error", service, StringComparison.Ordinal);
        Assert.DoesNotContain("LogError(ex", service, StringComparison.Ordinal);
        Assert.DoesNotContain("LogError(exception", service, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{email}", service, StringComparison.Ordinal);
        Assert.DoesNotContain("{raw}", service, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", service, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.ToString()", service, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }
}

/// <summary>
/// Throws after capturing the attempted message so the test can prove those
/// values never appear in logs. Not the shared RecordingMailSender double.
/// </summary>
public sealed class ThrowingAdminMailSender : IAdminMailSender
{
    public AdminMailMessage? Attempted { get; private set; }

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        Attempted = message;
        throw new InvalidOperationException(
            "dispatch failed to=" + message.To + " body=" + message.TextBody);
    }
}

public sealed class N4LogSinkProvider : ILoggerProvider
{
    public ConcurrentBag<string> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName)
        => new SinkLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    private sealed class SinkLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentBag<string> _entries;

        public SinkLogger(string category, ConcurrentBag<string> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var line = logLevel + " " + _category + " " + formatter(state, exception);
            if (exception is not null)
            {
                line += " " + exception;
            }

            _entries.Add(line);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public sealed class ThrowingMailWebApplicationFactory : IsolatedWebApplicationFactory
{
    public ThrowingAdminMailSender Sender { get; } = new();

    public N4LogSinkProvider LogSink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(LogSink);
        });
        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IAdminMailSender)).ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IAdminMailSender>(Sender);
        });
    }
}

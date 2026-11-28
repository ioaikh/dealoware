using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Application.Admin;
using Dealoware.Infrastructure.Mail;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

/// <summary>
/// T59 spec §2.8: when SES refuses a send, the exception carries the HTTP status as a number,
/// and the queue's log line gives that number and the error type. Nothing of the message (the
/// address, the subject, the body, the link or its token) and not the exception's text.
/// </summary>
public class AdminMailSendQueueLogTests
{
    private const string Recipient = "queue-log-recipient@example.test";
    private const string Token = "queue-log-test-token";
    private const string KeyId = "QUEUELOGKEYID";
    private const string SigningValue = "queue-log-signing-value";

    [Fact]
    public async Task SesRefusal_LogsTheStatusAndTheTypeOnly()
    {
        // SES's own answer names the recipient; none of it may reach the log.
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(
            "{\"message\":\"Email address is not verified. The following identities failed the check: " + Recipient + "\"}",
            HttpStatusCode.BadRequest));
        var sender = new SesMailSender(
            new SesMailOptions("sender@example.test", "test-region", KeyId, SigningValue),
            new HttpClient(handler));

        var line = await DispatchAndReadTheLogLineAsync(sender, "trace-queue-1");

        Assert.Contains("|Error|", line, StringComparison.Ordinal);
        Assert.Contains("ExceptionType=" + typeof(SesMailSendException).FullName, line, StringComparison.Ordinal);
        Assert.Contains("HttpStatus=400", line, StringComparison.Ordinal);
        Assert.Contains("TraceId=trace-queue-1", line, StringComparison.Ordinal);
        AssertNothingOfTheMessage(line);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task FailureBeforeSes_LogsNoStatus()
    {
        var line = await DispatchAndReadTheLogLineAsync(new DisabledMailSender(), "trace-queue-2");

        Assert.Contains("ExceptionType=System.InvalidOperationException", line, StringComparison.Ordinal);
        Assert.Contains("HttpStatus=" + AdminMailSendQueue.NoStatus + " ", line, StringComparison.Ordinal);
        AssertNothingOfTheMessage(line);
    }

    [Fact]
    public async Task SesRefusal_ExceptionCarriesTheStatusAsATypedProperty()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{\"message\":\"" + Recipient + "\"}", HttpStatusCode.TooManyRequests));
        var sender = new SesMailSender(
            new SesMailOptions("sender@example.test", "test-region", KeyId, SigningValue),
            new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<SesMailSendException>(() => sender.SendAsync(
            new Dealoware.Domain.Admin.AdminMailMessage(Recipient, AdminMailDispatcher.BootstrapSubject, "body " + Token)));

        Assert.Equal(429, ex.StatusCode);
        Assert.IsAssignableFrom<InvalidOperationException>(ex);
        Assert.DoesNotContain(Recipient, ex.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Token, ex.ToString(), StringComparison.Ordinal);
    }

    private static async Task<string> DispatchAndReadTheLogLineAsync(Dealoware.Domain.Admin.IAdminMailSender sender, string traceId)
    {
        var logs = new CollectingLoggerProvider();
        using var loggers = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(logs);
        });
        var queue = new AdminMailSendQueue(new AdminMailDispatcher(sender), loggers.CreateLogger<AdminMailSendQueue>());

        await queue.StartAsync(CancellationToken.None);
        try
        {
            Assert.False(await queue.EnqueueBootstrap(Recipient, Token, traceId).WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            await queue.StopAsync(CancellationToken.None);
        }

        return Assert.Single(logs.Lines);
    }

    private static void AssertNothingOfTheMessage(string line)
    {
        foreach (var hidden in new[]
                 {
                     Recipient,
                     Token,
                     AdminMailDispatcher.BootstrapSubject,
                     AdminMailPagePaths.BootstrapPath,
                     AdminMailPagePaths.Origin,
                     "one-time link",
                     "Mail send failed",
                     "not verified",
                     "not configured",
                     KeyId,
                     SigningValue
                 })
        {
            Assert.DoesNotContain(hidden, line, StringComparison.OrdinalIgnoreCase);
        }
    }
}

using System.Globalization;
using System.Threading.Channels;
using Dealoware.Application.Admin;
using Dealoware.Infrastructure.Mail;

namespace Dealoware.Api.Admin;

/// <summary>
/// SC-9: <see cref="AdminMailDispatcher"/> runs after the HTTP request returns.
/// N4: dispatch failures log exception type, SES's HTTP status and a fixed reason code
/// (T59 spec §2.8), and trace id only.
/// </summary>
public sealed class AdminMailSendQueue : IHostedService
{
    /// <summary>Logged as the status when the failure was not an answer from SES.</summary>
    public const string NoStatus = "none";

    /// <summary>Logged as the reason when the failure has no fixed reason code.</summary>
    public const string NoReason = "none";

    /// <summary>Logged as the reason when SES answered with a refusal.</summary>
    public const string SesRefusedReason = "ses-refused";

    /// <summary>Put before the credentials source's own code (timeout, http-500, bad-json, ...).</summary>
    public const string CredentialsReasonPrefix = "credentials-";

    private readonly Channel<QueuedMail> _channel =
        Channel.CreateUnbounded<QueuedMail>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly AdminMailDispatcher _mail;
    private readonly ILogger<AdminMailSendQueue> _log;
    private CancellationTokenSource? _run;
    private Task? _loop;

    public AdminMailSendQueue(AdminMailDispatcher mail, ILogger<AdminMailSendQueue> log)
    {
        _mail = mail;
        _log = log;
    }

    public void EnqueuePasswordReset(string to, string token, string? traceId)
    {
        Enqueue(MailLinkKind.Reset, to, token, traceId);
    }

    /// <summary>
    /// Queues the bootstrap link mail. The returned task completes when the
    /// mail has left the queue: true when the sender accepted it, false when
    /// dispatch failed. The one-shot bootstrap-link command waits on it so the
    /// process does not exit, or report success, before the mail is handed over.
    /// </summary>
    public Task<bool> EnqueueBootstrap(string to, string token, string? traceId)
        => Enqueue(MailLinkKind.Bootstrap, to, token, traceId);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _run = new CancellationTokenSource();
        _loop = DrainAsync(_run.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        if (_run is not null)
        {
            await _run.CancelAsync();
        }

        if (_loop is not null)
        {
            try
            {
                await _loop.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private Task<bool> Enqueue(MailLinkKind kind, string to, string token, string? traceId)
    {
        var queued = new QueuedMail(
            kind,
            to,
            token,
            string.IsNullOrWhiteSpace(traceId) ? "-" : traceId,
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        if (!_channel.Writer.TryWrite(queued))
        {
            queued.Sent.TrySetResult(false);
        }

        return queued.Sent.Task;
    }

    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var queued in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await (queued.Kind == MailLinkKind.Bootstrap
                        ? _mail.SendBootstrapLinkAsync(queued.To, queued.Token, stoppingToken)
                        : _mail.SendPasswordResetLinkAsync(queued.To, queued.Token, stoppingToken));
                    queued.Sent.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    // The error type, SES's HTTP status when it refused the send, and a fixed
                    // reason code. Never the address, the subject, the body or the exception's message.
                    var (httpStatus, reason) = DescribeFailure(ex);
                    _log.LogError(
                        "Admin mail dispatch failed. ExceptionType={ExceptionType} HttpStatus={HttpStatus} Reason={Reason} TraceId={TraceId}",
                        ex.GetType().FullName,
                        httpStatus,
                        reason,
                        queued.TraceId);
                    queued.Sent.TrySetResult(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// The status and reason a failed dispatch is logged with: always two strings, each either a
    /// number or code, or "none". Nothing in them comes from the message or from an answer body.
    /// </summary>
    internal static (string HttpStatus, string Reason) DescribeFailure(Exception ex) => ex switch
    {
        SesMailSendException refused => (refused.StatusCode.ToString(CultureInfo.InvariantCulture), SesRefusedReason),
        AwsContainerCredentialsException credentials => (NoStatus, CredentialsReasonPrefix + credentials.Reason),
        _ => (NoStatus, NoReason)
    };

    /// <summary>A class, not a record: no generated ToString that would print the token.</summary>
    private sealed class QueuedMail(
        MailLinkKind kind,
        string to,
        string token,
        string traceId,
        TaskCompletionSource<bool> sent)
    {
        public MailLinkKind Kind { get; } = kind;

        public string To { get; } = to;

        public string Token { get; } = token;

        public string TraceId { get; } = traceId;

        public TaskCompletionSource<bool> Sent { get; } = sent;
    }
}

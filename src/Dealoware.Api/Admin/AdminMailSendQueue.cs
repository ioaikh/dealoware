using System.Threading.Channels;
using Dealoware.Application.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// SC-9: <see cref="AdminMailDispatcher"/> runs after the HTTP request returns.
/// N4: dispatch failures log exception type and trace id only.
/// </summary>
public sealed class AdminMailSendQueue : IHostedService
{
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
                    await _mail.SendAsync(queued.To, queued.Kind, queued.Token, stoppingToken);
                    queued.Sent.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _log.LogError(
                        "Admin mail dispatch failed. ExceptionType={ExceptionType} TraceId={TraceId}",
                        ex.GetType().FullName,
                        queued.TraceId);
                    queued.Sent.TrySetResult(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

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

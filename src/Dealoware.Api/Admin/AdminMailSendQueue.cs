using System.Threading.Channels;
using Dealoware.Application.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// SC-9: <see cref="AdminMailDispatcher"/> runs after the HTTP request returns.
/// N4: dispatch failures log exception type and trace id only.
/// </summary>
public sealed class AdminMailSendQueue : IHostedService
{
    private readonly Channel<(string To, string Token, string TraceId)> _channel =
        Channel.CreateUnbounded<(string To, string Token, string TraceId)>(new UnboundedChannelOptions
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
        _channel.Writer.TryWrite((to, token, string.IsNullOrWhiteSpace(traceId) ? "-" : traceId));
    }

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

    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var (to, token, traceId) in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _mail.SendPasswordResetLinkAsync(to, token, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(
                        "Admin mail dispatch failed. ExceptionType={ExceptionType} TraceId={TraceId}",
                        ex.GetType().FullName,
                        traceId);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}

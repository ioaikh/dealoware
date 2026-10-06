using System.Threading.Channels;
using Dealoware.Application.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// SC-9: <see cref="AdminMailDispatcher"/> runs after the HTTP request returns.
/// </summary>
public sealed class AdminMailSendQueue : IHostedService
{
    private readonly Channel<(string To, string Token)> _channel =
        Channel.CreateUnbounded<(string To, string Token)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly AdminMailDispatcher _mail;
    private CancellationTokenSource? _run;
    private Task? _loop;

    public AdminMailSendQueue(AdminMailDispatcher mail)
    {
        _mail = mail;
    }

    public void EnqueuePasswordReset(string to, string token)
    {
        _channel.Writer.TryWrite((to, token));
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
            await foreach (var (to, token) in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _mail.SendPasswordResetLinkAsync(to, token, stoppingToken);
                }
                catch
                {
                    // The HTTP request already returned; do not surface sender faults.
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}

using System.Threading.Channels;
using Dealoware.Domain.Admin;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Unbounded in-process queue. The hosted loop calls <see cref="IAdminMailSender"/>
/// after the HTTP request has returned (SC-9).
/// </summary>
public sealed class ChannelAdminMailDispatcher : IAdminMailDispatcher, IHostedService
{
    private readonly Channel<AdminMailMessage> _channel =
        Channel.CreateUnbounded<AdminMailMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly IAdminMailSender _mail;
    private CancellationTokenSource? _run;
    private Task? _loop;

    public ChannelAdminMailDispatcher(IAdminMailSender mail)
    {
        _mail = mail;
    }

    public void Enqueue(AdminMailMessage message)
    {
        _channel.Writer.TryWrite(message);
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
            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _mail.SendAsync(message, stoppingToken);
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

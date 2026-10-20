using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>Hosted reader for <see cref="AdminMailQueue"/>. Sends after the request returns.</summary>
public sealed class AdminMailQueueHostedService : BackgroundService
{
    private readonly AdminMailQueue _queue;
    private readonly IAdminMailSender _sender;

    public AdminMailQueueHostedService(AdminMailQueue queue, IAdminMailSender sender)
    {
        _queue = queue;
        _sender = sender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            if (message is null)
            {
                continue;
            }

            try
            {
                await _sender.SendAsync(message, stoppingToken).ConfigureAwait(false);
            }
            catch
            {
                // Delivery is best-effort after the request has returned.
            }
        }
    }
}
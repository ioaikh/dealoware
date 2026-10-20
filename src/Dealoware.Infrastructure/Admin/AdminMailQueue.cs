using System.Threading.Channels;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Unbounded channel so reset mail leaves the request path (SC-9).
/// </summary>
public sealed class AdminMailQueue : IAdminMailQueue
{
    private readonly Channel<AdminMailMessage?> _channel = Channel.CreateUnbounded<AdminMailMessage?>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<AdminMailMessage?> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(AdminMailMessage? message, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(message, cancellationToken);
}

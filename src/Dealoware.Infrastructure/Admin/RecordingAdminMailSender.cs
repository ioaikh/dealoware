using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Records mail without sending. Step 6 replaces this with the SES adapter.
/// </summary>
public sealed class RecordingAdminMailSender : IAdminMailSender
{
    private int _sendCount;

    public int SendCount => _sendCount;

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.To);
        Interlocked.Increment(ref _sendCount);
        return Task.CompletedTask;
    }
}

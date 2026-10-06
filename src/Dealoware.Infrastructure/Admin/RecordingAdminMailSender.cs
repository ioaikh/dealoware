using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Records mail without sending. Step 6 replaces this with the SES adapter.
/// </summary>
public sealed class RecordingAdminMailSender : IAdminMailSender
{
    private readonly object _gate = new();
    private readonly List<AdminMailMessage> _sent = [];
    private int _sendCount;

    public int SendCount => _sendCount;

    public IReadOnlyList<AdminMailMessage> Sent
    {
        get
        {
            lock (_gate)
            {
                return _sent.ToList();
            }
        }
    }

    public AdminMailMessage? Last
    {
        get
        {
            lock (_gate)
            {
                return _sent.Count == 0 ? null : _sent[^1];
            }
        }
    }

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.To);
        lock (_gate)
        {
            _sent.Add(message);
            _sendCount = _sent.Count;
        }

        return Task.CompletedTask;
    }
}

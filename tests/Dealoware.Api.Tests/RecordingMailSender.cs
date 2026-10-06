using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// In-memory mail adapter for tests. Never talks to SES.
/// </summary>
public sealed class RecordingMailSender : IAdminMailSender
{
    private readonly object _gate = new();
    private readonly List<AdminMailMessage> _sent = [];

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

    public int SendCount
    {
        get
        {
            lock (_gate)
            {
                return _sent.Count;
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

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            _sent.Add(message);
        }

        return Task.CompletedTask;
    }
}

using System.Collections.Concurrent;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// In-memory mail adapter for tests. Never talks to SES.
/// </summary>
public sealed class RecordingMailSender : IAdminMailSender
{
    private readonly object _gate = new();
    private int _sendCount;

    public ConcurrentBag<AdminMailMessage> Sent { get; } = new();

    public AdminMailMessage? Last { get; private set; }

    public int SendCount => Volatile.Read(ref _sendCount);

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            Last = message;
            _sendCount++;
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }

    public async Task<AdminMailMessage> WaitForNextAsync(int previousCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        while (SendCount <= previousCount)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Reset mail did not leave the hosted queue in time.");
            }

            await Task.Delay(10);
        }

        return Last ?? throw new InvalidOperationException("Mail count advanced without a message.");
    }
}

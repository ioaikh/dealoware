using System.Collections.Concurrent;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Mail;

namespace Dealoware.Api.Tests;

/// <summary>
/// In-memory mail adapter for tests. Never talks to SES.
/// The only IAdminMailSender test double.
/// </summary>
public sealed class RecordingMailSender : IAdminMailSender
{
    private readonly object _gate = new();
    private int _sendCount;

    public ConcurrentBag<AdminMailMessage> Sent { get; } = new();

    public AdminMailMessage? Last { get; private set; }

    public int SendCount => Volatile.Read(ref _sendCount);

    public TimeSpan SendDelay { get; set; } = TimeSpan.Zero;

    public bool FailWithDisabledSender { get; set; }

    public async Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (FailWithDisabledSender)
        {
            await new DisabledMailSender().SendAsync(message, cancellationToken);
        }

        if (SendDelay > TimeSpan.Zero)
        {
            await Task.Delay(SendDelay, cancellationToken);
        }

        lock (_gate)
        {
            Last = message;
            _sendCount++;
        }

        Sent.Add(message);
    }

    public void Clear()
    {
        Sent.Clear();
        SendDelay = TimeSpan.Zero;
        FailWithDisabledSender = false;
        Last = null;
        Volatile.Write(ref _sendCount, 0);
    }

    public async Task WaitForSentAsync(int count, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (Sent.Count < count)
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Mail sender observed {Sent.Count} message(s); expected {count}.");
            }

            await Task.Delay(20, CancellationToken.None);
        }
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

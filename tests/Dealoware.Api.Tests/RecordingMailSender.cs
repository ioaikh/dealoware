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
    public ConcurrentBag<AdminMailMessage> Sent { get; } = new();

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

        Sent.Add(message);
    }

    public void Clear()
    {
        Sent.Clear();
        SendDelay = TimeSpan.Zero;
        FailWithDisabledSender = false;
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
}

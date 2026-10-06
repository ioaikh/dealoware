using System.Collections.Concurrent;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// In-memory mail adapter for tests. Never talks to SES.
/// </summary>
public sealed class RecordingMailSender : IAdminMailSender
{
    public ConcurrentBag<AdminMailMessage> Sent { get; } = new();

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

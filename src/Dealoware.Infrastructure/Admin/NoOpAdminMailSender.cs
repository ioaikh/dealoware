using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Placeholder mail adapter. The SES adapter is Step 6. Does not log the
/// message body or depend on AWS SDK types.
/// </summary>
public sealed class NoOpAdminMailSender : IAdminMailSender
{
    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Task.CompletedTask;
    }
}

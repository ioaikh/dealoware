using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Step 6 replaces this with the SES adapter. No AWS types here.
/// </summary>
public sealed class UnconfiguredAdminMailSender : IAdminMailSender
{
    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

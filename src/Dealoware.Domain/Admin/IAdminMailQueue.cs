namespace Dealoware.Domain.Admin;

/// <summary>
/// Hosted off-request-path queue for admin mail (SC-9). Enqueue must return
/// without waiting for <see cref="IAdminMailSender.SendAsync"/>.
/// </summary>
public interface IAdminMailQueue
{
    ValueTask EnqueueAsync(AdminMailMessage? message, CancellationToken cancellationToken);
}

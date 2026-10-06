namespace Dealoware.Domain.Mail;

/// <summary>
/// Admin mail port. Core application code sends only through this interface.
/// The SES adapter lives behind this port so Core does not take an AWS SDK dependency.
/// </summary>
public interface IAdminMailSender
{
    Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default);
}

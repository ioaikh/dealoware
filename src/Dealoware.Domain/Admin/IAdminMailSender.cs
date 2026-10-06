namespace Dealoware.Domain.Admin;

public sealed record AdminMailMessage(string To, string Subject, string TextBody);

public interface IAdminMailSender
{
    Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken);
}

using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// Registered when neither credential source is set (or the from-address or region is unset).
/// The host still starts (PoC has no spend unlock). A send fails closed with setting names
/// only — no secret values.
/// </summary>
public sealed class DisabledMailSender : IAdminMailSender
{
    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        throw new InvalidOperationException(SesMailOptions.NotConfiguredMessage);
    }
}

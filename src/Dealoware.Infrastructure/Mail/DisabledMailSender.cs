using Dealoware.Domain.Mail;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// Registered when mail environment names are unset. The host still starts (PoC has no spend unlock).
/// A send fails closed with environment names only — no secret values.
/// </summary>
public sealed class DisabledMailSender : IAdminMailSender
{
    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        throw new InvalidOperationException(
            "Mail sender is not configured. Set "
            + SesMailOptions.FromEnvironmentVariable + ", "
            + SesMailOptions.RegionEnvironmentVariable + ", "
            + SesMailOptions.AccessKeyEnvironmentVariable + ", and "
            + SesMailOptions.SecretKeyEnvironmentVariable + ".");
    }
}

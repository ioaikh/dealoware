using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

public static class AdminPasswordResetServiceExtensions
{
    public static IServiceCollection AddAdminPasswordReset(this IServiceCollection services)
    {
        services.AddScoped<IAdminPasswordResetTokenRepository, AdminPasswordResetTokenRepository>();
        services.AddScoped<IAdminCredentialStore, AdminCredentialStore>();
        services.AddSingleton<IAdminPasswordHasher, Pbkdf2AdminPasswordHasher>();
        services.AddSingleton<IAdminClock, SystemAdminClock>();
        services.AddSingleton<IAdminMailSender, UnconfiguredAdminMailSender>();
        services.AddSingleton<IAdminSecondFactorVerifier, PendingStep3SecondFactorVerifier>();
        return services;
    }
}

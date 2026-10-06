using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>Step 3 TOTP protector registration. Secrets from env only.</summary>
public static class AdminTotpServiceExtensions
{
    public static IServiceCollection AddAdminTotp(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var protector = TotpSecretProtector.Create(
            name => configuration[name] ?? Environment.GetEnvironmentVariable(name),
            isDevelopment);
        services.AddSingleton(protector);
        services.AddSingleton<AdminAntiForgeryService>();
        return services;
    }
}

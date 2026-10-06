using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>Step 3 TOTP protector registration. Secrets from env only.</summary>
public static class AdminTotpServiceExtensions
{
    public static IServiceCollection AddAdminTotp(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var get = (string name) => configuration[name] ?? Environment.GetEnvironmentVariable(name);
        var protector = TotpSecretProtector.Create(get, isDevelopment);
        services.AddSingleton(protector);
        services.AddSingleton(AdminRecoveryCodeHasher.Create(get));
        services.AddSingleton<AdminAntiForgeryService>();
        return services;
    }
}

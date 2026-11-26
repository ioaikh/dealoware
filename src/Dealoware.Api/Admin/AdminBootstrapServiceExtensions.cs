using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

public static class AdminBootstrapServiceExtensions
{
    /// <summary>
    /// Bootstrap services. The credential store, password hasher, clock, mail
    /// queue and IP throttle come from <c>AddAdminPasswordReset</c>.
    /// </summary>
    public static IServiceCollection AddAdminBootstrap(this IServiceCollection services)
    {
        services.AddScoped<IAdminBootstrapTokenRepository, AdminBootstrapTokenRepository>();
        services.AddScoped<AdminBootstrapService>();
        return services;
    }
}

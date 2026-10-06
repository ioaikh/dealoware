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
        services.AddAntiforgery(options =>
        {
            options.HeaderName = AdminAntiForgeryCookie.HeaderName;
            options.Cookie.Name = AdminAntiForgeryCookie.Name;
            options.Cookie.Path = AdminHostMiddleware.AdminPathPrefix;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.IsEssential = true;
        });
        return services;
    }
}

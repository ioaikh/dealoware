using Dealoware.Application.Admin;
using Dealoware.Domain.Mail;
using Dealoware.Infrastructure.Mail;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Admin;

/// <summary>
/// Registers the admin mail port, SES HTTP adapter (factory HttpClient), and dispatcher.
/// </summary>
public static class AdminMailServiceCollectionExtensions
{
    public static IServiceCollection AddAdminMail(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient("ses-mail", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<IAdminMailSender>(sp =>
        {
            string? GetEnv(string name) => configuration[name] ?? Environment.GetEnvironmentVariable(name);
            var mailOptions = SesMailOptions.TryCreate(GetEnv);
            if (mailOptions is null)
            {
                var environmentName = configuration[HostDefaults.EnvironmentKey]
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                    ?? Environments.Production;
                if (!string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine(
                        "Mail sender is disabled outside Development. Set "
                        + SesMailOptions.FromEnvironmentVariable + ", "
                        + SesMailOptions.RegionEnvironmentVariable + ", "
                        + SesMailOptions.AccessKeyEnvironmentVariable + ", and "
                        + SesMailOptions.SecretKeyEnvironmentVariable + ".");
                }

                return new DisabledMailSender();
            }

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ses-mail");
            return new SesMailSender(mailOptions, client);
        });

        services.AddSingleton<AdminMailDispatcher>();
        return services;
    }
}

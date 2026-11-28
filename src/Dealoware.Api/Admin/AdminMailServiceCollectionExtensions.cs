using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Mail;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Admin;

/// <summary>
/// Registers the admin mail port, SES HTTP adapter (factory HttpClient), and dispatcher.
/// The sender signs with the running service's own role credentials, fetched from the
/// container credentials endpoint (T59 answer 4). The fixed keys are read only in Development.
/// With neither source, mail stays off.
/// </summary>
public static class AdminMailServiceCollectionExtensions
{
    /// <summary>The client that talks to SES.</summary>
    public const string SesHttpClientName = "ses-mail";

    /// <summary>The client that fetches the role credentials. It keeps no request log.</summary>
    public const string CredentialsHttpClientName = "ses-mail-credentials";

    public static IServiceCollection AddAdminMail(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient(SesHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // The default request log would name the credentials address, so this client has none.
        services.AddHttpClient(CredentialsHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        }).RemoveAllLoggers();

        services.AddSingleton<IAdminMailSender>(sp =>
        {
            string? GetEnv(string name) => configuration[name] ?? Environment.GetEnvironmentVariable(name);
            // The host's own environment, the one every other Development-only rule reads.
            var isDevelopment = sp.GetRequiredService<IHostEnvironment>().IsDevelopment();

            if (!isDevelopment && SesMailOptions.HasStaticKeys(GetEnv))
            {
                Console.Error.WriteLine(SesMailOptions.StaticKeysIgnoredMessage);
            }

            var mailOptions = SesMailOptions.TryCreate(GetEnv, allowStaticKeys: isDevelopment);
            if (mailOptions is null)
            {
                if (!isDevelopment)
                {
                    Console.Error.WriteLine(SesMailOptions.NotConfiguredMessage);
                }

                return new DisabledMailSender();
            }

            var clients = sp.GetRequiredService<IHttpClientFactory>();
            return new SesMailSender(
                mailOptions,
                clients.CreateClient(SesHttpClientName),
                mailOptions.CreateCredentialSource(clients.CreateClient(CredentialsHttpClientName)));
        });

        services.AddSingleton<AdminMailDispatcher>();
        return services;
    }
}

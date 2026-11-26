using Dealoware.Api.Admin;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api;

/// <summary>
/// One-shot bootstrap-link entrypoint: <c>dotnet Dealoware.Api.dll bootstrap-link</c>
/// (or <c>dotnet run --project src/Dealoware.Api -- bootstrap-link</c>).
/// Issues the Core admin first-time setup link for the configured CoreOwner
/// (<c>CoreOwner:Email</c>) and mails it through the mail interface. This is
/// the only production trigger for that link. When the owner already has a
/// password it reports "already set up" and changes nothing.
/// Uses the same DB_* / connection-string path as the API; the runtime
/// (<c>dealoware_app</c>, DML-only) login is enough. Needs the mail settings.
/// Never prints the link, the token or the owner address.
/// </summary>
public static class AdminBootstrapLinkCommand
{
    public const string CommandName = "bootstrap-link";

    public const string SentMessage =
        "Bootstrap link sent to the configured Core admin owner. It works once and expires after 24 hours.";

    public const string AlreadySetUpMessage =
        "The Core admin owner is already set up. No bootstrap link was issued.";

    public const string OwnerNotConfiguredMessage =
        "CoreOwner:Email is not configured. No bootstrap link was issued.";

    public const string MailFailedMessage =
        "The bootstrap link could not be emailed. Check the mail settings and run the command again.";

    public static bool IsBootstrapLinkArgs(string[] args)
        => args.Length > 0
           && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Same fail-closed provider selection as the API: outside Development,
        // PostgreSQL with verified TLS only.
        try
        {
            AddServices(
                builder.Services,
                builder.Configuration,
                Environment.GetEnvironmentVariable,
                strictNonDevelopment: !builder.Environment.IsDevelopment());
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        // AddInfrastructure also registers API services this one-shot never resolves,
        // some of which need API-only keys, so skip the Development whole-graph check.
        builder.ConfigureContainer(new DefaultServiceProviderFactory());

        using var host = builder.Build();

        // Starts the mail queue reader; nothing else is hosted here.
        await host.StartAsync().ConfigureAwait(false);
        try
        {
            var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            return await ExecuteAsync(host.Services, Console.Out, Console.Error, stopping).ConfigureAwait(false);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The services the one-shot needs: database, owner options, mail, bootstrap.
    /// <paramref name="getEnv"/> is explicit so tests do not read process environment.
    /// </summary>
    public static void AddServices(
        IServiceCollection services,
        IConfiguration configuration,
        Func<string, string?> getEnv,
        bool strictNonDevelopment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? getEnv("DEALOWARE_CONNECTION_STRING")
            ?? "Data Source=dealoware.db";

        services.AddInfrastructure(connectionString, getEnv, strictNonDevelopment);
        services.Configure<CoreOwnerOptions>(configuration.GetSection(CoreOwnerOptions.SectionName));
        services.AddAdminMail(configuration);
        services.AddAdminPasswordReset();
        services.AddAdminBootstrap();
    }

    /// <summary>
    /// Issues the link and waits until the mail has been handed to the sender.
    /// Exit code 0: link sent, or owner already set up. Exit code 1: nothing usable was sent.
    /// </summary>
    public static async Task<int> ExecuteAsync(
        IServiceProvider services,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();

        var result = await bootstrap.IssueLinkAsync(cancellationToken).ConfigureAwait(false);
        switch (result.Outcome)
        {
            case AdminBootstrapLinkOutcome.AlreadySetUp:
                await output.WriteLineAsync(AlreadySetUpMessage).ConfigureAwait(false);
                return 0;

            case AdminBootstrapLinkOutcome.OwnerNotConfigured:
                await error.WriteLineAsync(OwnerNotConfiguredMessage).ConfigureAwait(false);
                return 1;
        }

        if (!await result.MailSent.WaitAsync(cancellationToken).ConfigureAwait(false))
        {
            await error.WriteLineAsync(MailFailedMessage).ConfigureAwait(false);
            return 1;
        }

        await output.WriteLineAsync(SentMessage).ConfigureAwait(false);
        return 0;
    }
}

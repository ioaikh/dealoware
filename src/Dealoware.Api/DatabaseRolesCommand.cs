using Dealoware.Infrastructure.Persistence;
using Npgsql;

namespace Dealoware.Api;

/// <summary>
/// One-shot database logins entrypoint (H3), like <see cref="DatabaseMigrateCommand"/>:
/// <list type="bullet">
///   <item><c>dotnet Dealoware.Api.dll dbroles</c>: as the master login (<c>DB_USERNAME</c> /
///     <c>DB_PASSWORD</c>), creates <c>dealoware_migrate</c> and <c>dealoware_app</c> and their
///     rights (H3 design §7 steps 1 to 5). Their passwords come from
///     <see cref="MigratePasswordVariable"/> and <see cref="AppPasswordVariable"/>.</item>
///   <item><c>dotnet Dealoware.Api.dll dbroles --verify</c>: logs in as <c>dealoware_app</c>
///     with <see cref="AppPasswordVariable"/> and runs the negative checks of §7 step 7 in a
///     transaction that is rolled back.</item>
/// </list>
/// Same <c>DB_HOST</c> / <c>DB_PORT</c> / <c>DB_NAME</c> and TLS (VerifyFull with the RDS root
/// certificate) as the API. Does not start the web host. Refused in Development. Prints only
/// step names and <c>ok</c> or <c>failed</c>; never a host, a password or a connection string.
/// Exit 0 when every step passed, 1 when a step failed, 2 when refused before connecting.
/// </summary>
public static class DatabaseRolesCommand
{
    public const string CommandName = "dbroles";
    public const string VerifyOption = "--verify";

    /// <summary>The password <c>dbroles</c> sets on <c>dealoware_app</c>, and <c>--verify</c> logs in with.</summary>
    public const string AppPasswordVariable = "DEALOWARE_DB_APP_PASSWORD";

    /// <summary>The password <c>dbroles</c> sets on <c>dealoware_migrate</c>.</summary>
    public const string MigratePasswordVariable = "DEALOWARE_DB_MIGRATE_PASSWORD";

    public const int ExitOk = 0;
    public const int ExitFailed = 1;
    public const int ExitRefused = 2;

    public static bool IsDbRolesArgs(string[] args)
        => args.Length > 0
           && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    public static Task<int> RunAsync(string[] args)
        => RunAsync(args, Environment.GetEnvironmentVariable, Console.Out);

    /// <summary>Same as <see cref="RunAsync(string[])"/> with an explicit environment and output (tests).</summary>
    public static async Task<int> RunAsync(
        string[] args,
        Func<string, string?> getEnv,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(getEnv);
        ArgumentNullException.ThrowIfNull(output);

        var verify = args.Length == 2 && string.Equals(args[1], VerifyOption, StringComparison.Ordinal);
        var prefix = verify ? $"{CommandName} {VerifyOption}" : CommandName;

        if (!IsDbRolesArgs(args) || (args.Length > 1 && !verify))
        {
            return Refuse(output, prefix, $"usage: {CommandName} [{VerifyOption}]");
        }

        if (IsDevelopment(getEnv))
        {
            return Refuse(output, prefix,
                "it does not run in Development (ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT)");
        }

        NpgsqlConnectionStringBuilder connection;
        try
        {
            // Strict, as the API outside Development: PostgreSQL only, VerifyFull, RDS root CA.
            var selection = DatabaseProviderSelector.Select(null, getEnv, strictNonDevelopment: true);
            connection = new NpgsqlConnectionStringBuilder(selection.ConnectionString)
            {
                ApplicationName = "dealoware-dbroles",
                Pooling = false,
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // The selector's messages are written to be safe, but none is needed here.
            return Refuse(output, prefix,
                "DB_HOST, DB_PORT, DB_NAME and the TLS root certificate must be set as for the API");
        }

        var appPassword = getEnv(AppPasswordVariable);
        if (!PostgresScramVerifier.IsAcceptablePassword(appPassword))
        {
            return Refuse(output, prefix, $"{AppPasswordVariable} must be 16 to 1024 printable ASCII characters");
        }

        if (verify)
        {
            // DB_USERNAME / DB_PASSWORD are not used: --verify logs in as dealoware_app only.
            connection.Username = DatabaseRoles.AppRole;
            connection.Password = appPassword;
            return await RunSafelyAsync(output, prefix, async () =>
            {
                await using var app = new NpgsqlConnection(connection.ConnectionString);
                return await DatabaseRoles.VerifyAsync(app, output, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        var migratePassword = getEnv(MigratePasswordVariable);
        if (!PostgresScramVerifier.IsAcceptablePassword(migratePassword))
        {
            return Refuse(output, prefix, $"{MigratePasswordVariable} must be 16 to 1024 printable ASCII characters");
        }

        if (string.IsNullOrEmpty(connection.Username) || string.IsNullOrEmpty(connection.Password))
        {
            return Refuse(output, prefix, "DB_USERNAME and DB_PASSWORD must hold the master login");
        }

        if (string.Equals(appPassword, migratePassword, StringComparison.Ordinal)
            || string.Equals(appPassword, connection.Password, StringComparison.Ordinal)
            || string.Equals(migratePassword, connection.Password, StringComparison.Ordinal))
        {
            return Refuse(output, prefix, "the master, migrate and app passwords must all differ");
        }

        if (string.Equals(connection.Username, DatabaseRoles.AppRole, StringComparison.Ordinal)
            || string.Equals(connection.Username, DatabaseRoles.MigrateRole, StringComparison.Ordinal))
        {
            return Refuse(output, prefix, "DB_USERNAME must be the master login, not one of the two it creates");
        }

        return await RunSafelyAsync(output, prefix, async () =>
        {
            await using var master = new NpgsqlConnection(connection.ConnectionString);
            return await DatabaseRoles.ApplyAsync(master, appPassword!, migratePassword!, output, cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// The last net: an exception that escapes the steps (closing the connection, say) is
    /// printed as a type or an error code, never with the driver's message, and exits 1.
    /// </summary>
    private static async Task<int> RunSafelyAsync(TextWriter output, string prefix, Func<Task<bool>> run)
    {
        try
        {
            return await run().ConfigureAwait(false) ? ExitOk : ExitFailed;
        }
        catch (Exception ex)
        {
            await output.WriteLineAsync($"{prefix}: failed ({DatabaseStepWriter.Describe(ex)})").ConfigureAwait(false);
            await output.FlushAsync().ConfigureAwait(false);
            return ExitFailed;
        }
    }

    private static bool IsDevelopment(Func<string, string?> getEnv)
        => string.Equals(getEnv("ASPNETCORE_ENVIRONMENT")?.Trim(), "Development", StringComparison.OrdinalIgnoreCase)
           || string.Equals(getEnv("DOTNET_ENVIRONMENT")?.Trim(), "Development", StringComparison.OrdinalIgnoreCase);

    private static int Refuse(TextWriter output, string prefix, string reason)
    {
        output.WriteLine($"{prefix}: refused: {reason}");
        output.Flush();
        return ExitRefused;
    }
}

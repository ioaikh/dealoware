using System.Globalization;
using System.Text.RegularExpressions;
using Dealoware.Infrastructure.Persistence;
using Npgsql;

namespace Dealoware.Api;

/// <summary>
/// One-shot read-only check: <c>dotnet Dealoware.Api.dll dbcheck --cutoff &lt;UTC time&gt;</c>,
/// like <see cref="DatabaseMigrateCommand"/>. It does not start the web host.
/// <para>
/// It connects as the API does (<c>DB_HOST</c>, <c>DB_PORT</c>, <c>DB_NAME</c>,
/// <c>DB_USERNAME</c>, <c>DB_PASSWORD</c>; outside Development with VerifyFull and the root
/// certificate) and is meant to run with the API's own login. It runs
/// <see cref="DatabaseCheck"/> in a read-only transaction and prints one JSON line: the rows
/// of each table up to the cutoff, the newest migration, and hashes of each table's newest
/// row up to the cutoff. It prints no column value, no host and no password.
/// </para>
/// <para>
/// The time must be ISO 8601 with <c>Z</c> or an explicit offset, e.g.
/// <c>2026-12-01T10:00:00Z</c>. Exit 0 with the JSON line; 1 when the check failed (one line
/// <c>dbcheck: failed (&lt;reason&gt;)</c>); 2 when refused before connecting.
/// </para>
/// </summary>
public static class DatabaseCheckCommand
{
    public const string CommandName = "dbcheck";
    public const string CutoffOption = "--cutoff";

    public const int ExitOk = 0;
    public const int ExitFailed = 1;
    public const int ExitRefused = 2;

    private static readonly Regex CutoffFormat = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant);

    public static bool IsDbCheckArgs(string[] args)
        => args.Length > 0
           && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    /// <summary>The cutoff from <c>dbcheck --cutoff &lt;time&gt;</c>, or <c>null</c> when the arguments are not exactly that.</summary>
    public static DateTimeOffset? ParseCutoff(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length != 3 || !IsDbCheckArgs(args) || !string.Equals(args[1], CutoffOption, StringComparison.Ordinal))
            return null;
        if (!CutoffFormat.IsMatch(args[2]))
            return null;
        return DateTimeOffset.TryParse(args[2], CultureInfo.InvariantCulture, DateTimeStyles.None, out var cutoff)
            ? cutoff.ToUniversalTime()
            : null;
    }

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

        var cutoff = ParseCutoff(args);
        if (cutoff is null)
        {
            return Refuse(output,
                $"usage: {CommandName} {CutoffOption} <UTC time, ISO 8601 with Z or an offset, e.g. 2026-12-01T10:00:00Z>");
        }

        string connectionString;
        try
        {
            var selection = DatabaseProviderSelector.Select(null, getEnv, strictNonDevelopment: !IsDevelopment(getEnv));
            if (selection.Provider != DatabaseProvider.Postgres)
                return Refuse(output, "it runs on PostgreSQL only: set DB_HOST, DB_PORT, DB_NAME, DB_USERNAME and DB_PASSWORD as for the API");
            connectionString = new NpgsqlConnectionStringBuilder(selection.ConnectionString)
            {
                ApplicationName = DatabaseCheck.ApplicationName,
                Pooling = false,
            }.ConnectionString;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // The selector's messages are written to be safe, but none is needed here.
            return Refuse(output, "DB_HOST, DB_PORT, DB_NAME and the TLS root certificate must be set as for the API");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            var result = await DatabaseCheck.RunAsync(connection, cutoff.Value, cancellationToken).ConfigureAwait(false);
            await output.WriteLineAsync(result.ToJson()).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            return ExitOk;
        }
        catch (Exception ex)
        {
            // A type or an error code, never the driver's message (it can name the host).
            await output.WriteLineAsync($"{CommandName}: failed ({DatabaseStepWriter.Describe(ex)})").ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            return ExitFailed;
        }
    }

    private static bool IsDevelopment(Func<string, string?> getEnv)
        => string.Equals(getEnv("ASPNETCORE_ENVIRONMENT")?.Trim(), "Development", StringComparison.OrdinalIgnoreCase)
           || string.Equals(getEnv("DOTNET_ENVIRONMENT")?.Trim(), "Development", StringComparison.OrdinalIgnoreCase);

    private static int Refuse(TextWriter output, string reason)
    {
        output.WriteLine($"{CommandName}: refused: {reason}");
        output.Flush();
        return ExitRefused;
    }
}

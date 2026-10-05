using Npgsql;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Database provider used by <see cref="DealowareDbContext"/>.
/// </summary>
public enum DatabaseProvider
{
    Sqlite,
    Postgres
}

/// <summary>
/// Resolved provider plus the connection string to hand to it.
/// </summary>
public sealed record DatabaseSelection(DatabaseProvider Provider, string ConnectionString);

/// <summary>
/// Chooses between SQLite (local/dev/tests) and PostgreSQL.
///
/// Order:
/// 1. If DB_HOST is set, build a PostgreSQL connection string from
///    DB_HOST, DB_PORT (default 5432), DB_NAME, DB_USERNAME, DB_PASSWORD with SSL Mode=Require.
/// 2. Else if the configured connection string contains "Host=" (case-insensitive), use PostgreSQL with it.
/// 3. Otherwise keep SQLite with the configured connection string.
///
/// When <paramref name="requireStrictPostgres"/> is true (non-Development), step 3 throws instead of
/// silently using SQLite. Values come from configuration / environment only. Never commit real secrets.
/// </summary>
public static class DatabaseProviderSelector
{
    public const int DefaultPostgresPort = 5432;

    public static DatabaseSelection Select(string? configuredConnectionString, bool requireStrictPostgres = false)
        => Select(configuredConnectionString, Environment.GetEnvironmentVariable, requireStrictPostgres);

    public static DatabaseSelection Select(
        string? configuredConnectionString,
        Func<string, string?> getEnv,
        bool requireStrictPostgres = false)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var dbHost = getEnv("DB_HOST");
        if (!string.IsNullOrWhiteSpace(dbHost))
        {
            return new DatabaseSelection(DatabaseProvider.Postgres, BuildPostgresFromEnvironment(dbHost.Trim(), getEnv));
        }

        if (!string.IsNullOrWhiteSpace(configuredConnectionString) &&
            configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return new DatabaseSelection(DatabaseProvider.Postgres, configuredConnectionString);
        }

        if (requireStrictPostgres)
        {
            throw new InvalidOperationException(
                "Non-Development environments require PostgreSQL. " +
                "Set DB_HOST (and related DB_* settings) or provide a connection string containing Host=.");
        }

        return new DatabaseSelection(DatabaseProvider.Sqlite, configuredConnectionString ?? string.Empty);
    }

    private static string BuildPostgresFromEnvironment(string host, Func<string, string?> getEnv)
    {
        var portValue = getEnv("DB_PORT");
        var port = DefaultPostgresPort;
        if (!string.IsNullOrWhiteSpace(portValue))
        {
            if (!int.TryParse(portValue.Trim(), out port) || port <= 0 || port > 65535)
            {
                throw new InvalidOperationException("DB_PORT must be a valid TCP port number.");
            }
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            SslMode = SslMode.Require
        };

        var database = getEnv("DB_NAME");
        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Database = database.Trim();
        }

        var username = getEnv("DB_USERNAME");
        if (!string.IsNullOrWhiteSpace(username))
        {
            builder.Username = username.Trim();
        }

        var password = getEnv("DB_PASSWORD");
        if (!string.IsNullOrEmpty(password))
        {
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}

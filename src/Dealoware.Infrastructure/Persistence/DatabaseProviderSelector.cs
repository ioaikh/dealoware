using System.Security.Cryptography.X509Certificates;
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
///    DB_HOST, DB_PORT (default 5432), DB_NAME, DB_USERNAME, DB_PASSWORD.
///    In strict mode (non-Development): SSL Mode=VerifyFull + RDS CA root certificate.
/// 2. Else if the configured connection string contains "Host=" (case-insensitive), use PostgreSQL.
///    In strict mode: parse and enforce VerifyFull + root certificate; reject weak modes.
/// 3. Otherwise keep SQLite with the configured connection string.
///
/// Values come from configuration / environment only. Never commit real secrets to source.
/// </summary>
public static class DatabaseProviderSelector
{
    public const int DefaultPostgresPort = 5432;
    
    /// <summary>
    /// Path to the AWS RDS global CA bundle baked into the container image.
    /// This is public trust material, not a secret.
    /// </summary>
    public const string RdsRootCertificatePath = "/app/certs/rds-global-bundle.pem";

    public static DatabaseSelection Select(string? configuredConnectionString)
        => Select(configuredConnectionString, Environment.GetEnvironmentVariable, requireVerifiedTls: true);

    public static DatabaseSelection Select(string? configuredConnectionString, Func<string, string?> getEnv)
        => Select(configuredConnectionString, getEnv, requireVerifiedTls: true);

    /// <summary>
    /// Selects the database provider and builds the connection string.
    /// </summary>
    /// <param name="configuredConnectionString">Connection string from configuration.</param>
    /// <param name="getEnv">Environment variable lookup function.</param>
    /// <param name="requireVerifiedTls">
    /// When true (non-Development), enforces SSL Mode=VerifyFull with the RDS root CA.
    /// When false (Development), allows weaker SSL modes for local Postgres.
    /// Matches the pattern used by JwtSigningKeyValidator.
    /// </param>
    /// <param name="fileExistsCheck">Optional file existence check (for testing).</param>
    /// <param name="certLoadCheck">Optional certificate load check (for testing).</param>
    public static DatabaseSelection Select(
        string? configuredConnectionString,
        Func<string, string?> getEnv,
        bool requireVerifiedTls,
        Func<string, bool>? fileExistsCheck = null,
        Func<string, bool>? certLoadCheck = null)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var dbHost = getEnv("DB_HOST");
        if (!string.IsNullOrWhiteSpace(dbHost))
        {
            var connectionString = BuildPostgresFromEnvironment(dbHost.Trim(), getEnv, requireVerifiedTls);
            
            if (requireVerifiedTls)
            {
                ValidateRootCertificateExists(fileExistsCheck, certLoadCheck);
            }
            
            return new DatabaseSelection(DatabaseProvider.Postgres, connectionString);
        }

        if (!string.IsNullOrWhiteSpace(configuredConnectionString) &&
            configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = requireVerifiedTls
                ? EnforceVerifyFullOnConnectionString(configuredConnectionString)
                : configuredConnectionString;
            
            if (requireVerifiedTls)
            {
                ValidateRootCertificateExists(fileExistsCheck, certLoadCheck);
            }
            
            return new DatabaseSelection(DatabaseProvider.Postgres, connectionString);
        }

        return new DatabaseSelection(DatabaseProvider.Sqlite, configuredConnectionString ?? string.Empty);
    }

    private static string BuildPostgresFromEnvironment(string host, Func<string, string?> getEnv, bool requireVerifiedTls)
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
            Port = port
        };

        if (requireVerifiedTls)
        {
            builder.SslMode = SslMode.VerifyFull;
            builder.RootCertificate = RdsRootCertificatePath;
        }
        else
        {
            builder.SslMode = SslMode.Require;
        }

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

    /// <summary>
    /// Parses a Host= connection string and enforces VerifyFull rules per the security brief.
    /// Throws if explicit weak SSL modes are set or TrustServerCertificate=true.
    /// </summary>
    private static string EnforceVerifyFullOnConnectionString(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to parse Postgres connection string. Check syntax without echoing the string.", ex);
        }

        var explicitSslMode = connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) ||
                              connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase);

        if (explicitSslMode)
        {
            if (builder.SslMode is SslMode.Disable or SslMode.Allow or SslMode.Require or SslMode.VerifyCA)
            {
                throw new InvalidOperationException(
                    $"Postgres SSL Mode '{builder.SslMode}' is not permitted outside Development. " +
                    "Production requires SSL Mode=VerifyFull for server certificate validation.");
            }
        }

        if (connectionString.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
        {
            if (builder.TrustServerCertificate)
            {
                throw new InvalidOperationException(
                    "Trust Server Certificate=true is not permitted outside Development. " +
                    "Production requires full server certificate validation.");
            }
        }

        builder.SslMode = SslMode.VerifyFull;

        if (string.IsNullOrEmpty(builder.RootCertificate))
        {
            builder.RootCertificate = RdsRootCertificatePath;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Validates that the root certificate bundle exists and is readable.
    /// This is a startup preflight check in strict mode.
    /// </summary>
    private static void ValidateRootCertificateExists(
        Func<string, bool>? fileExistsCheck = null,
        Func<string, bool>? certLoadCheck = null)
    {
        var path = RdsRootCertificatePath;
        
        var fileExists = fileExistsCheck ?? File.Exists;
        if (!fileExists(path))
        {
            throw new InvalidOperationException(
                $"Postgres TLS root certificate bundle missing or unreadable: {path}");
        }

        var certLoads = certLoadCheck ?? TryLoadCertificates;
        if (!certLoads(path))
        {
            throw new InvalidOperationException(
                $"Postgres TLS root certificate bundle contains no valid certificates: {path}");
        }
    }

    /// <summary>
    /// Attempts to load at least one certificate from the PEM file.
    /// </summary>
    private static bool TryLoadCertificates(string path)
    {
        try
        {
            var certs = new X509Certificate2Collection();
            certs.ImportFromPemFile(path);
            return certs.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}

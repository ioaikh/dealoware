using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Role facts for the current PostgreSQL login. Used by the runtime least-privilege
/// guard (H3). Values come from a read-only catalog query; this type is also the
/// input to the pure decision function so tests do not need a live database.
/// </summary>
public sealed record RuntimeLoginPrivilegeFacts(
    bool RolSuper,
    bool RolCreateRole,
    bool RolCreateDb,
    bool RolBypassRls,
    bool IsRdsSuperuserMember,
    bool OwnsAppSchemaTable);

/// <summary>
/// Long-lived API startup guard: the PostgreSQL login used at runtime must be
/// least-privilege (not superuser / CREATEROLE / CREATEDB / rds_superuser, and
/// must not own tables in the app schema). Development and non-Postgres providers
/// skip the check. The migrate one-shot must not call this type.
/// </summary>
public static class RuntimeLoginPrivilegeGuard
{
    /// <summary>
    /// Break-glass configuration key. Default is false. Setting this to true skips
    /// the runtime privilege check; it is not for routine use.
    /// </summary>
    public const string AllowPrivilegedRuntimeLoginKey = "Database:AllowPrivilegedRuntimeLogin";

    public const string PrivilegedLoginMessage =
        "The runtime database login is privileged (superuser, CREATEROLE, CREATEDB, " +
        "BYPASSRLS, inherited rds_superuser membership, or membership in a role that " +
        "owns application-schema tables). " +
        "The long-lived API must use a least-privilege DML login. Refusing to start. " +
        "Break-glass only: set Database:AllowPrivilegedRuntimeLogin=true.";

    public const string CouldNotVerifyMessage =
        "The runtime database login privilege check could not be completed. " +
        "The long-lived API refuses to start until the current login is verified as least-privilege.";

    public const string BreakGlassMessage =
        "Database:AllowPrivilegedRuntimeLogin is enabled (break-glass). " +
        "The runtime login privilege check is skipped. This is not for routine use.";

    /// <summary>
    /// Read-only catalog query. Uses <c>pg_has_role(..., 'MEMBER')</c> so inherited
    /// and SET-only membership counts. <c>rds_superuser</c> is gated on the role
    /// existing so non-RDS Postgres is false, not an error.
    /// </summary>
    public const string PrivilegeFactsSql =
        """
        SELECT
          r.rolsuper,
          r.rolcreaterole,
          r.rolcreatedb,
          r.rolbypassrls,
          EXISTS (
            SELECT 1
            FROM pg_catalog.pg_roles s
            WHERE s.rolname = 'rds_superuser'
              AND pg_catalog.pg_has_role(current_user, s.oid, 'MEMBER')
          ) AS is_rds_superuser,
          EXISTS (
            SELECT 1
            FROM pg_catalog.pg_tables t
            WHERE t.schemaname = current_schema()
              AND pg_catalog.pg_has_role(current_user, t.tableowner, 'MEMBER')
          ) AS owns_app_schema_table
        FROM pg_catalog.pg_roles r
        WHERE r.rolname = current_user
        """;

    /// <summary>
    /// Pure decision: any privileged fact means the login is not safe for the
    /// long-lived API process.
    /// </summary>
    public static bool IsPrivilegedRuntimeLogin(RuntimeLoginPrivilegeFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        return facts.RolSuper
               || facts.RolCreateRole
               || facts.RolCreateDb
               || facts.RolBypassRls
               || facts.IsRdsSuperuserMember
               || facts.OwnsAppSchemaTable;
    }

    /// <summary>
    /// The guard runs only for PostgreSQL outside Development, and only when the
    /// break-glass flag is off.
    /// </summary>
    public static bool ShouldEnforce(
        DatabaseProvider provider,
        string? environmentName,
        bool allowPrivilegedRuntimeLogin)
        => provider == DatabaseProvider.Postgres
           && !IsDevelopment(environmentName)
           && !allowPrivilegedRuntimeLogin;

    /// <summary>
    /// True when the check would have run except the explicit break-glass flag skipped it.
    /// </summary>
    public static bool IsBreakGlassActive(
        DatabaseProvider provider,
        string? environmentName,
        bool allowPrivilegedRuntimeLogin)
        => allowPrivilegedRuntimeLogin
           && provider == DatabaseProvider.Postgres
           && !IsDevelopment(environmentName);

    public static DatabaseProvider ResolveProvider(DealowareDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        var name = db.Database.ProviderName;
        if (!string.IsNullOrEmpty(name)
            && name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseProvider.Postgres;
        }

        return DatabaseProvider.Sqlite;
    }

    /// <summary>
    /// Enforces the privilege policy when <see cref="ShouldEnforce"/> is true.
    /// <paramref name="loadFacts"/> is not invoked when the guard is skipped
    /// (non-Postgres, Development, or break-glass).
    /// </summary>
    public static async Task EnforceIfRequiredAsync(
        DatabaseProvider provider,
        string? environmentName,
        bool allowPrivilegedRuntimeLogin,
        Func<CancellationToken, Task<RuntimeLoginPrivilegeFacts>> loadFacts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loadFacts);

        if (IsBreakGlassActive(provider, environmentName, allowPrivilegedRuntimeLogin))
        {
            Console.Error.WriteLine(BreakGlassMessage);
        }

        if (!ShouldEnforce(provider, environmentName, allowPrivilegedRuntimeLogin))
        {
            return;
        }

        RuntimeLoginPrivilegeFacts facts;
        try
        {
            facts = await loadFacts(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
            when (ex.Message == PrivilegedLoginMessage || ex.Message == CouldNotVerifyMessage)
        {
            throw;
        }
        catch (Exception)
        {
            // Catalog / driver errors can include hosts or user names — do not leak them.
            throw new InvalidOperationException(CouldNotVerifyMessage);
        }

        if (IsPrivilegedRuntimeLogin(facts))
        {
            throw new InvalidOperationException(PrivilegedLoginMessage);
        }
    }

    /// <summary>
    /// Runs the catalog query through the current <see cref="DealowareDbContext"/>
    /// connection and fails closed when the login is privileged or unverifiable.
    /// </summary>
    public static Task EnforceAsync(
        DealowareDbContext db,
        string? environmentName,
        bool allowPrivilegedRuntimeLogin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var provider = ResolveProvider(db);
        return EnforceIfRequiredAsync(
            provider,
            environmentName,
            allowPrivilegedRuntimeLogin,
            ct => QueryFactsAsync(db, ct),
            cancellationToken);
    }

    private static bool IsDevelopment(string? environmentName)
        => string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

    private static async Task<RuntimeLoginPrivilegeFacts> QueryFactsAsync(
        DealowareDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = PrivilegeFactsSql;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(CouldNotVerifyMessage);
            }

            return new RuntimeLoginPrivilegeFacts(
                RolSuper: ReadBoolean(reader, 0),
                RolCreateRole: ReadBoolean(reader, 1),
                RolCreateDb: ReadBoolean(reader, 2),
                RolBypassRls: ReadBoolean(reader, 3),
                IsRdsSuperuserMember: ReadBoolean(reader, 4),
                OwnsAppSchemaTable: ReadBoolean(reader, 5));
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static bool ReadBoolean(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(CouldNotVerifyMessage);
        }

        return reader.GetBoolean(ordinal);
    }
}

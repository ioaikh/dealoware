using System.Text.RegularExpressions;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Participants;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// H3: the two database logins on PostgreSQL, set up in code instead of by hand.
/// <list type="bullet">
///   <item><see cref="ApplyAsync"/> (<c>dbroles</c>, as the master): H3 design §7 steps 1 to 5.</item>
///   <item><see cref="RequireMigrateLoginAsync"/> (<c>migrate</c>, before the migrations): refuses
///     the master, a superuser or the owner of the database.</item>
///   <item><see cref="ApplyAppGrantsAsync"/> (<c>migrate</c>, as the owner of the tables, after
///     every migration): §7 step 6b.</item>
///   <item><see cref="VerifyAsync"/> (<c>dbroles --verify</c>, as <see cref="AppRole"/>):
///     the negative checks of §7 step 7, in a transaction that is rolled back.</item>
/// </list>
/// Every method prints only step names and <c>ok</c> or <c>failed</c>, with at most a
/// PostgreSQL error code. Never a host, a user's password, a connection string, an
/// exception message or a value from a row. Each step can be run again.
/// </summary>
public static partial class DatabaseRoles
{
    /// <summary>The long-lived API's login: rows only, never DDL.</summary>
    public const string AppRole = "dealoware_app";

    /// <summary>The <c>migrate</c> one-shot's login: owns the schema objects.</summary>
    public const string MigrateRole = "dealoware_migrate";

    /// <summary>The schema the model lives in.</summary>
    public const string Schema = "public";

    /// <summary>
    /// <c>GRANT ... WITH INHERIT FALSE, SET TRUE</c> needs PostgreSQL 16. RDS runs 16.9.
    /// </summary>
    public const int MinimumServerVersionNum = 160000;

    private const string InsufficientPrivilege = "42501";
    private const string VerifyProbe = "dbroles_verify";

    /// <summary>
    /// <c>dbroles</c>, run as the master login: creates the two logins if they are missing,
    /// sets their passwords (as SCRAM verifiers, never in plain text), lets the master act as
    /// <see cref="MigrateRole"/> without inheriting its rights, locks PUBLIC out, gives every
    /// existing table and sequence to <see cref="MigrateRole"/>, and sets the default rights
    /// on what <see cref="MigrateRole"/> creates later: read and add only for
    /// <see cref="AppRole"/> (Senior Security G1). All in one transaction.
    /// </summary>
    /// <returns>True when every step passed.</returns>
    public static async Task<bool> ApplyAsync(
        NpgsqlConnection master,
        string appPassword,
        string migratePassword,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(master);
        ArgumentNullException.ThrowIfNull(output);
        var steps = new DatabaseStepWriter(output, "dbroles");
        var ct = cancellationToken;

        if (!await steps.RunAsync("connect as the master login", () => OpenAsync(master, ct)))
            return steps.Finish(false);

        if (!await steps.RunAsync("PostgreSQL 16 or later", () => RequireServerVersionAsync(master, ct)))
            return steps.Finish(false);

        var tx = await BeginAsync(steps, master, ct).ConfigureAwait(false);
        if (tx is null)
            return steps.Finish(false);

        try
        {
            return steps.Finish(await ApplyStepsAsync(steps, master, tx, appPassword, migratePassword, ct)
                .ConfigureAwait(false));
        }
        finally
        {
            await DisposeQuietlyAsync(tx).ConfigureAwait(false);
        }
    }

    private static async Task<bool> ApplyStepsAsync(
        DatabaseStepWriter steps,
        NpgsqlConnection master,
        NpgsqlTransaction tx,
        string appPassword,
        string migratePassword,
        CancellationToken ct)
    {
        return
            await steps.RunAsync($"create the logins {MigrateRole} and {AppRole} if missing", async () =>
            {
                foreach (var role in new[] { MigrateRole, AppRole })
                {
                    var exists = await ScalarAsync<bool>(master, tx,
                        "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role)", ct,
                        ("role", role)).ConfigureAwait(false);
                    if (!exists)
                    {
                        await ExecAsync(master, tx,
                            $"CREATE ROLE {Q(role)} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS NOREPLICATION",
                            ct).ConfigureAwait(false);
                    }
                }
            })
            && await steps.RunAsync("set the two passwords", async () =>
            {
                await ExecAsync(master, tx,
                    $"ALTER ROLE {Q(MigrateRole)} WITH LOGIN PASSWORD '{Verifier(migratePassword)}'", ct)
                    .ConfigureAwait(false);
                await ExecAsync(master, tx,
                    $"ALTER ROLE {Q(AppRole)} WITH LOGIN PASSWORD '{Verifier(appPassword)}'", ct)
                    .ConfigureAwait(false);
            })
            && await steps.RunAsync("the two logins have no special rights and belong to no role", () =>
                RequirePlainLoginsAsync(master, tx, ct))
            && await steps.RunAsync($"the master may act as {MigrateRole} (SET only, no INHERIT)", () =>
                ExecAsync(master, tx,
                    $"GRANT {Q(MigrateRole)} TO CURRENT_USER WITH INHERIT FALSE, SET TRUE", ct))
            && await steps.RunAsync("lock PUBLIC out; grant connect and schema use", async () =>
            {
                var database = Q(await ScalarAsync<string>(master, tx, "SELECT current_database()", ct)
                    .ConfigureAwait(false));
                foreach (var sql in new[]
                         {
                             $"REVOKE ALL ON DATABASE {database} FROM PUBLIC",
                             $"REVOKE CREATE ON SCHEMA {Schema} FROM PUBLIC",
                             $"GRANT CONNECT ON DATABASE {database} TO {Q(MigrateRole)}, {Q(AppRole)}",
                             $"GRANT USAGE, CREATE ON SCHEMA {Schema} TO {Q(MigrateRole)}",
                             $"GRANT USAGE ON SCHEMA {Schema} TO {Q(AppRole)}",
                             $"REVOKE CREATE ON SCHEMA {Schema} FROM {Q(AppRole)}",
                         })
                {
                    await ExecAsync(master, tx, sql, ct).ConfigureAwait(false);
                }
            })
            && await steps.RunAsync($"give every existing table and sequence to {MigrateRole}", () =>
                TransferOwnershipAsync(master, tx, ct))
            && await steps.RunAsync($"default rights on new tables: read and add only for {AppRole}", async () =>
            {
                // As dealoware_migrate, so the defaults apply to what migrate creates.
                foreach (var sql in new[]
                         {
                             $"SET LOCAL ROLE {Q(MigrateRole)}",
                             $"ALTER DEFAULT PRIVILEGES IN SCHEMA {Schema} REVOKE ALL ON TABLES FROM {Q(AppRole)}",
                             $"ALTER DEFAULT PRIVILEGES IN SCHEMA {Schema} GRANT SELECT, INSERT ON TABLES TO {Q(AppRole)}",
                             $"ALTER DEFAULT PRIVILEGES IN SCHEMA {Schema} REVOKE ALL ON SEQUENCES FROM {Q(AppRole)}",
                             $"ALTER DEFAULT PRIVILEGES IN SCHEMA {Schema} GRANT USAGE, SELECT ON SEQUENCES TO {Q(AppRole)}",
                             "RESET ROLE",
                         })
                {
                    await ExecAsync(master, tx, sql, ct).ConfigureAwait(false);
                }
            })
            && await steps.RunAsync("commit", () => tx.CommitAsync(ct));
    }

    /// <summary>
    /// <c>migrate</c>, after the migrations, as the owner of the tables (H3 design §7 step 6b,
    /// repeated after every migration). Gives <see cref="AppRole"/> exactly: read, add, change
    /// and remove on <see cref="DatabaseTableGrants.MayChange"/>; read and add on
    /// <see cref="DatabaseTableGrants.ReadAndAddOnly"/>; read on the history table; use of
    /// the sequences. Refuses a database that holds a table in neither list.
    /// </summary>
    /// <returns>True when every step passed.</returns>
    public static async Task<bool> ApplyAppGrantsAsync(
        NpgsqlConnection owner,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(output);
        var steps = new DatabaseStepWriter(output, "migrate");
        var ct = cancellationToken;

        if (!await steps.RunAsync("connect", () => OpenAsync(owner, ct)))
            return steps.Finish(false);

        var tx = await BeginAsync(steps, owner, ct).ConfigureAwait(false);
        if (tx is null)
            return steps.Finish(false);

        try
        {
            return steps.Finish(await ApplyAppGrantsStepsAsync(steps, owner, tx, ct).ConfigureAwait(false));
        }
        finally
        {
            await DisposeQuietlyAsync(tx).ConfigureAwait(false);
        }
    }

    private static async Task<bool> ApplyAppGrantsStepsAsync(
        DatabaseStepWriter steps, NpgsqlConnection owner, NpgsqlTransaction tx, CancellationToken ct)
    {
        return
            await steps.RunAsync($"the login {AppRole} exists", async () =>
            {
                var exists = await ScalarAsync<bool>(owner, tx,
                    "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role)", ct,
                    ("role", AppRole)).ConfigureAwait(false);
                if (!exists)
                    throw new DatabaseStepException($"run dbroles first: the login {AppRole} does not exist");
            })
            && await steps.RunAsync("every table is in the list of tables that may be changed or in the other list", () =>
                RequireTablesMatchListAsync(owner, tx, ct))
            && await steps.RunAsync($"tables that may be changed: read, add, change, remove for {AppRole}", async () =>
            {
                foreach (var table in DatabaseTableGrants.MayChange)
                {
                    await SetExactTableRightsAsync(owner, tx, table, "SELECT, INSERT, UPDATE, DELETE", ct)
                        .ConfigureAwait(false);
                }
            })
            && await steps.RunAsync($"{DatabaseTableGrants.AuditTable}: read and add only for {AppRole}", async () =>
            {
                foreach (var table in DatabaseTableGrants.ReadAndAddOnly)
                {
                    await SetExactTableRightsAsync(owner, tx, table, "SELECT, INSERT", ct).ConfigureAwait(false);
                }
            })
            && await steps.RunAsync($"{DatabaseTableGrants.HistoryTable}: read only for {AppRole}", () =>
                SetExactTableRightsAsync(owner, tx, DatabaseTableGrants.HistoryTable, "SELECT", ct))
            && await steps.RunAsync($"sequences: use and read for {AppRole}", () =>
                ExecAsync(owner, tx,
                    $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {Schema} TO {Q(AppRole)}", ct))
            && await steps.RunAsync("commit", () => tx.CommitAsync(ct));
    }

    /// <summary>
    /// <c>dbroles --verify</c>, logged in as <see cref="AppRole"/>: passes only if the H3
    /// privilege check finds the login not privileged, DDL, history writes and audit-log
    /// changes are refused, and reading and adding rows work. Everything runs in one
    /// transaction that is rolled back, so nothing is left behind.
    /// </summary>
    /// <returns>True when every check passed.</returns>
    public static async Task<bool> VerifyAsync(
        NpgsqlConnection app,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(output);
        var steps = new DatabaseStepWriter(output, "dbroles --verify");
        var ct = cancellationToken;

        if (!await steps.RunAsync($"connect as {AppRole}", () => OpenAsync(app, ct)))
            return steps.Finish(false);

        var ok =
            await steps.RunAsync($"the login is {AppRole}", async () =>
            {
                var user = await ScalarAsync<string>(app, null, "SELECT current_user", ct).ConfigureAwait(false);
                if (!string.Equals(user, AppRole, StringComparison.Ordinal))
                    throw new DatabaseStepException($"the login is not {AppRole}");
            })
            && await steps.RunAsync("H3 privilege check: the login is not privileged", async () =>
            {
                var facts = await RuntimeLoginPrivilegeGuard.LoadFactsAsync(app, ct).ConfigureAwait(false);
                if (RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts))
                    throw new DatabaseStepException("the H3 check finds the login privileged");
            });
        if (!ok)
            return steps.Finish(false);

        // The transaction and the EF context start inside a step, so a dropped connection here
        // prints a step name and "failed", never the driver's message.
        NpgsqlTransaction? begun = null;
        DealowareDbContext? context = null;
        try
        {
            if (!await steps.RunAsync("begin a transaction", async () =>
                {
                    begun = await app.BeginTransactionAsync(ct).ConfigureAwait(false);
                    context = new DealowareDbContext(
                        new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql(app).Options);
                    await context.Database.UseTransactionAsync(begun, ct).ConfigureAwait(false);
                }))
            {
                return steps.Finish(false);
            }

            return steps.Finish(await VerifyStepsAsync(steps, app, begun!, context!, ct).ConfigureAwait(false));
        }
        finally
        {
            await DisposeQuietlyAsync(context).ConfigureAwait(false);
            await DisposeQuietlyAsync(begun).ConfigureAwait(false);
        }
    }

    private static async Task<bool> VerifyStepsAsync(
        DatabaseStepWriter steps,
        NpgsqlConnection app,
        NpgsqlTransaction tx,
        DealowareDbContext db,
        CancellationToken ct)
    {
        return
            await steps.RunAsync("CREATE TABLE is refused", () => RequireRefusedAsync(app, tx,
                $"CREATE TABLE {Schema}.{VerifyProbe} (id integer)", ct))
            && await steps.RunAsync($"adding a row to {DatabaseTableGrants.HistoryTable} is refused", () =>
                RequireRefusedAsync(app, tx,
                    $"INSERT INTO {Schema}.{Q(DatabaseTableGrants.HistoryTable)} (\"MigrationId\", \"ProductVersion\") " +
                    $"VALUES ('{VerifyProbe}', '0')", ct))
            && await steps.RunAsync($"UPDATE on {DatabaseTableGrants.AuditTable} is refused", () =>
                RequireRefusedAsync(app, tx,
                    $"UPDATE {Schema}.{Q(DatabaseTableGrants.AuditTable)} SET \"Action\" = \"Action\" WHERE false", ct))
            && await steps.RunAsync($"DELETE on {DatabaseTableGrants.AuditTable} is refused", () =>
                RequireRefusedAsync(app, tx,
                    $"DELETE FROM {Schema}.{Q(DatabaseTableGrants.AuditTable)} WHERE false", ct))
            && await steps.RunAsync($"TRUNCATE on {DatabaseTableGrants.AuditTable} is refused", () =>
                RequireRefusedAsync(app, tx,
                    $"TRUNCATE {Schema}.{Q(DatabaseTableGrants.AuditTable)}", ct))
            && await steps.RunAsync($"read and add on {DatabaseTableGrants.AuditTable} work", async () =>
            {
                var entry = AdminAuditEntry.CreateAuthEvent(VerifyProbe, VerifyProbe, VerifyProbe);
                db.AdminAuditLog.Add(entry);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                if (!await db.AdminAuditLog.AsNoTracking().AnyAsync(e => e.Id == entry.Id, ct).ConfigureAwait(false))
                    throw new DatabaseStepException("the added row cannot be read back");
            })
            && await steps.RunAsync("read and add on Participants work", async () =>
            {
                var participant = Participant.Create(VerifyProbe);
                db.Participants.Add(participant);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                if (!await db.Participants.AsNoTracking().AnyAsync(p => p.Id == participant.Id, ct).ConfigureAwait(false))
                    throw new DatabaseStepException("the added row cannot be read back");

                participant.Deactivate();
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                db.Participants.Remove(participant);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            })
            && await steps.RunAsync("roll everything back", () => tx.RollbackAsync(ct));
    }

    /// <summary>
    /// Read-only catalog query for <see cref="RequireMigrateLoginAsync"/>. <c>pg_has_role(...,
    /// 'MEMBER')</c> counts inherited and SET-only membership; <c>rds_superuser</c> is gated on
    /// the role existing, so a server that is not RDS answers false rather than an error.
    /// </summary>
    public const string MigrateLoginFactsSql =
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
            FROM pg_catalog.pg_database d
            WHERE d.datname = current_database()
              AND pg_catalog.pg_has_role(current_user, d.datdba, 'MEMBER')
          ) AS owns_database
        FROM pg_catalog.pg_roles r
        WHERE r.rolname = current_user
        """;

    /// <summary>
    /// The facts that make a login wrong for <c>migrate</c>: the master (on RDS it has
    /// CREATEROLE, CREATEDB and <c>rds_superuser</c>, and owns the database), a superuser, or
    /// any login that owns the database. Unlike the API's check, owning the tables is expected:
    /// <see cref="MigrateRole"/> owns them.
    /// </summary>
    public sealed record MigrateLoginFacts(
        bool RolSuper,
        bool RolCreateRole,
        bool RolCreateDb,
        bool RolBypassRls,
        bool IsRdsSuperuserMember,
        bool OwnsDatabase);

    /// <summary>Pure decision: any fact set means <c>migrate</c> must not run on this login.</summary>
    public static bool IsRefusedForMigrate(MigrateLoginFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.RolSuper
               || facts.RolCreateRole
               || facts.RolCreateDb
               || facts.RolBypassRls
               || facts.IsRdsSuperuserMember
               || facts.OwnsDatabase;
    }

    /// <summary>
    /// <c>migrate</c>, before any migration: refuses the master, a superuser or the owner of
    /// the database, as the API refuses a privileged login at start. Run as the master,
    /// <c>migrate</c> would make the master own every new table. Prints step names only.
    /// </summary>
    /// <returns>True when the login may run the migrations (nothing printed after the step).</returns>
    public static async Task<bool> RequireMigrateLoginAsync(
        NpgsqlConnection connection,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(output);
        var steps = new DatabaseStepWriter(output, "migrate");
        var ct = cancellationToken;

        var ok =
            await steps.RunAsync("connect", () => OpenAsync(connection, ct))
            && await steps.RunAsync("the login is not a superuser, the master or the owner of the database", async () =>
            {
                var facts = await LoadMigrateLoginFactsAsync(connection, ct).ConfigureAwait(false);
                if (IsRefusedForMigrate(facts))
                    throw new DatabaseStepException($"refused: migrate runs as {MigrateRole} only, never as the master");
            });

        return ok || steps.Finish(false);
    }

    private static async Task<MigrateLoginFacts> LoadMigrateLoginFactsAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(MigrateLoginFactsSql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || Enumerable.Range(0, 6).Any(reader.IsDBNull))
            throw new DatabaseStepException("the login could not be checked");
        return new MigrateLoginFacts(
            reader.GetBoolean(0), reader.GetBoolean(1), reader.GetBoolean(2),
            reader.GetBoolean(3), reader.GetBoolean(4), reader.GetBoolean(5));
    }

    /// <summary>
    /// Starts the transaction inside a step: <c>BeginTransactionAsync</c> can throw (a broken or
    /// busy connection), and an exception that escaped here would be printed by .NET in full,
    /// with the driver's message.
    /// </summary>
    /// <returns>The transaction, or null when the step failed (already printed).</returns>
    private static async Task<NpgsqlTransaction?> BeginAsync(
        DatabaseStepWriter steps, NpgsqlConnection connection, CancellationToken ct)
    {
        NpgsqlTransaction? tx = null;
        var ok = await steps.RunAsync("begin a transaction", async () =>
        {
            tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return ok ? tx : null;
    }

    /// <summary>Disposes without letting a driver exception, and its message, escape.</summary>
    private static async Task DisposeQuietlyAsync(IAsyncDisposable? disposable)
    {
        if (disposable is null)
            return;
        try
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A broken connection cannot roll back; the server ends the transaction itself.
        }
    }

    private static async Task OpenAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct).ConfigureAwait(false);
    }

    private static async Task RequireServerVersionAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var version = await ScalarAsync<string>(connection, null, "SHOW server_version_num", ct).ConfigureAwait(false);
        if (!int.TryParse(version, out var number) || number < MinimumServerVersionNum)
            throw new DatabaseStepException("PostgreSQL 16 or later is required");
    }

    private static async Task RequirePlainLoginsAsync(NpgsqlConnection connection, NpgsqlTransaction tx, CancellationToken ct)
    {
        const string sql =
            """
            SELECT count(*)
            FROM pg_catalog.pg_roles r
            WHERE r.rolname IN (@migrate, @app)
              AND r.rolcanlogin
              AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
              AND NOT r.rolbypassrls AND NOT r.rolreplication
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members m WHERE m.member = r.oid)
            """;
        var plain = await ScalarAsync<long>(connection, tx, sql, ct, ("migrate", MigrateRole), ("app", AppRole))
            .ConfigureAwait(false);
        if (plain != 2)
            throw new DatabaseStepException("a login has a special right or belongs to a role");
    }

    private static async Task TransferOwnershipAsync(NpgsqlConnection connection, NpgsqlTransaction tx, CancellationToken ct)
    {
        // Tables, then sequences that no table column owns (an owned sequence follows its table;
        // PostgreSQL refuses to move it on its own).
        const string sql =
            """
            SELECT c.relkind::text, c.relname
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = @schema
              AND c.relkind IN ('r', 'p', 'S')
              AND pg_catalog.pg_get_userbyid(c.relowner) <> @migrate
              AND NOT (c.relkind = 'S' AND EXISTS (
                    SELECT 1 FROM pg_catalog.pg_depend d
                    WHERE d.classid = 'pg_catalog.pg_class'::regclass AND d.objid = c.oid
                      AND d.refclassid = 'pg_catalog.pg_class'::regclass AND d.deptype IN ('a', 'i')))
            ORDER BY c.relkind DESC, c.relname
            """;
        var objects = new List<(string Kind, string Name)>();
        await using (var cmd = new NpgsqlCommand(sql, connection, tx))
        {
            cmd.Parameters.AddWithValue("schema", Schema);
            cmd.Parameters.AddWithValue("migrate", MigrateRole);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                objects.Add((reader.GetString(0), reader.GetString(1)));
        }

        foreach (var (kind, name) in objects)
        {
            var what = kind == "S" ? "SEQUENCE" : "TABLE";
            await ExecAsync(connection, tx, $"ALTER {what} {Schema}.{Q(name)} OWNER TO {Q(MigrateRole)}", ct)
                .ConfigureAwait(false);
        }
    }

    private static async Task RequireTablesMatchListAsync(NpgsqlConnection connection, NpgsqlTransaction tx, CancellationToken ct)
    {
        var inDatabase = new HashSet<string>(StringComparer.Ordinal);
        await using (var cmd = new NpgsqlCommand(
                         "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname = @schema", connection, tx))
        {
            cmd.Parameters.AddWithValue("schema", Schema);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                inDatabase.Add(reader.GetString(0));
        }

        var listed = new HashSet<string>(
            DatabaseTableGrants.MayChange.Concat(DatabaseTableGrants.ReadAndAddOnly).Append(DatabaseTableGrants.HistoryTable),
            StringComparer.Ordinal);

        // Names are not printed: a count is enough for the operator, and the test names them.
        var unlisted = inDatabase.Count(t => !listed.Contains(t));
        var missing = listed.Count(t => !inDatabase.Contains(t));
        if (unlisted > 0 || missing > 0)
        {
            throw new DatabaseStepException(
                $"{unlisted} table(s) in the database are in neither list; {missing} listed table(s) are missing");
        }
    }

    private static async Task SetExactTableRightsAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, string table, string rights, CancellationToken ct)
    {
        // REVOKE ALL first, so a broad grant from earlier (or a default privilege) cannot
        // leave UPDATE, DELETE, TRUNCATE, REFERENCES or TRIGGER behind. One transaction,
        // so the API never sees the gap.
        await ExecAsync(connection, tx, $"REVOKE ALL ON TABLE {Schema}.{Q(table)} FROM {Q(AppRole)}", ct)
            .ConfigureAwait(false);
        await ExecAsync(connection, tx, $"GRANT {rights} ON TABLE {Schema}.{Q(table)} TO {Q(AppRole)}", ct)
            .ConfigureAwait(false);
    }

    private static async Task RequireRefusedAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, string sql, CancellationToken ct)
    {
        await tx.SaveAsync(VerifyProbe, ct).ConfigureAwait(false);
        try
        {
            await ExecAsync(connection, tx, sql, ct).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (ex.SqlState == InsufficientPrivilege)
        {
            await tx.RollbackAsync(VerifyProbe, ct).ConfigureAwait(false);
            return;
        }

        await tx.RollbackAsync(VerifyProbe, ct).ConfigureAwait(false);
        throw new DatabaseStepException("it was allowed");
    }

    private static string Verifier(string password) => PostgresScramVerifier.Create(password);

    /// <summary>Double-quotes a fixed identifier after checking it is a plain name.</summary>
    private static string Q(string identifier)
    {
        if (!PlainIdentifier().IsMatch(identifier))
            throw new DatabaseStepException("an identifier is not a plain name");
        return "\"" + identifier + "\"";
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,62}$")]
    private static partial Regex PlainIdentifier();

    private static async Task ExecAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction? tx,
        string sql,
        CancellationToken ct,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (result is not T typed)
            throw new DatabaseStepException("an unexpected answer from the database");
        return typed;
    }
}

/// <summary>A step failed for a reason that is safe to print (fixed text, no data).</summary>
public sealed class DatabaseStepException(string reason) : Exception(reason);

/// <summary>
/// Prints <c>&lt;prefix&gt;: &lt;step&gt;: ok</c> or <c>... failed (&lt;reason&gt;)</c>.
/// The reason is a PostgreSQL error code, a fixed text from
/// <see cref="DatabaseStepException"/>, or an exception type name. Never an exception
/// message: driver messages can name the host or the login.
/// </summary>
public sealed class DatabaseStepWriter(TextWriter output, string prefix)
{
    public async Task<bool> RunAsync(string step, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            await output.WriteLineAsync($"{prefix}: {step}: ok").ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await output.WriteLineAsync($"{prefix}: {step}: failed ({Describe(ex)})").ConfigureAwait(false);
            return false;
        }
    }

    public bool Finish(bool ok)
    {
        output.WriteLine(ok
            ? $"{prefix}: every step ok"
            : $"{prefix}: stopped at the first failure; nothing after it ran");
        output.Flush();
        return ok;
    }

    public static string Describe(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case DatabaseStepException step:
                    return step.Message;
                case PostgresException pg:
                    return $"PostgreSQL error {pg.SqlState}";
            }
        }

        return ex switch
        {
            NpgsqlException or TimeoutException or System.Net.Sockets.SocketException =>
                "the database could not be reached or refused the login",
            OperationCanceledException => "cancelled",
            _ => ex.GetType().Name,
        };
    }
}

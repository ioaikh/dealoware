using System.Security.Cryptography;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// T58a, H3 database logins on a real PostgreSQL 16 (the CI PostgreSQL job; skipped without
/// <c>DB_HOST</c>). Each test builds a new, empty database owned by a master login WITHOUT
/// superuser, as on RDS, then runs what a fresh deployment runs: <c>dbroles</c>, the migrations
/// as <c>dealoware_migrate</c> with the post-migration rights, and <c>dbroles --verify</c>.
/// The CI server has no TLS, so these call <see cref="DatabaseRoles"/> directly; the command
/// itself, over TLS, runs in the CI job "New database rehearsal".
/// The two logins are server-wide, so the tests of this class run one after another (xUnit
/// runs one class's tests in sequence) and each starts by dropping what an earlier one left.
/// </summary>
public class DatabaseRolesPostgresTests
{
    private const string Database = "dealoware_t58a_roles";
    private const string Master = "dealoware_t58a_master";

    [PostgresOnlyFact]
    public async Task DbRoles_OnANewDatabase_IsIdempotent_AndCreatesPlainLogins()
    {
        var setup = await NewDatabaseAsync();

        var first = await ApplyAsync(setup);
        var second = await ApplyAsync(setup);

        Assert.True(first.Ok, first.Output);
        Assert.True(second.Ok, second.Output);
        Assert.Equal(first.Output, second.Output);
        AssertOnlyOkLines(first.Output, "dbroles");
        setup.AssertNothingSensitive(first.Output);

        await using var super = await OpenSuperAsync(Database);
        foreach (var role in new[] { DatabaseRoles.AppRole, DatabaseRoles.MigrateRole })
        {
            Assert.True(await ScalarAsync<bool>(super,
                $"SELECT rolcanlogin AND NOT (rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls OR rolreplication) " +
                $"FROM pg_roles WHERE rolname = '{role}'"), $"{role} must be a plain login");
        }

        // The master may SET ROLE dealoware_migrate but does not inherit its rights (design §7 step 2).
        Assert.True(await ScalarAsync<bool>(super,
            $"SELECT pg_has_role('{Master}', '{DatabaseRoles.MigrateRole}', 'SET')"));
        Assert.False(await ScalarAsync<bool>(super,
            $"SELECT pg_has_role('{Master}', '{DatabaseRoles.MigrateRole}', 'USAGE')"));

        // PUBLIC is locked out of the database; the two logins may connect.
        Assert.False(await ScalarAsync<bool>(super,
            $"SELECT has_database_privilege('public', '{Database}', 'CONNECT')"));
        Assert.True(await ScalarAsync<bool>(super,
            $"SELECT has_database_privilege('{DatabaseRoles.AppRole}', '{Database}', 'CONNECT')"));
        Assert.False(await ScalarAsync<bool>(super,
            $"SELECT has_schema_privilege('{DatabaseRoles.AppRole}', 'public', 'CREATE')"));
        Assert.True(await ScalarAsync<bool>(super,
            $"SELECT has_schema_privilege('{DatabaseRoles.MigrateRole}', 'public', 'CREATE')"));

        // Both passwords work, and were stored as SCRAM verifiers.
        await using (var app = new NpgsqlConnection(setup.ConnectionString(DatabaseRoles.AppRole, setup.AppPassword)))
            await app.OpenAsync();
        await using (var migrate = new NpgsqlConnection(setup.ConnectionString(DatabaseRoles.MigrateRole, setup.MigratePassword)))
            await migrate.OpenAsync();
    }

    [PostgresOnlyFact]
    public async Task Migrate_GivesTheAppExactlyTheListedRights_AndTheAppLoginIsNotPrivileged()
    {
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);

        var migrate = await MigrateAsync(setup);
        Assert.True(migrate.Ok, migrate.Output);
        AssertOnlyOkLines(migrate.Output, "migrate");
        setup.AssertNothingSensitive(migrate.Output);

        // Twice: the rights are re-applied after every migration.
        Assert.True((await MigrateAsync(setup)).Ok);

        await using var super = await OpenSuperAsync(Database);
        Assert.Equal(0L, await ScalarAsync<long>(super,
            $"SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND tableowner <> '{DatabaseRoles.MigrateRole}'"));

        foreach (var table in DatabaseTableGrants.MayChange)
            Assert.Equal("SELECT,INSERT,UPDATE,DELETE", await AppRightsAsync(super, table));
        foreach (var table in DatabaseTableGrants.ReadAndAddOnly)
            Assert.Equal("SELECT,INSERT", await AppRightsAsync(super, table));
        Assert.Equal("SELECT", await AppRightsAsync(super, DatabaseTableGrants.HistoryTable));

        await using var app = new NpgsqlConnection(setup.ConnectionString(DatabaseRoles.AppRole, setup.AppPassword));
        await app.OpenAsync();
        var facts = await RuntimeLoginPrivilegeGuard.LoadFactsAsync(app);
        Assert.False(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts));

        await using var master = new NpgsqlConnection(setup.ConnectionString(Master, setup.MasterPassword));
        await master.OpenAsync();
        Assert.True(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(
            await RuntimeLoginPrivilegeGuard.LoadFactsAsync(master)));
    }

    [PostgresOnlyFact]
    public async Task Migrate_BeforeDbRoles_FailsAndSaysSo()
    {
        // No dbroles: the two logins do not exist. The master runs the migrations here.
        var setup = await NewDatabaseAsync();
        await using var options = new NpgsqlConnection(setup.ConnectionString(Master, setup.MasterPassword));
        await using (var db = new DealowareDbContext(
                         new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql(options).Options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        using var output = new StringWriter();
        Assert.False(await DatabaseRoles.ApplyAppGrantsAsync(options, output));
        Assert.Contains("run dbroles first", output.ToString(), StringComparison.Ordinal);
    }

    [PostgresOnlyFact]
    public async Task Verify_OnANewDatabase_PassesEveryCheck_AndLeavesNothingBehind()
    {
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);
        Assert.True((await MigrateAsync(setup)).Ok);

        await using var super = await OpenSuperAsync(Database);
        var auditRows = await ScalarAsync<long>(super, "SELECT count(*) FROM \"AdminAuditLog\"");
        var participants = await ScalarAsync<long>(super, "SELECT count(*) FROM \"Participants\"");

        using var output = new StringWriter();
        await using var app = new NpgsqlConnection(setup.ConnectionString(DatabaseRoles.AppRole, setup.AppPassword));
        var ok = await DatabaseRoles.VerifyAsync(app, output);

        Assert.True(ok, output.ToString());
        AssertOnlyOkLines(output.ToString(), "dbroles --verify");
        setup.AssertNothingSensitive(output.ToString());
        Assert.Equal(auditRows, await ScalarAsync<long>(super, "SELECT count(*) FROM \"AdminAuditLog\""));
        Assert.Equal(participants, await ScalarAsync<long>(super, "SELECT count(*) FROM \"Participants\""));
    }

    [PostgresOnlyFact]
    public async Task Verify_FailsWhenTheAppMayChangeTheAuditLog()
    {
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);
        Assert.True((await MigrateAsync(setup)).Ok);

        // A broad grant made by hand after the migration.
        await using (var super = await OpenSuperAsync(Database))
        {
            await using var grant = new NpgsqlCommand(
                $"GRANT UPDATE ON \"AdminAuditLog\" TO {DatabaseRoles.AppRole}", super);
            await grant.ExecuteNonQueryAsync();
        }

        using var output = new StringWriter();
        await using var app = new NpgsqlConnection(setup.ConnectionString(DatabaseRoles.AppRole, setup.AppPassword));
        Assert.False(await DatabaseRoles.VerifyAsync(app, output));
        Assert.Contains("UPDATE on AdminAuditLog is refused: failed (it was allowed)", output.ToString(), StringComparison.Ordinal);
    }

    [PostgresOnlyFact]
    public async Task Verify_FailsOnTheMasterLogin()
    {
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);

        using var output = new StringWriter();
        await using var master = new NpgsqlConnection(setup.ConnectionString(Master, setup.MasterPassword));
        Assert.False(await DatabaseRoles.VerifyAsync(master, output));
        Assert.Contains($"the login is {DatabaseRoles.AppRole}: failed", output.ToString(), StringComparison.Ordinal);
    }

    [PostgresOnlyFact]
    public async Task MigrateLoginCheck_RefusesTheMasterAndASuperuser_AndAllowsDealowareMigrate()
    {
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);

        // The master (CREATEROLE, CREATEDB, owner of the database, as on RDS): refused.
        using (var output = new StringWriter())
        {
            await using var master = new NpgsqlConnection(setup.ConnectionString(Master, setup.MasterPassword));
            Assert.False(await DatabaseRoles.RequireMigrateLoginAsync(master, output));
            Assert.Contains("the login is not a superuser, the master or the owner of the database: failed (refused:",
                output.ToString(), StringComparison.Ordinal);
            setup.AssertNothingSensitive(output.ToString());
        }

        // The server's superuser: refused.
        using (var output = new StringWriter())
        {
            await using var super = await OpenSuperAsync(Database);
            Assert.False(await DatabaseRoles.RequireMigrateLoginAsync(super, output));
            Assert.Contains(": failed (refused:", output.ToString(), StringComparison.Ordinal);
        }

        // dealoware_migrate: allowed, and nothing is printed after the step (migrate goes on).
        using (var output = new StringWriter())
        {
            await using var migrate = new NpgsqlConnection(
                setup.ConnectionString(DatabaseRoles.MigrateRole, setup.MigratePassword));
            Assert.True(await DatabaseRoles.RequireMigrateLoginAsync(migrate, output), output.ToString());
            var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Equal(
                ["migrate: connect: ok", "migrate: the login is not a superuser, the master or the owner of the database: ok"],
                lines);
        }
    }

    [PostgresOnlyFact]
    public async Task TransactionStartFailure_PrintsAStepNameOnly_AndReturnsFalse()
    {
        // BeginTransactionAsync throws on a connection that is already in a transaction. Before
        // the fix that exception escaped, and .NET printed the driver's message and stack.
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);
        Assert.True((await MigrateAsync(setup)).Ok);

        var runs = new (string Prefix, string User, string Password, Func<NpgsqlConnection, TextWriter, Task<bool>> Run)[]
        {
            ("dbroles", Master, setup.MasterPassword,
                (c, o) => DatabaseRoles.ApplyAsync(c, setup.AppPassword, setup.MigratePassword, o)),
            ("migrate", DatabaseRoles.MigrateRole, setup.MigratePassword,
                (c, o) => DatabaseRoles.ApplyAppGrantsAsync(c, o)),
            ("dbroles --verify", DatabaseRoles.AppRole, setup.AppPassword,
                (c, o) => DatabaseRoles.VerifyAsync(c, o)),
        };

        foreach (var (prefix, user, password, run) in runs)
        {
            using var output = new StringWriter();
            await using var connection = new NpgsqlConnection(setup.ConnectionString(user, password));
            await connection.OpenAsync();
            await using var held = await connection.BeginTransactionAsync();

            var ok = await run(connection, output);

            Assert.False(ok, prefix);
            Assert.Contains($"{prefix}: begin a transaction: failed (InvalidOperationException)", output.ToString(),
                StringComparison.Ordinal);
            Assert.EndsWith($"{prefix}: stopped at the first failure; nothing after it ran", output.ToString().TrimEnd(),
                StringComparison.Ordinal);
            setup.AssertNothingSensitive(output.ToString());
        }
    }

    [PostgresOnlyFact]
    public async Task DroppedConnection_AtTransactionStart_PrintsNoHostPasswordOrConnectionString()
    {
        // The server ends the session after the connection was opened. Npgsql sends BEGIN with
        // the next command, so the failure shows in that step; nothing escapes.
        var setup = await NewDatabaseAsync();
        Assert.True((await ApplyAsync(setup)).Ok);
        Assert.True((await MigrateAsync(setup)).Ok);

        var runs = new (string Prefix, string User, string Password, Func<NpgsqlConnection, TextWriter, Task<bool>> Run)[]
        {
            ("migrate", DatabaseRoles.MigrateRole, setup.MigratePassword,
                (c, o) => DatabaseRoles.ApplyAppGrantsAsync(c, o)),
            ("dbroles", Master, setup.MasterPassword,
                (c, o) => DatabaseRoles.ApplyAsync(c, setup.AppPassword, setup.MigratePassword, o)),
        };

        foreach (var (prefix, user, password, run) in runs)
        {
            using var output = new StringWriter();
            await using var connection = new NpgsqlConnection(setup.ConnectionString(user, password));
            await connection.OpenAsync();
            await using (var super = await OpenSuperAsync(Database))
            {
                await using var kill = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", super);
                kill.Parameters.AddWithValue("pid", connection.ProcessID);
                Assert.True((bool)(await kill.ExecuteScalarAsync())!);
            }

            var ex = await Record.ExceptionAsync(async () => Assert.False(await run(connection, output), prefix));

            Assert.Null(ex);
            Assert.Contains(": failed (", output.ToString(), StringComparison.Ordinal);
            Assert.Contains($"{prefix}: stopped at the first failure", output.ToString(), StringComparison.Ordinal);
            setup.AssertNothingSensitive(output.ToString());
        }
    }

    private sealed record Setup(string Host, string Port, string MasterPassword, string AppPassword, string MigratePassword)
    {
        public string ConnectionString(string user, string password)
            => new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = int.Parse(Port),
                Database = Database,
                Username = user,
                Password = password,
                SslMode = SslMode.Disable,
                Pooling = false,
            }.ConnectionString;

        public void AssertNothingSensitive(string output)
        {
            foreach (var value in new[] { MasterPassword, AppPassword, MigratePassword, Host, "Host=", "Password=", "Username=" })
                Assert.DoesNotContain(value, output, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<Setup> NewDatabaseAsync()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")!;
        var port = Environment.GetEnvironmentVariable("DB_PORT") is { Length: > 0 } p ? p : "5432";
        var setup = new Setup(host, port,
            "m" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            "a" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            "g" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));

        await using var super = await OpenSuperAsync(database: null);
        await using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {Database} WITH (FORCE)", super))
            await drop.ExecuteNonQueryAsync();
        await DropOwnedEverywhereAsync(super);

        foreach (var sql in new[]
                 {
                     $"DROP ROLE IF EXISTS {DatabaseRoles.AppRole}",
                     $"DROP ROLE IF EXISTS {DatabaseRoles.MigrateRole}",
                     $"DROP ROLE IF EXISTS {Master}",
                     // As on RDS: the master may create roles and databases but is not a superuser.
                     $"CREATE ROLE {Master} LOGIN CREATEROLE CREATEDB PASSWORD '{setup.MasterPassword}'",
                     $"CREATE DATABASE {Database} OWNER {Master}",
                 })
        {
            await using var cmd = new NpgsqlCommand(sql, super);
            await cmd.ExecuteNonQueryAsync();
        }

        return setup;
    }

    /// <summary>
    /// The three logins are server-wide: a role that still holds rights in another database
    /// (an earlier run, the rehearsal script) cannot be dropped. Clear them first, in every
    /// database that takes connections. Test servers only.
    /// </summary>
    private static async Task DropOwnedEverywhereAsync(NpgsqlConnection super)
    {
        var roles = new List<string>();
        await using (var cmd = new NpgsqlCommand(
                         "SELECT rolname FROM pg_roles WHERE rolname = ANY(@names)", super))
        {
            cmd.Parameters.AddWithValue("names", new[] { DatabaseRoles.AppRole, DatabaseRoles.MigrateRole, Master });
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                roles.Add(reader.GetString(0));
        }

        if (roles.Count == 0)
            return;

        var databases = new List<string>();
        await using (var cmd = new NpgsqlCommand(
                         "SELECT datname FROM pg_database WHERE datallowconn AND NOT datistemplate", super))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                databases.Add(reader.GetString(0));
        }

        // One role at a time: a default ACL of dealoware_migrate that grants to dealoware_app
        // depends on both, and DROP OWNED BY both at once fails on it.
        foreach (var database in databases)
        {
            await using var each = await OpenSuperAsync(database);
            foreach (var role in roles)
            {
                await using var cmd = new NpgsqlCommand($"DROP OWNED BY {role}", each);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task<(bool Ok, string Output)> ApplyAsync(Setup setup)
    {
        using var output = new StringWriter();
        await using var master = new NpgsqlConnection(setup.ConnectionString(Master, setup.MasterPassword));
        var ok = await DatabaseRoles.ApplyAsync(master, setup.AppPassword, setup.MigratePassword, output);
        return (ok, output.ToString());
    }

    /// <summary>What the migrate command does: the migrations, then the rights, as dealoware_migrate.</summary>
    private static async Task<(bool Ok, string Output)> MigrateAsync(Setup setup)
    {
        await using var connection = new NpgsqlConnection(
            setup.ConnectionString(DatabaseRoles.MigrateRole, setup.MigratePassword));
        await using (var db = new DealowareDbContext(
                         new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql(connection).Options))
        {
            await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db);
        }

        using var output = new StringWriter();
        var ok = await DatabaseRoles.ApplyAppGrantsAsync(connection, output);
        return (ok, output.ToString());
    }

    private static async Task<NpgsqlConnection> OpenSuperAsync(string? database)
    {
        Assert.True(IsolatedWebApplicationFactory.TryBuildTestPostgresConnectionString(out var cs),
            "DB_HOST is required.");
        var builder = new NpgsqlConnectionStringBuilder(cs) { Pooling = false };
        if (database is not null)
            builder.Database = database;
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<string> AppRightsAsync(NpgsqlConnection super, string table)
    {
        var rights = new List<string>();
        foreach (var right in new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER" })
        {
            if (await ScalarAsync<bool>(super,
                    $"SELECT has_table_privilege('{DatabaseRoles.AppRole}', 'public.\"{table}\"', '{right}')"))
                rights.Add(right);
        }

        return string.Join(",", rights);
    }

    private static void AssertOnlyOkLines(string output, string prefix)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.NotEmpty(lines);
        Assert.All(lines, l => Assert.StartsWith(prefix + ": ", l, StringComparison.Ordinal));
        Assert.All(lines.Take(lines.Length - 1), l => Assert.EndsWith(": ok", l, StringComparison.Ordinal));
        Assert.Equal($"{prefix}: every step ok", lines[^1]);
    }
}

using System.Security.Cryptography;
using System.Text;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dealoware.Api.Tests;

/// <summary>
/// T58a, H3 database logins: the parts of <c>dbroles</c> that need no database. The run on
/// PostgreSQL is in <see cref="DatabaseRolesPostgresTests"/> and in the CI job
/// "New database rehearsal" (scripts/new-database-rehearsal.sh).
/// </summary>
public class DatabaseRolesCommandTests
{
    // Throwaway values; the tests check that none of them comes back in the output.
    private const string MarkerHost = "dbroles-marker-host.invalid";
    private const string MasterPassword = "MasterPw0123456789abcdefMASTER";
    private const string AppPassword = "AppPw0123456789abcdefghijAPP";
    private const string MigratePassword = "MigratePw0123456789abcdefMIG";

    [Theory]
    [InlineData(new[] { "dbroles" }, true)]
    [InlineData(new[] { "DBROLES" }, true)]
    [InlineData(new[] { "dbroles", "--verify" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "migrate" }, false)]
    [InlineData(new[] { "--dbroles" }, false)]
    [InlineData(new[] { "run", "dbroles" }, false)]
    public void IsDbRolesArgs_DetectsTheCommand(string[] args, bool expected)
    {
        Assert.Equal(expected, DatabaseRolesCommand.IsDbRolesArgs(args));
    }

    [Theory]
    [InlineData("ASPNETCORE_ENVIRONMENT")]
    [InlineData("DOTNET_ENVIRONMENT")]
    public async Task RefusedInDevelopment_BeforeAnyConnection(string variable)
    {
        foreach (var args in new[] { new[] { "dbroles" }, new[] { "dbroles", "--verify" } })
        {
            var env = ProductionEnvironment();
            env[variable] = "Development";
            var (code, output) = await RunAsync(args, env);

            Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
            Assert.Contains("refused: it does not run in Development", output, StringComparison.Ordinal);
            Assert.DoesNotContain("connect", output, StringComparison.Ordinal);
            AssertNothingSensitive(output);
        }
    }

    [Theory]
    [InlineData("--force")]
    [InlineData("--verify --force")]
    [InlineData("verify")]
    public async Task UnknownArguments_AreRefused(string extra)
    {
        var args = new[] { "dbroles" }.Concat(extra.Split(' ')).ToArray();
        var (code, output) = await RunAsync(args, ProductionEnvironment());

        Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
        Assert.Contains("usage: dbroles [--verify]", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingDatabaseSettings_AreRefused_WithoutValues()
    {
        var env = ProductionEnvironment();
        env.Remove("DB_HOST");
        var (code, output) = await RunAsync(["dbroles"], env);

        Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
        Assert.Contains("DB_HOST, DB_PORT, DB_NAME and the TLS root certificate", output, StringComparison.Ordinal);
        AssertNothingSensitive(output);
    }

    [Theory]
    [InlineData(DatabaseRolesCommand.AppPasswordVariable, null)]
    [InlineData(DatabaseRolesCommand.AppPasswordVariable, "short")]
    [InlineData(DatabaseRolesCommand.AppPasswordVariable, "has a space in it 0123456789")]
    [InlineData(DatabaseRolesCommand.MigratePasswordVariable, null)]
    [InlineData(DatabaseRolesCommand.MigratePasswordVariable, "short")]
    public async Task MissingOrWeakLoginPassword_IsRefused_NamingOnlyTheVariable(string variable, string? value)
    {
        var env = ProductionEnvironment();
        env[variable] = value;
        var (code, output) = await RunAsync(["dbroles"], env);

        Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
        Assert.Contains(variable, output, StringComparison.Ordinal);
        if (value is not null)
            Assert.DoesNotContain(value, output, StringComparison.Ordinal);
        AssertNothingSensitive(output);
    }

    [Theory]
    [InlineData(DatabaseRolesCommand.AppPasswordVariable, DatabaseRolesCommand.MigratePasswordVariable)]
    [InlineData(DatabaseRolesCommand.AppPasswordVariable, "DB_PASSWORD")]
    [InlineData(DatabaseRolesCommand.MigratePasswordVariable, "DB_PASSWORD")]
    public async Task ReusedPassword_IsRefused(string first, string second)
    {
        var env = ProductionEnvironment();
        env[first] = "SamePw0123456789abcdefghijSAME";
        env[second] = "SamePw0123456789abcdefghijSAME";
        var (code, output) = await RunAsync(["dbroles"], env);

        Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
        Assert.Contains("passwords must all differ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("SamePw", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DatabaseRoles.AppRole)]
    [InlineData(DatabaseRoles.MigrateRole)]
    public async Task OneOfTheCreatedLoginsAsMaster_IsRefused(string user)
    {
        var env = ProductionEnvironment();
        env["DB_USERNAME"] = user;
        var (code, output) = await RunAsync(["dbroles"], env);

        Assert.Equal(DatabaseRolesCommand.ExitRefused, code);
        Assert.Contains("DB_USERNAME must be the master login", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnreachableDatabase_FailsWithStepNamesOnly_NoPasswordHostOrConnectionString(bool verify)
    {
        string[] args = verify ? ["dbroles", "--verify"] : ["dbroles"];
        var (code, output) = await RunAsync(args, ProductionEnvironment());

        Assert.Equal(DatabaseRolesCommand.ExitFailed, code);
        Assert.Contains(": failed (", output, StringComparison.Ordinal);
        Assert.Contains("stopped at the first failure", output, StringComparison.Ordinal);
        AssertNothingSensitive(output);
    }

    [Fact]
    public void StepWriter_PrintsNoExceptionMessage()
    {
        var message = $"Host={MarkerHost};Password={MasterPassword}";
        Assert.DoesNotContain(MarkerHost, DatabaseStepWriter.Describe(new InvalidOperationException(message)));
        Assert.DoesNotContain(MarkerHost, DatabaseStepWriter.Describe(new Npgsql.NpgsqlException(message)));
        Assert.DoesNotContain(MarkerHost, DatabaseStepWriter.Describe(new Exception("outer", new TimeoutException(message))));
        Assert.Equal("fixed reason", DatabaseStepWriter.Describe(new DatabaseStepException("fixed reason")));
    }

    [Fact]
    public void Program_DbRolesCommand_ReturnsBeforeTheWebHost()
    {
        var program = ReadRepoFile("src/Dealoware.Api/Program.cs");
        var command = program.IndexOf(
            "Environment.ExitCode = await DatabaseRolesCommand.RunAsync(args);", StringComparison.Ordinal);
        var earlyReturn = program.IndexOf("return;", command, StringComparison.Ordinal);
        var webHost = program.IndexOf("WebApplication.CreateBuilder", StringComparison.Ordinal);

        Assert.True(command >= 0, "Program.cs must route the dbroles command.");
        Assert.True(earlyReturn > command && earlyReturn < webHost, "dbroles must return before the web host.");
    }

    [Fact]
    public void DatabaseMigrateCommand_AppliesTheAppGrantsAfterTheMigrations()
    {
        var source = ReadRepoFile("src/Dealoware.Api/DatabaseMigrateCommand.cs");
        var migrations = source.IndexOf("await DatabaseSchemaBootstrap.ApplyMigrationsAsync(", StringComparison.Ordinal);
        var grants = source.IndexOf("await DatabaseRoles.ApplyAppGrantsAsync(", StringComparison.Ordinal);

        Assert.True(migrations >= 0 && grants > migrations, "migrate must apply the H3 rights after the migrations.");
        Assert.Contains("return 1;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseMigrateCommand_ChecksTheLoginBeforeTheMigrations_AndStopsOnRefusal()
    {
        var source = ReadRepoFile("src/Dealoware.Api/DatabaseMigrateCommand.cs");
        var check = source.IndexOf("await DatabaseRoles.RequireMigrateLoginAsync(", StringComparison.Ordinal);
        var migrations = source.IndexOf("await DatabaseSchemaBootstrap.ApplyMigrationsAsync(", StringComparison.Ordinal);
        var stop = source.IndexOf("return 1;", check, StringComparison.Ordinal);

        Assert.True(check >= 0 && migrations > check, "migrate must check its login before any migration.");
        Assert.True(stop > check && stop < migrations, "a refused login must stop migrate before the migrations.");
        Assert.Contains("ShouldCheckLogin(db.Database.IsNpgsql()", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, "Production", null, true)]
    [InlineData(true, null, null, true)]
    [InlineData(true, "Staging", "Production", true)]
    [InlineData(true, "Development", null, false)]
    [InlineData(true, null, "Development", false)]
    [InlineData(false, "Production", null, false)]
    public void MigrateLoginCheck_RunsOnPostgresOutsideDevelopment(bool postgres, string? aspnet, string? dotnet, bool expected)
    {
        var env = new Dictionary<string, string?> { ["ASPNETCORE_ENVIRONMENT"] = aspnet, ["DOTNET_ENVIRONMENT"] = dotnet };
        Assert.Equal(expected, DatabaseMigrateCommand.ShouldCheckLogin(postgres, n => env.GetValueOrDefault(n)));
    }

    [Theory]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    public void MigrateLogin_AnyMasterLikeFact_IsRefused(bool super, bool createRole, bool createDb, bool bypassRls, bool rds, bool ownsDb)
    {
        Assert.True(DatabaseRoles.IsRefusedForMigrate(
            new DatabaseRoles.MigrateLoginFacts(super, createRole, createDb, bypassRls, rds, ownsDb)));
    }

    [Fact]
    public void MigrateLogin_PlainLoginThatOwnsTheTables_IsAllowed()
    {
        Assert.False(DatabaseRoles.IsRefusedForMigrate(
            new DatabaseRoles.MigrateLoginFacts(false, false, false, false, false, false)));
        Assert.Contains("pg_catalog.pg_has_role(current_user, d.datdba, 'MEMBER')", DatabaseRoles.MigrateLoginFactsSql,
            StringComparison.Ordinal);
        Assert.Contains("'rds_superuser'", DatabaseRoles.MigrateLoginFactsSql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrateLoginCheck_UnreachableDatabase_FailsWithStepNamesOnly()
    {
        using var output = new StringWriter();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var connection = new Npgsql.NpgsqlConnection(new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = MarkerHost, Database = "dealoware", Username = "dealoware", Password = MasterPassword, Timeout = 5,
        }.ConnectionString);

        Assert.False(await DatabaseRoles.RequireMigrateLoginAsync(connection, output, timeout.Token));
        Assert.Contains("migrate: connect: failed (", output.ToString(), StringComparison.Ordinal);
        AssertNothingSensitive(output.ToString());
    }

    private static Dictionary<string, string?> ProductionEnvironment()
        => new(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["DB_HOST"] = MarkerHost,
            ["DB_PORT"] = "5432",
            ["DB_NAME"] = "dealoware",
            ["DB_USERNAME"] = "dealoware",
            ["DB_PASSWORD"] = MasterPassword,
            [DatabaseRolesCommand.AppPasswordVariable] = AppPassword,
            [DatabaseRolesCommand.MigratePasswordVariable] = MigratePassword,
        };

    private static async Task<(int Code, string Output)> RunAsync(string[] args, Dictionary<string, string?> env)
    {
        using var output = new StringWriter();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var code = await DatabaseRolesCommand.RunAsync(
            args, name => env.TryGetValue(name, out var v) ? v : null, output, timeout.Token);
        return (code, output.ToString());
    }

    private static void AssertNothingSensitive(string output)
    {
        foreach (var value in new[]
                 {
                     MarkerHost, MasterPassword, AppPassword, MigratePassword,
                     "Host=", "Password=", "Username=", "SSL Mode=", "Root Certificate=",
                 })
        {
            Assert.DoesNotContain(value, output, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}

/// <summary>
/// T58a, specification §3.3: every table of the model is in exactly one of the two lists of
/// <see cref="DatabaseTableGrants"/>. Fails when a table is added to the model without being
/// put in one of them.
/// </summary>
public class DatabaseTableGrantsTests
{
    [Fact]
    public void EveryModelTable_IsInExactlyOneList()
    {
        var modelTables = ModelTableNames();
        var mayChange = DatabaseTableGrants.MayChange;
        var readAndAdd = DatabaseTableGrants.ReadAndAddOnly;

        var inNeither = modelTables.Where(t => !mayChange.Contains(t) && !readAndAdd.Contains(t)).ToList();
        Assert.True(inNeither.Count == 0,
            "Put each new table in DatabaseTableGrants.MayChange or DatabaseTableGrants.ReadAndAddOnly: " +
            string.Join(", ", inNeither));

        var inBoth = mayChange.Intersect(readAndAdd).ToList();
        Assert.True(inBoth.Count == 0, "A table is in both lists: " + string.Join(", ", inBoth));

        var notInModel = mayChange.Concat(readAndAdd).Where(t => !modelTables.Contains(t)).ToList();
        Assert.True(notInModel.Count == 0, "A listed table is not in the model: " + string.Join(", ", notInModel));

        Assert.Equal(mayChange.Count, mayChange.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(readAndAdd.Count, readAndAdd.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AuditLog_IsReadAndAddOnly_AndTheHistoryTableIsInNeitherList()
    {
        Assert.Contains(DatabaseTableGrants.AuditTable, DatabaseTableGrants.ReadAndAddOnly);
        Assert.DoesNotContain(DatabaseTableGrants.AuditTable, DatabaseTableGrants.MayChange);
        Assert.Equal(DatabaseMigrationBaseline.HistoryTableName, DatabaseTableGrants.HistoryTable);
        Assert.DoesNotContain(DatabaseTableGrants.HistoryTable, DatabaseTableGrants.MayChange);
        Assert.DoesNotContain(DatabaseTableGrants.HistoryTable, DatabaseTableGrants.ReadAndAddOnly);
    }

    [Fact]
    public void ModelTables_AllLiveInThePublicSchema()
    {
        using var db = NewContext();
        var schemas = db.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Where(t => t.GetTableName() is not null)
            .Select(t => t.GetSchema())
            .Distinct()
            .ToList();
        Assert.All(schemas, s => Assert.True(s is null or "public", "A table outside the public schema."));
    }

    private static HashSet<string> ModelTableNames()
    {
        using var db = NewContext();
        var names = db.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(names);
        return names;
    }

    private static DealowareDbContext NewContext()
        => new(new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql("Host=unused").Options);
}

/// <summary>
/// The SCRAM-SHA-256 verifier <c>dbroles</c> sends instead of a plain password.
/// </summary>
public class PostgresScramVerifierTests
{
    [Fact]
    public void DerivedKeys_MatchTheRfc7677Example()
    {
        // RFC 7677 §3: user "user", password "pencil".
        var salt = Convert.FromBase64String("W22ZaJ0SNY7soEsUEjb6gQ==");
        var (storedKey, serverKey) = PostgresScramVerifier.DeriveKeys("pencil", salt, 4096);

        const string clientFirstBare = "n=user,r=rOprNGfwEbeRWgbNEkqO";
        const string serverFirst = "r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096";
        const string clientFinalWithoutProof = "c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0";
        var authMessage = Encoding.UTF8.GetBytes($"{clientFirstBare},{serverFirst},{clientFinalWithoutProof}");

        var serverSignature = HMACSHA256.HashData(serverKey, authMessage);
        Assert.Equal("6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=", Convert.ToBase64String(serverSignature));

        // ClientProof = ClientKey XOR ClientSignature, and SHA256(ClientKey) = StoredKey.
        var clientSignature = HMACSHA256.HashData(storedKey, authMessage);
        var proof = Convert.FromBase64String("dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=");
        var clientKey = proof.Zip(clientSignature, (a, b) => (byte)(a ^ b)).ToArray();
        Assert.Equal(storedKey, SHA256.HashData(clientKey));
    }

    [Fact]
    public void Verifier_HasPostgresFormat_FreshSalt_AndNoPlainPassword()
    {
        const string password = "Throwaway0123456789abcdefABCDEF";
        var first = PostgresScramVerifier.Create(password);
        var second = PostgresScramVerifier.Create(password);

        Assert.Matches(@"^SCRAM-SHA-256\$4096:[A-Za-z0-9+/=]+\$[A-Za-z0-9+/=]+:[A-Za-z0-9+/=]+$", first);
        Assert.NotEqual(first, second);
        Assert.DoesNotContain(password, first, StringComparison.Ordinal);
        Assert.DoesNotContain("'", first, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tooshort0123456")]
    [InlineData("has a space 0123456789")]
    [InlineData("non-ascii-é-0123456789")]
    public void UnacceptablePasswords_AreRefused(string? password)
    {
        Assert.False(PostgresScramVerifier.IsAcceptablePassword(password));
        if (password is not null)
            Assert.Throws<ArgumentException>(() => PostgresScramVerifier.Create(password));
    }
}

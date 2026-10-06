using Dealoware.Api;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Tests;

/// <summary>
/// H3: least-privilege runtime login decision is a pure function over role facts.
/// The migrate one-shot must not invoke the guard. No live Postgres required.
/// </summary>
public class RuntimeLoginPrivilegeGuardTests
{
    private static readonly RuntimeLoginPrivilegeFacts LeastPrivilege = new(
        RolSuper: false,
        RolCreateRole: false,
        RolCreateDb: false,
        RolBypassRls: false,
        IsRdsSuperuserMember: false,
        OwnsAppSchemaTable: false);

    private static readonly RuntimeLoginPrivilegeFacts PrivilegedSuperuser = LeastPrivilege with { RolSuper = true };

    [Theory]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    [InlineData(true, true, true, true, true, true)]
    public void IsPrivilegedRuntimeLogin_AnyFlag_IsPrivileged(
        bool rolSuper,
        bool rolCreateRole,
        bool rolCreateDb,
        bool rolBypassRls,
        bool isRdsSuperuserMember,
        bool ownsAppSchemaTable)
    {
        var facts = new RuntimeLoginPrivilegeFacts(
            rolSuper, rolCreateRole, rolCreateDb, rolBypassRls, isRdsSuperuserMember, ownsAppSchemaTable);

        Assert.True(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts));
    }

    [Fact]
    public void IsPrivilegedRuntimeLogin_IndirectRdsSuperuserMembership_IsPrivileged()
    {
        // PrivilegeFactsSql uses pg_has_role(..., 'MEMBER'), so inherited / SET-only
        // membership in rds_superuser arrives as this fact.
        var facts = LeastPrivilege with { IsRdsSuperuserMember = true };
        Assert.True(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts));
    }

    [Fact]
    public void IsPrivilegedRuntimeLogin_OwnerByMembership_IsPrivileged()
    {
        // Table owned by dealoware_migrate / dealoware; current_user is a MEMBER
        // (including SET-only). PrivilegeFactsSql uses pg_has_role on tableowner.
        var facts = LeastPrivilege with { OwnsAppSchemaTable = true };
        Assert.True(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts));
    }

    [Fact]
    public void IsPrivilegedRuntimeLogin_BypassRls_IsPrivileged()
    {
        var facts = LeastPrivilege with { RolBypassRls = true };
        Assert.True(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(facts));
    }

    [Fact]
    public void PrivilegeFactsSql_UsesPgHasRoleForInheritedMembership()
    {
        var sql = RuntimeLoginPrivilegeGuard.PrivilegeFactsSql;
        Assert.Contains("rolbypassrls", sql, StringComparison.Ordinal);
        Assert.Contains("pg_has_role(current_user, s.oid, 'MEMBER')", sql, StringComparison.Ordinal);
        Assert.Contains("pg_has_role(current_user, t.tableowner, 'MEMBER')", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("m.member = r.oid", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("t.tableowner = current_user", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void IsPrivilegedRuntimeLogin_AllFalse_IsLeastPrivilege()
    {
        Assert.False(RuntimeLoginPrivilegeGuard.IsPrivilegedRuntimeLogin(LeastPrivilege));
    }

    [Theory]
    [InlineData(DatabaseProvider.Postgres, "Production", false, true)]
    [InlineData(DatabaseProvider.Postgres, "production", false, true)]
    [InlineData(DatabaseProvider.Postgres, "Staging", false, true)]
    [InlineData(DatabaseProvider.Postgres, "Testing", false, true)]
    [InlineData(DatabaseProvider.Postgres, "CustomEnv", false, true)]
    [InlineData(DatabaseProvider.Postgres, "Development", false, false)]
    [InlineData(DatabaseProvider.Postgres, "development", false, false)]
    [InlineData(DatabaseProvider.Sqlite, "Production", false, false)]
    [InlineData(DatabaseProvider.Sqlite, "Development", false, false)]
    [InlineData(DatabaseProvider.Postgres, "Production", true, false)]
    [InlineData(DatabaseProvider.Postgres, "Staging", true, false)]
    [InlineData(DatabaseProvider.Sqlite, "Production", true, false)]
    [InlineData(DatabaseProvider.Postgres, null, false, true)]
    [InlineData(DatabaseProvider.Postgres, "", false, true)]
    public void ShouldEnforce_OnlyPostgresNonDevelopmentWithoutBreakGlass(
        DatabaseProvider provider,
        string? environment,
        bool allowPrivileged,
        bool expected)
    {
        Assert.Equal(
            expected,
            RuntimeLoginPrivilegeGuard.ShouldEnforce(provider, environment, allowPrivileged));
    }

    [Fact]
    public void IsBreakGlassActive_OnlyWhenGuardWouldOtherwiseRun()
    {
        Assert.True(RuntimeLoginPrivilegeGuard.IsBreakGlassActive(
            DatabaseProvider.Postgres, "Production", allowPrivilegedRuntimeLogin: true));
        Assert.False(RuntimeLoginPrivilegeGuard.IsBreakGlassActive(
            DatabaseProvider.Postgres, "Production", allowPrivilegedRuntimeLogin: false));
        Assert.False(RuntimeLoginPrivilegeGuard.IsBreakGlassActive(
            DatabaseProvider.Postgres, "Development", allowPrivilegedRuntimeLogin: true));
        Assert.False(RuntimeLoginPrivilegeGuard.IsBreakGlassActive(
            DatabaseProvider.Sqlite, "Production", allowPrivilegedRuntimeLogin: true));
    }

    [Fact]
    public async Task EnforceIfRequired_LeastPrivilegeFacts_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            RuntimeLoginPrivilegeGuard.EnforceIfRequiredAsync(
                DatabaseProvider.Postgres,
                "Production",
                allowPrivilegedRuntimeLogin: false,
                _ => Task.FromResult(LeastPrivilege)));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EnforceIfRequired_PrivilegedFacts_ThrowsSafeMessage()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RuntimeLoginPrivilegeGuard.EnforceIfRequiredAsync(
                DatabaseProvider.Postgres,
                "Production",
                allowPrivilegedRuntimeLogin: false,
                _ => Task.FromResult(PrivilegedSuperuser)));

        Assert.Equal(RuntimeLoginPrivilegeGuard.PrivilegedLoginMessage, ex.Message);
        AssertSafeMessage(ex.Message);
    }

    [Fact]
    public async Task EnforceIfRequired_SkippedPaths_DoNotLoadFacts()
    {
        var cases = new (DatabaseProvider Provider, string? Environment, bool Allow)[]
        {
            (DatabaseProvider.Sqlite, "Production", false),
            (DatabaseProvider.Postgres, "Development", false),
            (DatabaseProvider.Postgres, "Production", true)
        };

        foreach (var (provider, environment, allow) in cases)
        {
            var called = false;
            await RuntimeLoginPrivilegeGuard.EnforceIfRequiredAsync(
                provider,
                environment,
                allow,
                _ =>
                {
                    called = true;
                    return Task.FromResult(PrivilegedSuperuser);
                });

            Assert.False(called, $"loadFacts was invoked for {provider}/{environment}/allow={allow}");
        }
    }

    [Fact]
    public async Task EnforceIfRequired_QueryFailure_FailsClosedWithoutInnerDetails()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RuntimeLoginPrivilegeGuard.EnforceIfRequiredAsync(
                DatabaseProvider.Postgres,
                "Production",
                allowPrivilegedRuntimeLogin: false,
                _ => throw new InvalidOperationException("Host=secret.example;Password=leaked")));

        Assert.Equal(RuntimeLoginPrivilegeGuard.CouldNotVerifyMessage, ex.Message);
        Assert.Null(ex.InnerException);
        AssertSafeMessage(ex.Message);
        Assert.DoesNotContain("Host=secret.example", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=leaked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnforceIfRequired_DriverException_IsWrappedWithoutConnectionDetails()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RuntimeLoginPrivilegeGuard.EnforceIfRequiredAsync(
                DatabaseProvider.Postgres,
                "Production",
                allowPrivilegedRuntimeLogin: false,
                _ => throw new TimeoutException("Failed to connect to Host=db.internal user=master")));

        Assert.Equal(RuntimeLoginPrivilegeGuard.CouldNotVerifyMessage, ex.Message);
        Assert.Null(ex.InnerException);
        AssertSafeMessage(ex.Message);
        Assert.DoesNotContain("Host=", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("db.internal", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("master", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Messages_ContainNoConnectionDetails()
    {
        AssertSafeMessage(RuntimeLoginPrivilegeGuard.PrivilegedLoginMessage);
        AssertSafeMessage(RuntimeLoginPrivilegeGuard.CouldNotVerifyMessage);
        AssertSafeMessage(RuntimeLoginPrivilegeGuard.BreakGlassMessage);
    }

    [Fact]
    public void ResolveProvider_SqliteContext_IsSqlite()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new DealowareDbContext(options);
        Assert.Equal(DatabaseProvider.Sqlite, RuntimeLoginPrivilegeGuard.ResolveProvider(db));
    }

    [Fact]
    public async Task EnforceAsync_SqliteContext_DoesNotQueryAndDoesNotThrow()
    {
        await using var connection = new SqliteConnection("Data Source=PrivilegeGuard_Sqlite;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new DealowareDbContext(options);
        var ex = await Record.ExceptionAsync(() =>
            RuntimeLoginPrivilegeGuard.EnforceAsync(db, "Production", allowPrivilegedRuntimeLogin: false));

        Assert.Null(ex);
    }

    [Fact]
    public void DatabaseMigrateCommand_Source_DoesNotInvokePrivilegeGuard()
    {
        var source = ReadRepoFile("src/Dealoware.Api/DatabaseMigrateCommand.cs");

        Assert.DoesNotContain("RuntimeLoginPrivilegeGuard", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowPrivilegedRuntimeLogin", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsPrivilegedRuntimeLogin", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EnforceIfRequiredAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EnforceAsync", source, StringComparison.Ordinal);
        Assert.Contains("ApplyMigrationsAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_InvokesGuardOnlyAfterMigrateEarlyReturn()
    {
        var source = ReadRepoFile("src/Dealoware.Api/Program.cs");

        var migrateReturn = source.IndexOf(
            "Environment.ExitCode = await DatabaseMigrateCommand.RunAsync(args);",
            StringComparison.Ordinal);
        var earlyReturn = source.IndexOf("return;", migrateReturn, StringComparison.Ordinal);
        var guardCall = source.IndexOf("RuntimeLoginPrivilegeGuard", StringComparison.Ordinal);

        Assert.True(migrateReturn >= 0, "Program.cs must keep the migrate entrypoint.");
        Assert.True(earlyReturn > migrateReturn, "Migrate path must return before the web host.");
        Assert.True(guardCall > earlyReturn, "Privilege guard must run only on the long-lived API path.");
        Assert.Contains("Environment.Exit(1)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void IsMigrateArgs_True_IsTheMigratePathThatSkipsTheGuard()
    {
        Assert.True(DatabaseMigrateCommand.IsMigrateArgs(["migrate"]));
        Assert.False(DatabaseMigrateCommand.IsMigrateArgs([]));
        Assert.False(DatabaseMigrateCommand.IsMigrateArgs(["run"]));
    }

    [Fact]
    public void ConfigKey_IsExplicitBreakGlassPath()
    {
        Assert.Equal("Database:AllowPrivilegedRuntimeLogin", RuntimeLoginPrivilegeGuard.AllowPrivilegedRuntimeLoginKey);
    }

    private static void AssertSafeMessage(string message)
    {
        Assert.DoesNotContain("Host=", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("arn:", message, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}

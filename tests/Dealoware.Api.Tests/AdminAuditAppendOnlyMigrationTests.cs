using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Dealoware.Infrastructure.Persistence.Migrations;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security O9: append-only trigger is Postgres-only and is not added to the
/// frozen 13-table / 15-table stamp specs.
/// </summary>
public class AdminAuditAppendOnlyMigrationTests
{
    [Fact]
    public void TdAdm101_Migration380_IsProviderConditionalAndRaisesOnMutate()
    {
        var source = ReadRepoFile("src/Dealoware.Infrastructure/Persistence/Migrations/20261006000380_AdminAuditLogAppendOnly.cs");
        Assert.Contains("IsPostgres", source, StringComparison.Ordinal);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", source, StringComparison.Ordinal);
        Assert.Contains("UPDATE OR DELETE", source, StringComparison.Ordinal);
        Assert.Contains("TRUNCATE", source, StringComparison.Ordinal);
        Assert.Contains("AdminAuditLog is append-only", source, StringComparison.Ordinal);
        Assert.Contains("\"AdminAuditLog\"", source, StringComparison.Ordinal);
        Assert.Contains("return;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BaselineSchema.Columns", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BaselineTableNames", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TdAdm101_FrozenStampSpecs_DoNotGainTriggerOrColumns()
    {
        Assert.DoesNotContain(
            AdminAuditLogAppendOnly.MigrationId,
            DatabaseMigrationBaseline.BaselineTableNames);
        Assert.Equal(13, DatabaseMigrationBaseline.Pre20TableNames.Count);
        Assert.Equal(15, DatabaseMigrationBaseline.BaselineTableNames.Count);
        Assert.Contains("AdminAuditLog", DatabaseMigrationBaseline.BaselineTableNames);
        Assert.DoesNotContain(
            BaselineSchema.Columns,
            c => c.Name.Contains("trigger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TdAdm101_RepositoryAndApi_HaveNoUpdateOrDeletePath()
    {
        var repo = typeof(IAdminAuditRepository).GetMethods().Select(m => m.Name).ToHashSet();
        Assert.Contains("AddAsync", repo);
        Assert.Contains("ListPageAsync", repo);
        Assert.DoesNotContain("UpdateAsync", repo);
        Assert.DoesNotContain("DeleteAsync", repo);

        var endpoints = ReadRepoFile("src/Dealoware.Api/Admin/AdminAuditEndpoints.cs");
        Assert.Contains("MapGet(\"/admin/api/audit\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("405", endpoints);
        Assert.DoesNotContain("MapPut", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/admin/api/audit\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void TdAdm052_AdminCookiePath_RemainsAdminRoot()
    {
        var options = Dealoware.Api.Admin.AdminSessionCookie.CreateOptions();
        Assert.Equal("/admin", options.Path);
    }

    private static string ReadRepoFile(string relativePath)
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

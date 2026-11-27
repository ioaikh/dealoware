using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dealoware.Api.Tests;

/// <summary>
/// T09c, <c>dbcheck</c> (T09 spec §5.2): the parts that need no database. The run on
/// PostgreSQL is in <see cref="DatabaseCheckPostgresTests"/>.
/// </summary>
public class DatabaseCheckCommandTests
{
    // Throwaway values; the tests check that none of them comes back in the output.
    private const string MarkerHost = "dbcheck-marker-host.invalid";
    private const string MarkerPassword = "CheckPw0123456789abcdefCHECK";

    [Theory]
    [InlineData(new[] { "dbcheck" }, true)]
    [InlineData(new[] { "DBCHECK", "--cutoff", "x" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "migrate" }, false)]
    [InlineData(new[] { "--dbcheck" }, false)]
    [InlineData(new[] { "run", "dbcheck" }, false)]
    public void IsDbCheckArgs_DetectsTheCommand(string[] args, bool expected)
    {
        Assert.Equal(expected, DatabaseCheckCommand.IsDbCheckArgs(args));
    }

    [Theory]
    [InlineData("2026-12-01T10:00:00Z", "2026-12-01T10:00:00.0000000+00:00")]
    [InlineData("2026-12-01T10:00:00.5Z", "2026-12-01T10:00:00.5000000+00:00")]
    [InlineData("2026-12-01T05:00:00-05:00", "2026-12-01T10:00:00.0000000+00:00")]
    public void ParseCutoff_TakesIso8601WithAnOffset_AsUtc(string value, string expected)
    {
        var cutoff = DatabaseCheckCommand.ParseCutoff(["dbcheck", "--cutoff", value]);
        Assert.NotNull(cutoff);
        Assert.Equal(TimeSpan.Zero, cutoff.Value.Offset);
        Assert.Equal(expected, cutoff.Value.ToString("o"));
    }

    [Theory]
    [InlineData("dbcheck")]
    [InlineData("dbcheck|--cutoff")]
    [InlineData("dbcheck|--cutoff|2026-12-01 10:00:00")]
    [InlineData("dbcheck|--cutoff|2026-12-01T10:00:00")]
    [InlineData("dbcheck|--cutoff|yesterday")]
    [InlineData("dbcheck|--cutoff|2026-13-01T10:00:00Z")]
    [InlineData("dbcheck|--since|2026-12-01T10:00:00Z")]
    [InlineData("dbcheck|--cutoff|2026-12-01T10:00:00Z|--write")]
    public async Task BadArguments_AreRefused_BeforeAnyConnection(string joined)
    {
        var args = joined.Split('|');
        using var output = new StringWriter();
        var code = await DatabaseCheckCommand.RunAsync(args, Env(("DB_HOST", MarkerHost), ("DB_PASSWORD", MarkerPassword)), output);

        Assert.Equal(DatabaseCheckCommand.ExitRefused, code);
        Assert.StartsWith("dbcheck: refused: usage: dbcheck --cutoff", output.ToString(), StringComparison.Ordinal);
        AssertNothingSensitive(output.ToString());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task WithoutPostgres_IsRefused_BeforeAnyConnection(string environment)
    {
        using var output = new StringWriter();
        var code = await DatabaseCheckCommand.RunAsync(
            ["dbcheck", "--cutoff", "2026-12-01T10:00:00Z"], Env(("ASPNETCORE_ENVIRONMENT", environment)), output);

        Assert.Equal(DatabaseCheckCommand.ExitRefused, code);
        Assert.StartsWith("dbcheck: refused:", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADatabaseThatCannotBeReached_FailsInOneLine_WithNoHostOrPassword()
    {
        // Outside Development: refused when the root certificate is missing (exit 2), or, where
        // the test host provides one, a failed connection (exit 1). Either way one line, no values.
        using var output = new StringWriter();
        var code = await DatabaseCheckCommand.RunAsync(
            ["dbcheck", "--cutoff", "2026-12-01T10:00:00Z"],
            Env(("ASPNETCORE_ENVIRONMENT", "Production"), ("DB_HOST", MarkerHost), ("DB_NAME", "dealoware"),
                ("DB_USERNAME", "dealoware_app"), ("DB_PASSWORD", MarkerPassword)),
            output);

        Assert.Contains(code, new[] { DatabaseCheckCommand.ExitFailed, DatabaseCheckCommand.ExitRefused });
        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Single(lines);
        Assert.Matches(@"^dbcheck: (failed \(|refused: )", lines[0]);
        AssertNothingSensitive(output.ToString());
    }

    [Fact]
    public void EveryModelTable_HasItsCreationColumnListed_AndEachListedColumnIsATimeOfTheModel()
    {
        using var db = new DealowareDbContext(
            new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql("Host=unused").Options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var tables = model.GetEntityTypes()
            .Where(t => t.GetTableName() is not null)
            .GroupBy(t => t.GetTableName()!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var missing = tables.Keys.Where(t => !DatabaseCheck.CreatedColumns.ContainsKey(t)).ToList();
        Assert.True(missing.Count == 0,
            "Put each new table in DatabaseCheck.CreatedColumns, with its creation column or null: " + string.Join(", ", missing));
        var notInModel = DatabaseCheck.CreatedColumns.Keys.Where(t => !tables.ContainsKey(t)).ToList();
        Assert.True(notInModel.Count == 0, "A listed table is not in the model: " + string.Join(", ", notInModel));

        foreach (var (table, column) in DatabaseCheck.CreatedColumns)
        {
            if (column is null)
                continue;
            var property = tables[table]
                .SelectMany(t => t.GetProperties())
                .FirstOrDefault(p => string.Equals(p.GetColumnName(), column, StringComparison.Ordinal));
            Assert.True(property is not null, $"{table}.{column} is not a column of the model");
            Assert.True(property!.ClrType == typeof(DateTimeOffset), $"{table}.{column} must be a non-null DateTimeOffset");
        }

        foreach (var (table, columns) in DatabaseCheck.ChangedColumns)
        {
            Assert.True(tables.ContainsKey(table), $"DatabaseCheck.ChangedColumns names {table}, which is not in the model");
            Assert.NotEmpty(columns);
            foreach (var column in columns)
            {
                var property = tables[table]
                    .SelectMany(t => t.GetProperties())
                    .FirstOrDefault(p => string.Equals(p.GetColumnName(), column, StringComparison.Ordinal));
                Assert.True(property is not null, $"{table}.{column} is not a column of the model");
                Assert.True(property!.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?),
                    $"{table}.{column} must be a DateTimeOffset");
            }
        }

        // A table with no creation time is cut through its artifact, or is one of the few that
        // cannot be cut (their counts get the weaker check). A new table forces a choice here.
        var uncuttable = DatabaseCheck.CreatedColumns.Where(kv => kv.Value is null && !DatabaseCheck.ParentCuts.ContainsKey(kv.Key))
            .Select(kv => kv.Key).ToList();
        Assert.Equal(["AdminAuthLockouts", "AdminCredentials", "AdminResetIpCounters", "AdminSessionStepUps"], uncuttable);

        // Each parent-cut step is a foreign key of the model, from that column to that table.
        foreach (var (table, path) in DatabaseCheck.ParentCuts)
        {
            Assert.Null(DatabaseCheck.CreatedColumns[table]);
            Assert.Equal(DatabaseCheck.ParentTable, path[^1].Table);
            var from = table;
            foreach (var (column, to) in path)
            {
                var fk = tables[from].SelectMany(t => t.GetForeignKeys())
                    .FirstOrDefault(f => f.Properties.Count == 1 && f.Properties[0].GetColumnName() == column
                                         && f.PrincipalEntityType.GetTableName() == to);
                Assert.True(fk is not null, $"{from}.{column} is not a foreign key to {to} in the model");
                from = to;
            }
        }

        // Each table with an UpdatedAt or DeletedAt column lists them as changed columns.
        foreach (var (table, types) in tables)
        {
            foreach (var column in types.SelectMany(t => t.GetProperties()).Select(p => p.GetColumnName())
                         .Where(c => c is "UpdatedAt" or "DeletedAt"))
            {
                Assert.True(DatabaseCheck.ChangedColumns.TryGetValue(table, out var listed) && listed.Contains(column),
                    $"{table}.{column} must be in DatabaseCheck.ChangedColumns");
            }
        }
    }

    [Fact]
    public void EveryTableTheEngineDeletesRowsFrom_IsInTheHardDeleteList()
    {
        // A tripwire on the source: each statement that deletes rows (ExecuteDelete, Remove,
        // RemoveRange) is matched to the DbSets it names. A new delete on another table fails
        // here until that table is added to DatabaseCheck.HardDeleteTables (the restore test
        // allows its count to drop on the live database).
        using var db = new DealowareDbContext(
            new DbContextOptionsBuilder<DealowareDbContext>().UseNpgsql("Host=unused").Options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var tableOfSet = typeof(DealowareDbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToDictionary(p => p.Name, p => model.FindEntityType(p.PropertyType.GetGenericArguments()[0])!.GetTableName()!,
                StringComparer.Ordinal);

        var deleting = new Regex(@"ExecuteDelete(Async)?\s*\(|\.Remove\s*\(|\.RemoveRange\s*\(", RegexOptions.CultureInvariant);
        var memberName = new Regex(@"\.(\w+)\b", RegexOptions.CultureInvariant);
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var src = Path.Combine(RepoRoot(), "src");
        foreach (var file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 // dbroles --verify adds and removes a probe row in a transaction it rolls back.
                                 && Path.GetFileName(f) != "DatabaseRoles.cs"))
        {
            foreach (var statement in File.ReadAllText(file).Split(';').Where(s => deleting.IsMatch(s)))
            {
                foreach (Match m in memberName.Matches(statement))
                {
                    if (tableOfSet.TryGetValue(m.Groups[1].Value, out var table))
                        found.Add(table);
                }
            }
        }

        Assert.NotEmpty(found);
        // A delete also removes the rows of tables whose foreign key to it cascades.
        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var type in model.GetEntityTypes().Where(t => t.GetTableName() is not null))
            {
                if (!found.Contains(type.GetTableName()!) &&
                    type.GetForeignKeys().Any(f => f.DeleteBehavior == DeleteBehavior.Cascade && found.Contains(f.PrincipalEntityType.GetTableName()!)))
                {
                    found.Add(type.GetTableName()!);
                    grew = true;
                }
            }
        }

        var notListed = found.Except(DatabaseCheck.HardDeleteTables).ToList();
        Assert.True(notListed.Count == 0, "Rows are deleted from these tables; add them to DatabaseCheck.HardDeleteTables: " + string.Join(", ", notListed));
        var notFound = DatabaseCheck.HardDeleteTables.Except(found).ToList();
        Assert.True(notFound.Count == 0, "Listed in DatabaseCheck.HardDeleteTables, but no delete was found in the source: " + string.Join(", ", notFound));
    }

    [Fact]
    public void Json_IsOneLine_WithCountsHashesAndNamesOnly()
    {
        var result = new DatabaseCheckResult(
            new DateTimeOffset(2026, 12, 1, 5, 0, 0, TimeSpan.FromHours(-5)),
            "20261126000600_AdminDeleteConfirmTokens",
            [
                new DatabaseCheckTable("AdminCredentials", null, ["PasswordUpdatedAt"], false, null, 1, []),
                new DatabaseCheckTable("ArtifactValues", "Artifacts.CreatedAt", ["Artifacts.UpdatedAt", "Artifacts.DeletedAt"], false, 3, 1, []),
                new DatabaseCheckTable("Participants", "CreatedAt", ["UpdatedAt", "DeletedAt"], false, 2, 0,
                    [new DatabaseCheckRecord(new string('a', 64), new string('b', 64))]),
            ]);

        var json = result.ToJson();

        Assert.DoesNotContain('\n', json);
        Assert.Equal(
            "{\"check\":\"dbcheck\",\"cutoff\":\"2026-12-01T10:00:00.0000000Z\",\"migration\":\"20261126000600_AdminDeleteConfirmTokens\"," +
            "\"tables\":[{\"table\":\"AdminCredentials\",\"createdColumn\":null,\"changedColumns\":[\"PasswordUpdatedAt\"],\"hardDeletes\":false,\"rows\":null,\"uncutRows\":1,\"known\":[]}," +
            "{\"table\":\"ArtifactValues\",\"createdColumn\":\"Artifacts.CreatedAt\",\"changedColumns\":[\"Artifacts.UpdatedAt\",\"Artifacts.DeletedAt\"],\"hardDeletes\":false,\"rows\":3,\"uncutRows\":1,\"known\":[]}," +
            "{\"table\":\"Participants\",\"createdColumn\":\"CreatedAt\",\"changedColumns\":[\"UpdatedAt\",\"DeletedAt\"],\"hardDeletes\":false,\"rows\":2,\"uncutRows\":0," +
            "\"known\":[{\"keySha256\":\"" + new string('a', 64) + "\",\"rowSha256\":\"" + new string('b', 64) + "\"}]}]}",
            json);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(3, parsed.RootElement.GetProperty("tables").GetArrayLength());
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }

    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
        return name => map.TryGetValue(name, out var v) ? v : null;
    }

    private static void AssertNothingSensitive(string output)
    {
        foreach (var value in new[] { MarkerHost, MarkerPassword, "Host=", "Password=" })
            Assert.DoesNotContain(value, output, StringComparison.OrdinalIgnoreCase);
    }
}

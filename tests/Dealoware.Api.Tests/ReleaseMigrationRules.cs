using System.Diagnostics;
using System.Text.RegularExpressions;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Dealoware.Api.Tests;

/// <summary>
/// The database-change check of a release (T09 spec §2.2): between two releases the
/// database only gains. The previous release must keep working on the new schema, because
/// a rollback redeploys the previous image and the schema is never rolled back.
/// <para>
/// Every migration added since the last <c>release-*</c> tag is read as EF's own list of
/// operations, for both providers, and refused when it:
/// </para>
/// <list type="bullet">
/// <item>drops a table, a column, a key, a constraint, an index, a sequence or a schema;</item>
/// <item>renames a table or a column;</item>
/// <item>changes a column's type;</item>
/// <item>makes an existing column required, with or without a default (and adds a new
/// required column without a default to a table the previous release writes);</item>
/// <item>narrows a column (a shorter length, a smaller precision or scale), or adds a check
/// constraint, a unique index, a unique constraint or a foreign key to a table the previous
/// release writes;</item>
/// <item>runs raw SQL with <c>DROP</c>, <c>RENAME</c>, <c>ALTER ... TYPE</c>,
/// <c>SET NOT NULL</c>, <c>ADD CONSTRAINT</c>, <c>TRUNCATE</c> or <c>DELETE</c>.</item>
/// </list>
/// A table created in this release, and not in the last release's model, is the new code's
/// alone, so changes to it are not counted. A table dropped and made again under the same
/// name is not new. A migration whose name ends in <see cref="ContractSuffix"/> passes only if a
/// comment in it names the release in which the code stopped using what it removes
/// (<see cref="ContractComment"/>), and that release is tagged. A migration that was in the
/// last release must not change at all.
/// </summary>
public static partial class ReleaseMigrationRules
{
    public const string ContractSuffix = "_Contract";

    public const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";
    public const string PostgresProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public static readonly IReadOnlyList<string> Providers = [SqliteProvider, PostgresProvider];

    /// <summary>The repository path of the migrations, forward slashes.</summary>
    public const string MigrationsFolder = "src/Dealoware.Infrastructure/Persistence/Migrations";

    /// <summary>
    /// The comment a <c>_Contract</c> migration carries, for example
    /// <c>// Code stopped using this in release-12</c>.
    /// </summary>
    [GeneratedRegex(@"//\s*Code stopped using this in (release-\d+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex ContractComment();

    [GeneratedRegex(@"\bDROP\b|\bRENAME\b|\bALTER\b[\s\S]*?\bTYPE\b|\bSET\s+NOT\s+NULL\b|\bADD\s+CONSTRAINT\b|\bTRUNCATE\b|\bDELETE\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RiskySql();

    /// <summary>A migration file name: <c>&lt;14-digit time&gt;_&lt;Name&gt;.cs</c>, or its Designer.</summary>
    [GeneratedRegex(@"^\d{14}_\w+?(\.Designer)?\.cs$", RegexOptions.CultureInvariant)]
    public static partial Regex MigrationFileName();

    public sealed record Finding(string MigrationId, string Rule, string Detail)
    {
        public override string ToString() => $"{MigrationId}: {Rule}: {Detail}";
    }

    /// <summary>A migration of this release: its ID, its source text, and its operations per provider.</summary>
    public sealed record ReleaseMigration(string Id, string Source, IReadOnlyList<IReadOnlyList<MigrationOperation>> OperationsByProvider);

    public static ReleaseMigration Read(string id, string source, Func<string, Migration> create)
        => new(id, source, Providers.Select(p => (IReadOnlyList<MigrationOperation>)create(p).UpOperations.ToList()).ToList());

    /// <summary>The tables a set of migrations creates or renames into (by name, all providers).</summary>
    public static IReadOnlySet<string> TablesCreatedBy(IEnumerable<ReleaseMigration> migrations)
        => migrations
            .SelectMany(m => m.OperationsByProvider.SelectMany(ops => ops))
            .SelectMany(op => op switch
            {
                CreateTableOperation t => new[] { TableKey(t.Schema, t.Name) },
                RenameTableOperation r => new[] { TableKey(r.NewSchema ?? r.Schema, r.NewName ?? r.Name) },
                _ => Array.Empty<string>()
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The findings for this release's migrations. Empty means the release only adds.
    /// <paramref name="releasedTables"/> are the tables of the last release's model (see
    /// <see cref="TablesCreatedBy"/> over the released migrations): a table counts as new only
    /// if it is not one of them, so a table dropped and made again is not new.
    /// </summary>
    public static IReadOnlyList<Finding> Check(
        IReadOnlyList<ReleaseMigration> added,
        IReadOnlySet<string> taggedReleases,
        IReadOnlySet<string>? releasedTables = null)
    {
        var newTables = TablesCreatedBy(added)
            .Where(t => releasedTables is null || !releasedTables.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var findings = new List<Finding>();
        foreach (var migration in added)
        {
            var own = new List<Finding>();
            foreach (var ops in migration.OperationsByProvider)
            {
                foreach (var op in ops)
                {
                    foreach (var (rule, detail) in Judge(op, newTables))
                    {
                        var f = new Finding(migration.Id, rule, detail);
                        if (!own.Contains(f))
                        {
                            own.Add(f);
                        }
                    }
                }
            }

            if (!migration.Id.EndsWith(ContractSuffix, StringComparison.Ordinal))
            {
                findings.AddRange(own);
                continue;
            }

            // A contract migration: allowed to remove, once the code that used it is gone.
            var named = ContractComment().Match(migration.Source);
            if (!named.Success)
            {
                findings.Add(new Finding(migration.Id, "contract",
                    "a _Contract migration needs a comment '// Code stopped using this in release-<n>'"));
            }
            else if (!taggedReleases.Contains(named.Groups[1].Value.ToLowerInvariant()))
            {
                findings.Add(new Finding(migration.Id, "contract",
                    $"the release it names, {named.Groups[1].Value}, is not tagged"));
            }
        }

        return findings;
    }

    private static IEnumerable<(string Rule, string Detail)> Judge(MigrationOperation op, IReadOnlySet<string> newTables)
    {
        bool IsNew(string? schema, string? table) => table is not null && newTables.Contains(TableKey(schema, table));

        switch (op)
        {
            case SqlOperation sql:
                if (RiskySql().Match(sql.Sql) is { Success: true } m)
                {
                    yield return ("raw SQL", $"'{m.Value.ToUpperInvariant()}' in raw SQL");
                }

                yield break;

            case RenameTableOperation r when !IsNew(r.Schema, r.Name):
                yield return ("rename", $"table {r.Name} renamed to {r.NewName}");
                yield break;

            case RenameColumnOperation r when !IsNew(r.Schema, r.Table):
                yield return ("rename", $"column {r.Table}.{r.Name} renamed to {r.NewName}");
                yield break;

            case AlterColumnOperation a when !IsNew(a.Schema, a.Table):
                foreach (var finding in JudgeAlter(a))
                {
                    yield return finding;
                }

                yield break;

            case AddColumnOperation c when !IsNew(c.Schema, c.Table)
                                           && !c.IsNullable
                                           && c.DefaultValue is null
                                           && c.DefaultValueSql is null
                                           && c.ComputedColumnSql is null:
                yield return ("required without default", $"new column {c.Table}.{c.Name} is required and has no default");
                yield break;

            case AddCheckConstraintOperation k when !IsNew(k.Schema, k.Table):
                yield return ("narrowed", $"check constraint {k.Name} added to {k.Table}");
                yield break;

            case CreateIndexOperation i when i.IsUnique && !IsNew(i.Schema, i.Table):
                yield return ("narrowed", $"unique index {i.Name} added to {i.Table}");
                yield break;

            case AddUniqueConstraintOperation u when !IsNew(u.Schema, u.Table):
                yield return ("narrowed", $"unique constraint {u.Name} added to {u.Table}");
                yield break;

            case AddForeignKeyOperation f when !IsNew(f.Schema, f.Table):
                yield return ("narrowed", $"foreign key {f.Name} added to {f.Table}");
                yield break;
        }

        if (op.GetType().Name.StartsWith("Drop", StringComparison.Ordinal) && !IsNew(SchemaOf(op), TableOf(op)))
        {
            yield return ("drop", Describe(op));
        }
    }

    private static IEnumerable<(string Rule, string Detail)> JudgeAlter(AlterColumnOperation a)
    {
        var old = a.OldColumn;
        var column = $"{a.Table}.{a.Name}";
        var clrChanged = old.ClrType is not null && a.ClrType is not null && old.ClrType != a.ClrType;
        var storeChanged = old.ColumnType is not null && a.ColumnType is not null
                           && !string.Equals(old.ColumnType, a.ColumnType, StringComparison.OrdinalIgnoreCase);
        if (clrChanged || storeChanged)
        {
            yield return ("type change", $"column {column} changes type");
        }

        // Even with a default: the previous release may still write NULL into it.
        if (old.IsNullable && !a.IsNullable)
        {
            yield return ("made required", $"column {column} becomes required");
        }

        if (a.MaxLength is { } len && (old.MaxLength is null || len < old.MaxLength))
        {
            yield return ("narrowed", $"column {column} gets a shorter length");
        }

        if ((a.Precision is { } p && (old.Precision is null || p < old.Precision))
            || (a.Scale is { } s && old.Scale is { } os && s < os))
        {
            yield return ("narrowed", $"column {column} gets a smaller precision or scale");
        }
    }

    private static string TableKey(string? schema, string table) => $"{schema ?? string.Empty}.{table}";

    private static string? TableOf(MigrationOperation op)
        => op switch
        {
            DropTableOperation t => t.Name,
            ITableMigrationOperation t => t.Table,
            _ => null
        };

    private static string? SchemaOf(MigrationOperation op)
        => op switch
        {
            DropTableOperation t => t.Schema,
            ITableMigrationOperation t => t.Schema,
            _ => null
        };

    private static string Describe(MigrationOperation op)
    {
        var kind = op.GetType().Name.Replace("Operation", string.Empty, StringComparison.Ordinal);
        return op switch
        {
            DropTableOperation t => $"{kind} {t.Name}",
            DropColumnOperation c => $"{kind} {c.Table}.{c.Name}",
            DropIndexOperation i => $"{kind} {i.Name}",
            ITableMigrationOperation t => $"{kind} on {t.Table}",
            _ => kind
        };
    }

    // ---- What changed since the last release: read from git -------------------------------

    /// <summary>What git says about the migrations since the last release tag.</summary>
    public sealed record History(
        string? LastRelease,
        IReadOnlySet<string> TaggedReleases,
        IReadOnlyList<string> AddedIds,
        IReadOnlyList<string> ReleasedIds,
        IReadOnlyList<Finding> ReleasedChanged);

    /// <summary>
    /// The repository's history, or null and the reason when it cannot be read (no git, not a
    /// checkout, a shallow clone without the tags).
    /// </summary>
    public static (History? History, string? WhyNot) ReadHistory(string repoRoot, IReadOnlyCollection<string> migrationIds)
    {
        var shallow = Git(repoRoot, "rev-parse", "--is-shallow-repository");
        if (shallow is null)
        {
            return (null, "git is not available here, or this is not a git checkout");
        }

        if (shallow.Trim() == "true")
        {
            return (null, "this is a shallow clone: the release tags and their history are not here");
        }

        var tagText = Git(repoRoot, "tag", "--list", "release-*", "--merged", "HEAD");
        if (tagText is null)
        {
            return (null, "git tag failed");
        }

        var tags = tagText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => ReleaseTag().IsMatch(t))
            .Select(t => t.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        var last = tags.OrderByDescending(t => long.Parse(t["release-".Length..])).FirstOrDefault();
        var onDisk = MigrationFilesOnDisk(repoRoot);

        if (last is null)
        {
            // No release yet: no previous release has to keep working.
            return (new History(null, tags, [], [], []), null);
        }

        var atTagText = Git(repoRoot, "ls-tree", "-r", "--name-only", last, "--", MigrationsFolder + "/");
        if (atTagText is null)
        {
            return (null, $"git ls-tree {last} failed");
        }

        var atTag = atTagText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p[(p.LastIndexOf('/') + 1)..])
            .Where(n => MigrationFileName().IsMatch(n))
            .ToHashSet(StringComparer.Ordinal);

        var changed = new List<Finding>();
        foreach (var file in atTag.Order(StringComparer.Ordinal))
        {
            var id = IdOf(file);
            if (!onDisk.Contains(file))
            {
                changed.Add(new Finding(id, "released migration removed", $"{file} was in {last}"));
                continue;
            }

            var then = Git(repoRoot, "show", $"{last}:{MigrationsFolder}/{file}");
            var now = File.ReadAllText(Path.Combine(repoRoot, MigrationsFolder, file));
            if (then is null || Normalize(then) != Normalize(now))
            {
                changed.Add(new Finding(id, "released migration changed", $"{file} differs from {last}"));
            }
        }

        var added = migrationIds
            .Where(id => !atTag.Contains(id + ".cs"))
            .Order(StringComparer.Ordinal)
            .ToList();
        var released = migrationIds
            .Where(id => atTag.Contains(id + ".cs"))
            .Order(StringComparer.Ordinal)
            .ToList();
        return (new History(last, tags, added, released, changed), null);
    }

    public static IReadOnlySet<string> MigrationFilesOnDisk(string repoRoot)
        => Directory.EnumerateFiles(Path.Combine(repoRoot, MigrationsFolder))
            .Select(Path.GetFileName)
            .Where(n => n is not null && MigrationFileName().IsMatch(n))
            .Select(n => n!)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Every migration EF knows, by ID, with a factory for a given provider.</summary>
    public static IReadOnlyDictionary<string, Func<string, Migration>> MigrationsOfTheApp()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new DealowareDbContext(options);
        var assembly = db.GetService<IMigrationsAssembly>();
        return assembly.Migrations.ToDictionary(
            kv => kv.Key,
            kv => (Func<string, Migration>)(provider => assembly.CreateMigration(kv.Value, provider)),
            StringComparer.Ordinal);
    }

    public static string? FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln"))
                && Directory.Exists(Path.Combine(dir.FullName, MigrationsFolder)))
            {
                return dir.FullName;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^release-\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseTag();

    private static string IdOf(string file)
        => file.EndsWith(".Designer.cs", StringComparison.Ordinal) ? file[..^".Designer.cs".Length] : file[..^".cs".Length];

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string? Git(string repoRoot, params string[] args)
    {
        try
        {
            var start = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // A migration's text is UTF-8 (comments carry dashes and quotes); the console's
                // own code page would change those characters and make a file look changed.
                StandardOutputEncoding = new System.Text.UTF8Encoding(false),
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            };
            start.ArgumentList.Add("-C");
            start.ArgumentList.Add(repoRoot);
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("core.autocrlf=false");
            foreach (var a in args)
            {
                start.ArgumentList.Add(a);
            }

            using var git = Process.Start(start);
            if (git is null)
            {
                return null;
            }

            var errTask = git.StandardError.ReadToEndAsync();
            var output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            _ = errTask.Result;
            return git.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}

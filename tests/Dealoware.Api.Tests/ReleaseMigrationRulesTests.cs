using Microsoft.EntityFrameworkCore.Migrations;
using Xunit.Abstractions;

namespace Dealoware.Api.Tests;

/// <summary>
/// The database-change check of a release (T09 spec §2.2, <see cref="ReleaseMigrationRules"/>).
/// <para>
/// <see cref="TheMigrationsSinceTheLastRelease_OnlyAdd"/> is the check itself: it reads the
/// migrations added since the last <c>release-*</c> tag from git. It needs the full history and
/// the tags. CI's "Database changes" job runs it with them and with
/// <c>DEALOWARE_RELEASE_MIGRATION_CHECK=required</c>, where a missing history is a failure.
/// Elsewhere (the shallow clone of the main test job) it says why it could not look, and passes.
/// </para>
/// The other tests show each rule on migrations written for the test.
/// </summary>
public class ReleaseMigrationRulesTests
{
    public const string RequiredVariable = "DEALOWARE_RELEASE_MIGRATION_CHECK";

    private static readonly IReadOnlySet<string> NoReleases = new HashSet<string>();

    private readonly ITestOutputHelper _output;

    public ReleaseMigrationRulesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ---- The check on this repository ----------------------------------------------------

    [Fact]
    public void TheMigrationsSinceTheLastRelease_OnlyAdd()
    {
        var required = string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "required", StringComparison.OrdinalIgnoreCase);
        var root = ReleaseMigrationRules.FindRepoRoot();
        if (root is null)
        {
            Assert.False(required, "The repository was not found above the test's folder.");
            _output.WriteLine("Not checked: the repository was not found above the test's folder.");
            return;
        }

        var migrations = ReleaseMigrationRules.MigrationsOfTheApp();
        var (history, whyNot) = ReleaseMigrationRules.ReadHistory(root, migrations.Keys.ToList());
        if (history is null)
        {
            Assert.False(required, $"Not checked: {whyNot}. The CI job must check out the full history with its tags.");
            _output.WriteLine($"Not checked: {whyNot}. CI's 'Database changes' job checks it.");
            return;
        }

        if (history.LastRelease is null)
        {
            _output.WriteLine("No release-* tag yet: there is no previous release that has to keep working. Nothing to check.");
            return;
        }

        var added = history.AddedIds
            .Select(id => ReleaseMigrationRules.Read(id, SourceOf(root, id), migrations[id]))
            .ToList();
        var findings = history.ReleasedChanged.Concat(ReleaseMigrationRules.Check(added, history.TaggedReleases)).ToList();

        _output.WriteLine($"Last release: {history.LastRelease}. Migrations added since: {(added.Count == 0 ? "none" : string.Join(", ", history.AddedIds))}.");
        Assert.True(
            findings.Count == 0,
            $"Since {history.LastRelease}, the database must only gain (T09 spec §2.2):\n  " + string.Join("\n  ", findings)
            + "\nA removal goes in a later migration named *_Contract, with the comment '// Code stopped using this in release-<n>'.");
    }

    [Fact]
    public void EveryMigration_IsTheFileOfTheSameName()
    {
        // The check finds a migration's file by its ID; a migration in another file would not be read.
        var root = ReleaseMigrationRules.FindRepoRoot();
        if (root is null)
        {
            _output.WriteLine("Not checked: the repository was not found above the test's folder.");
            return;
        }

        var onDisk = ReleaseMigrationRules.MigrationFilesOnDisk(root);
        var ids = ReleaseMigrationRules.MigrationsOfTheApp().Keys.ToList();
        Assert.NotEmpty(ids);
        foreach (var id in ids)
        {
            Assert.Contains(id + ".cs", onDisk);
        }
    }

    // ---- Each rule ------------------------------------------------------------------------

    [Fact]
    public void ATestMigrationThatDropsAColumn_IsRefused()
    {
        var findings = Check(new DropsAColumn());

        var f = Assert.Single(findings);
        Assert.Equal("drop", f.Rule);
        Assert.Contains("Participants.DisplayName", f.Detail);
    }

    [Theory]
    [InlineData(typeof(DropsATable), "drop")]
    [InlineData(typeof(DropsAnIndex), "drop")]
    [InlineData(typeof(DropsAForeignKey), "drop")]
    [InlineData(typeof(RenamesAColumn), "rename")]
    [InlineData(typeof(RenamesATable), "rename")]
    [InlineData(typeof(ChangesAType), "type change")]
    [InlineData(typeof(MakesAColumnRequired), "required without default")]
    [InlineData(typeof(AddsARequiredColumnWithNoDefault), "required without default")]
    [InlineData(typeof(ShortensAColumn), "narrowed")]
    [InlineData(typeof(AddsACheckToAnOldTable), "narrowed")]
    [InlineData(typeof(RawSqlDrop), "raw SQL")]
    [InlineData(typeof(RawSqlRename), "raw SQL")]
    [InlineData(typeof(RawSqlAlterType), "raw SQL")]
    public void EachKindOfRemoval_IsRefused(Type migration, string rule)
    {
        var findings = Check((Migration)Activator.CreateInstance(migration)!);

        Assert.Contains(findings, f => f.Rule == rule);
    }

    [Theory]
    [InlineData(typeof(AddsATable))]
    [InlineData(typeof(AddsANullableColumn))]
    [InlineData(typeof(AddsARequiredColumnWithADefault))]
    [InlineData(typeof(AddsAnIndex))]
    [InlineData(typeof(MakesAColumnRequiredWithADefault))]
    [InlineData(typeof(LengthensAColumn))]
    [InlineData(typeof(RawSqlThatOnlyAdds))]
    public void AnAddition_Passes(Type migration)
    {
        var findings = Check((Migration)Activator.CreateInstance(migration)!);

        Assert.Empty(findings);
    }

    [Fact]
    public void ATableMadeInTheSameRelease_MayStillChange()
    {
        // The previous release never saw it.
        var findings = ReleaseMigrationRules.Check(
            [Read("20990101000000_AddsATable", new AddsATable()), Read("20990101000001_ChangesTheNewTable", new ChangesTheNewTable())],
            NoReleases);

        Assert.Empty(findings);
    }

    [Fact]
    public void AContractMigration_WithTheTaggedRelease_Passes()
    {
        var source = "// Code stopped using this in release-3\n";
        var findings = ReleaseMigrationRules.Check(
            [ReleaseMigrationRules.Read("20990101000000_DropOldName_Contract", source, p => WithProvider(new DropsAColumn(), p))],
            new HashSet<string> { "release-3", "release-4" });

        Assert.Empty(findings);
    }

    [Fact]
    public void AContractMigration_WithoutTheComment_IsRefused()
    {
        var findings = ReleaseMigrationRules.Check(
            [ReleaseMigrationRules.Read("20990101000000_DropOldName_Contract", "// no release named\n", p => WithProvider(new DropsAColumn(), p))],
            new HashSet<string> { "release-3" });

        Assert.Equal("contract", Assert.Single(findings).Rule);
    }

    [Fact]
    public void AContractMigration_NamingAReleaseThatIsNotTagged_IsRefused()
    {
        var findings = ReleaseMigrationRules.Check(
            [ReleaseMigrationRules.Read("20990101000000_DropOldName_Contract", "// Code stopped using this in release-9\n", p => WithProvider(new DropsAColumn(), p))],
            new HashSet<string> { "release-3" });

        var f = Assert.Single(findings);
        Assert.Equal("contract", f.Rule);
        Assert.Contains("release-9", f.Detail);
    }

    [Fact]
    public void TheContractComment_DoesNotExcuseAMigrationWithAnotherName()
    {
        var findings = ReleaseMigrationRules.Check(
            [ReleaseMigrationRules.Read("20990101000000_DropOldName", "// Code stopped using this in release-3\n", p => WithProvider(new DropsAColumn(), p))],
            new HashSet<string> { "release-3" });

        Assert.Equal("drop", Assert.Single(findings).Rule);
    }

    [Fact]
    public void AProviderOnlyRemoval_IsFound()
    {
        // Raw SQL only on PostgreSQL: each provider's operations are read.
        var findings = Check(new DropsOnPostgresOnly());

        Assert.Equal("raw SQL", Assert.Single(findings).Rule);
    }

    // ---- helpers --------------------------------------------------------------------------

    private static IReadOnlyList<ReleaseMigrationRules.Finding> Check(Migration migration)
        => ReleaseMigrationRules.Check([Read("20990101000000_" + migration.GetType().Name, migration)], NoReleases);

    private static ReleaseMigrationRules.ReleaseMigration Read(string id, Migration migration)
        => ReleaseMigrationRules.Read(id, string.Empty, p => WithProvider((Migration)Activator.CreateInstance(migration.GetType())!, p));

    private static Migration WithProvider(Migration migration, string provider)
    {
        migration.ActiveProvider = provider;
        return migration;
    }

    private static string SourceOf(string root, string id)
        => File.ReadAllText(Path.Combine(root, ReleaseMigrationRules.MigrationsFolder, id + ".cs"));

    // ---- Migrations written for the test. Not in the app: no [Migration] or [DbContext]. ----

    private sealed class DropsAColumn : Migration
    {
        protected override void Up(MigrationBuilder b) => b.DropColumn(name: "DisplayName", table: "Participants");
    }

    private sealed class DropsATable : Migration
    {
        protected override void Up(MigrationBuilder b) => b.DropTable(name: "Strategies");
    }

    private sealed class DropsAnIndex : Migration
    {
        protected override void Up(MigrationBuilder b) => b.DropIndex(name: "IX_Offers_NegotiationId", table: "Offers");
    }

    private sealed class DropsAForeignKey : Migration
    {
        protected override void Up(MigrationBuilder b) => b.DropForeignKey(name: "FK_Offers_Negotiations", table: "Offers");
    }

    private sealed class RenamesAColumn : Migration
    {
        protected override void Up(MigrationBuilder b) => b.RenameColumn(name: "Terms", table: "Offers", newName: "Conditions");
    }

    private sealed class RenamesATable : Migration
    {
        protected override void Up(MigrationBuilder b) => b.RenameTable(name: "Offers", newName: "Bids");
    }

    private sealed class ChangesAType : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AlterColumn<long>(name: "Amount", table: "Offers", nullable: false, oldClrType: typeof(decimal), oldNullable: false);
    }

    private sealed class MakesAColumnRequired : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AlterColumn<string>(name: "Terms", table: "Offers", maxLength: 500, nullable: false,
                oldClrType: typeof(string), oldMaxLength: 500, oldNullable: true);
    }

    private sealed class MakesAColumnRequiredWithADefault : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AlterColumn<string>(name: "Terms", table: "Offers", maxLength: 500, nullable: false, defaultValue: "",
                oldClrType: typeof(string), oldMaxLength: 500, oldNullable: true);
    }

    private sealed class AddsARequiredColumnWithNoDefault : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AddColumn<int>(name: "Priority", table: "Offers", nullable: false);
    }

    private sealed class AddsARequiredColumnWithADefault : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AddColumn<int>(name: "Priority", table: "Offers", nullable: false, defaultValue: 0);
    }

    private sealed class AddsANullableColumn : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AddColumn<string>(name: "Note", table: "Offers", maxLength: 200, nullable: true);
    }

    private sealed class ShortensAColumn : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AlterColumn<string>(name: "Terms", table: "Offers", maxLength: 100, nullable: true,
                oldClrType: typeof(string), oldMaxLength: 500, oldNullable: true);
    }

    private sealed class LengthensAColumn : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AlterColumn<string>(name: "Terms", table: "Offers", maxLength: 1000, nullable: true,
                oldClrType: typeof(string), oldMaxLength: 500, oldNullable: true);
    }

    private sealed class AddsACheckToAnOldTable : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.AddCheckConstraint(name: "CK_Offers_Amount", table: "Offers", sql: "\"Amount\" > 0");
    }

    private sealed class AddsATable : Migration
    {
        protected override void Up(MigrationBuilder b)
        {
            b.CreateTable(
                name: "ReleaseNotes",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false),
                    Text = t.Column<string>(maxLength: 200, nullable: false)
                },
                constraints: t => t.PrimaryKey("PK_ReleaseNotes", x => x.Id));
            b.AddCheckConstraint(name: "CK_ReleaseNotes_Text", table: "ReleaseNotes", sql: "length(\"Text\") > 0");
        }
    }

    private sealed class ChangesTheNewTable : Migration
    {
        protected override void Up(MigrationBuilder b)
        {
            b.DropColumn(name: "Text", table: "ReleaseNotes");
            b.RenameTable(name: "ReleaseNotes", newName: "ReleaseNotesV2");
        }
    }

    private sealed class AddsAnIndex : Migration
    {
        protected override void Up(MigrationBuilder b)
            => b.CreateIndex(name: "IX_Offers_Currency", table: "Offers", column: "Currency");
    }

    private sealed class RawSqlDrop : Migration
    {
        protected override void Up(MigrationBuilder b) => b.Sql("drop view if exists offer_totals;");
    }

    private sealed class RawSqlRename : Migration
    {
        protected override void Up(MigrationBuilder b) => b.Sql("ALTER TABLE \"Offers\" RENAME COLUMN \"Terms\" TO \"Conditions\";");
    }

    private sealed class RawSqlAlterType : Migration
    {
        protected override void Up(MigrationBuilder b) => b.Sql("ALTER TABLE \"Offers\" ALTER COLUMN \"Amount\" TYPE bigint;");
    }

    private sealed class RawSqlThatOnlyAdds : Migration
    {
        protected override void Up(MigrationBuilder b) => b.Sql("CREATE INDEX IF NOT EXISTS ix_offers_sent ON \"Offers\" (\"SentAt\");");
    }

    private sealed class DropsOnPostgresOnly : Migration
    {
        protected override void Up(MigrationBuilder b)
        {
            if (ActiveProvider == ReleaseMigrationRules.PostgresProvider)
            {
                b.Sql("DROP TRIGGER IF EXISTS offers_touch ON \"Offers\";");
            }
        }
    }
}

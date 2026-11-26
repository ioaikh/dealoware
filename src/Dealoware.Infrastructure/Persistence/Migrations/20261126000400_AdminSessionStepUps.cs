using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// T04 S-A12: one step-up row per signed-in session (TD-ADM-174 "within
/// 5 minutes"), with the key of a re-enrolment started and not yet confirmed.
/// A new table only: no existing table or column changes, so every existing
/// row and every query of the previous release are untouched. Deleted with
/// its session (ON DELETE CASCADE). Provider-agnostic so SQLite tests and
/// PostgreSQL migrate both apply. Incremental after the frozen 15-table
/// stamp. Do not add this table to BaselineSchema / DatabaseMigrationBaseline
/// table lists.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration(MigrationId)]
public partial class AdminSessionStepUps : Migration
{
    public const string MigrationId = "20261126000400_AdminSessionStepUps";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminSessionStepUps",
            columns: table => new
            {
                SessionId = table.Column<Guid>(nullable: false),
                SteppedUpAt = table.Column<DateTimeOffset>(nullable: false),
                CodesIssuedAt = table.Column<DateTimeOffset>(nullable: true),
                PendingTotpSecretCipher = table.Column<string>(maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminSessionStepUps", x => x.SessionId);
                table.ForeignKey(
                    name: "FK_AdminSessionStepUps_AdminSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "AdminSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminSessionStepUps");
    }
}

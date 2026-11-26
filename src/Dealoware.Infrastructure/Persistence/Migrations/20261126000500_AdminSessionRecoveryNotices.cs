using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// T04 UXR-A40 (TD-ADM-UI-auth-16): one row per signed-in session that was
/// opened with a recovery code and whose banner the owner has not dismissed.
/// A new table only: no existing table or column changes, so every existing
/// row and every query of the previous release are untouched. Deleted with
/// its session (ON DELETE CASCADE). Provider-agnostic so SQLite tests and
/// PostgreSQL migrate both apply. Incremental after the frozen 15-table
/// stamp. Do not add this table to BaselineSchema / DatabaseMigrationBaseline
/// table lists.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration(MigrationId)]
public partial class AdminSessionRecoveryNotices : Migration
{
    public const string MigrationId = "20261126000500_AdminSessionRecoveryNotices";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminSessionRecoveryNotices",
            columns: table => new
            {
                SessionId = table.Column<Guid>(nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminSessionRecoveryNotices", x => x.SessionId);
                table.ForeignKey(
                    name: "FK_AdminSessionRecoveryNotices_AdminSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "AdminSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminSessionRecoveryNotices");
    }
}

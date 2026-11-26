using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// T06a, A7 Step 10 (Spec §9, SA §3.4): the confirm tokens of admin deletes,
/// stored as their SHA-256 only, one row per delete intent, deleted with the
/// session it belongs to (ON DELETE CASCADE). A new table only: no existing
/// table or column changes, so every existing row and every query of the
/// previous release are untouched. Provider-agnostic so SQLite tests and
/// PostgreSQL migrate both apply. Incremental after the frozen 15-table
/// stamp. Do not add this table to BaselineSchema / DatabaseMigrationBaseline
/// table lists.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration(MigrationId)]
public partial class AdminDeleteConfirmTokens : Migration
{
    public const string MigrationId = "20261126000600_AdminDeleteConfirmTokens";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminDeleteConfirmTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                SessionId = table.Column<Guid>(nullable: false),
                EntityType = table.Column<string>(maxLength: 32, nullable: false),
                EntityId = table.Column<Guid>(nullable: false),
                CascadeHash = table.Column<string>(maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminDeleteConfirmTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdminDeleteConfirmTokens_AdminSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "AdminSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminDeleteConfirmTokens_TokenHash",
            table: "AdminDeleteConfirmTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminDeleteConfirmTokens_SessionId",
            table: "AdminDeleteConfirmTokens",
            column: "SessionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminDeleteConfirmTokens");
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Confirm-token store for CoreOwner soft-delete. Single .cs file, no snapshot edit.
/// Provider-agnostic types (SQLite tests + PostgreSQL). Do not add this table to
/// PR #23's frozen 13-table or 15-table specs.
/// dealoware_app DML on this table only: SELECT, INSERT, UPDATE, DELETE
/// (default privileges for later migrate-owned tables). No extra grants.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000400_AdminDeleteConfirmTokens")]
public partial class AdminDeleteConfirmTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminDeleteConfirmTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                ActorEmail = table.Column<string>(maxLength: 256, nullable: false),
                ActorSessionId = table.Column<Guid>(nullable: false),
                EntityType = table.Column<string>(maxLength: 32, nullable: false),
                EntityId = table.Column<Guid>(nullable: false),
                CascadeSetHash = table.Column<string>(maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminDeleteConfirmTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminDeleteConfirmTokens_TokenHash",
            table: "AdminDeleteConfirmTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminDeleteConfirmTokens_ExpiresAt",
            table: "AdminDeleteConfirmTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_AdminDeleteConfirmTokens_EntityType_EntityId",
            table: "AdminDeleteConfirmTokens",
            columns: new[] { "EntityType", "EntityId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminDeleteConfirmTokens");
    }
}

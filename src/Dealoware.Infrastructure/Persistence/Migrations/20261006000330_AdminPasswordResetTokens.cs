using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Password-reset tokens (hashed, 1h, single-use) and CoreOwner credential hash.
/// Incremental after the frozen 15-table stamp. Do not add these tables to
/// BaselineSchema / DatabaseMigrationBaseline table lists.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000330_AdminPasswordResetTokens")]
public partial class AdminPasswordResetTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminPasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                TokenHash = table.Column<string>(maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminPasswordResetTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminPasswordResetTokens_TokenHash",
            table: "AdminPasswordResetTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminPasswordResetTokens_Email",
            table: "AdminPasswordResetTokens",
            column: "Email");

        migrationBuilder.CreateIndex(
            name: "IX_AdminPasswordResetTokens_ExpiresAt",
            table: "AdminPasswordResetTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateTable(
            name: "AdminCredentials",
            columns: table => new
            {
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 512, nullable: false),
                PasswordUpdatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminCredentials", x => x.Email);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminPasswordResetTokens");
        migrationBuilder.DropTable(name: "AdminCredentials");
    }
}

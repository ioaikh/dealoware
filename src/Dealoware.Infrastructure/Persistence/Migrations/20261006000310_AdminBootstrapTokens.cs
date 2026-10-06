using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// CoreOwner credential row and hashed single-use bootstrap tokens.
/// Provider-agnostic column types so SQLite (tests) and PostgreSQL (migrate one-shot) both apply.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000310_AdminBootstrapTokens")]
public class AdminBootstrapTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminCredentials",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 512, nullable: true),
                PasswordSetAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminCredentials", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminCredentials_Email",
            table: "AdminCredentials",
            column: "Email",
            unique: true);

        migrationBuilder.CreateTable(
            name: "AdminBootstrapTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminBootstrapTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminBootstrapTokens_TokenHash",
            table: "AdminBootstrapTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminBootstrapTokens_ExpiresAt",
            table: "AdminBootstrapTokens",
            column: "ExpiresAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminBootstrapTokens");
        migrationBuilder.DropTable(name: "AdminCredentials");
    }
}

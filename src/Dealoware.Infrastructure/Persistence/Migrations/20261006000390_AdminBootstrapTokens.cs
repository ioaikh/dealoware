using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Bootstrap (first-time setup) tokens: hashed, 24h, single-use.
/// Provider-agnostic so SQLite tests and PostgreSQL migrate both apply.
/// Incremental after the frozen 15-table stamp. Do not add this table to
/// BaselineSchema / DatabaseMigrationBaseline table lists.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000390_AdminBootstrapTokens")]
public partial class AdminBootstrapTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminBootstrapTokens",
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
                table.PrimaryKey("PK_AdminBootstrapTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminBootstrapTokens_TokenHash",
            table: "AdminBootstrapTokens",
            column: "TokenHash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminBootstrapTokens");
    }
}

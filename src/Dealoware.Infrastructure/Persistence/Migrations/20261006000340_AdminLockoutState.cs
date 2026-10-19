using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Cross-instance admin lockout counters, locks, and pending/link tokens.
/// Provider-agnostic so SQLite tests and PostgreSQL migrate both apply.
/// Not part of the frozen #23 13-table / 15-table stamp specs.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000340_AdminLockoutState")]
public partial class AdminLockoutState : Migration
{
    public const string MigrationId = "20261006000340_AdminLockoutState";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminAuthFailureEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Scope = table.Column<string>(maxLength: 32, nullable: false),
                SubjectKey = table.Column<string>(maxLength: 256, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuthFailureEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuthFailureEvents_Scope_SubjectKey_OccurredAt",
            table: "AdminAuthFailureEvents",
            columns: new[] { "Scope", "SubjectKey", "OccurredAt" });

        migrationBuilder.CreateTable(
            name: "AdminAuthLockouts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Scope = table.Column<string>(maxLength: 32, nullable: false),
                SubjectKey = table.Column<string>(maxLength: 256, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                Version = table.Column<uint>(nullable: false, defaultValue: 0u)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuthLockouts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuthLockouts_Scope_SubjectKey",
            table: "AdminAuthLockouts",
            columns: new[] { "Scope", "SubjectKey" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "AdminAuthTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Kind = table.Column<string>(maxLength: 32, nullable: false),
                TokenHash = table.Column<string>(maxLength: 128, nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuthTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuthTokens_TokenHash",
            table: "AdminAuthTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuthTokens_Kind_Email",
            table: "AdminAuthTokens",
            columns: new[] { "Kind", "Email" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminAuthFailureEvents");
        migrationBuilder.DropTable(name: "AdminAuthLockouts");
        migrationBuilder.DropTable(name: "AdminAuthTokens");
    }
}

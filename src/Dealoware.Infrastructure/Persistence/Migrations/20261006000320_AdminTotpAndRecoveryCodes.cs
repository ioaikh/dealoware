using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// TOTP secret (encrypted), hashed recovery codes, and pending-auth tokens for two-step sign-in.
/// Does not change the frozen #23 13-table / 15-table stamp specs. Applies after stamp via MigrateAsync.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000320_AdminTotpAndRecoveryCodes")]
public partial class AdminTotpAndRecoveryCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminCoreOwnerAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 512, nullable: true),
                TotpSecretCipher = table.Column<string>(maxLength: 512, nullable: true),
                TotpEnrolledAt = table.Column<DateTimeOffset>(nullable: true),
                RecoveryCodesIssued = table.Column<bool>(nullable: false),
                LastUsedTotpTimestep = table.Column<long>(nullable: true),
                PendingTotpSecretCipher = table.Column<string>(maxLength: 512, nullable: true),
                RecoveryCodesRevealCipher = table.Column<string>(maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminCoreOwnerAccounts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminCoreOwnerAccounts_Email",
            table: "AdminCoreOwnerAccounts",
            column: "Email",
            unique: true);

        migrationBuilder.CreateTable(
            name: "AdminRecoveryCodes",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                AccountId = table.Column<Guid>(nullable: false),
                CodeHash = table.Column<string>(maxLength: 128, nullable: false),
                UsedAt = table.Column<DateTimeOffset>(nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminRecoveryCodes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminRecoveryCodes_AccountId",
            table: "AdminRecoveryCodes",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_AdminRecoveryCodes_AccountId_CodeHash",
            table: "AdminRecoveryCodes",
            columns: new[] { "AccountId", "CodeHash" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "AdminPendingAuths",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                TokenHash = table.Column<string>(maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true),
                FailedCodeAttempts = table.Column<int>(nullable: false),
                ReturnPath = table.Column<string>(maxLength: 2048, nullable: false, defaultValue: "/admin/")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminPendingAuths", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminPendingAuths_TokenHash",
            table: "AdminPendingAuths",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AdminPendingAuths_Email",
            table: "AdminPendingAuths",
            column: "Email");

        migrationBuilder.CreateIndex(
            name: "IX_AdminPendingAuths_ExpiresAt",
            table: "AdminPendingAuths",
            column: "ExpiresAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminPendingAuths");
        migrationBuilder.DropTable(name: "AdminRecoveryCodes");
        migrationBuilder.DropTable(name: "AdminCoreOwnerAccounts");
    }
}

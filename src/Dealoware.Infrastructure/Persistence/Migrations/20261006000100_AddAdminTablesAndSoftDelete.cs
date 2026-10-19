using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Admin session + append-only audit tables, and DeletedAt / Version on the four admin entities.
/// Provider-agnostic column types so SQLite (tests) and PostgreSQL (migrate one-shot) both apply.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000100_AddAdminTablesAndSoftDelete")]
public partial class AddAdminTablesAndSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAt",
            table: "Participants",
            nullable: true);

        migrationBuilder.AddColumn<uint>(
            name: "Version",
            table: "Participants",
            nullable: false,
            defaultValue: 0u);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAt",
            table: "Artifacts",
            nullable: true);

        migrationBuilder.AddColumn<uint>(
            name: "Version",
            table: "Artifacts",
            nullable: false,
            defaultValue: 0u);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAt",
            table: "Negotiations",
            nullable: true);

        migrationBuilder.AddColumn<uint>(
            name: "Version",
            table: "Negotiations",
            nullable: false,
            defaultValue: 0u);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAt",
            table: "Offers",
            nullable: true);

        migrationBuilder.AddColumn<uint>(
            name: "Version",
            table: "Offers",
            nullable: false,
            defaultValue: 0u);

        migrationBuilder.CreateTable(
            name: "AdminSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                LastActivityAt = table.Column<DateTimeOffset>(nullable: false),
                AbsoluteExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                TotpVerified = table.Column<bool>(nullable: false),
                IpHmac = table.Column<string>(maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminSessions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminSessions_Email",
            table: "AdminSessions",
            column: "Email");

        migrationBuilder.CreateIndex(
            name: "IX_AdminSessions_AbsoluteExpiresAt",
            table: "AdminSessions",
            column: "AbsoluteExpiresAt");

        migrationBuilder.CreateTable(
            name: "AdminAuditLog",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Timestamp = table.Column<DateTimeOffset>(nullable: false),
                Action = table.Column<string>(maxLength: 64, nullable: false),
                ActorEmail = table.Column<string>(maxLength: 256, nullable: false),
                IpHmac = table.Column<string>(maxLength: 128, nullable: false),
                EntityType = table.Column<string>(maxLength: 64, nullable: true),
                EntityId = table.Column<Guid>(nullable: true),
                ReasonClass = table.Column<string>(maxLength: 64, nullable: true),
                CorrelationId = table.Column<Guid>(nullable: true),
                BeforeSnapshot = table.Column<string>(maxLength: 5000, nullable: true),
                AfterSnapshot = table.Column<string>(maxLength: 5000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuditLog", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuditLog_Timestamp",
            table: "AdminAuditLog",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuditLog_Action",
            table: "AdminAuditLog",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuditLog_ActorEmail",
            table: "AdminAuditLog",
            column: "ActorEmail");

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuditLog_EntityType",
            table: "AdminAuditLog",
            column: "EntityType");

        migrationBuilder.CreateIndex(
            name: "IX_AdminAuditLog_CorrelationId",
            table: "AdminAuditLog",
            column: "CorrelationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminAuditLog");
        migrationBuilder.DropTable(name: "AdminSessions");

        migrationBuilder.DropColumn(name: "DeletedAt", table: "Participants");
        migrationBuilder.DropColumn(name: "Version", table: "Participants");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Artifacts");
        migrationBuilder.DropColumn(name: "Version", table: "Artifacts");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Negotiations");
        migrationBuilder.DropColumn(name: "Version", table: "Negotiations");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Offers");
        migrationBuilder.DropColumn(name: "Version", table: "Offers");
    }
}

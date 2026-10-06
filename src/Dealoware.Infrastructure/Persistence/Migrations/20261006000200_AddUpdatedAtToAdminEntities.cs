using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Nullable UTC UpdatedAt on the four admin entities.
/// Provider-agnostic column types so SQLite (tests) and PostgreSQL (migrate one-shot) both apply.
/// Ordered after 20261006000100_AddAdminTablesAndSoftDelete.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000200_AddUpdatedAtToAdminEntities")]
public partial class AddUpdatedAtToAdminEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "Participants",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "Artifacts",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "Negotiations",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "Offers",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Participants");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Artifacts");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Negotiations");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Offers");
    }
}

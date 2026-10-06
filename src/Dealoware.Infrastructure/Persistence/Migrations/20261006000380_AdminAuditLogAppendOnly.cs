using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations;

/// <summary>
/// Security O9: PostgreSQL trigger refuses UPDATE, DELETE and TRUNCATE of AdminAuditLog.
/// Provider-conditional — skipped on SQLite (test host). The frozen 13-table and
/// 15-table stamp specs are unchanged; this migration applies after stamp via MigrateAsync.
/// </summary>
[DbContext(typeof(DealowareDbContext))]
[Migration("20261006000380_AdminAuditLogAppendOnly")]
public partial class AdminAuditLogAppendOnly : Migration
{
    public const string MigrationId = "20261006000380_AdminAuditLogAppendOnly";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!IsPostgres(migrationBuilder))
            return;

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION dealoware_admin_audit_log_append_only()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
              RAISE EXCEPTION 'AdminAuditLog is append-only';
            END;
            $$;

            DROP TRIGGER IF EXISTS admin_audit_log_append_only_row ON "AdminAuditLog";
            CREATE TRIGGER admin_audit_log_append_only_row
            BEFORE UPDATE OR DELETE ON "AdminAuditLog"
            FOR EACH ROW
            EXECUTE PROCEDURE dealoware_admin_audit_log_append_only();

            DROP TRIGGER IF EXISTS admin_audit_log_append_only_truncate ON "AdminAuditLog";
            CREATE TRIGGER admin_audit_log_append_only_truncate
            BEFORE TRUNCATE ON "AdminAuditLog"
            FOR EACH STATEMENT
            EXECUTE PROCEDURE dealoware_admin_audit_log_append_only();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!IsPostgres(migrationBuilder))
            return;

        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS admin_audit_log_append_only_truncate ON "AdminAuditLog";
            DROP TRIGGER IF EXISTS admin_audit_log_append_only_row ON "AdminAuditLog";
            DROP FUNCTION IF EXISTS dealoware_admin_audit_log_append_only();
            """);
    }

    private static bool IsPostgres(MigrationBuilder migrationBuilder)
        => string.Equals(
            migrationBuilder.ActiveProvider,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal);
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealoware.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Initial schema matching Development EnsureCreated / schema bootstrap.
    /// Dated before A7 PR #20 (<c>20261006000100_AddAdminTablesAndSoftDelete</c>)
    /// so that incremental migration applies after this baseline once merged.
    /// Fresh databases run <see cref="Up"/>; existing complete schemas are
    /// stamped by <see cref="Dealoware.Infrastructure.Persistence.DatabaseMigrationBaseline"/> instead of re-creating tables.
    /// </summary>
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OfferId = table.Column<Guid>(nullable: false),
                    NegotiationId = table.Column<Guid>(nullable: false),
                    GrantorSub = table.Column<string>(maxLength: 100, nullable: false),
                    GranteeSub = table.Column<string>(maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeyCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ParticipantId = table.Column<Guid>(nullable: false),
                    KeyPrefix = table.Column<string>(maxLength: 16, nullable: false),
                    KeyHash = table.Column<string>(maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OwnerParticipantId = table.Column<string>(maxLength: 256, nullable: false),
                    Intent = table.Column<string>(maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Locations = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Negotiations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ArtifactId = table.Column<Guid>(nullable: false),
                    PartyAParticipantId = table.Column<string>(maxLength: 100, nullable: false),
                    PartyBParticipantId = table.Column<string>(maxLength: 100, nullable: false),
                    PartyAIntent = table.Column<string>(maxLength: 50, nullable: false),
                    PartyBIntent = table.Column<string>(maxLength: 50, nullable: false),
                    Status = table.Column<int>(nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negotiations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ParticipantSub = table.Column<string>(maxLength: 256, nullable: false),
                    LimitUnits = table.Column<long>(nullable: false),
                    UsedUnits = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Sub = table.Column<string>(maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 256, nullable: true),
                    LoginEmail = table.Column<string>(maxLength: 256, nullable: true),
                    ContactEmail = table.Column<string>(maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevokedTokens",
                columns: table => new
                {
                    Jti = table.Column<string>(maxLength: 256, nullable: false),
                    Sub = table.Column<string>(maxLength: 256, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedTokens", x => x.Jti);
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OwnerParticipantId = table.Column<string>(maxLength: 256, nullable: false),
                    Name = table.Column<string>(maxLength: 500, nullable: true),
                    StrategyBody = table.Column<string>(maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtifactValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Amount = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(maxLength: 16, nullable: false),
                    ArtifactId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactValues_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubjectEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 4096, nullable: false),
                    Description = table.Column<string>(maxLength: 4096, nullable: false),
                    ArtifactId = table.Column<Guid>(nullable: true),
                    Facts = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectEntities_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimePeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Start = table.Column<DateTimeOffset>(nullable: false),
                    End = table.Column<DateTimeOffset>(nullable: false),
                    ArtifactId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimePeriods_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    NegotiationId = table.Column<Guid>(nullable: false),
                    FromParticipantId = table.Column<string>(maxLength: 100, nullable: false),
                    ToParticipantId = table.Column<string>(maxLength: 100, nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(maxLength: 3, nullable: true),
                    Terms = table.Column<string>(maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Negotiations_NegotiationId",
                        column: x => x.NegotiationId,
                        principalTable: "Negotiations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Type = table.Column<string>(maxLength: 1024, nullable: false),
                    Value = table.Column<string>(maxLength: 1024, nullable: false),
                    SubjectEntityId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityProperties_SubjectEntities_SubjectEntityId",
                        column: x => x.SubjectEntityId,
                        principalTable: "SubjectEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptGrants_GrantorSub",
                table: "AcceptGrants",
                column: "GrantorSub");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptGrants_NegotiationId_GranteeSub",
                table: "AcceptGrants",
                columns: new[] { "NegotiationId", "GranteeSub" });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptGrants_OfferId_GranteeSub",
                table: "AcceptGrants",
                columns: new[] { "OfferId", "GranteeSub" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyCredentials_KeyPrefix",
                table: "ApiKeyCredentials",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyCredentials_ParticipantId",
                table: "ApiKeyCredentials",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_OwnerParticipantId",
                table: "Artifacts",
                column: "OwnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactValues_ArtifactId",
                table: "ArtifactValues",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityProperties_SubjectEntityId",
                table: "EntityProperties",
                column: "SubjectEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_ArtifactId",
                table: "Negotiations",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_PartyAParticipantId",
                table: "Negotiations",
                column: "PartyAParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_PartyBParticipantId",
                table: "Negotiations",
                column: "PartyBParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Negotiations_Status",
                table: "Negotiations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_FromParticipantId",
                table: "Offers",
                column: "FromParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_NegotiationId",
                table: "Offers",
                column: "NegotiationId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Status",
                table: "Offers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ToParticipantId",
                table: "Offers",
                column: "ToParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantBudgets_ParticipantSub",
                table: "ParticipantBudgets",
                column: "ParticipantSub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_Sub",
                table: "Participants",
                column: "Sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevokedTokens_ExpiresAt",
                table: "RevokedTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_OwnerParticipantId",
                table: "Strategies",
                column: "OwnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_OwnerParticipantId_IsActive",
                table: "Strategies",
                columns: new[] { "OwnerParticipantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEntities_ArtifactId",
                table: "SubjectEntities",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_TimePeriods_ArtifactId",
                table: "TimePeriods",
                column: "ArtifactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptGrants");

            migrationBuilder.DropTable(
                name: "ApiKeyCredentials");

            migrationBuilder.DropTable(
                name: "ArtifactValues");

            migrationBuilder.DropTable(
                name: "EntityProperties");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "ParticipantBudgets");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "RevokedTokens");

            migrationBuilder.DropTable(
                name: "Strategies");

            migrationBuilder.DropTable(
                name: "TimePeriods");

            migrationBuilder.DropTable(
                name: "SubjectEntities");

            migrationBuilder.DropTable(
                name: "Negotiations");

            migrationBuilder.DropTable(
                name: "Artifacts");
        }
    }
}

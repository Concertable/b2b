using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concertable.B2B.Application.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "application");

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    OpportunityId = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    DealType = table.Column<int>(type: "integer", nullable: false),
                    AcceptanceOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TermsFingerprint = table.Column<string>(type: "text", nullable: false),
                    ArtistESignature_AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtistESignature_DrawnSignatureImage = table.Column<string>(type: "text", nullable: true),
                    ArtistESignature_Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    ArtistESignature_SignatoryName = table.Column<string>(type: "text", nullable: false),
                    ArtistESignature_UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ArtistESignature_UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConcertAvailabilities",
                schema: "application",
                columns: table => new
                {
                    ConcertId = table.Column<int>(type: "integer", nullable: false),
                    OpportunityId = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertAvailabilities", x => x.ConcertId);
                });

            migrationBuilder.CreateTable(
                name: "VerifyPayments",
                schema: "application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifyPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerifyPayments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "application",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AcceptanceOperationId",
                schema: "application",
                table: "Applications",
                column: "AcceptanceOperationId",
                unique: true,
                filter: "\"AcceptanceOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OpportunityId",
                schema: "application",
                table: "Applications",
                column: "OpportunityId",
                unique: true,
                filter: "\"State\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OpportunityId_ArtistId",
                schema: "application",
                table: "Applications",
                columns: new[] { "OpportunityId", "ArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConcertAvailabilities_ArtistId_StartDate",
                schema: "application",
                table: "ConcertAvailabilities",
                columns: new[] { "ArtistId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ConcertAvailabilities_OpportunityId",
                schema: "application",
                table: "ConcertAvailabilities",
                column: "OpportunityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConcertAvailabilities_VenueId_StartDate",
                schema: "application",
                table: "ConcertAvailabilities",
                columns: new[] { "VenueId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VerifyPayments_ApplicationId",
                schema: "application",
                table: "VerifyPayments",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConcertAvailabilities",
                schema: "application");

            migrationBuilder.DropTable(
                name: "VerifyPayments",
                schema: "application");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "application");
        }
    }
}

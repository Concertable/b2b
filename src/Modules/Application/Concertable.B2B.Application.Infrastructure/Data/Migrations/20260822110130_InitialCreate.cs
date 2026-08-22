using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    DealType = table.Column<int>(type: "int", nullable: false),
                    AcceptanceOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermsFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    ArtistESignature_AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistESignature_DrawnSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtistESignature_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    ArtistESignature_SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistESignature_UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ArtistESignature_UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethodId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifyPayments",
                schema: "application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                filter: "[AcceptanceOperationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OpportunityId_ArtistId",
                schema: "application",
                table: "Applications",
                columns: new[] { "OpportunityId", "ArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifyPayments_ApplicationId",
                schema: "application",
                table: "VerifyPayments",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifyPayments_ProviderTransactionId",
                schema: "application",
                table: "VerifyPayments",
                column: "ProviderTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifyPayments",
                schema: "application");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "application");
        }
    }
}

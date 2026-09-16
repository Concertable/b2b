using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Booking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    DealType = table.Column<int>(type: "int", nullable: false),
                    ExpectedFinancialOperation = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CancellationOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FinancialFailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingAccessGrants",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedByTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingAccessGrants_Bookings_ResourceId",
                        column: x => x.ResourceId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DealType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    TermsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlatformTermsVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MandateTermsVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PdfBlobName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistSignature_AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistSignature_DrawnSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtistSignature_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    ArtistSignature_SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistSignature_UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ArtistSignature_UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Commitment_ClientReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Commitment_OperationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Period_End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period_Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenueSignature_AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenueSignature_DrawnSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VenueSignature_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    VenueSignature_SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VenueSignature_UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    VenueSignature_UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Guarantee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HireFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContractAccessGrants",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedByTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAccessGrants_Contracts_ResourceId",
                        column: x => x.ResourceId,
                        principalSchema: "booking",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingAccessGrants_TenantId_Scope_ResourceId_MemberUserId",
                schema: "booking",
                table: "BookingAccessGrants",
                columns: new[] { "TenantId", "Scope", "ResourceId", "MemberUserId" },
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BookingAccessGrants_Member",
                schema: "booking",
                table: "BookingAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Scope", "MemberUserId" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BookingAccessGrants_TenantWide",
                schema: "booking",
                table: "BookingAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Scope" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ApplicationId",
                schema: "booking",
                table: "Bookings",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CancellationOperationId",
                schema: "booking",
                table: "Bookings",
                column: "CancellationOperationId",
                unique: true,
                filter: "[CancellationOperationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OperationId",
                schema: "booking",
                table: "Bookings",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAccessGrants_TenantId_Scope_ResourceId_MemberUserId",
                schema: "booking",
                table: "ContractAccessGrants",
                columns: new[] { "TenantId", "Scope", "ResourceId", "MemberUserId" },
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ContractAccessGrants_Member",
                schema: "booking",
                table: "ContractAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Scope", "MemberUserId" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ContractAccessGrants_TenantWide",
                schema: "booking",
                table: "ContractAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Scope" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BookingId",
                schema: "booking",
                table: "Contracts",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAccessGrants",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ContractAccessGrants",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Contracts",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "booking");
        }
    }
}

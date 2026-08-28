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
                    RequiresDoorRevenue = table.Column<bool>(type: "bit", nullable: false),
                    ExpectedFinancialOperation = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CancellationOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FinancialFailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FinancialOperationReferenceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    PaymentMethodId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Guarantee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
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
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DealType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    TermsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlatformTermsVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PdfBlobName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistSignature_AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistSignature_DrawnSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtistSignature_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    ArtistSignature_SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistSignature_UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ArtistSignature_UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period_End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period_Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenueSignature_AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VenueSignature_DrawnSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VenueSignature_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    VenueSignature_SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VenueSignature_UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    VenueSignature_UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Contracts",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "booking");
        }
    }
}

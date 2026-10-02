using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    OpportunityId = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    DealType = table.Column<int>(type: "integer", nullable: false),
                    ExpectedFinancialOperation = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Genres = table.Column<int[]>(type: "integer[]", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CancellationOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialFailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FinancialFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    VenueName = table.Column<string>(type: "text", nullable: false),
                    ArtistName = table.Column<string>(type: "text", nullable: false),
                    DealType = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    TermsText = table.Column<string>(type: "text", nullable: false),
                    PlatformTermsVersion = table.Column<string>(type: "text", nullable: false),
                    MandateTermsVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PdfBlobName = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtistSignature_AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtistSignature_DrawnSignatureImage = table.Column<string>(type: "text", nullable: true),
                    ArtistSignature_Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    ArtistSignature_SignatoryName = table.Column<string>(type: "text", nullable: false),
                    ArtistSignature_UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ArtistSignature_UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Commitment_ClientReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Commitment_OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Period_End = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Period_Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VenueSignature_AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VenueSignature_DrawnSignatureImage = table.Column<string>(type: "text", nullable: true),
                    VenueSignature_Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    VenueSignature_SignatoryName = table.Column<string>(type: "text", nullable: false),
                    VenueSignature_UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    VenueSignature_UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    Guarantee = table.Column<decimal>(type: "numeric", nullable: true),
                    Fee = table.Column<decimal>(type: "numeric", nullable: true),
                    HireFee = table.Column<decimal>(type: "numeric", nullable: true)
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
                filter: "\"CancellationOperationId\" IS NOT NULL");

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

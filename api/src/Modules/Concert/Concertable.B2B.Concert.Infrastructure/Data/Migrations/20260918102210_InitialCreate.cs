using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concertable.B2B.Concert.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "concert");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "ArtistReadModels",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: false),
                    BannerUrl = table.Column<string>(type: "text", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false),
                    Town = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistReadModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConcertRatingProjections",
                schema: "concert",
                columns: table => new
                {
                    ConcertId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertRatingProjections", x => x.ConcertId);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TaxPointUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DealType = table.Column<int>(type: "integer", nullable: false),
                    PdfBlobName = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amounts_Gross = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amounts_Net = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amounts_Rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Amounts_Vat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Customer_AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Customer_AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Customer_City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Customer_Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Customer_LegalName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Customer_Postcode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Customer_TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Customer_VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Supplier_AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Supplier_AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Supplier_City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Supplier_Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Supplier_LegalName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Supplier_Postcode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Supplier_TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Supplier_VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSequences",
                schema: "concert",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSequences", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "SelfBillingAgreements",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlatformTermsVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClauseText = table.Column<string>(type: "text", nullable: false),
                    PdfBlobName = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Supplier_AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Supplier_AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Supplier_City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Supplier_Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Supplier_LegalName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Supplier_Postcode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Supplier_TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Supplier_VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SupplierESignature_AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SupplierESignature_DrawnSignatureImage = table.Column<string>(type: "text", nullable: true),
                    SupplierESignature_Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    SupplierESignature_SignatoryName = table.Column<string>(type: "text", nullable: false),
                    SupplierESignature_UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SupplierESignature_UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfBillingAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VenueReadModels",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    About = table.Column<string>(type: "text", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false),
                    Town = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry (point, 4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueReadModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtistReadModelGenres",
                schema: "concert",
                columns: table => new
                {
                    ArtistReadModelId = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistReadModelGenres", x => new { x.ArtistReadModelId, x.Genre });
                    table.ForeignKey(
                        name: "FK_ArtistReadModelGenres_ArtistReadModels_ArtistReadModelId",
                        column: x => x.ArtistReadModelId,
                        principalSchema: "concert",
                        principalTable: "ArtistReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Concerts",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    OpportunityId = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    DealType = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CancellationOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettlementOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettlementGrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    About = table.Column<string>(type: "text", nullable: false),
                    BannerUrl = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTickets = table.Column<int>(type: "integer", nullable: false),
                    TicketsSold = table.Column<int>(type: "integer", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Genres = table.Column<int[]>(type: "integer[]", nullable: false),
                    FinancialFailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FinancialFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettlementPaymentReference_ClientReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SettlementPaymentReference_OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    DoorRevenue = table.Column<decimal>(type: "numeric", nullable: true),
                    Fee = table.Column<decimal>(type: "numeric", nullable: true),
                    HireFee = table.Column<decimal>(type: "numeric", nullable: true),
                    Guarantee = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Concerts_ArtistReadModels_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "concert",
                        principalTable: "ArtistReadModels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Concerts_VenueReadModels_VenueId",
                        column: x => x.VenueId,
                        principalSchema: "concert",
                        principalTable: "VenueReadModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConcertImages",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConcertId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcertImages_Concerts_ConcertId",
                        column: x => x.ConcertId,
                        principalSchema: "concert",
                        principalTable: "Concerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtistReadModels_TenantId",
                schema: "concert",
                table: "ArtistReadModels",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConcertImages_ConcertId",
                schema: "concert",
                table: "ConcertImages",
                column: "ConcertId");

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_ArtistId",
                schema: "concert",
                table: "Concerts",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_BookingId",
                schema: "concert",
                table: "Concerts",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_CancellationOperationId",
                schema: "concert",
                table: "Concerts",
                column: "CancellationOperationId",
                unique: true,
                filter: "\"CancellationOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_SettlementOperationId",
                schema: "concert",
                table: "Concerts",
                column: "SettlementOperationId",
                unique: true,
                filter: "\"SettlementOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_VenueId",
                schema: "concert",
                table: "Concerts",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId",
                schema: "concert",
                table: "Invoices",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueReadModels_TenantId",
                schema: "concert",
                table: "VenueReadModels",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistReadModelGenres",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "ConcertImages",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "ConcertRatingProjections",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "InvoiceSequences",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "SelfBillingAgreements",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "Concerts",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "ArtistReadModels",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "VenueReadModels",
                schema: "concert");
        }
    }
}

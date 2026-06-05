using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

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

            migrationBuilder.CreateTable(
                name: "ArtistReadModels",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    ConcertId = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertRatingProjections", x => x.ConcertId);
                });

            migrationBuilder.CreateTable(
                name: "VenueReadModels",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    About = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false)
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
                    ArtistReadModelId = table.Column<int>(type: "int", nullable: false),
                    Genre = table.Column<int>(type: "int", nullable: false)
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
                name: "Opportunities",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opportunities_VenueReadModels_VenueId",
                        column: x => x.VenueId,
                        principalSchema: "concert",
                        principalTable: "VenueReadModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    CurrentStage = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    PaymentMethodId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_ArtistReadModels_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "concert",
                        principalTable: "ArtistReadModels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applications_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalSchema: "concert",
                        principalTable: "Opportunities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    CurrentStage = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    PaymentMethodId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "concert",
                        principalTable: "Applications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Concerts",
                schema: "concert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    About = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTickets = table.Column<int>(type: "int", nullable: false),
                    TicketsSold = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    CurrentStage = table.Column<int>(type: "int", nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                        name: "FK_Concerts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "concert",
                        principalTable: "Bookings",
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConcertId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "IX_Applications_ArtistId",
                schema: "concert",
                table: "Applications",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OpportunityId_ArtistId",
                schema: "concert",
                table: "Applications",
                columns: new[] { "OpportunityId", "ArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArtistReadModels_UserId",
                schema: "concert",
                table: "ArtistReadModels",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ApplicationId",
                schema: "concert",
                table: "Bookings",
                column: "ApplicationId",
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
                name: "IX_Concerts_VenueId",
                schema: "concert",
                table: "Concerts",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_ContractId",
                schema: "concert",
                table: "Opportunities",
                column: "ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_VenueId",
                schema: "concert",
                table: "Opportunities",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueReadModels_UserId",
                schema: "concert",
                table: "VenueReadModels",
                column: "UserId",
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
                name: "Concerts",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "ArtistReadModels",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "Opportunities",
                schema: "concert");

            migrationBuilder.DropTable(
                name: "VenueReadModels",
                schema: "concert");
        }
    }
}

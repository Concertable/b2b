using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Deal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "deal");

            migrationBuilder.CreateTable(
                name: "Deals",
                schema: "deal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoorSplitDeals",
                schema: "deal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorSplitDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoorSplitDeals_Deals_Id",
                        column: x => x.Id,
                        principalSchema: "deal",
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlatFeeDeals",
                schema: "deal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlatFeeDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlatFeeDeals_Deals_Id",
                        column: x => x.Id,
                        principalSchema: "deal",
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueHireDeals",
                schema: "deal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    HireFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueHireDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueHireDeals_Deals_Id",
                        column: x => x.Id,
                        principalSchema: "deal",
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VersusDeals",
                schema: "deal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Guarantee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersusDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersusDeals_Deals_Id",
                        column: x => x.Id,
                        principalSchema: "deal",
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoorSplitDeals",
                schema: "deal");

            migrationBuilder.DropTable(
                name: "FlatFeeDeals",
                schema: "deal");

            migrationBuilder.DropTable(
                name: "VenueHireDeals",
                schema: "deal");

            migrationBuilder.DropTable(
                name: "VersusDeals",
                schema: "deal");

            migrationBuilder.DropTable(
                name: "Deals",
                schema: "deal");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.Contract.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "contract");

            migrationBuilder.CreateTable(
                name: "Contracts",
                schema: "contract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoorSplitContracts",
                schema: "contract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorSplitContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoorSplitContracts_Contracts_Id",
                        column: x => x.Id,
                        principalSchema: "contract",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlatFeeContracts",
                schema: "contract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlatFeeContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlatFeeContracts_Contracts_Id",
                        column: x => x.Id,
                        principalSchema: "contract",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueHireContracts",
                schema: "contract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    HireFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueHireContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueHireContracts_Contracts_Id",
                        column: x => x.Id,
                        principalSchema: "contract",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VersusContracts",
                schema: "contract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Guarantee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ArtistDoorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersusContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersusContracts_Contracts_Id",
                        column: x => x.Id,
                        principalSchema: "contract",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoorSplitContracts",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "FlatFeeContracts",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "VenueHireContracts",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "VersusContracts",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "Contracts",
                schema: "contract");
        }
    }
}

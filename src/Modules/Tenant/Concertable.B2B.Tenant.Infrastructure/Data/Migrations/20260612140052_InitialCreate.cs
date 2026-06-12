using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Tenant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Compliance_VatRegistered = table.Column<bool>(type: "bit", nullable: true),
                    Compliance_VatNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Compliance_SellerIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Compliance_RegisteredAddress_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Compliance_RegisteredAddress_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Compliance_RegisteredAddress_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Compliance_RegisteredAddress_Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Compliance_RegisteredAddress_Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Compliance_BankReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenant");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Application.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignApplicationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applications_OpportunityId",
                schema: "application",
                table: "Applications",
                column: "OpportunityId",
                unique: true,
                filter: "[State] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_OpportunityId",
                schema: "application",
                table: "Applications");
        }
    }
}

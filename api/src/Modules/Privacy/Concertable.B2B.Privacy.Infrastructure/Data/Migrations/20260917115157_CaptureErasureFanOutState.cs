using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Privacy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CaptureErasureFanOutState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubjectEmail",
                schema: "privacy",
                table: "SubjectErasureRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoundDownTenantIds",
                schema: "privacy",
                table: "SubjectErasureRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubjectEmail",
                schema: "privacy",
                table: "SubjectErasureRequests");

            migrationBuilder.DropColumn(
                name: "WoundDownTenantIds",
                schema: "privacy",
                table: "SubjectErasureRequests");
        }
    }
}

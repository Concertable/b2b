using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Privacy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "privacy");

            migrationBuilder.CreateTable(
                name: "SubjectErasureRequests",
                schema: "privacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeferralReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectErasureRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectErasureRequests_SubjectId",
                schema: "privacy",
                table: "SubjectErasureRequests",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectErasureRequests",
                schema: "privacy");
        }
    }
}

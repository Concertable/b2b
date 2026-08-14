using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Conversations.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conversations");

            migrationBuilder.CreateTable(
                name: "ContentReports",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageExcerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HiddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HiddenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThreadReadStates",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadReadStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ArtistTenantId",
                schema: "conversations",
                table: "ContentReports",
                column: "ArtistTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_MessageId",
                schema: "conversations",
                table: "ContentReports",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_VenueTenantId",
                schema: "conversations",
                table: "ContentReports",
                column: "VenueTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ArtistTenantId",
                schema: "conversations",
                table: "Messages",
                column: "ArtistTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_VenueTenantId",
                schema: "conversations",
                table: "Messages",
                column: "VenueTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadReadStates_VenueTenantId_ArtistTenantId_UserId",
                schema: "conversations",
                table: "ThreadReadStates",
                columns: new[] { "VenueTenantId", "ArtistTenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentReports",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ThreadReadStates",
                schema: "conversations");
        }
    }
}

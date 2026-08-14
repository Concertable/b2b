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
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantProfiles",
                schema: "conversations",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantProfiles", x => x.TenantId);
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
                name: "Messages",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ParticipantProfiles",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ThreadReadStates",
                schema: "conversations");
        }
    }
}

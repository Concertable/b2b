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
                    ThreadId = table.Column<int>(type: "int", nullable: false),
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
                name: "Threads",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Threads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HiddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HiddenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "conversations",
                        principalTable: "Threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadAccessGrants",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Facet = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedByTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadAccessGrants_Threads_ResourceId",
                        column: x => x.ResourceId,
                        principalSchema: "conversations",
                        principalTable: "Threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadReadStates",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadReadStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadReadStates_Threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "conversations",
                        principalTable: "Threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_MessageId",
                schema: "conversations",
                table: "ContentReports",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ThreadId_ReporterTenantId",
                schema: "conversations",
                table: "ContentReports",
                columns: new[] { "ThreadId", "ReporterTenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderTenantId",
                schema: "conversations",
                table: "Messages",
                column: "SenderTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ThreadId_SentDate",
                schema: "conversations",
                table: "Messages",
                columns: new[] { "ThreadId", "SentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadAccessGrants_TenantId_Facet_ResourceId_MemberUserId",
                schema: "conversations",
                table: "ThreadAccessGrants",
                columns: new[] { "TenantId", "Facet", "ResourceId", "MemberUserId" },
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ThreadAccessGrants_Member",
                schema: "conversations",
                table: "ThreadAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Facet", "MemberUserId" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ThreadAccessGrants_TenantWide",
                schema: "conversations",
                table: "ThreadAccessGrants",
                columns: new[] { "ResourceId", "TenantId", "Facet" },
                unique: true,
                filter: "[RevokedAt] IS NULL AND [MemberUserId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadReadStates_ThreadId_TenantId_UserId",
                schema: "conversations",
                table: "ThreadReadStates",
                columns: new[] { "ThreadId", "TenantId", "UserId" },
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
                name: "ParticipantProfiles",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ThreadAccessGrants",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ThreadReadStates",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "Threads",
                schema: "conversations");
        }
    }
}

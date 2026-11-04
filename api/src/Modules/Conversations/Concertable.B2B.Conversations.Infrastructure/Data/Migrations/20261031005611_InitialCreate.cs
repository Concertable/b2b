using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    ReporterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    MessageExcerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageSequence = table.Column<long>(type: "bigint", nullable: false),
                    AccessVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantDisplays",
                schema: "conversations",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayVersion = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDisplays", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "ConversationAssignments",
                schema: "conversations",
                columns: table => new
                {
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationAssignments", x => new { x.ConversationId, x.MembershipId });
                    table.ForeignKey(
                        name: "FK_ConversationAssignments_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "conversations",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationCreationReceipts",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    CreatorTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationCreationReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationCreationReceipts_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "conversations",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                schema: "conversations",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => new { x.ConversationId, x.TenantId });
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "conversations",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationReadPositions",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadSequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationReadPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationReadPositions_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "conversations",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SenderTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentByMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HiddenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HiddenByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RestoredByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "conversations",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ConversationId_ReporterTenantId_ReporterUser~",
                schema: "conversations",
                table: "ContentReports",
                columns: new[] { "ConversationId", "ReporterTenantId", "ReporterUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_MessageId",
                schema: "conversations",
                table: "ContentReports",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAssignments_MembershipId",
                schema: "conversations",
                table: "ConversationAssignments",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCreationReceipts_ConversationId",
                schema: "conversations",
                table: "ConversationCreationReceipts",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCreationReceipts_CreatorTenantId_CreatedByMembe~",
                schema: "conversations",
                table: "ConversationCreationReceipts",
                columns: new[] { "CreatorTenantId", "CreatedByMembershipId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_TenantId",
                schema: "conversations",
                table: "ConversationParticipants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReadPositions_ConversationId_MembershipId",
                schema: "conversations",
                table: "ConversationReadPositions",
                columns: new[] { "ConversationId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReadPositions_TenantId_MembershipId",
                schema: "conversations",
                table: "ConversationReadPositions",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SentAt",
                schema: "conversations",
                table: "Messages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SentByMembershipId_RequestId",
                schema: "conversations",
                table: "Messages",
                columns: new[] { "ConversationId", "SentByMembershipId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_Sequence",
                schema: "conversations",
                table: "Messages",
                columns: new[] { "ConversationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderTenantId",
                schema: "conversations",
                table: "Messages",
                column: "SenderTenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentReports",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ConversationAssignments",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ConversationCreationReceipts",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ConversationParticipants",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "ConversationReadPositions",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "TenantDisplays",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "conversations");
        }
    }
}

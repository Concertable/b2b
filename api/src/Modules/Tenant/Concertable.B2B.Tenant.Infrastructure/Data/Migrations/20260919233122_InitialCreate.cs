using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "Activities",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InviterMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterPermissionVersion = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PermissionVersion = table.Column<long>(type: "bigint", nullable: false),
                    InvitedByMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayVersion = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EligibilityVersion = table.Column<long>(type: "bigint", nullable: false),
                    TaxCompliance_VatNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxCompliance_SellerIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxCompliance_RegisteredAddress_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxCompliance_RegisteredAddress_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxCompliance_RegisteredAddress_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxCompliance_RegisteredAddress_Postcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxCompliance_RegisteredAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxCompliance_BankReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxCompliance_HoldsMusicLicence = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Verifications",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByAdminSub = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessActivities",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessActivities_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenant",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationDocuments",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantVerificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationDocuments_Verifications_TenantVerificationId",
                        column: x => x.TenantVerificationId,
                        principalSchema: "tenant",
                        principalTable: "Verifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_At",
                schema: "tenant",
                table: "Activities",
                columns: new[] { "TenantId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_SourceKey",
                schema: "tenant",
                table: "Activities",
                columns: new[] { "TenantId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivities_TenantId_Kind",
                schema: "tenant",
                table: "BusinessActivities",
                columns: new[] { "TenantId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Email",
                schema: "tenant",
                table: "Invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TenantId_Email",
                schema: "tenant",
                table: "Invitations",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_UserId",
                schema: "tenant",
                table: "Memberships",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                schema: "tenant",
                table: "Memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedByUserId",
                schema: "tenant",
                table: "Tenants",
                column: "CreatedByUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDocuments_TenantVerificationId",
                schema: "tenant",
                table: "VerificationDocuments",
                column: "TenantVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Verifications_TenantId",
                schema: "tenant",
                table: "Verifications",
                column: "TenantId",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE VIEW tenant."MembershipAuthority" AS
                SELECT
                    "Id" AS "MembershipId",
                    "TenantId",
                    "UserId",
                    "PermissionVersion"
                FROM tenant."Memberships";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP VIEW tenant."MembershipAuthority";""");

            migrationBuilder.DropTable(
                name: "Activities",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "BusinessActivities",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "Invitations",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "Memberships",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "VerificationDocuments",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "Verifications",
                schema: "tenant");
        }
    }
}

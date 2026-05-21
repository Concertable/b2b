using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Concertable.User.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user");

            migrationBuilder.CreateTable(
                name: "AdminProfiles",
                schema: "user",
                columns: table => new
                {
                    Sub = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminProfiles", x => x.Sub);
                });

            migrationBuilder.CreateTable(
                name: "ArtistManagerProfiles",
                schema: "user",
                columns: table => new
                {
                    Sub = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistManagerProfiles", x => x.Sub);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                schema: "user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<Point>(type: "geography", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VenueManagerProfiles",
                schema: "user",
                columns: table => new
                {
                    Sub = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueManagerProfiles", x => x.Sub);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_Token",
                schema: "user",
                table: "EmailVerificationTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserId",
                schema: "user",
                table: "EmailVerificationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                schema: "user",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                schema: "user",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Role",
                schema: "user",
                table: "Users",
                columns: new[] { "Email", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminProfiles",
                schema: "user");

            migrationBuilder.DropTable(
                name: "ArtistManagerProfiles",
                schema: "user");

            migrationBuilder.DropTable(
                name: "EmailVerificationTokens",
                schema: "user");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens",
                schema: "user");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "user");

            migrationBuilder.DropTable(
                name: "VenueManagerProfiles",
                schema: "user");
        }
    }
}

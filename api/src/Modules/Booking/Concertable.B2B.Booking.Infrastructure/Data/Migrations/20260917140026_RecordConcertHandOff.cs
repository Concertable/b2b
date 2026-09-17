using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concertable.B2B.Booking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecordConcertHandOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HandedOffAtUtc",
                schema: "booking",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [booking].[Bookings]
                SET [HandedOffAtUtc] = SYSUTCDATETIME()
                WHERE [HandedOffAtUtc] IS NULL AND [State] = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandedOffAtUtc",
                schema: "booking",
                table: "Bookings");
        }
    }
}

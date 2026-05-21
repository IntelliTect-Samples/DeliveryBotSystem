using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BotNetApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Bots",
                columns: new[] { "Id", "BatteryLevel", "IsOnline", "IsServicingCustomer", "LastUpdated", "Name" },
                values: new object[,]
                {
                    { 1, 92, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BOT-ALPHA" },
                    { 2, 61, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BOT-BRAVO" },
                    { 3, 8, false, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BOT-CHARLIE" },
                    { 4, 77, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BOT-DELTA" }
                });
        }
    }
}

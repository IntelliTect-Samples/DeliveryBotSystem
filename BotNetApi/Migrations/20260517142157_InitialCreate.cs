using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BotNetApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StockLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryLevel = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsServicingCustomer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bots", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Bots",
                columns: new[] { "Id", "BatteryLevel", "IsOnline", "IsServicingCustomer", "LastUpdated", "Latitude", "Longitude", "Name", "StockLevel" },
                values: new object[,]
                {
                    { 1, 92, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 47.658799999999999, -117.426, "BOT-ALPHA", "High" },
                    { 2, 61, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 47.6721, -117.3982, "BOT-BRAVO", "Medium" },
                    { 3, 8, false, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 47.654299999999999, -117.43899999999999, "BOT-CHARLIE", "Low" },
                    { 4, 77, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 47.648899999999998, -117.4143, "BOT-DELTA", "High" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bots");
        }
    }
}

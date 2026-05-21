using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotNetApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocationAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "StockLevel",
                table: "Bots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Bots",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Bots",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "StockLevel",
                table: "Bots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Latitude", "Longitude", "StockLevel" },
                values: new object[] { 47.658799999999999, -117.426, "High" });

            migrationBuilder.UpdateData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Latitude", "Longitude", "StockLevel" },
                values: new object[] { 47.6721, -117.3982, "Medium" });

            migrationBuilder.UpdateData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Latitude", "Longitude", "StockLevel" },
                values: new object[] { 47.654299999999999, -117.43899999999999, "Low" });

            migrationBuilder.UpdateData(
                table: "Bots",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Latitude", "Longitude", "StockLevel" },
                values: new object[] { 47.648899999999998, -117.4143, "High" });
        }
    }
}

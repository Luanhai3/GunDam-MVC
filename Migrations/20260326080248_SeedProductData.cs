using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GunDammvc.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Description", "Grade", "ImageUrl", "Name", "Price", "Stock" },
                values: new object[,]
                {
                    { 1, "A modern High Grade interpretation of the original Gundam, celebrating the 40th anniversary of Gunpla.", "HG", "", "HG 1/144 RX-78-2 Gundam [Beyond Global]", 250000m, 20 },
                    { 2, "The iconic angel-winged Gundam from 'Gundam Wing: Endless Waltz', recreated in stunning Real Grade detail.", "RG", "", "RG 1/144 Wing Gundam Zero EW", 350000m, 15 },
                    { 3, "Char Aznable's final mobile suit, designed by Hajime Katoki. A masterpiece of engineering and design.", "MG", "", "MG 1/100 Sazabi Ver.Ka", 1200000m, 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}

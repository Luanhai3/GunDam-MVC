using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GunDammvc.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsUsedToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsUsed",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TierDiscountAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsUsed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TierDiscountAmount",
                table: "Orders");
        }
    }
}

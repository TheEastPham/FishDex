using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAquariumWaterTypeAndStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Aquariums");

            migrationBuilder.AddColumn<int>(
                name: "Style",
                table: "Aquariums",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaterType",
                table: "Aquariums",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Style",
                table: "Aquariums");

            migrationBuilder.DropColumn(
                name: "WaterType",
                table: "Aquariums");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Aquariums",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}

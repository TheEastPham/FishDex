using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class Story1_9_DetailEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LongevityCaptive",
                table: "Species",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "Families",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Order",
                table: "Families",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Schooling",
                table: "Ecologies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Shoaling",
                table: "Ecologies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Solitary",
                table: "Ecologies",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LongevityCaptive",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "Class",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "Schooling",
                table: "Ecologies");

            migrationBuilder.DropColumn(
                name: "Shoaling",
                table: "Ecologies");

            migrationBuilder.DropColumn(
                name: "Solitary",
                table: "Ecologies");
        }
    }
}

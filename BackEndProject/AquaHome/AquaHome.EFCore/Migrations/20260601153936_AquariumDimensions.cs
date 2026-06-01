using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AquariumDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VolumeLiters",
                table: "Aquariums",
                newName: "WidthCm");

            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "Aquariums",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LengthCm",
                table: "Aquariums",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Aquariums");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Aquariums");

            migrationBuilder.RenameColumn(
                name: "WidthCm",
                table: "Aquariums",
                newName: "VolumeLiters");
        }
    }
}

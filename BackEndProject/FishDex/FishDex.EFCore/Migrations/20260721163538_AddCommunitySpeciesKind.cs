using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunitySpeciesKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "SpeciesSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedKind",
                table: "SpeciesSnapshots",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "SpeciesSnapshots");

            migrationBuilder.DropColumn(
                name: "SuggestedKind",
                table: "SpeciesSnapshots");
        }
    }
}

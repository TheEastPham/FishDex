using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class ContestEntryTitleDescriptionRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ContestEntries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ContestEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ContestEntries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContestEntries_UserId_ContestId",
                table: "ContestEntries",
                columns: new[] { "UserId", "ContestId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContestEntries_UserId_ContestId",
                table: "ContestEntries");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ContestEntries");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ContestEntries");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ContestEntries");
        }
    }
}

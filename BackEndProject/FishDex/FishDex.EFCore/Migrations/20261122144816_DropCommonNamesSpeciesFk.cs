using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class DropCommonNamesSpeciesFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommonNames_Species_SpecCode",
                table: "CommonNames");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_CommonNames_Species_SpecCode",
                table: "CommonNames",
                column: "SpecCode",
                principalTable: "Species",
                principalColumn: "SpecCode",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

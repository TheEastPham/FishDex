using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class ContestPrizeTiersAndSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "ContestEntries");

            migrationBuilder.RenameColumn(
                name: "ContestAward",
                table: "AquariumSnapshots",
                newName: "AwardTierLevel");

            migrationBuilder.RenameIndex(
                name: "IX_AquariumSnapshots_IsActive_WaterType_Style_ContestAward_Lik~",
                table: "AquariumSnapshots",
                newName: "IX_AquariumSnapshots_IsActive_WaterType_Style_AwardTierLevel_L~");

            migrationBuilder.AddColumn<Guid>(
                name: "PrizeTierId",
                table: "ContestEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwardTierName",
                table: "AquariumSnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContestPrizeTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false),
                    SlotCount = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestPrizeTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContestPrizeTiers_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContestSponsors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SponsorTier = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestSponsors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContestSponsors_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContestEntries_PrizeTierId",
                table: "ContestEntries",
                column: "PrizeTierId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestPrizeTiers_ContestId_DisplayOrder",
                table: "ContestPrizeTiers",
                columns: new[] { "ContestId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContestSponsors_ContestId_SponsorTier_DisplayOrder",
                table: "ContestSponsors",
                columns: new[] { "ContestId", "SponsorTier", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContestEntries_ContestPrizeTiers_PrizeTierId",
                table: "ContestEntries",
                column: "PrizeTierId",
                principalTable: "ContestPrizeTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestEntries_ContestPrizeTiers_PrizeTierId",
                table: "ContestEntries");

            migrationBuilder.DropTable(
                name: "ContestPrizeTiers");

            migrationBuilder.DropTable(
                name: "ContestSponsors");

            migrationBuilder.DropIndex(
                name: "IX_ContestEntries_PrizeTierId",
                table: "ContestEntries");

            migrationBuilder.DropColumn(
                name: "PrizeTierId",
                table: "ContestEntries");

            migrationBuilder.DropColumn(
                name: "AwardTierName",
                table: "AquariumSnapshots");

            migrationBuilder.RenameColumn(
                name: "AwardTierLevel",
                table: "AquariumSnapshots",
                newName: "ContestAward");

            migrationBuilder.RenameIndex(
                name: "IX_AquariumSnapshots_IsActive_WaterType_Style_AwardTierLevel_L~",
                table: "AquariumSnapshots",
                newName: "IX_AquariumSnapshots_IsActive_WaterType_Style_ContestAward_Lik~");

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "ContestEntries",
                type: "integer",
                nullable: true);
        }
    }
}

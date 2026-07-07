using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotAndContest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    YouTubePlaylistId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AquariumSnapshotLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AquariumSnapshotLikes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AquariumSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AquariumId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WaterType = table.Column<int>(type: "integer", nullable: false),
                    Style = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    FishSpeciesCount = table.Column<int>(type: "integer", nullable: false),
                    ContestEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContestAward = table.Column<int>(type: "integer", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YoutubeVideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SnapshotData = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AquariumSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AquariumSnapshots_Aquariums_AquariumId",
                        column: x => x.AquariumId,
                        principalTable: "Aquariums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContestEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AquariumSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoR2Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VideoSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    VideoDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    YouTubeViewCount = table.Column<long>(type: "bigint", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContestEntries_AquariumSnapshots_AquariumSnapshotId",
                        column: x => x.AquariumSnapshotId,
                        principalTable: "AquariumSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestEntries_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AquariumSnapshotLikes_SnapshotId_UserId",
                table: "AquariumSnapshotLikes",
                columns: new[] { "SnapshotId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AquariumSnapshots_AquariumId",
                table: "AquariumSnapshots",
                column: "AquariumId");

            migrationBuilder.CreateIndex(
                name: "IX_AquariumSnapshots_ContestEntryId",
                table: "AquariumSnapshots",
                column: "ContestEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AquariumSnapshots_IsActive_WaterType_Style_ContestAward_Lik~",
                table: "AquariumSnapshots",
                columns: new[] { "IsActive", "WaterType", "Style", "ContestAward", "LikeCount" },
                descending: new[] { false, false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AquariumSnapshots_Slug",
                table: "AquariumSnapshots",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContestEntries_AquariumSnapshotId",
                table: "ContestEntries",
                column: "AquariumSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestEntries_ContestId",
                table: "ContestEntries",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestEntries_Status",
                table: "ContestEntries",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_AquariumSnapshotLikes_AquariumSnapshots_SnapshotId",
                table: "AquariumSnapshotLikes",
                column: "SnapshotId",
                principalTable: "AquariumSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AquariumSnapshots_ContestEntries_ContestEntryId",
                table: "AquariumSnapshots",
                column: "ContestEntryId",
                principalTable: "ContestEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestEntries_AquariumSnapshots_AquariumSnapshotId",
                table: "ContestEntries");

            migrationBuilder.DropTable(
                name: "AquariumSnapshotLikes");

            migrationBuilder.DropTable(
                name: "AquariumSnapshots");

            migrationBuilder.DropTable(
                name: "ContestEntries");

            migrationBuilder.DropTable(
                name: "Contests");
        }
    }
}

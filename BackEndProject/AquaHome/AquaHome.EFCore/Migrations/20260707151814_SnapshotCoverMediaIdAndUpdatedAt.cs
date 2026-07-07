using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotCoverMediaIdAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "AquariumSnapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "CoverMediaId",
                table: "AquariumSnapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AquariumSnapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverMediaId",
                table: "AquariumSnapshots");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AquariumSnapshots");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "AquariumSnapshots",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aquariums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LengthCm = table.Column<double>(type: "double precision", nullable: true),
                    WidthCm = table.Column<double>(type: "double precision", nullable: true),
                    HeightCm = table.Column<double>(type: "double precision", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aquariums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => new { x.UserId, x.SpecCode });
                });

            migrationBuilder.CreateTable(
                name: "AquariumFish",
                columns: table => new
                {
                    AquariumId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AquariumFish", x => new { x.AquariumId, x.SpecCode });
                    table.ForeignKey(
                        name: "FK_AquariumFish_Aquariums_AquariumId",
                        column: x => x.AquariumId,
                        principalTable: "Aquariums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AquariumFish");

            migrationBuilder.DropTable(
                name: "UserFavorites");

            migrationBuilder.DropTable(
                name: "Aquariums");
        }
    }
}

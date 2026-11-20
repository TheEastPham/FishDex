using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAquariumTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AquariumTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AquariumId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Reminded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AquariumTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AquariumTasks_Aquariums_AquariumId",
                        column: x => x.AquariumId,
                        principalTable: "Aquariums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AquariumTasks_AquariumId",
                table: "AquariumTasks",
                column: "AquariumId");

            migrationBuilder.CreateIndex(
                name: "IX_AquariumTasks_IsCompleted_Reminded_DueAt",
                table: "AquariumTasks",
                columns: new[] { "IsCompleted", "Reminded", "DueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AquariumTasks");
        }
    }
}

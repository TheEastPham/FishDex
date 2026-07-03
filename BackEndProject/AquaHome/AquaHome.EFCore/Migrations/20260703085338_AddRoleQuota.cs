using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuotaUsages",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotaType = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaUsages", x => new { x.UserId, x.QuotaType, x.Day });
                });

            migrationBuilder.CreateTable(
                name: "RoleQuotas",
                columns: table => new
                {
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxFavorites = table.Column<int>(type: "integer", nullable: false),
                    MaxAquariums = table.Column<int>(type: "integer", nullable: false),
                    SearchPerDay = table.Column<int>(type: "integer", nullable: false),
                    AiQaPerDay = table.Column<int>(type: "integer", nullable: false),
                    ImageSearchPerDay = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleQuotas", x => x.Role);
                });

            migrationBuilder.InsertData(
                table: "RoleQuotas",
                columns: new[] { "Role", "AiQaPerDay", "ImageSearchPerDay", "MaxAquariums", "MaxFavorites", "SearchPerDay", "UpdatedAt" },
                values: new object[,]
                {
                    { "ContentAdmin", -1, -1, -1, -1, -1, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Guest", 3, 3, 2, 10, 20, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Member", 15, 20, 10, 100, 115, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "SystemAdmin", -1, -1, -1, -1, -1, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotaUsages");

            migrationBuilder.DropTable(
                name: "RoleQuotas");
        }
    }
}

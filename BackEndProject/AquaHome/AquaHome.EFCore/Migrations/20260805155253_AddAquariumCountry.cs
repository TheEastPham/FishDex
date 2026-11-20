using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAquariumCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Aquariums",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            // Backfill Việt Nam ("704" = C_Code của FishBase) cho bể tạo trước khi có
            // tính năng chọn quốc gia. Thêm tay vì EF không sinh phần này.
            // Hệ quả có lợi: cá trong các bể hiện có sẽ thành lô dữ liệu market đầu tiên
            // cho Việt Nam khi worker gom lần đầu.
            migrationBuilder.Sql(@"UPDATE ""Aquariums"" SET ""CountryCode"" = '704' WHERE ""CountryCode"" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Aquariums_CountryCode",
                table: "Aquariums",
                column: "CountryCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Aquariums_CountryCode",
                table: "Aquariums");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Aquariums");
        }
    }
}

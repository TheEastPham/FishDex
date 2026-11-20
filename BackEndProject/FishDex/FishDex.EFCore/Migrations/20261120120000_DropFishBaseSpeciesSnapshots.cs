using FishDex.EFCore.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <summary>
    /// Bỏ cache FishBase trong <c>SpeciesSnapshots</c>. Species Profile giờ đọc thẳng bảng FishBase
    /// gốc, nên các dòng <c>DataSource = FishBase (0)</c> chỉ còn là bản sao cũ — được dựng bằng rule
    /// ảnh/ecology/tên đã lệch so với trang detail và không bao giờ được làm mới.
    ///
    /// Chỉ xoá dữ liệu, không đổi schema: bảng vẫn là nơi lưu chính của loài cộng đồng
    /// (<c>DataSource = Community (1)</c>), những dòng đó giữ nguyên.
    ///
    /// Migration chỉ có dữ liệu nên không có file Designer/model snapshot đi kèm — model không đổi.
    /// Down không làm gì: dữ liệu bị xoá là cache, không có gì để khôi phục.
    /// </summary>
    [DbContext(typeof(FishDexDbContext))]
    [Migration("20261120120000_DropFishBaseSpeciesSnapshots")]
    public partial class DropFishBaseSpeciesSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM "SpeciesSnapshots" WHERE "DataSource" = 0;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class EnableTrigramSearch : Migration
    {
        /// <summary>
        /// Bật pg_trgm để dò tên gần giống khi user submit loài lai.
        ///
        /// Phần lớn submission hybrid sai thực chất là loài đã biết dưới tên thương mại địa
        /// phương — bắt được nhóm đó bằng so khớp mờ thì đỡ cho admin, mà không cần mạng,
        /// không cần AI, không rủi ro bịa dữ liệu.
        ///
        /// Ba index GIN trigram phủ đúng ba nguồn cần đối chiếu: loài FishBase đã nạp,
        /// index toàn bộ FishBase, và các loài cộng đồng đã submit.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Species_SpeciesName_trgm""
                  ON ""Species"" USING gin (""SpeciesName"" gin_trgm_ops);");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_FishBaseSpeciesIndex_SpeciesName_trgm""
                  ON ""FishBaseSpeciesIndex"" USING gin (""SpeciesName"" gin_trgm_ops);");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_SpeciesSnapshots_SpeciesName_trgm""
                  ON ""SpeciesSnapshots"" USING gin (""SpeciesName"" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_SpeciesSnapshots_SpeciesName_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_FishBaseSpeciesIndex_SpeciesName_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Species_SpeciesName_trgm"";");

            // Không DROP EXTENSION: có thể thứ khác đang dùng, và bật lại thì rẻ.
        }
    }
}

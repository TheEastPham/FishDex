using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommonName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommonNames",
                columns: table => new
                {
                    AutoCtr = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    StockCode = table.Column<int>(type: "integer", nullable: true),
                    ComName = table.Column<string>(type: "text", nullable: false),
                    Transliteration = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    NameType = table.Column<string>(type: "text", nullable: true),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonNames", x => x.AutoCtr);
                    table.ForeignKey(
                        name: "FK_CommonNames_Species_SpecCode",
                        column: x => x.SpecCode,
                        principalTable: "Species",
                        principalColumn: "SpecCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommonNames_Language_IsPreferred",
                table: "CommonNames",
                columns: new[] { "Language", "IsPreferred" });

            migrationBuilder.CreateIndex(
                name: "IX_CommonNames_SpecCode",
                table: "CommonNames",
                column: "SpecCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommonNames");
        }
    }
}

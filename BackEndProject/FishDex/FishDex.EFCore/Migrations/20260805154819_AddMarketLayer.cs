using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FishBaseSpeciesIndex",
                columns: table => new
                {
                    SpecCode = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpeciesName = table.Column<string>(type: "text", nullable: false),
                    Genus = table.Column<string>(type: "text", nullable: true),
                    FamCode = table.Column<int>(type: "integer", nullable: true),
                    Fresh = table.Column<bool>(type: "boolean", nullable: false),
                    Brack = table.Column<bool>(type: "boolean", nullable: false),
                    Aquarium = table.Column<string>(type: "text", nullable: true),
                    IsLoaded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishBaseSpeciesIndex", x => x.SpecCode);
                });

            migrationBuilder.CreateTable(
                name: "TradedSpecies",
                columns: table => new
                {
                    CountryCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    TradeStatus = table.Column<int>(type: "integer", nullable: true),
                    LegalStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LegalNote = table.Column<string>(type: "text", nullable: true),
                    LegalSourceUrl = table.Column<string>(type: "text", nullable: true),
                    CareLevel = table.Column<int>(type: "integer", nullable: true),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AddedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradedSpecies", x => new { x.CountryCode, x.SpecCode });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FishBaseSpeciesIndex_IsLoaded",
                table: "FishBaseSpeciesIndex",
                column: "IsLoaded");

            migrationBuilder.CreateIndex(
                name: "IX_FishBaseSpeciesIndex_SpeciesName",
                table: "FishBaseSpeciesIndex",
                column: "SpeciesName");

            migrationBuilder.CreateIndex(
                name: "IX_TradedSpecies_CountryCode_Status_TradeStatus",
                table: "TradedSpecies",
                columns: new[] { "CountryCode", "Status", "TradeStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TradedSpecies_SpecCode",
                table: "TradedSpecies",
                column: "SpecCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FishBaseSpeciesIndex");

            migrationBuilder.DropTable(
                name: "TradedSpecies");
        }
    }
}

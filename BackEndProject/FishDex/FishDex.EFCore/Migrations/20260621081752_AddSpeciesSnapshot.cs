using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeciesSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeciesSnapshots",
                columns: table => new
                {
                    SpecCode = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataSource = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    SpeciesName = table.Column<string>(type: "text", nullable: false),
                    FamilyName = table.Column<string>(type: "text", nullable: true),
                    GenusName = table.Column<string>(type: "text", nullable: true),
                    CommonName = table.Column<string>(type: "text", nullable: true),
                    WaterType = table.Column<int>(type: "integer", nullable: false),
                    TempMin = table.Column<double>(type: "double precision", nullable: true),
                    TempMax = table.Column<double>(type: "double precision", nullable: true),
                    PhMin = table.Column<double>(type: "double precision", nullable: true),
                    PhMax = table.Column<double>(type: "double precision", nullable: true),
                    DhMin = table.Column<double>(type: "double precision", nullable: true),
                    DhMax = table.Column<double>(type: "double precision", nullable: true),
                    Length = table.Column<decimal>(type: "numeric", nullable: true),
                    LongevityCaptive = table.Column<double>(type: "double precision", nullable: true),
                    DemersPelag = table.Column<string>(type: "text", nullable: true),
                    Schooling = table.Column<bool>(type: "boolean", nullable: true),
                    Shoaling = table.Column<bool>(type: "boolean", nullable: true),
                    Solitary = table.Column<bool>(type: "boolean", nullable: true),
                    FeedingType = table.Column<string>(type: "text", nullable: true),
                    FeedingPosition = table.Column<string>(type: "text", nullable: true),
                    ActivityPattern = table.Column<string>(type: "text", nullable: true),
                    RequiresLiveFood = table.Column<bool>(type: "boolean", nullable: true),
                    Aggressiveness = table.Column<string>(type: "text", nullable: true),
                    FinNippingRisk = table.Column<bool>(type: "boolean", nullable: true),
                    JumpingRisk = table.Column<bool>(type: "boolean", nullable: true),
                    CareLevel = table.Column<int>(type: "integer", nullable: true),
                    MinTankLiters = table.Column<int>(type: "integer", nullable: true),
                    ThumbnailObjectKey = table.Column<string>(type: "text", nullable: true),
                    MaleImageObjectKey = table.Column<string>(type: "text", nullable: true),
                    FemaleImageObjectKey = table.Column<string>(type: "text", nullable: true),
                    ContributedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    PopulatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PopulatedFrom = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesSnapshots", x => x.SpecCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesSnapshots_DataSource",
                table: "SpeciesSnapshots",
                column: "DataSource");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesSnapshots_PopulatedAt",
                table: "SpeciesSnapshots",
                column: "PopulatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeciesSnapshots");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ecologies",
                columns: table => new
                {
                    EcologyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    StockCode = table.Column<string>(type: "text", nullable: false),
                    EcologyRefNo = table.Column<string>(type: "text", nullable: false),
                    autoctr = table.Column<int>(type: "integer", nullable: false),
                    Entered = table.Column<string>(type: "text", nullable: false),
                    Dateentered = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<string>(type: "text", nullable: false),
                    Datemodified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Expert = table.Column<string>(type: "text", nullable: false),
                    Datechecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TS = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ecologies", x => x.EcologyId);
                });

            migrationBuilder.CreateTable(
                name: "EcosystemRef",
                columns: table => new
                {
                    E_CODE = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcosystemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EcosystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NorthernLat = table.Column<double>(type: "double precision", nullable: true),
                    SouthernLat = table.Column<double>(type: "double precision", nullable: true),
                    WesternLat = table.Column<double>(type: "double precision", nullable: true),
                    EasternLat = table.Column<double>(type: "double precision", nullable: true),
                    Area = table.Column<double>(type: "double precision", nullable: true),
                    DrainageArea = table.Column<double>(type: "double precision", nullable: true),
                    RiverLength = table.Column<double>(type: "double precision", nullable: true),
                    Salinity = table.Column<double>(type: "double precision", nullable: true),
                    AverageDepth = table.Column<double>(type: "double precision", nullable: true),
                    MaxDepth = table.Column<double>(type: "double precision", nullable: true),
                    TempSurface = table.Column<double>(type: "double precision", nullable: true),
                    TempDepth = table.Column<double>(type: "double precision", nullable: true),
                    Polar = table.Column<bool>(type: "boolean", nullable: false),
                    Boreal = table.Column<bool>(type: "boolean", nullable: false),
                    Temperate = table.Column<bool>(type: "boolean", nullable: false),
                    Subtropical = table.Column<bool>(type: "boolean", nullable: false),
                    Tropical = table.Column<bool>(type: "boolean", nullable: false),
                    MEOW = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MPA = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalCount = table.Column<int>(type: "integer", nullable: true),
                    TotalFamCount = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcosystemRef", x => x.E_CODE);
                });

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamCode = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CommonName = table.Column<string>(type: "text", nullable: false),
                    BodyShapeI = table.Column<string>(type: "text", nullable: false),
                    SwimMode = table.Column<string>(type: "text", nullable: false),
                    ReproductiveGuild = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Occurrences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    Locality = table.Column<string>(type: "text", nullable: true),
                    Gazetteer = table.Column<string>(type: "text", nullable: true),
                    LatitudeDec = table.Column<double>(type: "double precision", nullable: false),
                    LongitudeDec = table.Column<double>(type: "double precision", nullable: false),
                    Province = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occurrences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    AssociationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    AssociationRef = table.Column<string>(type: "text", nullable: false),
                    Parasitism = table.Column<bool>(type: "boolean", nullable: false),
                    Solitary = table.Column<bool>(type: "boolean", nullable: false),
                    Symbiosis = table.Column<bool>(type: "boolean", nullable: false),
                    Symphorism = table.Column<bool>(type: "boolean", nullable: false),
                    Commensalism = table.Column<bool>(type: "boolean", nullable: false),
                    Mutualism = table.Column<bool>(type: "boolean", nullable: false),
                    Epiphytic = table.Column<bool>(type: "boolean", nullable: false),
                    Schooling = table.Column<bool>(type: "boolean", nullable: false),
                    SchoolingFrequency = table.Column<string>(type: "text", nullable: false),
                    SchoolingLifestage = table.Column<string>(type: "text", nullable: false),
                    Shoaling = table.Column<bool>(type: "boolean", nullable: false),
                    ShoalingFrequency = table.Column<string>(type: "text", nullable: false),
                    ShoalingLifestage = table.Column<string>(type: "text", nullable: false),
                    SchoolShoalRef = table.Column<string>(type: "text", nullable: false),
                    AssociationsWith = table.Column<string>(type: "text", nullable: false),
                    AssociationsRemarks = table.Column<string>(type: "text", nullable: false),
                    OutsideHost = table.Column<bool>(type: "boolean", nullable: false),
                    OHRemarks = table.Column<string>(type: "text", nullable: false),
                    InsideHost = table.Column<bool>(type: "boolean", nullable: false),
                    IHRemarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.AssociationId);
                    table.ForeignKey(
                        name: "FK_Associations_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CircadianBehaviors",
                columns: table => new
                {
                    CircadianId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    Circadian1 = table.Column<string>(type: "text", nullable: false),
                    Circadian2 = table.Column<string>(type: "text", nullable: false),
                    Circadian3 = table.Column<string>(type: "text", nullable: false),
                    BioAspect1 = table.Column<string>(type: "text", nullable: false),
                    BioAspect2 = table.Column<string>(type: "text", nullable: false),
                    BioAspect3 = table.Column<string>(type: "text", nullable: false),
                    RemarksCircadian = table.Column<string>(type: "text", nullable: false),
                    CircadianRef = table.Column<string>(type: "text", nullable: false),
                    CircadianAlsoRef = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircadianBehaviors", x => x.CircadianId);
                    table.ForeignKey(
                        name: "FK_CircadianBehaviors_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedingAndDiets",
                columns: table => new
                {
                    FeedingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    Herbivory2 = table.Column<bool>(type: "boolean", nullable: false),
                    HerbivoryRef = table.Column<string>(type: "text", nullable: false),
                    FeedingType = table.Column<string>(type: "text", nullable: false),
                    FeedingTypeRef = table.Column<string>(type: "text", nullable: false),
                    DietTroph = table.Column<decimal>(type: "numeric", nullable: false),
                    DietSeTroph = table.Column<decimal>(type: "numeric", nullable: false),
                    DietTLu = table.Column<decimal>(type: "numeric", nullable: false),
                    DietseTLu = table.Column<decimal>(type: "numeric", nullable: false),
                    DietRemark = table.Column<string>(type: "text", nullable: false),
                    DietRef = table.Column<string>(type: "text", nullable: false),
                    FoodTroph = table.Column<decimal>(type: "numeric", nullable: false),
                    FoodSeTroph = table.Column<decimal>(type: "numeric", nullable: false),
                    FoodRemark = table.Column<string>(type: "text", nullable: false),
                    FoodRef = table.Column<string>(type: "text", nullable: false),
                    AddRems = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedingAndDiets", x => x.FeedingId);
                    table.ForeignKey(
                        name: "FK_FeedingAndDiets_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitatZones",
                columns: table => new
                {
                    HabitatZoneId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    HabitatsRef = table.Column<string>(type: "text", nullable: false),
                    Neritic = table.Column<bool>(type: "boolean", nullable: false),
                    SupraLittoralZone = table.Column<bool>(type: "boolean", nullable: false),
                    Saltmarshes = table.Column<bool>(type: "boolean", nullable: false),
                    LittoralZone = table.Column<bool>(type: "boolean", nullable: false),
                    TidePools = table.Column<bool>(type: "boolean", nullable: false),
                    Intertidal = table.Column<bool>(type: "boolean", nullable: false),
                    SubLittoral = table.Column<bool>(type: "boolean", nullable: false),
                    Caves = table.Column<bool>(type: "boolean", nullable: false),
                    Oceanic = table.Column<bool>(type: "boolean", nullable: false),
                    Epipelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Mesopelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Bathypelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Abyssopelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Hadopelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Estuaries = table.Column<bool>(type: "boolean", nullable: false),
                    Mangroves = table.Column<bool>(type: "boolean", nullable: false),
                    MarshesSwamps = table.Column<bool>(type: "boolean", nullable: false),
                    CaveAnchialine = table.Column<bool>(type: "boolean", nullable: false),
                    Stream = table.Column<bool>(type: "boolean", nullable: false),
                    Lakes = table.Column<bool>(type: "boolean", nullable: false),
                    Cave = table.Column<bool>(type: "boolean", nullable: false),
                    Cave2 = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitatZones", x => x.HabitatZoneId);
                    table.ForeignKey(
                        name: "FK_HabitatZones_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialHabitats",
                columns: table => new
                {
                    SpecialHabitatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    SpecialHabitatRef = table.Column<string>(type: "text", nullable: false),
                    Macrophyte = table.Column<bool>(type: "boolean", nullable: false),
                    BedsBivalve = table.Column<bool>(type: "boolean", nullable: false),
                    BedsRock = table.Column<bool>(type: "boolean", nullable: false),
                    SeaGrassBeds = table.Column<bool>(type: "boolean", nullable: false),
                    BedsOthers = table.Column<bool>(type: "boolean", nullable: false),
                    CoralReefs = table.Column<bool>(type: "boolean", nullable: false),
                    ReefExclusive = table.Column<bool>(type: "boolean", nullable: false),
                    DropOffs = table.Column<bool>(type: "boolean", nullable: false),
                    ReefFlats = table.Column<bool>(type: "boolean", nullable: false),
                    Lagoons = table.Column<bool>(type: "boolean", nullable: false),
                    Burrows = table.Column<bool>(type: "boolean", nullable: false),
                    Tunnels = table.Column<bool>(type: "boolean", nullable: false),
                    Guyots = table.Column<bool>(type: "boolean", nullable: false),
                    Crevices = table.Column<bool>(type: "boolean", nullable: false),
                    Seamounts = table.Column<bool>(type: "boolean", nullable: false),
                    ColdSeeps = table.Column<bool>(type: "boolean", nullable: false),
                    HydrothermalVents = table.Column<bool>(type: "boolean", nullable: false),
                    DeepWaterCorals = table.Column<bool>(type: "boolean", nullable: false),
                    Vegetation = table.Column<bool>(type: "boolean", nullable: false),
                    Leaves = table.Column<bool>(type: "boolean", nullable: false),
                    Stems = table.Column<bool>(type: "boolean", nullable: false),
                    Roots = table.Column<bool>(type: "boolean", nullable: false),
                    Driftwood = table.Column<bool>(type: "boolean", nullable: false),
                    OInverterbrates = table.Column<bool>(type: "boolean", nullable: false),
                    OIRemarks = table.Column<string>(type: "text", nullable: false),
                    Verterbrates = table.Column<bool>(type: "boolean", nullable: false),
                    VRemarks = table.Column<string>(type: "text", nullable: false),
                    Pilings = table.Column<bool>(type: "boolean", nullable: false),
                    RicePaddies = table.Column<bool>(type: "boolean", nullable: false),
                    BoatHulls = table.Column<bool>(type: "boolean", nullable: false),
                    Corals = table.Column<bool>(type: "boolean", nullable: false),
                    SoftCorals = table.Column<bool>(type: "boolean", nullable: false),
                    OnPolyp = table.Column<bool>(type: "boolean", nullable: false),
                    BetweenPolyps = table.Column<bool>(type: "boolean", nullable: false),
                    HardCorals = table.Column<bool>(type: "boolean", nullable: false),
                    OnExoskeleton = table.Column<bool>(type: "boolean", nullable: false),
                    InterstitialSpaces = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialHabitats", x => x.SpecialHabitatId);
                    table.ForeignKey(
                        name: "FK_SpecialHabitats_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Substrates",
                columns: table => new
                {
                    SubstrateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcologyId = table.Column<int>(type: "integer", nullable: false),
                    SubstrateRef = table.Column<string>(type: "text", nullable: false),
                    Benthic = table.Column<bool>(type: "boolean", nullable: false),
                    Sessile = table.Column<bool>(type: "boolean", nullable: false),
                    Mobile = table.Column<bool>(type: "boolean", nullable: false),
                    Demersal = table.Column<bool>(type: "boolean", nullable: false),
                    Endofauna = table.Column<bool>(type: "boolean", nullable: false),
                    Pelagic = table.Column<bool>(type: "boolean", nullable: false),
                    Megabenthos = table.Column<bool>(type: "boolean", nullable: false),
                    Macrobenthos = table.Column<bool>(type: "boolean", nullable: false),
                    Meiobenthos = table.Column<bool>(type: "boolean", nullable: false),
                    SoftBottom = table.Column<bool>(type: "boolean", nullable: false),
                    Sand = table.Column<bool>(type: "boolean", nullable: false),
                    Coarse = table.Column<bool>(type: "boolean", nullable: false),
                    Fine = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<bool>(type: "boolean", nullable: false),
                    Sloping = table.Column<bool>(type: "boolean", nullable: false),
                    Silt = table.Column<bool>(type: "boolean", nullable: false),
                    Mud = table.Column<bool>(type: "boolean", nullable: false),
                    Ooze = table.Column<bool>(type: "boolean", nullable: false),
                    Detritus = table.Column<bool>(type: "boolean", nullable: false),
                    Organic = table.Column<bool>(type: "boolean", nullable: false),
                    HardBottom = table.Column<bool>(type: "boolean", nullable: false),
                    Rocky = table.Column<bool>(type: "boolean", nullable: false),
                    Rubble = table.Column<bool>(type: "boolean", nullable: false),
                    Gravel = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Substrates", x => x.SubstrateId);
                    table.ForeignKey(
                        name: "FK_Substrates_Ecologies_EcologyId",
                        column: x => x.EcologyId,
                        principalTable: "Ecologies",
                        principalColumn: "EcologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ecosystem",
                columns: table => new
                {
                    AutoCtr = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    E_CODE = table.Column<int>(type: "integer", nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    StockCode = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentPresence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Abundance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LifeStage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    EcosystemRefNo = table.Column<int>(type: "integer", nullable: true),
                    Entered = table.Column<string>(type: "text", nullable: true),
                    Dateentered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Modified = table.Column<string>(type: "text", nullable: true),
                    Datemodified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TS = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ecosystem", x => x.AutoCtr);
                    table.ForeignKey(
                        name: "FK_Ecosystem_EcosystemRef_E_CODE",
                        column: x => x.E_CODE,
                        principalTable: "EcosystemRef",
                        principalColumn: "E_CODE",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Genuses",
                columns: table => new
                {
                    GenusCode = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenusName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genuses", x => x.GenusCode);
                    table.ForeignKey(
                        name: "FK_Genuses_Families_FamId",
                        column: x => x.FamId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    GenusCode = table.Column<int>(type: "integer", nullable: false),
                    FamCode = table.Column<int>(type: "integer", nullable: false),
                    FamId = table.Column<Guid>(type: "uuid", nullable: false),
                    WaterType = table.Column<int>(type: "integer", nullable: false),
                    SpeciesName = table.Column<string>(type: "text", nullable: false),
                    SpeciesRefNo = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    BodyShapeI = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    AuthorRef = table.Column<string>(type: "text", nullable: false),
                    Remark = table.Column<string>(type: "text", nullable: false),
                    TaxIssue = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<decimal>(type: "numeric", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    Dangerous = table.Column<string>(type: "text", nullable: false),
                    Vulnerability = table.Column<int>(type: "integer", nullable: true),
                    VulnerabilityClimate = table.Column<int>(type: "integer", nullable: true),
                    AirBreathing = table.Column<string>(type: "text", nullable: false),
                    LifeCycle = table.Column<string>(type: "text", nullable: false),
                    DemersPelag = table.Column<string>(type: "text", nullable: false),
                    MaxLengthRef = table.Column<string>(type: "text", nullable: false),
                    LengthFemale = table.Column<double>(type: "double precision", nullable: true),
                    LongevityWild = table.Column<double>(type: "double precision", nullable: true),
                    PicPreferredNameM = table.Column<string>(type: "text", nullable: false),
                    PicPreferredNameF = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.Id);
                    table.UniqueConstraint("AK_Species_SpecCode", x => x.SpecCode);
                    table.ForeignKey(
                        name: "FK_Species_Families_FamId",
                        column: x => x.FamId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Species_Genuses_GenusCode",
                        column: x => x.GenusCode,
                        principalTable: "Genuses",
                        principalColumn: "GenusCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    SynOC = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StockDefs = table.Column<string>(type: "text", nullable: false),
                    StockDefsGeneral = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocalUnique = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_Stock_Species_SpecCode",
                        column: x => x.SpecCode,
                        principalTable: "Species",
                        principalColumn: "SpecCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecCode = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PictureType = table.Column<string>(type: "text", nullable: false),
                    LifeStage = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<double>(type: "double precision", nullable: true),
                    LengthType = table.Column<string>(type: "text", nullable: true),
                    BestPic = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    PicPreferred = table.Column<bool>(type: "boolean", nullable: true),
                    PicPreferredMale = table.Column<bool>(type: "boolean", nullable: true),
                    PicPreferredFem = table.Column<bool>(type: "boolean", nullable: true),
                    PicPreferredJuv = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemImages_Species_SpecCode",
                        column: x => x.SpecCode,
                        principalTable: "Species",
                        principalColumn: "SpecCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MorphData",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    Speccode = table.Column<int>(type: "integer", nullable: true),
                    MorphDatRefNo = table.Column<string>(type: "text", nullable: false),
                    AppearancePic = table.Column<string>(type: "text", nullable: false),
                    EaseofID = table.Column<string>(type: "text", nullable: false),
                    BodyShapeI = table.Column<string>(type: "text", nullable: false),
                    BodyShapeII = table.Column<string>(type: "text", nullable: false),
                    Forehead = table.Column<string>(type: "text", nullable: false),
                    OperculumPresent = table.Column<string>(type: "text", nullable: false),
                    TypeofEyes = table.Column<string>(type: "text", nullable: false),
                    TypeofMouth = table.Column<string>(type: "text", nullable: false),
                    PosofMouth = table.Column<string>(type: "text", nullable: false),
                    GasBladder = table.Column<string>(type: "text", nullable: false),
                    SexualAttributes = table.Column<string>(type: "text", nullable: false),
                    SexMorphology = table.Column<string>(type: "text", nullable: false),
                    RemarkSex = table.Column<string>(type: "text", nullable: false),
                    Females = table.Column<int>(type: "integer", nullable: true),
                    Males = table.Column<int>(type: "integer", nullable: true),
                    Entered = table.Column<int>(type: "integer", nullable: true),
                    DateEntered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Modified = table.Column<int>(type: "integer", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Expert = table.Column<int>(type: "integer", nullable: true),
                    DateChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TS = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphData", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphData_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockConservation",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    IUCN_Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IUCN_Assessment = table.Column<string>(type: "text", nullable: false),
                    IUCN_DateAssessed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IUCN_ID = table.Column<int>(type: "integer", nullable: true),
                    IUCN_IDAssess = table.Column<int>(type: "integer", nullable: true),
                    Protected = table.Column<bool>(type: "boolean", nullable: false),
                    CITES_Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CITES_Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CITES_Ref = table.Column<string>(type: "text", nullable: false),
                    CITES_Remarks = table.Column<string>(type: "text", nullable: false),
                    CMS = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockConservation", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_StockConservation_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockDataAvailability",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    Aquamaps = table.Column<string>(type: "text", nullable: false),
                    SCRFA_data = table.Column<string>(type: "text", nullable: false),
                    Morphology = table.Column<bool>(type: "boolean", nullable: true),
                    Vision = table.Column<string>(type: "text", nullable: false),
                    Brains = table.Column<string>(type: "text", nullable: false),
                    Gillarea = table.Column<string>(type: "text", nullable: false),
                    Speed = table.Column<string>(type: "text", nullable: false),
                    Metabolism = table.Column<string>(type: "text", nullable: false),
                    Abnorm = table.Column<string>(type: "text", nullable: false),
                    Ecology = table.Column<string>(type: "text", nullable: false),
                    Occurrence = table.Column<string>(type: "text", nullable: false),
                    Abundance = table.Column<string>(type: "text", nullable: false),
                    PopDyn = table.Column<string>(type: "text", nullable: false),
                    Ecotoxicology = table.Column<string>(type: "text", nullable: false),
                    Reproduction = table.Column<string>(type: "text", nullable: false),
                    Spawning = table.Column<string>(type: "text", nullable: false),
                    Fecundity = table.Column<string>(type: "text", nullable: false),
                    Maturity = table.Column<string>(type: "text", nullable: false),
                    MatSizes = table.Column<string>(type: "text", nullable: false),
                    Eggs = table.Column<string>(type: "text", nullable: false),
                    EggDevelop = table.Column<string>(type: "text", nullable: false),
                    Larvae = table.Column<string>(type: "text", nullable: false),
                    LarvDyn = table.Column<string>(type: "text", nullable: false),
                    LarvSpeed = table.Column<string>(type: "text", nullable: false),
                    Diet = table.Column<string>(type: "text", nullable: false),
                    Food = table.Column<string>(type: "text", nullable: false),
                    Foods = table.Column<string>(type: "text", nullable: false),
                    Ration = table.Column<string>(type: "text", nullable: false),
                    Predators = table.Column<string>(type: "text", nullable: false),
                    Genetics = table.Column<string>(type: "text", nullable: false),
                    Allele = table.Column<string>(type: "text", nullable: false),
                    GeneticStudies = table.Column<string>(type: "text", nullable: false),
                    Strains = table.Column<string>(type: "text", nullable: false),
                    Aquaculture = table.Column<string>(type: "text", nullable: false),
                    FAOAqua = table.Column<string>(type: "text", nullable: false),
                    Catches = table.Column<string>(type: "text", nullable: false),
                    Processing = table.Column<string>(type: "text", nullable: false),
                    Introductions = table.Column<string>(type: "text", nullable: false),
                    CountryComp = table.Column<string>(type: "text", nullable: false),
                    Broodstock = table.Column<string>(type: "text", nullable: false),
                    EggNursery = table.Column<string>(type: "text", nullable: false),
                    FryNursery = table.Column<string>(type: "text", nullable: false),
                    LarvalNursery = table.Column<string>(type: "text", nullable: false),
                    LengthWeight = table.Column<string>(type: "text", nullable: false),
                    LengthRelations = table.Column<string>(type: "text", nullable: false),
                    LengthFrequency = table.Column<string>(type: "text", nullable: false),
                    Sounds = table.Column<string>(type: "text", nullable: false),
                    FishSounds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDataAvailability", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_StockDataAvailability_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockEnvironment",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    Northernmost = table.Column<double>(type: "double precision", nullable: true),
                    NorthSouthN = table.Column<string>(type: "text", nullable: false),
                    Southermost = table.Column<double>(type: "double precision", nullable: true),
                    NorthSouthS = table.Column<string>(type: "text", nullable: false),
                    Westernmost = table.Column<double>(type: "double precision", nullable: true),
                    WestEastW = table.Column<string>(type: "text", nullable: false),
                    Easternmost = table.Column<double>(type: "double precision", nullable: true),
                    WestEastE = table.Column<string>(type: "text", nullable: false),
                    BoundingRef = table.Column<string>(type: "text", nullable: false),
                    BoundingMethod = table.Column<string>(type: "text", nullable: false),
                    TempMin = table.Column<double>(type: "double precision", nullable: true),
                    TempMax = table.Column<double>(type: "double precision", nullable: true),
                    TempPreferred = table.Column<double>(type: "double precision", nullable: true),
                    TempPref25 = table.Column<double>(type: "double precision", nullable: true),
                    TempPref50 = table.Column<double>(type: "double precision", nullable: true),
                    TempPref75 = table.Column<double>(type: "double precision", nullable: true),
                    EnvTemp = table.Column<double>(type: "double precision", nullable: true),
                    PHMin = table.Column<double>(type: "double precision", nullable: true),
                    PHMax = table.Column<double>(type: "double precision", nullable: true),
                    DHMin = table.Column<double>(type: "double precision", nullable: true),
                    DHMax = table.Column<double>(type: "double precision", nullable: true),
                    Resilience = table.Column<int>(type: "integer", nullable: true),
                    ResilienceRemark = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockEnvironment", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_StockEnvironment_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockExternalRefs",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    GenBankID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RfeID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FIGIS_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EcotoxID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GMAD_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SAUP_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SAUP_Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SAUP = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BOLD_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MitoRef = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AusMuseum = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FishTrace = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IGFAName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EssayID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ICESStockID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OsteoBaseID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DORIS_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FishipediaID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SocotraAtlasID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AFORO_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FishSounds_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StocksRefNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockExternalRefs", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_StockExternalRefs_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMetadata",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    Entered = table.Column<int>(type: "integer", nullable: true),
                    DateEntered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Modified = table.Column<int>(type: "integer", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Expert = table.Column<int>(type: "integer", nullable: true),
                    DateChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TS = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMetadata", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_StockMetadata_Stock_StockCode",
                        column: x => x.StockCode,
                        principalTable: "Stock",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorphFins",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    Dfinno = table.Column<int>(type: "integer", nullable: true),
                    DorsalFinI = table.Column<string>(type: "text", nullable: false),
                    DorsalFinII = table.Column<string>(type: "text", nullable: false),
                    DorsalAttributes = table.Column<string>(type: "text", nullable: false),
                    DorsalSpinesMin = table.Column<int>(type: "integer", nullable: true),
                    DorsalSpinesMax = table.Column<int>(type: "integer", nullable: true),
                    DorsalSoftRaysMin = table.Column<int>(type: "integer", nullable: true),
                    DorsalSoftRaysMax = table.Column<int>(type: "integer", nullable: true),
                    Notched = table.Column<string>(type: "text", nullable: false),
                    DFinletsmin = table.Column<int>(type: "integer", nullable: true),
                    DFinletsmax = table.Column<int>(type: "integer", nullable: true),
                    Adifin = table.Column<string>(type: "text", nullable: false),
                    Afinno = table.Column<int>(type: "integer", nullable: true),
                    AnalFinI = table.Column<string>(type: "text", nullable: false),
                    AnalFinII = table.Column<string>(type: "text", nullable: false),
                    AnalFinSpinesMin = table.Column<int>(type: "integer", nullable: true),
                    AnalFinSpinesMax = table.Column<int>(type: "integer", nullable: true),
                    Araymin = table.Column<int>(type: "integer", nullable: true),
                    Araymax = table.Column<int>(type: "integer", nullable: true),
                    PectoralAttributes = table.Column<string>(type: "text", nullable: false),
                    Pspines2 = table.Column<string>(type: "text", nullable: false),
                    Praymin = table.Column<int>(type: "integer", nullable: true),
                    Praymax = table.Column<int>(type: "integer", nullable: true),
                    PelvicsAttributes = table.Column<string>(type: "text", nullable: false),
                    VPosition = table.Column<string>(type: "text", nullable: false),
                    VPosition2 = table.Column<string>(type: "text", nullable: false),
                    Vspines = table.Column<string>(type: "text", nullable: false),
                    Vraymin = table.Column<int>(type: "integer", nullable: true),
                    Vraymax = table.Column<int>(type: "integer", nullable: true),
                    CaudalFinI = table.Column<string>(type: "text", nullable: false),
                    CaudalFinII = table.Column<string>(type: "text", nullable: false),
                    CShape = table.Column<string>(type: "text", nullable: false),
                    Attributes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphFins", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphFins_MorphData_StockCode",
                        column: x => x.StockCode,
                        principalTable: "MorphData",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorphMeristics",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    TypeofScales = table.Column<string>(type: "text", nullable: false),
                    Scutes = table.Column<string>(type: "text", nullable: false),
                    Keels = table.Column<string>(type: "text", nullable: false),
                    LateralLinesNo = table.Column<int>(type: "integer", nullable: true),
                    LLinterrupted = table.Column<string>(type: "text", nullable: false),
                    ScalesLateralmin = table.Column<int>(type: "integer", nullable: true),
                    ScalesLateralmax = table.Column<int>(type: "integer", nullable: true),
                    PoredScalesMin = table.Column<int>(type: "integer", nullable: true),
                    PoredScalesMax = table.Column<int>(type: "integer", nullable: true),
                    LatSeriesMin = table.Column<int>(type: "integer", nullable: true),
                    LatSeriesMax = table.Column<int>(type: "integer", nullable: true),
                    ScaleRowsAboveMin = table.Column<int>(type: "integer", nullable: true),
                    ScaleRowsAboveMax = table.Column<int>(type: "integer", nullable: true),
                    ScaleRowsBelowMin = table.Column<int>(type: "integer", nullable: true),
                    ScaleRowsBelowMax = table.Column<int>(type: "integer", nullable: true),
                    ScalesPeduncMin = table.Column<int>(type: "integer", nullable: true),
                    ScalesPeduncMax = table.Column<int>(type: "integer", nullable: true),
                    BarbelsNo = table.Column<int>(type: "integer", nullable: true),
                    BarbelsType = table.Column<string>(type: "text", nullable: false),
                    GillCleftsNo = table.Column<int>(type: "integer", nullable: true),
                    Spiracle = table.Column<string>(type: "text", nullable: false),
                    GillRakersLowMin = table.Column<int>(type: "integer", nullable: true),
                    GillRakersLowMax = table.Column<int>(type: "integer", nullable: true),
                    GillRakersUpMin = table.Column<int>(type: "integer", nullable: true),
                    GillRakersUpMax = table.Column<int>(type: "integer", nullable: true),
                    GillRakersTotalMin = table.Column<int>(type: "integer", nullable: true),
                    GillRakersTotalMax = table.Column<int>(type: "integer", nullable: true),
                    Vertebrae = table.Column<string>(type: "text", nullable: false),
                    VertebraePreanMin = table.Column<int>(type: "integer", nullable: true),
                    VertebraePreanMax = table.Column<int>(type: "integer", nullable: true),
                    VertebraeTotalMin = table.Column<int>(type: "integer", nullable: true),
                    VertebraeTotalMax = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphMeristics", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphMeristics_MorphData_StockCode",
                        column: x => x.StockCode,
                        principalTable: "MorphData",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorphMetrics",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    StandardLengthCm = table.Column<double>(type: "double precision", nullable: true),
                    Forklength = table.Column<double>(type: "double precision", nullable: true),
                    Totallength = table.Column<double>(type: "double precision", nullable: true),
                    HeadLength = table.Column<double>(type: "double precision", nullable: true),
                    PreDorsalLength = table.Column<double>(type: "double precision", nullable: true),
                    PrePelvicsLength = table.Column<double>(type: "double precision", nullable: true),
                    PreAnalLength = table.Column<double>(type: "double precision", nullable: true),
                    PreorbitalLength = table.Column<double>(type: "double precision", nullable: true),
                    EyeLength = table.Column<double>(type: "double precision", nullable: true),
                    PeduncleLength = table.Column<double>(type: "double precision", nullable: true),
                    PostHeadDepth = table.Column<double>(type: "double precision", nullable: true),
                    PostTrunkDepth = table.Column<double>(type: "double precision", nullable: true),
                    MaximumDepth = table.Column<double>(type: "double precision", nullable: true),
                    PeduncleDepth = table.Column<double>(type: "double precision", nullable: true),
                    CaudalHeight = table.Column<double>(type: "double precision", nullable: true),
                    SimilarSpecies1 = table.Column<string>(type: "text", nullable: false),
                    SimilarSpec1Remarks = table.Column<string>(type: "text", nullable: false),
                    SimilarSpecies2 = table.Column<string>(type: "text", nullable: false),
                    SimilarSpec2Remarks = table.Column<string>(type: "text", nullable: false),
                    OtherRef1 = table.Column<string>(type: "text", nullable: false),
                    OtherRef2 = table.Column<string>(type: "text", nullable: false),
                    AddChars = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphMetrics", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphMetrics_MorphData_StockCode",
                        column: x => x.StockCode,
                        principalTable: "MorphData",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorphPigmentation",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    SexColors = table.Column<string>(type: "text", nullable: false),
                    StrikingFeatures = table.Column<string>(type: "text", nullable: false),
                    HorStripesTTI = table.Column<string>(type: "text", nullable: false),
                    HorStripesTTII = table.Column<string>(type: "text", nullable: false),
                    VerStripesTTI = table.Column<string>(type: "text", nullable: false),
                    VerStripesTTII = table.Column<string>(type: "text", nullable: false),
                    VerStripesTTIII = table.Column<string>(type: "text", nullable: false),
                    DiaStripesTTI = table.Column<string>(type: "text", nullable: false),
                    DiaStripesTTII = table.Column<string>(type: "text", nullable: false),
                    DiaStripesTTIII = table.Column<string>(type: "text", nullable: false),
                    CurStripesTTI = table.Column<string>(type: "text", nullable: false),
                    CurStripesTTII = table.Column<string>(type: "text", nullable: false),
                    CurStripesTTIII = table.Column<string>(type: "text", nullable: false),
                    SpotsTTI = table.Column<string>(type: "text", nullable: false),
                    SpotsTTII = table.Column<string>(type: "text", nullable: false),
                    SpotsTTIII = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphPigmentation", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphPigmentation_MorphData_StockCode",
                        column: x => x.StockCode,
                        principalTable: "MorphData",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorphTeeth",
                columns: table => new
                {
                    StockCode = table.Column<int>(type: "integer", nullable: false),
                    MandibleTeeth = table.Column<string>(type: "text", nullable: false),
                    MandibleTeethT1 = table.Column<string>(type: "text", nullable: false),
                    MandibleTeethT2 = table.Column<string>(type: "text", nullable: false),
                    MandibleRowsMin = table.Column<string>(type: "text", nullable: false),
                    MandibleRowsMax = table.Column<string>(type: "text", nullable: false),
                    MaxillaTeeth = table.Column<string>(type: "text", nullable: false),
                    MaxillaTeethT1 = table.Column<string>(type: "text", nullable: false),
                    MaxillaTeethT2 = table.Column<string>(type: "text", nullable: false),
                    MaxillaRowsMin = table.Column<string>(type: "text", nullable: false),
                    MaxillaRowsMax = table.Column<string>(type: "text", nullable: false),
                    VomerineTeeth = table.Column<string>(type: "text", nullable: false),
                    VomerineTeethT1 = table.Column<string>(type: "text", nullable: false),
                    VomerineTeethT2 = table.Column<string>(type: "text", nullable: false),
                    VomerineRowsMin = table.Column<string>(type: "text", nullable: false),
                    VomerineRowsMax = table.Column<string>(type: "text", nullable: false),
                    Palatine = table.Column<string>(type: "text", nullable: false),
                    PalatineTeethT1 = table.Column<string>(type: "text", nullable: false),
                    PalatineTeethT2 = table.Column<string>(type: "text", nullable: false),
                    PalatineRowsMin = table.Column<string>(type: "text", nullable: false),
                    PalatineRowsMax = table.Column<string>(type: "text", nullable: false),
                    PharyngealTeeth = table.Column<string>(type: "text", nullable: false),
                    PharyngealTeethT1 = table.Column<string>(type: "text", nullable: false),
                    PharyngealTeethT2 = table.Column<string>(type: "text", nullable: false),
                    PharyngealRowsMin = table.Column<string>(type: "text", nullable: false),
                    PharyngealRowsMax = table.Column<string>(type: "text", nullable: false),
                    TeethonTongue = table.Column<string>(type: "text", nullable: false),
                    Lipsteeth = table.Column<string>(type: "text", nullable: false),
                    DermalTeeth = table.Column<string>(type: "text", nullable: false),
                    CommentonTeeth = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphTeeth", x => x.StockCode);
                    table.ForeignKey(
                        name: "FK_MorphTeeth_MorphData_StockCode",
                        column: x => x.StockCode,
                        principalTable: "MorphData",
                        principalColumn: "StockCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Associations_EcologyId",
                table: "Associations",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircadianBehaviors_EcologyId",
                table: "CircadianBehaviors",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ecologies_SpecCode",
                table: "Ecologies",
                column: "SpecCode");

            migrationBuilder.CreateIndex(
                name: "IX_Ecosystem_E_CODE",
                table: "Ecosystem",
                column: "E_CODE");

            migrationBuilder.CreateIndex(
                name: "IX_Ecosystem_SpecCode",
                table: "Ecosystem",
                column: "SpecCode");

            migrationBuilder.CreateIndex(
                name: "IX_Families_FamCode",
                table: "Families",
                column: "FamCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedingAndDiets_EcologyId",
                table: "FeedingAndDiets",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genuses_FamId",
                table: "Genuses",
                column: "FamId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitatZones_EcologyId",
                table: "HabitatZones",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_SpecCode",
                table: "Occurrences",
                column: "SpecCode");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialHabitats_EcologyId",
                table: "SpecialHabitats",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Species_FamId",
                table: "Species",
                column: "FamId");

            migrationBuilder.CreateIndex(
                name: "IX_Species_GenusCode",
                table: "Species",
                column: "GenusCode");

            migrationBuilder.CreateIndex(
                name: "IX_Species_SpecCode",
                table: "Species",
                column: "SpecCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_SpecCode",
                table: "Stock",
                column: "SpecCode");

            migrationBuilder.CreateIndex(
                name: "IX_Substrates_EcologyId",
                table: "Substrates",
                column: "EcologyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemImages_SpecCode",
                table: "SystemImages",
                column: "SpecCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropTable(
                name: "CircadianBehaviors");

            migrationBuilder.DropTable(
                name: "Ecosystem");

            migrationBuilder.DropTable(
                name: "FeedingAndDiets");

            migrationBuilder.DropTable(
                name: "HabitatZones");

            migrationBuilder.DropTable(
                name: "MorphFins");

            migrationBuilder.DropTable(
                name: "MorphMeristics");

            migrationBuilder.DropTable(
                name: "MorphMetrics");

            migrationBuilder.DropTable(
                name: "MorphPigmentation");

            migrationBuilder.DropTable(
                name: "MorphTeeth");

            migrationBuilder.DropTable(
                name: "Occurrences");

            migrationBuilder.DropTable(
                name: "SpecialHabitats");

            migrationBuilder.DropTable(
                name: "StockConservation");

            migrationBuilder.DropTable(
                name: "StockDataAvailability");

            migrationBuilder.DropTable(
                name: "StockEnvironment");

            migrationBuilder.DropTable(
                name: "StockExternalRefs");

            migrationBuilder.DropTable(
                name: "StockMetadata");

            migrationBuilder.DropTable(
                name: "Substrates");

            migrationBuilder.DropTable(
                name: "SystemImages");

            migrationBuilder.DropTable(
                name: "EcosystemRef");

            migrationBuilder.DropTable(
                name: "MorphData");

            migrationBuilder.DropTable(
                name: "Ecologies");

            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "Species");

            migrationBuilder.DropTable(
                name: "Genuses");

            migrationBuilder.DropTable(
                name: "Families");
        }
    }
}

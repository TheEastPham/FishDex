// Bảng cờ (Flags) hoặc tóm tắt xem Stock này có dữ liệu ở các mảng nào

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Stocks;

[Table("StockDataAvailability")]
public class StockDataAvailability
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; }

    // --- Maps & Distributions ---
    public string Aquamaps { get; set; } = null!;
    public string SCRFA_data { get; set; } = null!; // Spawning Aggregations

    // --- Biological Characteristics (Morphology & Physiology) ---
    public bool? Morphology { get; set; }
    public string Vision { get; set; } = null!;
    public string Brains { get; set; } = null!;
    public string Gillarea { get; set; } = null!;
    public string Speed { get; set; } = null!; // Swimming speed
    public string Metabolism { get; set; } = null!;
    public string Abnorm { get; set; } = null!; // Abnormalities

    // --- Ecology & Life History ---
    public string Ecology { get; set; } = null!;
    public string Occurrence { get; set; } = null!;
    public string Abundance { get; set; } = null!;
    public string PopDyn { get; set; } = null!; // Population dynamics
    public string Ecotoxicology { get; set; } = null!; // Dữ liệu độc học (khác với EcotoxID)

    // --- Reproduction & Development ---
    public string Reproduction { get; set; } = null!;
    public string Spawning { get; set; } = null!;
    public string Fecundity { get; set; } = null!;
    public string Maturity { get; set; } = null!;
    public string MatSizes { get; set; } = null!;
    public string Eggs { get; set; } = null!;
    public string EggDevelop { get; set; } = null!;
    public string Larvae { get; set; } = null!;
    public string LarvDyn { get; set; } = null!;
    public string LarvSpeed { get; set; } = null!;
    
    // --- Diet & Trophic ---
    public string Diet { get; set; } = null!;
    public string Food { get; set; } = null!; // Linked to foodecosystemtype
    public string Foods { get; set; } = null!; // Specific food items
    public string Ration { get; set; } = null!;
    public string Predators { get; set; } = null!;

    // --- Genetics ---
    public string Genetics { get; set; } = null!;
    public string Allele { get; set; } = null!;
    public string GeneticStudies { get; set; } = null!;
    public string Strains { get; set; } = null!;

    // --- Fisheries & Aquaculture ---
    public string Aquaculture { get; set; } = null!;
    public string FAOAqua { get; set; } = null!;
    public string Catches { get; set; } = null!;
    public string Processing { get; set; } = null!;
    public string Introductions { get; set; } = null!;
    public string CountryComp { get; set; } = null!;
    public string Broodstock { get; set; } = null!;

    // --- Nursery Grounds ---
    public string EggNursery { get; set; } = null!;
    public string FryNursery { get; set; } = null!;
    public string LarvalNursery { get; set; } = null!;

    // --- Metrics & Sounds ---
    public string LengthWeight { get; set; } = null!;
    public string LengthRelations { get; set; } = null!;
    public string LengthFrequency { get; set; } = null!;
    public string Sounds { get; set; } = null!; // General sounds data
    public string FishSounds { get; set; } = null!; // Specific FishSounds data content

    // Navigation Property
    public virtual Stock Stock { get; set; } = null!;
}
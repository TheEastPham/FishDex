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
    public string Aquamaps { get; set; }
    public string SCRFA_data { get; set; } // Spawning Aggregations

    // --- Biological Characteristics (Morphology & Physiology) ---
    public bool? Morphology { get; set; }
    public string Vision { get; set; }
    public string Brains { get; set; }
    public string Gillarea { get; set; }
    public string Speed { get; set; }        // Swimming speed
    public string Metabolism { get; set; }
    public string Abnorm { get; set; }       // Abnormalities

    // --- Ecology & Life History ---
    public string Ecology { get; set; }
    public string Occurrence { get; set; }
    public string Abundance { get; set; }
    public string PopDyn { get; set; }       // Population dynamics
    public string Ecotoxicology { get; set; } // Dữ liệu độc học (khác với EcotoxID)

    // --- Reproduction & Development ---
    public string Reproduction { get; set; }
    public string Spawning { get; set; }
    public string Fecundity { get; set; }
    public string Maturity { get; set; }
    public string MatSizes { get; set; }
    public string Eggs { get; set; }
    public string EggDevelop { get; set; }
    public string Larvae { get; set; }
    public string LarvDyn { get; set; }
    public string LarvSpeed { get; set; }
    
    // --- Diet & Trophic ---
    public string Diet { get; set; }
    public string Food { get; set; }         // Linked to foodecosystemtype
    public string Foods { get; set; }        // Specific food items
    public string Ration { get; set; }
    public string Predators { get; set; }

    // --- Genetics ---
    public string Genetics { get; set; }
    public string Allele { get; set; }
    public string GeneticStudies { get; set; }
    public string Strains { get; set; }

    // --- Fisheries & Aquaculture ---
    public string Aquaculture { get; set; }
    public string FAOAqua { get; set; }
    public string Catches { get; set; }
    public string Processing { get; set; }
    public string Introductions { get; set; }
    public string CountryComp { get; set; }
    public string Broodstock { get; set; }

    // --- Nursery Grounds ---
    public string EggNursery { get; set; }
    public string FryNursery { get; set; }
    public string LarvalNursery { get; set; }

    // --- Metrics & Sounds ---
    public string LengthWeight { get; set; }
    public string LengthRelations { get; set; }
    public string LengthFrequency { get; set; }
    public string Sounds { get; set; }       // General sounds data
    public string FishSounds { get; set; }   // Specific FishSounds data content

    // Navigation Property
    public virtual Stock Stock { get; set; }
}
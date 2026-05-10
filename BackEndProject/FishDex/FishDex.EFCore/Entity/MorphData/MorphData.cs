using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphData")]
public class MorphData
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; } // PK, FK linked to Stock table

    public int? Speccode { get; set; }
    public string MorphDatRefNo { get; set; }
    
    // --- Identification ---
    public string AppearancePic { get; set; }
    public string EaseofID { get; set; }
    
    // --- Body Shape & General Features ---
    public string BodyShapeI { get; set; }  // e.g., fusiform, eel-like
    public string BodyShapeII { get; set; }
    public string Forehead { get; set; }
    public string OperculumPresent { get; set; }
    public string TypeofEyes { get; set; }
    public string TypeofMouth { get; set; }
    public string PosofMouth { get; set; }
    public string GasBladder { get; set; }

    // --- Sexual Attributes ---
    public string SexualAttributes { get; set; }
    public string SexMorphology { get; set; }
    public string RemarkSex { get; set; }
    
    // --- Sample Sizes ---
    public int? Females { get; set; }
    public int? Males { get; set; }

    // --- Metadata (Audit) ---
    public int? Entered { get; set; }
    public DateTime? DateEntered { get; set; }
    public int? Modified { get; set; }
    public DateTime? DateModified { get; set; }
    public int? Expert { get; set; }
    public DateTime? DateChecked { get; set; }
    public DateTime? TS { get; set; }

    // --- Navigation Properties ---
    public virtual MorphTeeth? Teeth { get; set; }
    public virtual MorphPigmentation? Pigmentation { get; set; }
    public virtual MorphFins? Fins { get; set; }
    public virtual MorphMeristics? Meristics { get; set; } // Scales, Gills, Vertebrae
    public virtual MorphMetrics? Metrics { get; set; } // Measurements
}
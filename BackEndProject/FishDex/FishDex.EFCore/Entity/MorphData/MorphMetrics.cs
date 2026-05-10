using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphMetrics")]
public class MorphMetrics
{
    [Key, ForeignKey("MorphData")]
    public int StockCode { get; set; }

    // --- Lengths (Chiều dài) ---
    public double? StandardLengthCm { get; set; }
    public double? Forklength { get; set; }
    public double? Totallength { get; set; }
    public double? HeadLength { get; set; }
    public double? PreDorsalLength { get; set; }
    public double? PrePelvicsLength { get; set; }
    public double? PreAnalLength { get; set; }
    public double? PreorbitalLength { get; set; }
    public double? EyeLength { get; set; }
    public double? PeduncleLength { get; set; }

    // --- Depths/Heights (Độ sâu/Chiều cao) ---
    public double? PostHeadDepth { get; set; }
    public double? PostTrunkDepth { get; set; }
    public double? MaximumDepth { get; set; }
    public double? PeduncleDepth { get; set; }
    public double? CaudalHeight { get; set; }

    // --- Similar Species & References (Liên kết tham khảo) ---
    public string SimilarSpecies1 { get; set; }
    public string SimilarSpec1Remarks { get; set; }
    public string SimilarSpecies2 { get; set; }
    public string SimilarSpec2Remarks { get; set; }
    public string OtherRef1 { get; set; }
    public string OtherRef2 { get; set; }
    public string AddChars { get; set; }

    public virtual MorphData MorphData { get; set; }
}
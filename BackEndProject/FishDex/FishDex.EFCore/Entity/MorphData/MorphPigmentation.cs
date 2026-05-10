using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphPigmentation")]
public class MorphPigmentation
{
    [Key, ForeignKey("MorphData")]
    public int StockCode { get; set; }

    public string SexColors { get; set; }
    public string StrikingFeatures { get; set; }

    // --- Stripes (Sọc) ---
    // Horizontal (Ngang)
    public string HorStripesTTI { get; set; }
    public string HorStripesTTII { get; set; }
    
    // Vertical (Dọc)
    public string VerStripesTTI { get; set; }
    public string VerStripesTTII { get; set; }
    public string VerStripesTTIII { get; set; }

    // Diagonal (Chéo)
    public string DiaStripesTTI { get; set; }
    public string DiaStripesTTII { get; set; }
    public string DiaStripesTTIII { get; set; }

    // Curved (Cong)
    public string CurStripesTTI { get; set; }
    public string CurStripesTTII { get; set; }
    public string CurStripesTTIII { get; set; }

    // --- Spots (Đốm) ---
    public string SpotsTTI { get; set; }
    public string SpotsTTII { get; set; }
    public string SpotsTTIII { get; set; }

    public virtual MorphData MorphData { get; set; }
}
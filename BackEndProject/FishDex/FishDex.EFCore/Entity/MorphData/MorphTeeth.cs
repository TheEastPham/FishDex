using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphTeeth")]
public class MorphTeeth
{
    [Key, ForeignKey("MorphData")]
    public int StockCode { get; set; }

    // --- Mandible (Hàm dưới) ---
    public string MandibleTeeth { get; set; }
    public string MandibleTeethT1 { get; set; }
    public string MandibleTeethT2 { get; set; }
    public string MandibleRowsMin { get; set; }
    public string MandibleRowsMax { get; set; }

    // --- Maxilla (Hàm trên) ---
    public string MaxillaTeeth { get; set; }
    public string MaxillaTeethT1 { get; set; }
    public string MaxillaTeethT2 { get; set; }
    public string MaxillaRowsMin { get; set; }
    public string MaxillaRowsMax { get; set; }

    // --- Vomerine (Răng lá mía) ---
    public string VomerineTeeth { get; set; }
    public string VomerineTeethT1 { get; set; }
    public string VomerineTeethT2 { get; set; }
    public string VomerineRowsMin { get; set; }
    public string VomerineRowsMax { get; set; }

    // --- Palatine (Răng hàm vòm) ---
    public string Palatine { get; set; }
    public string PalatineTeethT1 { get; set; }
    public string PalatineTeethT2 { get; set; }
    public string PalatineRowsMin { get; set; }
    public string PalatineRowsMax { get; set; }

    // --- Pharyngeal (Răng hầu) ---
    public string PharyngealTeeth { get; set; }
    public string PharyngealTeethT1 { get; set; }
    public string PharyngealTeethT2 { get; set; }
    public string PharyngealRowsMin { get; set; }
    public string PharyngealRowsMax { get; set; }

    // --- Others ---
    public string TeethonTongue { get; set; }
    public string Lipsteeth { get; set; }
    public string DermalTeeth { get; set; }
    public string CommentonTeeth { get; set; }

    public virtual MorphData MorphData { get; set; }
}
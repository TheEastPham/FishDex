using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphFins")]
public class MorphFins
{
    [Key, ForeignKey("MorphData")]
    public int StockCode { get; set; }

    public int? Dfinno { get; set; }
    public string? DorsalFinI { get; set; }
    public string? DorsalFinII { get; set; }
    public string? DorsalAttributes { get; set; }
    public int? DorsalSpinesMin { get; set; }
    public int? DorsalSpinesMax { get; set; }
    public int? DorsalSoftRaysMin { get; set; }
    public int? DorsalSoftRaysMax { get; set; }
    public string? Notched { get; set; }
    public int? DFinletsmin { get; set; }
    public int? DFinletsmax { get; set; }
    public string? Adifin { get; set; }
    public int? Afinno { get; set; }
    public string? AnalFinI { get; set; }
    public string? AnalFinII { get; set; }
    public int? AnalFinSpinesMin { get; set; }
    public int? AnalFinSpinesMax { get; set; }
    public int? Araymin { get; set; }
    public int? Araymax { get; set; }
    public string? PectoralAttributes { get; set; }
    public string? Pspines2 { get; set; }
    public int? Praymin { get; set; }
    public int? Praymax { get; set; }
    public string? PelvicsAttributes { get; set; }
    public string? VPosition { get; set; }
    public string? VPosition2 { get; set; }
    public string? Vspines { get; set; }
    public int? Vraymin { get; set; }
    public int? Vraymax { get; set; }
    public string? CaudalFinI { get; set; }
    public string? CaudalFinII { get; set; }
    public string? CShape { get; set; }
    public string? Attributes { get; set; }

    public virtual MorphData MorphData { get; set; }
}

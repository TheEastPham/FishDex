using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphMeristics")]
public class MorphMeristics
{
    [Key, ForeignKey("MorphData")]
    public int StockCode { get; set; }

    public string? TypeofScales { get; set; }
    public string? Scutes { get; set; }
    public string? Keels { get; set; }
    public int? LateralLinesNo { get; set; }
    public string? LLinterrupted { get; set; }
    public int? ScalesLateralmin { get; set; }
    public int? ScalesLateralmax { get; set; }
    public int? PoredScalesMin { get; set; }
    public int? PoredScalesMax { get; set; }
    public int? LatSeriesMin { get; set; }
    public int? LatSeriesMax { get; set; }
    public int? ScaleRowsAboveMin { get; set; }
    public int? ScaleRowsAboveMax { get; set; }
    public int? ScaleRowsBelowMin { get; set; }
    public int? ScaleRowsBelowMax { get; set; }
    public int? ScalesPeduncMin { get; set; }
    public int? ScalesPeduncMax { get; set; }
    public int? BarbelsNo { get; set; }
    public string? BarbelsType { get; set; }
    public int? GillCleftsNo { get; set; }
    public string? Spiracle { get; set; }
    public int? GillRakersLowMin { get; set; }
    public int? GillRakersLowMax { get; set; }
    public int? GillRakersUpMin { get; set; }
    public int? GillRakersUpMax { get; set; }
    public int? GillRakersTotalMin { get; set; }
    public int? GillRakersTotalMax { get; set; }
    public string? Vertebrae { get; set; }
    public int? VertebraePreanMin { get; set; }
    public int? VertebraePreanMax { get; set; }
    public int? VertebraeTotalMin { get; set; }
    public int? VertebraeTotalMax { get; set; }

    public virtual MorphData MorphData { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.MorphData;

[Table("MorphData")]
public class MorphData
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; }

    public int? Speccode { get; set; }
    public string? MorphDatRefNo { get; set; }
    public string? AppearancePic { get; set; }
    public string? EaseofID { get; set; }
    public string? BodyShapeI { get; set; }
    public string? BodyShapeII { get; set; }
    public string? Forehead { get; set; }
    public string? OperculumPresent { get; set; }
    public string? TypeofEyes { get; set; }
    public string? TypeofMouth { get; set; }
    public string? PosofMouth { get; set; }
    public string? GasBladder { get; set; }
    public string? SexualAttributes { get; set; }
    public string? SexMorphology { get; set; }
    public string? RemarkSex { get; set; }
    public int? Females { get; set; }
    public int? Males { get; set; }
    public int? Entered { get; set; }
    public DateTime? DateEntered { get; set; }
    public int? Modified { get; set; }
    public DateTime? DateModified { get; set; }
    public int? Expert { get; set; }
    public DateTime? DateChecked { get; set; }
    public DateTime? TS { get; set; }

    public virtual MorphTeeth? Teeth { get; set; }
    public virtual MorphPigmentation? Pigmentation { get; set; }
    public virtual MorphFins? Fins { get; set; }
    public virtual MorphMeristics? Meristics { get; set; }
    public virtual MorphMetrics? Metrics { get; set; }
}

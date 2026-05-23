using System;
using System.Collections.Generic;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Stocks;
using FishLover.Shared.Common.Enum;

namespace FishDex.EFCore.Entity.Species;

public class Species
{
    public Guid Id { get; set; }
    public int SpecCode { get; set; }
    public int? GenusCode { get; set; }
    public int FamCode { get; set; }
    public Guid FamId { get; set; }
    public WaterType WaterType { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public string? SpeciesRefNo { get; set; }
    public string? Author { get; set; }
    public string? BodyShapeI { get; set; }
    public string? Source { get; set; }
    public string? AuthorRef { get; set; }
    public string? Remark { get; set; }
    public string? TaxIssue { get; set; }
    public decimal? Length { get; set; }
    public decimal? Weight { get; set; }
    public string? Comments { get; set; }
    public string? Dangerous { get; set; }
    public int? Vulnerability { get; set; }
    public int? VulnerabilityClimate { get; set; }
    public string? AirBreathing { get; set; }
    public string? LifeCycle { get; set; }
    public string? DemersPelag { get; set; }
    public string? MaxLengthRef { get; set; }
    public double? LengthFemale { get; set; }
    public double? LongevityWild { get; set; }
    public string? PicPreferredNameM { get; set; }
    public string? PicPreferredNameF { get; set; }

    public virtual Genus? Genus { get; set; }
    public virtual Family Family { get; set; }
    public ICollection<CommonName> CommonNames { get; set; } = [];
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<SystemImage> Pictures { get; set; }
}

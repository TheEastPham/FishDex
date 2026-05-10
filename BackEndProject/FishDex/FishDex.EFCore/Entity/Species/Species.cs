using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Stocks;
using FishLover.Shared.Common.Enum;

namespace FishDex.EFCore.Entity.Species;

public class Species
{
    public Guid Id { get; set; }
    public int SpecCode { get; set; }
    public int GenusCode { get; set; }
    public int FamCode { get; set; }
    public Guid FamId { get; set; } 
    public WaterType WaterType { get; set; } 
    public string SpeciesName { get; set; } = string.Empty;
    public string SpeciesRefNo { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string BodyShapeI { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string AuthorRef { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public string TaxIssue { get; set; } = string.Empty;
    public decimal? Length { get; set; }
    public decimal? Weight { get; set; }
    public string Comments { get; set; } = string.Empty;
    public string Dangerous { get; set; } = string.Empty;
    public int? Vulnerability { get; set; }
    public int? VulnerabilityClimate { get; set; }
    public string AirBreathing { get; set; } = string.Empty;
    public string LifeCycle { get; set; }
    
    // CommonInfo
    public string DemersPelag { get; set; } // swimmingPosition - DemersPelag
    public string MaxLengthRef { get; set; } // Tham chiếu
    public double? LengthFemale { get; set; } // approxMaxSize - Length (Max Female)
    public double? LongevityWild { get; set; } // averageLifespan - LongevityWild

    // Sexual Dimorphism
    public string PicPreferredNameM { get; set; } // photoMale - PicPreferredNameM (link đến PicturesMain)
    public string PicPreferredNameF { get; set; } // photoFemale - PicPreferredNameF (link đến PicturesMain)
    
    public virtual Genus Genus { get; set; }
    public virtual Family Family { get; set; }
    // public ICollection<CommonNameEntity> CommonNames { get; set; }
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<MorphData.MorphData> MorphData { get; set; }
    public ICollection<SystemImage> Pictures { get; set; }
}
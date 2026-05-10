namespace FishDex.EFCore.Entity.Media;

public class SystemImage
{
    public Guid Id { get; set; }
    public int SpecCode { get; set; }
    public string Name { get; set; } = string.Empty; // PicName
    public string PictureType { get; set; } = string.Empty;
    public string? LifeStage { get; set; } // "adult", "juvenile", "larva", v.v.
    public double? Size { get; set; }
    public string? LengthType { get; set; } // "TL", "SL", v.v.
    public string? BestPic { get; set; }
    public int? Score { get; set; }
    public bool? PicPreferred { get; set; }
    public bool? PicPreferredMale { get; set; }
    public bool? PicPreferredFem { get; set; }
    public bool? PicPreferredJuv { get; set; }
}
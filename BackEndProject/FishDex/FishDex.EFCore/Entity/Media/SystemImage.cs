using System;
using System.IO;

namespace FishDex.EFCore.Entity.Media;

public class SystemImage
{
    public Guid Id { get; set; }
    public int SpecCode { get; set; }
    public string Name { get; set; } = string.Empty; // PicName (tên file gốc, e.g. "Cacal_u9.jpg")

    /// <summary>Object key trên MinIO/S3: {SpecCode}/{Id}{ext} — dùng key này cho GetPresignedUrlAsync.</summary>
    public string ObjectKey => $"{SpecCode}/{Id}{Path.GetExtension(Name)}";
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
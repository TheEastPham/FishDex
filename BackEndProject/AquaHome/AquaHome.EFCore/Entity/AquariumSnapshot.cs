namespace AquaHome.EFCore.Entity;

public class AquariumSnapshot
{
    public Guid Id { get; set; }
    public Guid AquariumId { get; set; }
    public Guid UserId { get; set; } // denorm — tránh JOIN ngược lại Aquarium khi kiểm tra ownership
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } // = CreatedAt lúc tạo mới; cập nhật khi user ghi đè (republish)
    public bool IsActive { get; set; } = true;

    public int WaterType { get; set; } // copy từ Aquarium lúc publish — queryable, không JOIN
    public int Style { get; set; }     // copy từ Aquarium lúc publish — queryable, không JOIN

    public int LikeCount { get; set; }
    public int FishSpeciesCount { get; set; }

    public Guid? ContestEntryId { get; set; }
    // Denorm từ ContestEntry.PrizeTier lúc admin finalize contest — null = chưa đoạt giải (hoặc chưa tham gia)
    public string? AwardTierName { get; set; }
    public int? AwardTierLevel { get; set; } // PrizeTierLevel — cho FE chọn màu badge, không JOIN

    /// <summary>Trỏ tới AquariumMedia đã upload — resolve presigned URL mới mỗi lần serve (tránh lưu URL hết hạn).</summary>
    public Guid? CoverMediaId { get; set; }
    public string? YoutubeVideoUrl { get; set; }

    /// <summary>Render-only: fish list, distributionPoints, equipment, parameters. KHÔNG query bên trong JSON.</summary>
    public string SnapshotData { get; set; } = string.Empty;

    public Aquarium Aquarium { get; set; } = null!;
    public ContestEntry? ContestEntry { get; set; }
}

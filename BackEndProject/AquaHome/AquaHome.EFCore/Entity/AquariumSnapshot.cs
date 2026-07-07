namespace AquaHome.EFCore.Entity;

public class AquariumSnapshot
{
    public Guid Id { get; set; }
    public Guid AquariumId { get; set; }
    public Guid UserId { get; set; } // denorm — tránh JOIN ngược lại Aquarium khi kiểm tra ownership
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public int WaterType { get; set; } // copy từ Aquarium lúc publish — queryable, không JOIN
    public int Style { get; set; }     // copy từ Aquarium lúc publish — queryable, không JOIN

    public int LikeCount { get; set; }
    public int FishSpeciesCount { get; set; }

    public Guid? ContestEntryId { get; set; }
    public int? ContestAward { get; set; } // null = chưa tham gia contest

    public string? CoverImageUrl { get; set; }
    public string? YoutubeVideoUrl { get; set; }

    /// <summary>Render-only: fish list, distributionPoints, equipment, parameters. KHÔNG query bên trong JSON.</summary>
    public string SnapshotData { get; set; } = string.Empty;

    public Aquarium Aquarium { get; set; } = null!;
    public ContestEntry? ContestEntry { get; set; }
}

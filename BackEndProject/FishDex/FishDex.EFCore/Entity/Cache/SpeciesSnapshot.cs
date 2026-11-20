using FishLover.Shared.Common.Enum;

namespace FishDex.EFCore.Entity.Cache;

public class SpeciesSnapshot
{
    public int SpecCode { get; set; }

    // ── Source & verification ─────────────────────────────────────
    public SnapshotDataSource DataSource { get; set; }
    public bool IsVerified { get; set; }

    // ── Taxonomy ──────────────────────────────────────────────────
    public string SpeciesName { get; set; } = string.Empty;
    public string? FamilyName { get; set; }
    public string? GenusName { get; set; }
    public string? CommonName { get; set; }

    // ── Water conditions ──────────────────────────────────────────
    public WaterType WaterType { get; set; }
    public double? TempMin { get; set; }
    public double? TempMax { get; set; }
    public double? PhMin { get; set; }
    public double? PhMax { get; set; }
    public double? DhMin { get; set; }
    public double? DhMax { get; set; }

    // ── Biology ───────────────────────────────────────────────────
    public decimal? Length { get; set; }
    public double? LongevityCaptive { get; set; }
    public string? DemersPelag { get; set; }

    // ── Behavior (from FishBase — no inference, store raw values) ─
    public bool? Schooling { get; set; }
    public bool? Shoaling { get; set; }
    public bool? Solitary { get; set; }
    public string? FeedingType { get; set; }
    public string? FeedingPosition { get; set; }
    public string? ActivityPattern { get; set; }

    // ── Care profile (community-contributed or manually curated) ──
    public bool? RequiresLiveFood { get; set; }
    public string? Aggressiveness { get; set; }
    public bool? FinNippingRisk { get; set; }
    public bool? JumpingRisk { get; set; }
    public SnapshotCareLevel? CareLevel { get; set; }
    public int? MinTankLiters { get; set; }

    // ── Images ────────────────────────────────────────────────────
    public string? ThumbnailObjectKey { get; set; }
    public string? MaleImageObjectKey { get; set; }
    public string? FemaleImageObjectKey { get; set; }

    // ── Moderation (for community submissions) ────────────────────
    public Guid? ContributedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Loại loài user tự chọn khi submit — chỉ là gợi ý, admin xác nhận lại ở <see cref="Kind"/> khi duyệt.</summary>
    public CommunitySpeciesKind? SuggestedKind { get; set; }
    /// <summary>Loại loài admin xác nhận khi duyệt. Null cho tới khi verify lần đầu.</summary>
    public CommunitySpeciesKind? Kind { get; set; }

    // ── Metadata ──────────────────────────────────────────────────
    public DateTime PopulatedAt { get; set; }
    public SnapshotPopulatedFrom PopulatedFrom { get; set; }
}

public enum SnapshotDataSource
{
    FishBase  = 0,
    Community = 1,
}

public enum SnapshotCareLevel
{
    Beginner     = 0,
    Intermediate = 1,
    Expert       = 2,
}

public enum SnapshotPopulatedFrom
{
    FishBase = 0,
    Manual   = 1,
}

/// <summary>
/// Phân loại do user submit gợi ý (SuggestedKind) và admin xác nhận (Kind) khi duyệt community species.
/// Natural = loài có thật, cần admin tự đối chiếu SpecCode FishBase và chạy lại ETL để lấy full data + ảnh gốc
/// (quy trình thủ công, ngoài phạm vi app). Hybrid = loài lai tạo, dùng thẳng data user submit.
/// </summary>
public enum CommunitySpeciesKind
{
    Natural = 0,
    Hybrid  = 1,
}

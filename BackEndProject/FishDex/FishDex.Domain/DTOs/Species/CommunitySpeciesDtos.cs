using FishDex.EFCore.Entity.Cache;
using FishLover.Shared.Common.Enum;

namespace FishDex.Domain.DTOs.Species;

/// <summary>
/// User submit 1 loài (không có trong FishBase) → lưu vào SpeciesSnapshot
/// dạng Community, IsVerified=false, chờ admin duyệt. SpecCode auto-cấp ≥ 500000.
/// Chỉ SpeciesName + WaterType bắt buộc; còn lại optional (để null nếu người gửi không biết).
/// </summary>
public record SubmitCommunitySpeciesRequest(
    string SpeciesName,
    WaterType WaterType,
    string? CommonName = null,
    string? FamilyName = null,
    string? GenusName = null,
    // Gợi ý của user: lai tạo hay tự nhiên — admin xác nhận lại khi duyệt.
    CommunitySpeciesKind? SuggestedKind = null,
    double? TempMin = null,
    double? TempMax = null,
    double? PhMin = null,
    double? PhMax = null,
    double? DhMin = null,
    double? DhMax = null,
    decimal? Length = null,
    double? LongevityCaptive = null,
    string? FeedingType = null,
    string? FeedingPosition = null,
    string? ActivityPattern = null,
    bool? RequiresLiveFood = null,
    string? Aggressiveness = null,
    bool? FinNippingRisk = null,
    bool? JumpingRisk = null,
    SnapshotCareLevel? CareLevel = null,
    int? MinTankLiters = null);

/// <summary>
/// Một loài có tên gần giống tên user đang gõ. <paramref name="Outcome"/> quyết định FE dẫn
/// người dùng đi đâu: chọn thẳng loài đã có, yêu cầu nạp từ FishBase, hay báo trùng hàng đợi.
/// </summary>
public record SimilarSpeciesDto(
    int SpecCode,
    string SpeciesName,
    SimilarSpeciesOutcome Outcome,
    // Score: độ giống 0..1, để FE quyết mức độ nhấn mạnh cảnh báo.
    double Score);

public enum SimilarSpeciesOutcome
{
    /// <summary>Đã có trong FishDex — chọn thẳng, không cần submit gì.</summary>
    AlreadyInFishDex = 0,
    /// <summary>Có trong FishBase nhưng chưa nạp — đi luồng yêu cầu migration.</summary>
    NeedsMigration = 1,
    /// <summary>Đã có người khác submit và đang chờ duyệt.</summary>
    AlreadySubmitted = 2,
}

public record RejectCommunitySpeciesRequest(string Reason);

/// <summary>Admin duyệt — có thể xác nhận lại Kind (mặc định giữ SuggestedKind nếu không truyền).</summary>
public record VerifyCommunitySpeciesRequest(CommunitySpeciesKind? Kind = null);

/// <summary>
/// Dùng cho "loài tôi gửi", trang admin duyệt, và làm nguồn prefill khi sửa (Update) — vì vậy mang đủ
/// field editable (khớp SubmitCommunitySpeciesRequest), không chỉ set gọn để hiển thị list.
/// </summary>
public record CommunitySpeciesDto(
    int SpecCode,
    string SpeciesName,
    string? CommonName,
    string? FamilyName,
    string? GenusName,
    WaterType WaterType,
    bool IsVerified,
    string? RejectionReason,
    Guid? ContributedBy,
    string? ImageUrl,
    DateTime PopulatedAt,
    CommunitySpeciesKind? SuggestedKind,
    CommunitySpeciesKind? Kind,
    double? TempMin,
    double? TempMax,
    double? PhMin,
    double? PhMax,
    double? DhMin,
    double? DhMax,
    decimal? Length,
    double? LongevityCaptive,
    string? FeedingType,
    string? FeedingPosition,
    string? ActivityPattern,
    bool? RequiresLiveFood,
    string? Aggressiveness,
    bool? FinNippingRisk,
    bool? JumpingRisk,
    SnapshotCareLevel? CareLevel,
    int? MinTankLiters);

public record CommunityImageUploadResultDto(string UploadUrl, string ObjectKey);
public record CommunityImageUploadRequest(string FileName, string ContentType);
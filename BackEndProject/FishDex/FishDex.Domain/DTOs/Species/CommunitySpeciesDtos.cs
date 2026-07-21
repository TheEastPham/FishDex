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

public record RejectCommunitySpeciesRequest(string Reason);

/// <summary>Admin duyệt — có thể xác nhận lại Kind (mặc định giữ SuggestedKind nếu không truyền).</summary>
public record VerifyCommunitySpeciesRequest(CommunitySpeciesKind? Kind = null);

/// <summary>Bản gọn để list — dùng cho "loài tôi gửi" và trang admin duyệt.</summary>
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
    CommunitySpeciesKind? Kind);

public record CommunityImageUploadResultDto(string UploadUrl, string ObjectKey);
public record CommunityImageUploadRequest(string FileName, string ContentType);
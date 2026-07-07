using AquaHome.Domain.Enums;

namespace AquaHome.Domain.DTOs;

public record SnapshotFishDto(
    int SpecCode,
    string SpeciesName,
    string? CommonName,
    string? ImageUrl,
    int Quantity,
    IReadOnlyList<DistributionPointDto> DistributionPoints);

/// <summary>Nội dung JSONB render-only của AquariumSnapshot — KHÔNG query bên trong.</summary>
public record SnapshotDataDto(
    string AquariumName,
    string? OwnerNickname,   // nickname chủ bể tại thời điểm publish — hiển thị trên trang public
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    double? VolumeLiters,
    string? Description,
    IReadOnlyList<SnapshotFishDto> Fish);

public record SnapshotPreviewDto(
    WaterType? WaterType,
    AquariumStyle? Style,
    int FishSpeciesCount,
    SnapshotDataDto SnapshotData);

public record PublishSnapshotRequest(
    Guid? CoverMediaId,
    /// <summary>Null = tạo snapshot mới. Có giá trị = ghi đè (giữ nguyên Slug/Id/LikeCount) snapshot đang active này.</summary>
    Guid? TargetSnapshotId);

/// <summary>Bản gọn cho GET /snapshots/mine — trang quản lý "bể đã public của tôi" + contest entry form chọn bể.</summary>
public record MySnapshotDto(
    Guid Id,
    Guid AquariumId,
    string Slug,
    string AquariumName,
    WaterType WaterType,
    AquariumStyle Style,
    int FishSpeciesCount,
    int LikeCount,
    string? CoverImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AquariumSnapshotDto(
    Guid Id,
    string Slug,
    WaterType WaterType,
    AquariumStyle Style,
    int LikeCount,
    int FishSpeciesCount,
    ContestAward? ContestAward,
    string? CoverImageUrl,
    string? YoutubeVideoUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SnapshotDataDto? SnapshotData,
    bool LikedByMe);

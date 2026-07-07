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

public record PublishSnapshotRequest(string? CoverImageUrl);

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
    SnapshotDataDto? SnapshotData,
    bool LikedByMe);

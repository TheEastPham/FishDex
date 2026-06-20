using AquaHome.Domain.Enums;

namespace AquaHome.Domain.DTOs;

public record AquariumDto(
    Guid Id,
    string Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    double? VolumeLiters,   // computed: L×W×H/1000
    WaterType? WaterType,
    AquariumStyle? Style,
    string? Description,
    DateTime CreatedAt,
    int FishCount,
    int TotalQuantity);

public record CreateAquariumRequest(
    string Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    WaterType? WaterType,
    AquariumStyle? Style,
    string? Description);

public record UpdateAquariumRequest(
    string? Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    WaterType? WaterType,
    AquariumStyle? Style,
    string? Description);

public record AddFishRequest(int SpecCode, int Quantity = 1);

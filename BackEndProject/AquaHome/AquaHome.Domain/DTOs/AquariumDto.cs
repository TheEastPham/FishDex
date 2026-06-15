namespace AquaHome.Domain.DTOs;

public record AquariumDto(
    Guid Id,
    string Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    double? VolumeLiters,   // computed: L×W×H/1000
    string? Type,
    string? Description,
    DateTime CreatedAt,
    int FishCount);

public record CreateAquariumRequest(
    string Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    string? Type,
    string? Description);

public record UpdateAquariumRequest(
    string? Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    string? Type,
    string? Description);

public record AddFishRequest(int SpecCode, int Quantity = 1);

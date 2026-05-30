namespace AquaHome.Domain.DTOs;

public record AquariumDto(
    Guid Id,
    string Name,
    double? VolumeLiters,
    string? Type,
    string? Description,
    DateTime CreatedAt,
    int FishCount);

public record CreateAquariumRequest(
    string Name,
    double? VolumeLiters,
    string? Type,
    string? Description);

public record UpdateAquariumRequest(
    string? Name,
    double? VolumeLiters,
    string? Type,
    string? Description);

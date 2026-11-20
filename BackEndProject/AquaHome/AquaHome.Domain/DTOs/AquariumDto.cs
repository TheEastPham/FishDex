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
    int TotalQuantity,
    // C_Code của FishBase (vd "704" = Việt Nam). Quốc gia gắn vào BỂ chứ không vào user,
    // để đặt nó ở nơi con cá thực sự đang ở và cho phép một người có bể ở nhiều nước.
    string? CountryCode);

public record CreateAquariumRequest(
    string Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    WaterType? WaterType,
    AquariumStyle? Style,
    string? Description,
    string? CountryCode = null);

public record UpdateAquariumRequest(
    string? Name,
    double? LengthCm,
    double? WidthCm,
    double? HeightCm,
    WaterType? WaterType,
    AquariumStyle? Style,
    string? Description,
    string? CountryCode = null);

public record AddFishRequest(int SpecCode, int Quantity = 1);

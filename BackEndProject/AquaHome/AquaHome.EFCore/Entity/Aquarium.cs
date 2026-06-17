namespace AquaHome.EFCore.Entity;

public class Aquarium
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? LengthCm { get; set; }
    public double? WidthCm { get; set; }
    public double? HeightCm { get; set; }
    public int? WaterType { get; set; }
    public int? Style { get; set; }

    /// <summary>Tính từ L × W × H (cm³ → lít). Null nếu thiếu bất kỳ chiều nào.</summary>
    public double? VolumeLiters => (LengthCm.HasValue && WidthCm.HasValue && HeightCm.HasValue)
        ? Math.Round(LengthCm.Value * WidthCm.Value * HeightCm.Value / 1000.0, 1)
        : null;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<AquariumFish> Fish { get; set; } = [];
}

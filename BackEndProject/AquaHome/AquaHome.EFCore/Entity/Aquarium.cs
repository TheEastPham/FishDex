namespace AquaHome.EFCore.Entity;

public class Aquarium
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quốc gia đặt bể, dạng C_Code của FishBase (vd "704" = Việt Nam) để khớp với
    /// <c>TradedSpecies.CountryCode</c> ở FishDex.
    ///
    /// <para>Gắn quốc gia vào BỂ chứ không vào user: đặt nó ở nơi con cá thực sự đang ở,
    /// không phải nơi chủ tài khoản khai. Nhờ vậy một người sống ở nhiều nước có thể có
    /// nhiều bể ở nhiều nước.</para>
    ///
    /// <para>Đây là nguồn dữ liệu chính cho Market Layer: bể ở quốc gia X có cá Z nghĩa là
    /// quốc gia X bán cá Z. Worker gom định kỳ rồi đẩy sang FishDex — <b>không kèm bất kỳ
    /// tham chiếu user nào</b>, để không ai truy ngược được từ danh sách market về chủ bể.</para>
    ///
    /// <para>Nullable vì bể tạo trước khi có tính năng này; migration backfill "704".</para>
    /// </summary>
    public string? CountryCode { get; set; }
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

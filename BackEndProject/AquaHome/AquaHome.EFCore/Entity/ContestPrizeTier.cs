namespace AquaHome.EFCore.Entity;

/// <summary>
/// Hạng giải của 1 contest, do admin tự định nghĩa (tên, số lượng). Lúc tạo contest, hệ thống seed sẵn
/// 4 tier mẫu (Nhất/Nhì/Ba/Khuyến khích) — admin có thể sửa tên, đổi SlotCount, thêm/xóa tier.
/// </summary>
public class ContestPrizeTier
{
    public Guid Id { get; set; }
    public Guid ContestId { get; set; }
    public string Name { get; set; } = string.Empty; // "Giải Nhất", "Giải Khuyến khích", admin tự đặt
    public int TierLevel { get; set; }   // PrizeTierLevel — chỉ để chọn màu/icon, không ảnh hưởng logic
    public int SlotCount { get; set; }   // số lượng giải ở hạng này — set theo từng contest
    public int DisplayOrder { get; set; }
    public string? Description { get; set; } // mô tả giải thưởng, vd "5.000.000đ + áo FishLover"

    /// <summary>Ảnh giải thưởng (optional) — resolve presigned URL mới mỗi lần đọc, giống CoverMediaId/sponsor logo.</summary>
    public string? ImageObjectKey { get; set; }

    public Contest Contest { get; set; } = null!;
}

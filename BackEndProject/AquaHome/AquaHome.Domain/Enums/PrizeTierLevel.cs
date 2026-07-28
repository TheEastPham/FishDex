namespace AquaHome.Domain.Enums;

/// <summary>Chỉ dùng để FE chọn màu/icon huy chương — không ảnh hưởng logic xếp hạng.</summary>
public enum PrizeTierLevel
{
    Gold          = 1, // Giải Nhất
    Silver        = 2, // Giải Nhì
    Bronze        = 3, // Giải Ba
    Encouragement = 4, // Giải Khuyến khích
    Custom        = 5, // Hạng giải admin tự đặt thêm
}

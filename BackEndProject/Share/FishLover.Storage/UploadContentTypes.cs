namespace FishLover.Storage;

/// <summary>
/// Content-type được nhận qua presigned PUT và đuôi file tương ứng. Đuôi object key suy từ content-type (đã ký vào
/// presigned PUT), không lấy theo tên file client gửi — tên file là dữ liệu client tự khai.
/// </summary>
/// <remarks>
/// Key đã lưu trong DB nên đổi đuôi của một content-type là mất object cũ (xem key policy của từng service);
/// chỉ thêm loại mới, không sửa loại đã có.
/// </remarks>
public static class UploadContentTypes
{
    /// <summary>Ảnh thường: ảnh bể, ảnh loài cộng đồng.</summary>
    public static readonly IReadOnlyDictionary<string, string> Images = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    /// <summary>Logo sponsor, ảnh hạng giải: như <see cref="Images"/> cộng SVG.</summary>
    public static readonly IReadOnlyDictionary<string, string> Logos = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg",
    };

    /// <summary>Video dự thi.</summary>
    public static readonly IReadOnlyDictionary<string, string> Videos = new Dictionary<string, string>
    {
        ["video/mp4"] = ".mp4",
        ["video/quicktime"] = ".mov",
        ["video/webm"] = ".webm",
    };
}

namespace AquaHome.EFCore.Entity;

/// <summary>
/// Ảnh dùng trong bài. Block image trong content.json chỉ trỏ tới <c>assetId</c>; API detail trả
/// kèm presigned URL để FE map lúc render — nhờ vậy bucket vẫn private mà URL hết hạn không làm
/// chết ảnh trong bài. Ảnh không gắn ngôn ngữ: bản dịch nào cũng dùng lại được.
/// </summary>
public class ArticleAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }

    public string ObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long Bytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Article Article { get; set; } = null!;
}

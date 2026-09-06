namespace AquaHome.EFCore.Entity;

/// <summary>
/// Một bản ngôn ngữ của bài viết: tiêu đề + mô tả ngắn nằm ở DB (để list và search),
/// nội dung (content.json) nằm trên R2. Thiếu bản dịch thì service fallback en → vi.
/// </summary>
public class ArticleTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }

    /// <summary>vi | en | de | zh — khớp locale của FE.</summary>
    public string Language { get; set; } = "vi";

    public string Title { get; set; } = string.Empty;
    /// <summary>Mô tả ngắn hiển thị ở trang list — không phải mở bài.</summary>
    public string? Summary { get; set; }

    /// <summary>{prefix}/{lang}/content.json — FE fetch trực tiếp bằng presigned GET.</summary>
    public string ContentObjectKey { get; set; } = string.Empty;

    /// <summary>Tổng số block của cả 3 mục — admin thấy ngay bản dịch nào còn sơ sài.</summary>
    public int BlockCount { get; set; }
    public int WordCount { get; set; }
    public int ReadingMinutes { get; set; }
    public long ContentBytes { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Article Article { get; set; } = null!;
}

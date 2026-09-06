namespace AquaHome.EFCore.Entity;

/// <summary>
/// Metadata bài viết. Nội dung thật KHÔNG nằm trong DB — mỗi bài là một folder trên R2
/// (<c>aquahome/{env}/articles/{Id}/</c>) chứa content.json theo từng ngôn ngữ và ảnh trong bài.
/// DB chỉ giữ những gì cần query: slug, type, level, tag, trạng thái, view.
/// </summary>
public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>URL-friendly, unique — FE gọi /api/articles/{slug}.</summary>
    public string Slug { get; set; } = string.Empty;

    public int Type { get; set; }         // ArticleType
    public int ReadingLevel { get; set; } // ReadingLevel
    public int Status { get; set; }       // ArticleStatus: Draft/Published/Archived

    /// <summary>
    /// Template render mà FE phải dùng cho bài này. Suy ra từ Type lúc tạo, lưu lại thành cột
    /// để bài cũ không đổi cách hiển thị khi mai này Type được map sang template khác.
    /// </summary>
    public string TemplateKey { get; set; } = "standard";

    /// <summary>Postgres text[] — filter bằng Tags.Contains(tag), không cần bảng phụ.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Thumbnail của bài trên R2: {prefix}/cover{ext}.</summary>
    public string? CoverObjectKey { get; set; }

    public Guid AuthorUserId { get; set; }
    /// <summary>Denorm nickname lúc tạo — khỏi gọi UserManagement mỗi lần list bài.</summary>
    public string? AuthorName { get; set; }

    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }

    public DateTime? PublishedAt { get; set; } // null khi còn Draft
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ArticleTranslation> Translations { get; set; } = [];
    public ICollection<ArticleAsset> Assets { get; set; } = [];

    /// <summary>Folder gốc của bài trên R2. Xóa bài = xóa cả prefix này.</summary>
    public string ObjectKeyPrefix(string env) => $"aquahome/{env}/articles/{Id}";
}

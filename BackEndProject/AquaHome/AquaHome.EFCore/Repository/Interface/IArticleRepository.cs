using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IArticleRepository
{
    /// <summary>Kèm Translations + Assets — service cần cả hai để dựng DTO và validate assetId.</summary>
    Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>publishedOnly = true chỉ trả bài Status=Published (dùng cho API công khai).</summary>
    Task<Article?> GetBySlugAsync(string slug, bool publishedOnly, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Trang list công khai: chỉ Published, sort PublishedAt DESC (bài ghim lên đầu).</summary>
    Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPublishedAsync(
        int? type, int? readingLevel, string? tag, string? q,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Trang admin: mọi trạng thái, sort UpdatedAt DESC.</summary>
    Task<(IReadOnlyList<Article> Items, int TotalCount)> GetForAdminAsync(
        int? status, string? q, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Article article, CancellationToken ct = default);
    void Remove(Article article);

    Task AddTranslationAsync(ArticleTranslation translation, CancellationToken ct = default);
    void RemoveTranslation(ArticleTranslation translation);

    Task AddAssetAsync(ArticleAsset asset, CancellationToken ct = default);
    void RemoveAsset(ArticleAsset asset);

    /// <summary>Tăng ViewCount bằng UPDATE thẳng — không load entity, không đụng UpdatedAt.</summary>
    Task IncrementViewCountAsync(string slug, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class ArticleRepository(AquaHomeDbContext db) : IArticleRepository
{
    public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Articles
            .Include(a => a.Translations)
            .Include(a => a.Assets)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Article?> GetBySlugAsync(string slug, bool publishedOnly, CancellationToken ct = default)
    {
        var query = db.Articles
            .Include(a => a.Translations)
            .Include(a => a.Assets)
            .Where(a => a.Slug == slug);

        if (publishedOnly)
            query = query.Where(a => a.Status == 1); // ArticleStatus.Published

        return query.FirstOrDefaultAsync(ct);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        => db.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct);

    public async Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPublishedAsync(
        int? type, int? readingLevel, string? tag, string? q,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Articles
            .Include(a => a.Translations)
            .Where(a => a.Status == 1); // ArticleStatus.Published

        if (type.HasValue)         query = query.Where(a => a.Type == type.Value);
        if (readingLevel.HasValue) query = query.Where(a => a.ReadingLevel == readingLevel.Value);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(a => a.Tags.Contains(tag));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(a => a.Translations.Any(t =>
                EF.Functions.ILike(t.Title, pattern) ||
                (t.Summary != null && EF.Functions.ILike(t.Summary, pattern))));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Article> Items, int TotalCount)> GetForAdminAsync(
        int? status, string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Articles
            .Include(a => a.Translations)
            .Include(a => a.Assets)
            .AsQueryable();

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.Slug, pattern) ||
                a.Translations.Any(t => EF.Functions.ILike(t.Title, pattern)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Article article, CancellationToken ct = default)
        => await db.Articles.AddAsync(article, ct);

    public void Remove(Article article) => db.Articles.Remove(article);

    public async Task AddTranslationAsync(ArticleTranslation translation, CancellationToken ct = default)
        => await db.ArticleTranslations.AddAsync(translation, ct);

    public void RemoveTranslation(ArticleTranslation translation)
        => db.ArticleTranslations.Remove(translation);

    public async Task AddAssetAsync(ArticleAsset asset, CancellationToken ct = default)
        => await db.ArticleAssets.AddAsync(asset, ct);

    public void RemoveAsset(ArticleAsset asset) => db.ArticleAssets.Remove(asset);

    public Task IncrementViewCountAsync(string slug, CancellationToken ct = default)
        => db.Articles
            .Where(a => a.Slug == slug && a.Status == 1) // ArticleStatus.Published
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

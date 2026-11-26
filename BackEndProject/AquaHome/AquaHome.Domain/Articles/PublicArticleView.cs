using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.EFCore.Entity;
using FishLover.Storage;

namespace AquaHome.Domain.Articles;

/// <summary>Người đang đọc: ngôn ngữ đã chuẩn hoá (xem <see cref="ArticleLanguages.Normalize"/>) và đã đăng nhập chưa.</summary>
public sealed record ArticleReader(string Language, bool IsAuthenticated)
{
    public static ArticleReader For(string? requestedLanguage, bool isAuthenticated)
        => new(ArticleLanguages.Normalize(requestedLanguage), isAuthenticated);
}

/// <summary>
/// Góc nhìn công khai của một bài viết: (bài, người đọc) → mục danh sách hoặc trang chi tiết đã áp luật.
/// Mọi đường trả bài cho người đọc (list, detail, related, và trang mới về sau) đi qua đây, để hai luật
/// chỉ nằm một chỗ:
/// <list type="bullet">
/// <item>Chọn bản dịch: đúng ngôn ngữ người đọc, thiếu thì en → vi → bản bất kỳ.</item>
/// <item>Khoá bài: khách chưa đăng nhập chỉ đọc được bài Beginner. Bài khoá vẫn trả metadata (khách thấy bài
/// hay mới có lý do đăng ký) nhưng KHÔNG phát presigned URL của nội dung lẫn ảnh trong bài — URL đã phát
/// ra là ai cầm cũng đọc được, nên phải chặn ở đây chứ không chỉ ẩn trên giao diện.</item>
/// </list>
/// Trả <c>null</c> khi bài chưa có bản dịch nào: không có gì để hiển thị.
/// </summary>
public sealed class PublicArticleView(IStorageService storage)
{
    public async Task<ArticleListItemDto?> ToListItemAsync(Article article, ArticleReader reader, CancellationToken ct = default)
    {
        var translation = ResolveTranslation(article, reader.Language);
        if (translation is null) return null;

        return new ArticleListItemDto(
            article.Id,
            article.Slug,
            (ArticleType)article.Type,
            (ReadingLevel)article.ReadingLevel,
            article.TemplateKey,
            article.Tags,
            translation.Language,
            translation.Title,
            translation.Summary,
            await UrlAsync(article.CoverObjectKey, ct),
            article.AuthorName,
            translation.ReadingMinutes,
            article.ViewCount,
            article.IsFeatured,
            IsLocked(article, reader),
            article.PublishedAt);
    }

    public async Task<ArticleDetailDto?> ToDetailAsync(Article article, ArticleReader reader, CancellationToken ct = default)
    {
        var translation = ResolveTranslation(article, reader.Language);
        if (translation is null) return null;

        var locked = IsLocked(article, reader);

        var assets = new List<ArticleAssetDto>(locked ? 0 : article.Assets.Count);
        if (!locked)
            foreach (var asset in article.Assets)
                assets.Add(new ArticleAssetDto(
                    asset.Id, await UrlAsync(asset.ObjectKey, ct), asset.FileName, asset.ContentType, asset.Bytes));

        return new ArticleDetailDto(
            article.Id,
            article.Slug,
            (ArticleType)article.Type,
            (ReadingLevel)article.ReadingLevel,
            article.TemplateKey,
            article.Tags,
            translation.Language,
            reader.Language,
            article.Translations.Select(t => t.Language).OrderBy(l => l).ToList(),
            translation.Title,
            translation.Summary,
            await UrlAsync(article.CoverObjectKey, ct),
            article.AuthorName,
            translation.ReadingMinutes,
            translation.WordCount,
            article.ViewCount,
            article.PublishedAt,
            translation.UpdatedAt,
            locked ? null : await UrlAsync(translation.ContentObjectKey, ct),
            locked,
            assets);
    }

    private static bool IsLocked(Article article, ArticleReader reader)
        => !reader.IsAuthenticated && (ReadingLevel)article.ReadingLevel != ReadingLevel.Beginner;

    private static ArticleTranslation? ResolveTranslation(Article article, string language)
    {
        var exact = article.Translations.FirstOrDefault(t => t.Language == language);
        if (exact is not null) return exact;

        foreach (var fallback in ArticleLanguages.Fallback)
        {
            var match = article.Translations.FirstOrDefault(t => t.Language == fallback);
            if (match is not null) return match;
        }

        return article.Translations.OrderBy(t => t.Language).FirstOrDefault();
    }

    private Task<string?> UrlAsync(string? objectKey, CancellationToken ct)
        => string.IsNullOrEmpty(objectKey)
            ? Task.FromResult<string?>(null)
            : storage.GetPresignedUrlAsync(objectKey, ct);
}

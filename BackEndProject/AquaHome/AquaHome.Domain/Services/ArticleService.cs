using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Extensions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

/// <summary>
/// Bài viết: DB giữ metadata, R2 giữ nội dung. Mỗi bản dịch là một file content.json trong
/// folder của bài; ảnh trong bài là ArticleAsset riêng, block chỉ trỏ assetId nên URL presigned
/// hết hạn cũng không làm chết nội dung đã lưu.
/// </summary>
public class ArticleService(
    IArticleRepository articleRepo,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    ICurrentUserSession currentUser,
    ILogger<ArticleService> logger) : IArticleService
{
    /// <summary>
    /// Bỏ field null và không escape unicode khi ghi content.json: mỗi block có 8 field mà chỉ
    /// dùng 2-3, còn tiếng Việt bị escape thành \uXXXX làm file phình gấp rưỡi — FE tải file này
    /// trực tiếp từ R2 nên mỗi byte đều tính. An toàn vì file chỉ được đọc như JSON, không nhúng HTML.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Khớp locale của FE. Ngôn ngữ ngoài danh sách này bị từ chối.</summary>
    private static readonly string[] SupportedLanguages = ["vi", "en", "de", "zh"];

    /// <summary>Thiếu bản dịch đang xem thì lùi theo thứ tự này (chốt cùng FE).</summary>
    private static readonly string[] FallbackLanguages = ["en", "vi"];

    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private const long MaxCoverBytes = 3 * 1024 * 1024;
    private const long MaxAssetBytes = 5 * 1024 * 1024;
    private const int MaxTags = 10;
    private const int MaxTagChars = 30;
    private const int MaxPageSize = 50;

    private string Env => storageOptions.Value.Environment;

    // ── Công khai ────────────────────────────────────────────────────────────

    /// <summary>
    /// Bài Beginner mở cho tất cả; từ Intermediate trở lên phải đăng nhập mới đọc được nội dung.
    /// Metadata thì vẫn trả cho khách — họ phải thấy bài hay thì mới có lý do đăng ký tài khoản.
    /// </summary>
    private static bool NeedsAuth(Article article)
        => (ReadingLevel)article.ReadingLevel != ReadingLevel.Beginner;

    public async Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(
        string? language, ArticleType? type, ReadingLevel? readingLevel,
        string? tag, string? q, int page, int pageSize, bool isAuthenticated, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await articleRepo.GetPublishedAsync(
            type.HasValue ? (int)type.Value : null,
            readingLevel.HasValue ? (int)readingLevel.Value : null,
            NormalizeTag(tag), q, page, pageSize, ct);

        var lang = NormalizeLanguage(language);
        var list = new List<ArticleListItemDto>(items.Count);

        foreach (var article in items)
        {
            var translation = ResolveTranslation(article, lang);
            if (translation is null) continue; // bài chưa có bản dịch nào — không có gì để hiển thị

            list.Add(new ArticleListItemDto(
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
                !isAuthenticated && NeedsAuth(article),
                article.PublishedAt));
        }

        return new PagedResult<ArticleListItemDto>
        {
            Items = list,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ArticleDetailDto?> GetBySlugAsync(
        string slug, string? language, bool isAuthenticated, CancellationToken ct = default)
    {
        var article = await articleRepo.GetBySlugAsync(slug, publishedOnly: true, ct);
        if (article is null) return null;

        var requested = NormalizeLanguage(language);
        var translation = ResolveTranslation(article, requested);
        if (translation is null) return null;

        // Bài khóa: không phát presigned URL của content.json lẫn của ảnh trong bài. Chặn ở đây
        // chứ không chỉ ẩn trên giao diện — URL đã phát ra là ai cầm cũng đọc được.
        var locked = !isAuthenticated && NeedsAuth(article);

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
            requested,
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

    public Task IncrementViewAsync(string slug, CancellationToken ct = default)
        => articleRepo.IncrementViewCountAsync(slug, ct);

    // ── Admin ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<AdminArticleDto>> GetForAdminAsync(
        ArticleStatus? status, string? q, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await articleRepo.GetForAdminAsync(
            status.HasValue ? (int)status.Value : null, q, page, pageSize, ct);

        var list = new List<AdminArticleDto>(items.Count);
        foreach (var article in items)
            list.Add(await ToAdminDtoAsync(article, ct));

        return new PagedResult<AdminArticleDto>
        {
            Items = list,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AdminArticleDto?> GetByIdForAdminAsync(Guid id, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        return article is null ? null : await ToAdminDtoAsync(article, ct);
    }

    public async Task<AdminArticleDto> CreateAsync(CreateArticleRequest request, CancellationToken ct = default)
    {
        var language = RequireSupportedLanguage(request.Language);
        var title = RequireTitle(request.Title);

        var now = DateTime.UtcNow;
        var article = new Article
        {
            Id           = Guid.NewGuid(),
            Slug         = await GenerateUniqueSlugAsync(request.Slug ?? title, null, ct),
            Type         = (int)request.Type,
            ReadingLevel = (int)request.ReadingLevel,
            Status       = (int)ArticleStatus.Draft,
            TemplateKey  = TemplateForType(request.Type),
            Tags         = NormalizeTags(request.Tags),
            AuthorUserId = currentUser.UserId,
            AuthorName   = currentUser.UserName,
            CreatedAt    = now,
            UpdatedAt    = now,
        };

        // Bài mới chưa có ảnh nào nên block image chắc chắn không hợp lệ ở bước này —
        // admin upload ảnh xong rồi mới ghi block image qua PUT translation.
        var normalized = ArticleContentBuilder.Normalize(request.Content, article.TemplateKey, []);
        var contentKey = ContentKey(article, language);
        var bytes = await WriteContentAsync(contentKey, normalized.Content, ct);

        article.Translations.Add(new ArticleTranslation
        {
            Id               = Guid.NewGuid(),
            ArticleId        = article.Id,
            Language         = language,
            Title            = title,
            Summary          = Truncate(request.Summary, 500),
            ContentObjectKey = contentKey,
            BlockCount       = normalized.BlockCount,
            WordCount        = normalized.WordCount,
            ReadingMinutes   = normalized.ReadingMinutes,
            ContentBytes     = bytes,
            UpdatedAt        = now,
        });

        await articleRepo.AddAsync(article, ct);
        await articleRepo.SaveChangesAsync(ct);

        logger.LogInformation("Article {Slug} created by {UserId}", article.Slug, currentUser.UserId);
        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<AdminArticleDto?> UpdateAsync(Guid id, UpdateArticleRequest request, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        article.Type         = (int)request.Type;
        article.ReadingLevel = (int)request.ReadingLevel;
        article.Tags         = NormalizeTags(request.Tags);
        article.IsFeatured   = request.IsFeatured;
        article.UpdatedAt    = DateTime.UtcNow;

        // Slug đổi thì link cũ chết — chỉ đổi khi admin chủ động gửi giá trị khác.
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = Slugify(request.Slug);
            if (slug != article.Slug)
                article.Slug = await GenerateUniqueSlugAsync(slug, article.Id, ct);
        }

        await articleRepo.SaveChangesAsync(ct);
        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<AdminArticleDto?> UpsertTranslationAsync(
        Guid id, string language, UpsertTranslationRequest request, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        var lang = RequireSupportedLanguage(language);
        var title = RequireTitle(request.Title);

        var assetIds = article.Assets.Select(a => a.Id).ToHashSet();
        var normalized = ArticleContentBuilder.Normalize(request.Content, article.TemplateKey, assetIds);

        var now = DateTime.UtcNow;
        var contentKey = ContentKey(article, lang);
        var bytes = await WriteContentAsync(contentKey, normalized.Content, ct);

        var translation = article.Translations.FirstOrDefault(t => t.Language == lang);
        if (translation is null)
        {
            translation = new ArticleTranslation
            {
                Id        = Guid.NewGuid(),
                ArticleId = article.Id,
                Language  = lang,
            };
            await articleRepo.AddTranslationAsync(translation, ct);
            article.Translations.Add(translation);
        }

        translation.Title            = title;
        translation.Summary          = Truncate(request.Summary, 500);
        translation.ContentObjectKey = contentKey;
        translation.BlockCount       = normalized.BlockCount;
        translation.WordCount        = normalized.WordCount;
        translation.ReadingMinutes   = normalized.ReadingMinutes;
        translation.ContentBytes     = bytes;
        translation.UpdatedAt        = now;

        article.UpdatedAt = now;

        await articleRepo.SaveChangesAsync(ct);
        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<AdminArticleDto?> DeleteTranslationAsync(Guid id, string language, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        var lang = RequireSupportedLanguage(language);
        var translation = article.Translations.FirstOrDefault(t => t.Language == lang);
        if (translation is null) return await ToAdminDtoAsync(article, ct);

        // Xóa bản dịch cuối cùng thì bài mất luôn tiêu đề — muốn bỏ hẳn thì xóa cả bài.
        if (article.Translations.Count == 1)
            throw new ArticleValidationException(
                ["Không xóa được bản dịch cuối cùng — hãy xóa cả bài viết nếu muốn bỏ."]);

        articleRepo.RemoveTranslation(translation);
        article.Translations.Remove(translation);
        article.UpdatedAt = DateTime.UtcNow;

        await articleRepo.SaveChangesAsync(ct);
        await storage.DeleteAsync(translation.ContentObjectKey, ct);

        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<ArticleAssetDto?> AddAssetAsync(Guid id, UploadedFile file, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        RequireImage(file, MaxAssetBytes);

        var asset = new ArticleAsset
        {
            Id          = Guid.NewGuid(),
            ArticleId   = article.Id,
            ContentType = file.ContentType,
            FileName    = Truncate(file.FileName, 260),
            Bytes       = file.Content.Length,
            CreatedAt   = DateTime.UtcNow,
        };
        asset.ObjectKey = $"{article.ObjectKeyPrefix(Env)}/assets/{asset.Id}{ExtensionFor(file)}";

        await storage.PutObjectAsync(asset.ObjectKey, file.Content, file.ContentType, ct);

        await articleRepo.AddAssetAsync(asset, ct);
        article.UpdatedAt = DateTime.UtcNow;
        await articleRepo.SaveChangesAsync(ct);

        return new ArticleAssetDto(
            asset.Id, await UrlAsync(asset.ObjectKey, ct), asset.FileName, asset.ContentType, asset.Bytes);
    }

    public async Task<bool> DeleteAssetAsync(Guid id, Guid assetId, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        var asset = article?.Assets.FirstOrDefault(a => a.Id == assetId);
        if (article is null || asset is null) return false;

        // Block image còn trỏ tới assetId này sẽ thành ảnh thiếu — FE bỏ qua block không tìm được
        // asset, nên bài vẫn đọc được; admin tự dọn block trong lần sửa nội dung sau.
        articleRepo.RemoveAsset(asset);
        article.Assets.Remove(asset);
        article.UpdatedAt = DateTime.UtcNow;

        await articleRepo.SaveChangesAsync(ct);
        await storage.DeleteAsync(asset.ObjectKey, ct);
        return true;
    }

    public async Task<AdminArticleDto?> SetCoverAsync(Guid id, UploadedFile file, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        RequireImage(file, MaxCoverBytes);

        var key = $"{article.ObjectKeyPrefix(Env)}/cover{ExtensionFor(file)}";
        await storage.PutObjectAsync(key, file.Content, file.ContentType, ct);

        // Đổi định dạng ảnh (jpg sang webp) làm key khác nhau, phải dọn file cũ không thì mồ côi.
        var previous = article.CoverObjectKey;
        article.CoverObjectKey = key;
        article.UpdatedAt = DateTime.UtcNow;
        await articleRepo.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(previous) && previous != key)
            await storage.DeleteAsync(previous, ct);

        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<AdminArticleDto?> SetStatusAsync(Guid id, ArticleStatus status, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return null;

        if (status == ArticleStatus.Published && !article.Translations.Any(t => t.BlockCount > 0))
            throw new ArticleValidationException(
                ["Bài chưa có nội dung — thêm ít nhất 1 block vào thân bài trước khi publish."]);

        article.Status = (int)status;
        // Giữ nguyên PublishedAt của lần publish đầu: unpublish rồi publish lại không đẩy bài
        // lên đầu danh sách như bài mới.
        if (status == ArticleStatus.Published)
            article.PublishedAt ??= DateTime.UtcNow;

        article.UpdatedAt = DateTime.UtcNow;
        await articleRepo.SaveChangesAsync(ct);

        return await ToAdminDtoAsync(article, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var article = await articleRepo.GetByIdAsync(id, ct);
        if (article is null) return false;

        var prefix = article.ObjectKeyPrefix(Env);
        var slug = article.Slug;

        articleRepo.Remove(article); // cascade xóa Translations + Assets
        await articleRepo.SaveChangesAsync(ct);

        await storage.DeleteByPrefixAsync(prefix, ct);
        logger.LogInformation("Article {Slug} deleted by {UserId}", slug, currentUser.UserId);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<AdminArticleDto> ToAdminDtoAsync(Article article, CancellationToken ct)
    {
        var translations = new List<AdminArticleTranslationDto>(article.Translations.Count);
        foreach (var t in article.Translations.OrderBy(t => t.Language))
            translations.Add(new AdminArticleTranslationDto(
                t.Language, t.Title, t.Summary, t.BlockCount, t.WordCount, t.ReadingMinutes,
                await UrlAsync(t.ContentObjectKey, ct), t.UpdatedAt));

        var assets = new List<ArticleAssetDto>(article.Assets.Count);
        foreach (var a in article.Assets.OrderBy(a => a.CreatedAt))
            assets.Add(new ArticleAssetDto(
                a.Id, await UrlAsync(a.ObjectKey, ct), a.FileName, a.ContentType, a.Bytes));

        return new AdminArticleDto(
            article.Id,
            article.Slug,
            (ArticleType)article.Type,
            (ReadingLevel)article.ReadingLevel,
            (ArticleStatus)article.Status,
            article.TemplateKey,
            article.Tags,
            await UrlAsync(article.CoverObjectKey, ct),
            article.AuthorName,
            article.IsFeatured,
            article.ViewCount,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            translations,
            assets);
    }

    private Task<string?> UrlAsync(string? objectKey, CancellationToken ct)
        => string.IsNullOrEmpty(objectKey)
            ? Task.FromResult<string?>(null)
            : storage.GetPresignedUrlAsync(objectKey, ct);

    private async Task<long> WriteContentAsync(string objectKey, ArticleContentDto content, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(content, JsonOptions);
        await storage.PutObjectAsync(objectKey, json, "application/json", ct);
        return json.Length;
    }

    private string ContentKey(Article article, string language)
        => $"{article.ObjectKeyPrefix(Env)}/{language}/content.json";

    /// <summary>Phase này mọi loại bài dùng chung template "standard".</summary>
    private static string TemplateForType(ArticleType type) => "standard";

    private static ArticleTranslation? ResolveTranslation(Article article, string language)
    {
        var exact = article.Translations.FirstOrDefault(t => t.Language == language);
        if (exact is not null) return exact;

        foreach (var fallback in FallbackLanguages)
        {
            var match = article.Translations.FirstOrDefault(t => t.Language == fallback);
            if (match is not null) return match;
        }

        return article.Translations.OrderBy(t => t.Language).FirstOrDefault();
    }

    private static string NormalizeLanguage(string? language)
    {
        var trimmed = language?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return "vi";

        // FE có thể gửi "vi-VN" / "en-US" — chỉ lấy phần ngôn ngữ.
        var code = trimmed[..Math.Min(2, trimmed.Length)].ToLowerInvariant();
        return SupportedLanguages.Contains(code) ? code : "vi";
    }

    private static string RequireSupportedLanguage(string? language)
    {
        var code = language?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(code) || !SupportedLanguages.Contains(code))
            throw new ArticleValidationException(
                [$"Ngôn ngữ '{language}' không hỗ trợ (chỉ nhận {string.Join(", ", SupportedLanguages)})"]);
        return code;
    }

    private static string RequireTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArticleValidationException(["Tiêu đề không được để trống"]);
        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }

    private static void RequireImage(UploadedFile file, long maxBytes)
    {
        var errors = new List<string>();

        if (!AllowedImageTypes.Contains(file.ContentType?.ToLowerInvariant() ?? string.Empty))
            errors.Add($"Định dạng ảnh '{file.ContentType}' không hỗ trợ (chỉ nhận {string.Join(", ", AllowedImageTypes)})");

        if (file.Content.Length == 0)
            errors.Add("File ảnh rỗng");
        else if (file.Content.Length > maxBytes)
            errors.Add($"Ảnh vượt {maxBytes / 1024 / 1024}MB (đang {file.Content.Length / 1024 / 1024.0:0.0}MB)");

        if (errors.Count > 0) throw new ArticleValidationException(errors);
    }

    private static string ExtensionFor(UploadedFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!string.IsNullOrEmpty(ext) && ext.Length <= 5) return ext.ToLowerInvariant();

        return file.ContentType?.ToLowerInvariant() switch
        {
            "image/png"  => ".png",
            "image/webp" => ".webp",
            "image/gif"  => ".gif",
            _            => ".jpg",
        };
    }

    private static List<string> NormalizeTags(IReadOnlyList<string>? tags)
        => (tags ?? [])
            .Select(NormalizeTag)
            .Where(t => !string.IsNullOrEmpty(t))
            .Cast<string>()
            .Distinct()
            .Take(MaxTags)
            .ToList();

    private static string? NormalizeTag(string? tag)
    {
        var slug = Slugify(tag ?? string.Empty);
        if (string.IsNullOrEmpty(slug)) return null;
        return slug.Length > MaxTagChars ? slug[..MaxTagChars] : slug;
    }

    private async Task<string> GenerateUniqueSlugAsync(string source, Guid? excludeId, CancellationToken ct)
    {
        var baseSlug = Slugify(source);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "bai-viet";

        if (!await articleRepo.SlugExistsAsync(baseSlug, excludeId, ct)) return baseSlug;

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{suffix++}";
        } while (await articleRepo.SlugExistsAsync(candidate, excludeId, ct));

        return candidate;
    }

    /// <summary>Bỏ dấu tiếng Việt, chữ thường, ký tự lạ thành gạch ngang.</summary>
    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            // FormD không tách được đ/Đ — map tay, không thì slug lọt ký tự phải percent-encode.
            if (c is 'đ') sb.Append('d');
            else if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.' or '/') sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');

        return slug.Length > 150 ? slug[..150].Trim('-') : slug;
    }

    private static string? Truncate(string? value, int maxChars)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return trimmed.Length > maxChars ? trimmed[..maxChars] : trimmed;
    }
}

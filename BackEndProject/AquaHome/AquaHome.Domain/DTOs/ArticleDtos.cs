using AquaHome.Domain.Enums;

namespace AquaHome.Domain.DTOs;

/// <summary>File admin upload (thumbnail hoặc ảnh trong bài) — Domain không phụ thuộc IFormFile.</summary>
public record UploadedFile(string FileName, string ContentType, byte[] Content);

public record ArticleAssetDto(Guid Id, string? Url, string? FileName, string ContentType, long Bytes);

/// <summary>Card ở trang list — chỉ metadata, không kèm nội dung.</summary>
public record ArticleListItemDto(
    Guid Id,
    string Slug,
    ArticleType Type,
    ReadingLevel ReadingLevel,
    string TemplateKey,
    IReadOnlyList<string> Tags,
    string Language,               // ngôn ngữ thực trả về (có thể là bản fallback)
    string Title,
    string? Summary,
    string? ThumbnailUrl,
    string? AuthorName,
    int ReadingMinutes,
    int ViewCount,
    bool IsFeatured,
    /// <summary>Khách chưa đăng nhập chỉ đọc được bài Beginner — cờ này để FE gắn ổ khóa lên card.</summary>
    bool RequiresAuth,
    DateTime? PublishedAt);

/// <summary>
/// Trang detail. Nội dung KHÔNG nhúng trong response: FE fetch ContentUrl (presigned GET tới
/// content.json trên R2) rồi render theo TemplateKey, ảnh lấy từ map Assets theo assetId.
/// </summary>
public record ArticleDetailDto(
    Guid Id,
    string Slug,
    ArticleType Type,
    ReadingLevel ReadingLevel,
    string TemplateKey,
    IReadOnlyList<string> Tags,
    string Language,
    string RequestedLanguage,
    IReadOnlyList<string> AvailableLanguages,
    string Title,
    string? Summary,
    string? ThumbnailUrl,
    string? AuthorName,
    int ReadingMinutes,
    int WordCount,
    int ViewCount,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    /// <summary>Null khi bài yêu cầu đăng nhập mà người gọi chưa đăng nhập — không phát URL nội dung.</summary>
    string? ContentUrl,
    /// <summary>True = bài này cần đăng nhập mới đọc được nội dung.</summary>
    bool RequiresAuth,
    IReadOnlyList<ArticleAssetDto> Assets);

public record AdminArticleTranslationDto(
    string Language,
    string Title,
    string? Summary,
    int BlockCount,
    int WordCount,
    int ReadingMinutes,
    string? ContentUrl,
    DateTime UpdatedAt);

public record AdminArticleDto(
    Guid Id,
    string Slug,
    ArticleType Type,
    ReadingLevel ReadingLevel,
    ArticleStatus Status,
    string TemplateKey,
    IReadOnlyList<string> Tags,
    string? ThumbnailUrl,
    string? AuthorName,
    bool IsFeatured,
    int ViewCount,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AdminArticleTranslationDto> Translations,
    IReadOnlyList<ArticleAssetDto> Assets);

public record CreateArticleRequest(
    ArticleType Type,
    ReadingLevel ReadingLevel,
    IReadOnlyList<string>? Tags,
    /// <summary>Bỏ trống thì BE tự sinh từ Title (bỏ dấu, gạch ngang).</summary>
    string? Slug,
    string Language,
    string Title,
    string? Summary,
    ArticleContentInput? Content);

public record UpdateArticleRequest(
    ArticleType Type,
    ReadingLevel ReadingLevel,
    IReadOnlyList<string>? Tags,
    string? Slug,
    bool IsFeatured);

public record UpsertTranslationRequest(
    string Title,
    string? Summary,
    ArticleContentInput? Content);

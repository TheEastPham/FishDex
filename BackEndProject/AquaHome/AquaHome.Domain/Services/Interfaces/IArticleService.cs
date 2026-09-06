using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using FishLover.Shared.Common;

namespace AquaHome.Domain.Services.Interfaces;

public interface IArticleService
{
    // ── Công khai ────────────────────────────────────────────────────────────
    /// <summary>isAuthenticated quyết định bài nào mở nội dung — khách chỉ đọc được bài Beginner.</summary>
    Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(
        string? language, ArticleType? type, ReadingLevel? readingLevel,
        string? tag, string? q, int page, int pageSize, bool isAuthenticated, CancellationToken ct = default);

    Task<ArticleDetailDto?> GetBySlugAsync(string slug, string? language, bool isAuthenticated, CancellationToken ct = default);

    Task IncrementViewAsync(string slug, CancellationToken ct = default);

    // ── Admin ────────────────────────────────────────────────────────────────
    Task<PagedResult<AdminArticleDto>> GetForAdminAsync(
        ArticleStatus? status, string? q, int page, int pageSize, CancellationToken ct = default);

    Task<AdminArticleDto?> GetByIdForAdminAsync(Guid id, CancellationToken ct = default);

    Task<AdminArticleDto> CreateAsync(CreateArticleRequest request, CancellationToken ct = default);
    Task<AdminArticleDto?> UpdateAsync(Guid id, UpdateArticleRequest request, CancellationToken ct = default);

    /// <summary>Tạo mới hoặc ghi đè bản dịch: validate block → ghi content.json lên R2 → cập nhật DB.</summary>
    Task<AdminArticleDto?> UpsertTranslationAsync(
        Guid id, string language, UpsertTranslationRequest request, CancellationToken ct = default);

    Task<AdminArticleDto?> DeleteTranslationAsync(Guid id, string language, CancellationToken ct = default);

    /// <summary>Ảnh dùng trong bài — trả về assetId để admin chèn vào block image.</summary>
    Task<ArticleAssetDto?> AddAssetAsync(Guid id, UploadedFile file, CancellationToken ct = default);
    Task<bool> DeleteAssetAsync(Guid id, Guid assetId, CancellationToken ct = default);

    Task<AdminArticleDto?> SetCoverAsync(Guid id, UploadedFile file, CancellationToken ct = default);

    Task<AdminArticleDto?> SetStatusAsync(Guid id, ArticleStatus status, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

/// <summary>
/// Quản lý bài viết — ContentAdmin và SystemAdmin. Luồng chuẩn khi soạn bài:
/// tạo bài (metadata + bản dịch đầu) → upload thumbnail → upload ảnh trong bài (nhận assetId)
/// → PUT bản dịch kèm block nội dung → publish.
/// </summary>
[ApiController]
[Route("api/admin/articles")]
[Authorize(Policy = "RequireContentAdmin")]
public class AdminArticlesController(IArticleService articleService) : ControllerBase
{
    private const long MaxUploadBytes = 6 * 1024 * 1024; // ảnh trong bài tối đa 5MB + phần bao multipart

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] ArticleStatus? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await articleService.GetForAdminAsync(status, q, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var article = await articleService.GetByIdForAdminAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Tạo bài mới (luôn ở trạng thái Draft). Content được phép rỗng để soạn dần.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request, CancellationToken ct)
    {
        var article = await articleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    /// <summary>Sửa metadata: type, level, tag, slug, ghim. Nội dung sửa ở endpoint bản dịch.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest request, CancellationToken ct)
    {
        var article = await articleService.UpdateAsync(id, request, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Tạo mới hoặc ghi đè bản dịch (title + summary + block nội dung).</summary>
    [HttpPut("{id:guid}/translations/{language}")]
    public async Task<IActionResult> UpsertTranslation(
        Guid id, string language, [FromBody] UpsertTranslationRequest request, CancellationToken ct)
    {
        var article = await articleService.UpsertTranslationAsync(id, language, request, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpDelete("{id:guid}/translations/{language}")]
    public async Task<IActionResult> DeleteTranslation(Guid id, string language, CancellationToken ct)
    {
        var article = await articleService.DeleteTranslationAsync(id, language, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Upload ảnh dùng trong bài — trả assetId để chèn vào block image.</summary>
    [HttpPost("{id:guid}/assets")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> AddAsset(Guid id, IFormFile file, CancellationToken ct)
    {
        var uploaded = await ReadFileAsync(file, ct);
        if (uploaded is null) return BadRequest(new { error = "missing_file", message = "Chưa chọn file ảnh." });

        var asset = await articleService.AddAssetAsync(id, uploaded, ct);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid id, Guid assetId, CancellationToken ct)
    {
        var deleted = await articleService.DeleteAssetAsync(id, assetId, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Thumbnail của bài — ghi đè ảnh cũ.</summary>
    [HttpPost("{id:guid}/cover")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> SetCover(Guid id, IFormFile file, CancellationToken ct)
    {
        var uploaded = await ReadFileAsync(file, ct);
        if (uploaded is null) return BadRequest(new { error = "missing_file", message = "Chưa chọn file ảnh." });

        var article = await articleService.SetCoverAsync(id, uploaded, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken ct) => SetStatus(id, ArticleStatus.Published, ct);

    /// <summary>Gỡ khỏi trang công khai, đưa về Draft — giữ nguyên PublishedAt của lần publish đầu.</summary>
    [HttpPost("{id:guid}/unpublish")]
    public Task<IActionResult> Unpublish(Guid id, CancellationToken ct) => SetStatus(id, ArticleStatus.Draft, ct);

    [HttpPost("{id:guid}/archive")]
    public Task<IActionResult> Archive(Guid id, CancellationToken ct) => SetStatus(id, ArticleStatus.Archived, ct);

    /// <summary>Xóa hẳn bài: hàng DB (cascade bản dịch + ảnh) và cả folder của bài trên R2.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await articleService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult> SetStatus(Guid id, ArticleStatus status, CancellationToken ct)
    {
        var article = await articleService.SetStatusAsync(id, status, ct);
        return article is null ? NotFound() : Ok(article);
    }

    private static async Task<UploadedFile?> ReadFileAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        return new UploadedFile(
            file.FileName ?? "image",
            file.ContentType ?? "application/octet-stream",
            buffer.ToArray());
    }
}

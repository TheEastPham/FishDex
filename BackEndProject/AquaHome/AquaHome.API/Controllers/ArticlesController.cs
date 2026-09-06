using AquaHome.Domain.Enums;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

/// <summary>
/// Bài viết công khai. Response chỉ có metadata: nội dung nằm ở <c>contentUrl</c> (presigned GET
/// tới content.json trên R2) để FE fetch rồi render theo <c>templateKey</c>, ảnh lấy từ mảng
/// <c>assets</c> theo assetId của block image.
/// </summary>
[ApiController]
[Route("api/articles")]
public class ArticlesController(IArticleService articleService) : ControllerBase
{
    /// <summary>Danh sách bài đã publish — filter theo type/level/tag/từ khóa, phân trang.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetList(
        [FromQuery] string? lang,
        [FromQuery] ArticleType? type,
        [FromQuery] ReadingLevel? level,
        [FromQuery] string? tag,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await articleService.GetPublishedAsync(
            lang, type, level, tag, q, page, pageSize, await IsSignedInAsync(), ct);
        return Ok(result);
    }

    /// <summary>Chi tiết bài. Thiếu bản dịch của lang thì trả bản fallback (en → vi) và báo qua field language.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] string? lang, CancellationToken ct)
    {
        var article = await articleService.GetBySlugAsync(slug, lang, await IsSignedInAsync(), ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>
    /// Hai endpoint trên là AllowAnonymous nên middleware chỉ thử scheme mặc định (Bearer).
    /// Token của FE lại do OpenIddict phát, nên phải hỏi thêm scheme đó — thiếu bước này thì
    /// người đã đăng nhập vẫn bị coi là khách và bài Intermediate/Advanced khóa nhầm.
    /// </summary>
    private async Task<bool> IsSignedInAsync()
    {
        if (User.Identity?.IsAuthenticated == true) return true;

        var result = await HttpContext.AuthenticateAsync("OpenIddict");
        return result.Succeeded;
    }

    /// <summary>Đếm lượt xem. Không auth — FE gọi một lần khi mở trang detail.</summary>
    [HttpPost("{slug}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> IncrementView(string slug, CancellationToken ct)
    {
        await articleService.IncrementViewAsync(slug, ct);
        return NoContent();
    }
}

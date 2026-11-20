using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/public/snapshots")]
public class PublicSnapshotsController(ISnapshotService snapshotService) : ControllerBase
{
    /// <summary>Gallery — filter theo waterType/style/contest, sort likes|newest, phân trang. Không cần auth.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetGallery(
        [FromQuery] int? waterType,
        [FromQuery] int? style,
        [FromQuery] string? contest,
        [FromQuery] string sort = "likes",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await snapshotService.GetGalleryAsync(waterType, style, contest, sort, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Chi tiết 1 snapshot. Không cần auth.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var snapshot = await snapshotService.GetBySlugAsync(slug, ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>Like — tránh abuse, 1 user / 1 snapshot. Auth required.</summary>
    [HttpPost("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> Like(Guid id, CancellationToken ct)
    {
        var ok = await snapshotService.LikeAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> Unlike(Guid id, CancellationToken ct)
    {
        var ok = await snapshotService.UnlikeAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}

using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[Authorize]
[ApiController]
[Route("api/aquariums/{aquariumId:guid}/snapshot")]
public class AquariumSnapshotController(ISnapshotService snapshotService) : ControllerBase
{
    /// <summary>Gen preview (không lưu DB) để user xem trước fish list + world map + cover photo.</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(Guid aquariumId, CancellationToken ct)
    {
        var preview = await snapshotService.PreviewAsync(aquariumId, ct);
        return preview is null ? NotFound() : Ok(preview);
    }

    /// <summary>Confirm publish — lưu DB; auto-archive snapshot cũ nhất nếu đã đủ 5.</summary>
    [HttpPost("publish")]
    public async Task<IActionResult> Publish(Guid aquariumId, [FromBody] PublishSnapshotRequest request, CancellationToken ct)
    {
        var snapshot = await snapshotService.PublishAsync(aquariumId, request, ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }
}

[Authorize]
[ApiController]
[Route("api/snapshots")]
public class SnapshotsController(ISnapshotService snapshotService) : ControllerBase
{
    /// <summary>User có thể unpublish bất kỳ lúc nào.</summary>
    [HttpPatch("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        var ok = await snapshotService.UnpublishAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}

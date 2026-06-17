using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[Authorize]
[ApiController]
[Route("api/aquariums/{aquariumId:guid}/media")]
public class AquariumMediaController(IAquariumMediaService mediaService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách ảnh của bể, kèm presigned GET URL.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMedia(Guid aquariumId, CancellationToken ct)
    {
        var list = await mediaService.GetMediaAsync(aquariumId, ct);
        return Ok(list);
    }

    /// <summary>
    /// Bước 1 — Request presigned PUT URL để FE upload thẳng lên MinIO.
    /// Body: { fileName: "photo.jpg", contentType: "image/jpeg" }
    /// </summary>
    [HttpPost("presign")]
    public async Task<IActionResult> RequestUpload(
        Guid aquariumId,
        [FromBody] PresignRequest request,
        CancellationToken ct)
    {
        var result = await mediaService.RequestUploadAsync(
            aquariumId, request.FileName, request.ContentType, ct);

        if (result is null)
            return BadRequest(new { error = "Upload request failed. Check ownership, photo limit (max 10), or content type." });

        return Ok(result);
    }

    /// <summary>
    /// Bước 2 — FE đã PUT xong lên MinIO, gọi endpoint này để confirm.
    /// </summary>
    [HttpPost("{mediaId:guid}/confirm")]
    public async Task<IActionResult> ConfirmUpload(
        Guid aquariumId, Guid mediaId, CancellationToken ct)
    {
        var dto = await mediaService.ConfirmUploadAsync(aquariumId, mediaId, ct);
        if (dto is null)
            return NotFound();

        return Ok(dto);
    }

    /// <summary>Xóa ảnh khỏi bể và storage.</summary>
    [HttpDelete("{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(
        Guid aquariumId, Guid mediaId, CancellationToken ct)
    {
        var deleted = await mediaService.DeleteAsync(aquariumId, mediaId, ct);
        return deleted ? NoContent() : NotFound();
    }
}

public record PresignRequest(string FileName, string ContentType);

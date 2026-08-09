using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Community species (loài lai tạo không có trong FishBase) — user submit + admin moderation.
/// Lưu vào SpeciesSnapshot (Community, SpecCode ≥ 500000). Loài verified sẽ hiện ở search/detail.
/// </summary>
[ApiController]
[Route("api/species/community")]
[Authorize]
public class CommunitySpeciesController(ICommunitySpeciesService service) : ControllerBase
{
    /// <summary>
    /// Tra tên gần giống trước khi submit — FE gọi khi user gõ xong tên loài, rồi hiện
    /// "có phải bạn định nói…". Không chặn submit, chỉ tư vấn.
    /// </summary>
    [HttpGet("similar")]
    public async Task<ActionResult<IReadOnlyList<SimilarSpeciesDto>>> FindSimilar(
        [FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Ok(Array.Empty<SimilarSpeciesDto>());

        return Ok(await service.FindSimilarAsync(name, ct));
    }

    /// <summary>User submit 1 loài lai tạo mới. Trả về bản ghi vừa tạo (IsVerified=false, chờ duyệt).</summary>
    [HttpPost]
    public async Task<ActionResult<CommunitySpeciesDto>> Submit(
        [FromBody] SubmitCommunitySpeciesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SpeciesName))
            return BadRequest("SpeciesName là bắt buộc.");

        var created = await service.SubmitAsync(request, ct);
        return CreatedAtAction(nameof(GetMine), new { }, created);
    }

    /// <summary>Chủ sở hữu sửa lại loài đang chờ duyệt (vd: gõ nhầm). Chỉ cho sửa khi chưa duyệt/từ chối.</summary>
    [HttpPatch("{specCode:int}")]
    public async Task<IActionResult> Update(int specCode, [FromBody] SubmitCommunitySpeciesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SpeciesName))
            return BadRequest("SpeciesName là bắt buộc.");

        var result = await service.UpdateAsync(specCode, request, ct);
        return result.Outcome switch
        {
            UpdateCommunitySpeciesOutcome.Updated    => Ok(result.Dto),
            UpdateCommunitySpeciesOutcome.NotFound   => NotFound(),
            UpdateCommunitySpeciesOutcome.NotPending => BadRequest("Loài này đã được duyệt hoặc từ chối, không thể sửa."),
            _                                        => BadRequest(),
        };
    }

    /// <summary>Danh sách loài community do user hiện tại gửi (mọi trạng thái).</summary>
    [HttpGet("mine")]
    public Task<IReadOnlyList<CommunitySpeciesDto>> GetMine(CancellationToken ct)
        => service.GetMineAsync(ct);

    /// <summary>Danh sách chờ duyệt — admin.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = "RequireContentAdmin")]
    public Task<IReadOnlyList<CommunitySpeciesDto>> GetPending(CancellationToken ct)
        => service.GetPendingAsync(ct);

    /// <summary>Admin duyệt → loài xuất hiện ở search/detail. Body optional để xác nhận lại Kind.</summary>
    [HttpPatch("{specCode:int}/verify")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Verify(int specCode, [FromBody] VerifyCommunitySpeciesRequest? request, CancellationToken ct)
        => await service.VerifyAsync(specCode, request?.Kind, ct) ? NoContent() : NotFound();

    /// <summary>Admin từ chối (kèm lý do).</summary>
    [HttpPatch("{specCode:int}/reject")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Reject(int specCode, [FromBody] RejectCommunitySpeciesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason là bắt buộc.");

        return await service.RejectAsync(specCode, request.Reason, ct) ? NoContent() : NotFound();
    }

    /// <summary>Chủ sở hữu tự xoá hẳn loài mình đã gửi (chưa được duyệt — kể cả đã bị admin từ chối).</summary>
    [HttpDelete("{specCode:int}")]
    public async Task<IActionResult> Delete(int specCode, CancellationToken ct)
        => await service.DeleteAsync(specCode, ct) ? NoContent() : NotFound();

    /// <summary>Chủ sở hữu (người submit) xin presigned URL để upload ảnh cho loài vừa gửi.</summary>
    [HttpPost("{specCode:int}/image/presign")]
    public async Task<ActionResult<CommunityImageUploadResultDto>> RequestImageUpload(
        int specCode, [FromBody] CommunityImageUploadRequest request, CancellationToken ct)
    {
        var result = await service.RequestImageUploadAsync(specCode, request.FileName, request.ContentType, ct);
        return result is null ? BadRequest("Không thể tạo upload URL — kiểm tra định dạng ảnh hoặc quyền sở hữu.") : Ok(result);
    }
}

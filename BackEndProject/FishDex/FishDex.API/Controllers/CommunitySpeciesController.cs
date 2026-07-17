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

    /// <summary>Danh sách loài community do user hiện tại gửi (mọi trạng thái).</summary>
    [HttpGet("mine")]
    public Task<IReadOnlyList<CommunitySpeciesDto>> GetMine(CancellationToken ct)
        => service.GetMineAsync(ct);

    /// <summary>Danh sách chờ duyệt — admin.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = "RequireContentAdmin")]
    public Task<IReadOnlyList<CommunitySpeciesDto>> GetPending(CancellationToken ct)
        => service.GetPendingAsync(ct);

    /// <summary>Admin duyệt → loài xuất hiện ở search/detail.</summary>
    [HttpPatch("{specCode:int}/verify")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Verify(int specCode, CancellationToken ct)
        => await service.VerifyAsync(specCode, ct) ? NoContent() : NotFound();

    /// <summary>Admin từ chối (kèm lý do).</summary>
    [HttpPatch("{specCode:int}/reject")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Reject(int specCode, [FromBody] RejectCommunitySpeciesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason là bắt buộc.");

        return await service.RejectAsync(specCode, request.Reason, ct) ? NoContent() : NotFound();
    }
}

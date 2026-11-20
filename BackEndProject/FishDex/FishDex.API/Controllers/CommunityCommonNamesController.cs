using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// User đóng góp tên địa phương (tiếng Việt) cho loài FishBase — submit + admin moderation.
/// Lưu vào CommonNames (ContributedBy != null); tên verified hiện ở detail/search/summaries.
/// </summary>
[ApiController]
[Route("api/species")]
[Authorize]
public class CommunityCommonNamesController(ICommunityCommonNameService service) : ControllerBase
{
    /// <summary>Gửi 1 tên địa phương cho loài {specCode} (FishBase).</summary>
    [HttpPost("{specCode:int}/common-names")]
    public async Task<IActionResult> Submit(int specCode, [FromBody] SubmitCommonNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ComName))
            return BadRequest("ComName là bắt buộc.");

        var result = await service.SubmitAsync(specCode, request, ct);
        return result.Outcome switch
        {
            SubmitCommonNameOutcome.Created        => StatusCode(StatusCodes.Status201Created, result.Dto),
            SubmitCommonNameOutcome.SpeciesNotFound => NotFound($"Không tìm thấy loài #{specCode}."),
            SubmitCommonNameOutcome.Duplicate       => Conflict("Tên này đã tồn tại cho loài."),
            SubmitCommonNameOutcome.PendingExists    => Conflict("Bạn đã gửi 1 tên khác cho loài này và đang chờ duyệt."),
            SubmitCommonNameOutcome.UnsupportedLanguage => BadRequest("Ngôn ngữ không được hỗ trợ."),
            _                                       => BadRequest(),
        };
    }

    /// <summary>Chủ sở hữu sửa lại tên đang chờ duyệt (vd: gõ nhầm). Chỉ cho sửa khi chưa được duyệt/từ chối.</summary>
    [HttpPatch("common-names/{autoCtr:int}")]
    public async Task<IActionResult> Update(int autoCtr, [FromBody] UpdateCommonNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ComName))
            return BadRequest("ComName là bắt buộc.");

        var result = await service.UpdateAsync(autoCtr, request.ComName, ct);
        return result.Outcome switch
        {
            UpdateCommonNameOutcome.Updated    => Ok(result.Dto),
            UpdateCommonNameOutcome.NotFound   => NotFound(),
            UpdateCommonNameOutcome.NotPending => BadRequest("Tên này đã được duyệt hoặc từ chối, không thể sửa."),
            UpdateCommonNameOutcome.Duplicate  => Conflict("Tên này đã tồn tại cho loài."),
            _                                  => BadRequest(),
        };
    }

    /// <summary>Tên do user hiện tại đã gửi (mọi trạng thái).</summary>
    [HttpGet("common-names/mine")]
    public Task<IReadOnlyList<CommunityCommonNameDto>> GetMine(CancellationToken ct)
        => service.GetMineAsync(ct);

    /// <summary>Danh sách chờ duyệt — admin.</summary>
    [HttpGet("common-names/pending")]
    [Authorize(Policy = "RequireContentAdmin")]
    public Task<IReadOnlyList<CommunityCommonNameDto>> GetPending(CancellationToken ct)
        => service.GetPendingAsync(ct);

    [HttpPatch("common-names/{autoCtr:int}/verify")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Verify(int autoCtr, CancellationToken ct)
        => await service.VerifyAsync(autoCtr, ct) ? NoContent() : NotFound();

    /// <summary>Duyệt hàng loạt nhiều tên trong 1 request.</summary>
    [HttpPatch("common-names/verify-batch")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> VerifyBatch([FromBody] VerifyCommonNamesBatchRequest request, CancellationToken ct)
    {
        var count = await service.VerifyBatchAsync(request.AutoCtrs ?? [], ct);
        return Ok(new { verified = count });
    }

    [HttpPatch("common-names/{autoCtr:int}/reject")]
    [Authorize(Policy = "RequireContentAdmin")]
    public async Task<IActionResult> Reject(int autoCtr, [FromBody] RejectCommonNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason là bắt buộc.");

        return await service.RejectAsync(autoCtr, request.Reason, ct) ? NoContent() : NotFound();
    }

    /// <summary>Chủ sở hữu tự xoá hẳn tên mình đã gửi (chưa được duyệt — kể cả đã bị từ chối).</summary>
    [HttpDelete("common-names/{autoCtr:int}")]
    public async Task<IActionResult> Delete(int autoCtr, CancellationToken ct)
        => await service.DeleteAsync(autoCtr, ct) ? NoContent() : NotFound();
}

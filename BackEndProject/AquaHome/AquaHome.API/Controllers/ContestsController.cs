using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/contests")]
public class ContestsController(IContestService contestService) : ControllerBase
{
    /// <summary>Danh sách contest Active. Không cần auth.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Ok(await contestService.GetActiveAsync(ct));

    /// <summary>Leaderboard — sort YouTubeViewCount desc. Không cần auth.</summary>
    [HttpGet("{id:guid}/leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard(Guid id, CancellationToken ct)
        => Ok(await contestService.GetLeaderboardAsync(id, ct));

    /// <summary>User submit entry: presigned PUT URL cho video lên R2 + chọn AquariumSnapshot.</summary>
    [Authorize]
    [HttpPost("{id:guid}/entries")]
    public async Task<IActionResult> SubmitEntry(Guid id, [FromBody] SubmitEntryRequest request, CancellationToken ct)
    {
        var result = await contestService.SubmitEntryAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>FE gọi sau khi PUT xong video lên R2 — trigger auto-validate + upload YouTube Unlisted.</summary>
    [Authorize]
    [HttpPost("{id:guid}/entries/{entryId:guid}/confirm-upload")]
    public async Task<IActionResult> ConfirmUpload(Guid id, Guid entryId, CancellationToken ct)
    {
        var ok = await contestService.ConfirmUploadAsync(id, entryId, ct);
        return ok ? NoContent() : BadRequest();
    }

    /// <summary>Admin review: danh sách entries Status=UploadedDraft, chờ approve/reject.</summary>
    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpGet("entries/pending-review")]
    public async Task<IActionResult> GetPendingReview(CancellationToken ct)
        => Ok(await contestService.GetPendingReviewAsync(ct));

    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpPatch("{id:guid}/entries/{entryId:guid}/approve")]
    public async Task<IActionResult> ApproveEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        var ok = await contestService.ApproveEntryAsync(id, entryId, ct);
        return ok ? NoContent() : NotFound();
    }

    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpPatch("{id:guid}/entries/{entryId:guid}/reject")]
    public async Task<IActionResult> RejectEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        var ok = await contestService.RejectEntryAsync(id, entryId, ct);
        return ok ? NoContent() : NotFound();
    }
}

[Authorize(Policy = "RequireSystemAdmin")]
[ApiController]
[Route("api/admin/contests")]
public class AdminContestsController(IContestService contestService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await contestService.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContestRequest request, CancellationToken ct)
        => Ok(await contestService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContestRequest request, CancellationToken ct)
    {
        var updated = await contestService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Chốt giải: gán PrizeTier cho từng entry đã Published, denorm xuống snapshot, set contest Ended.</summary>
    [HttpPatch("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeContestRequest request, CancellationToken ct)
    {
        var ok = await contestService.FinalizeAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Prize tiers ──────────────────────────────────────────
    [HttpPost("{id:guid}/prize-tiers")]
    public async Task<IActionResult> CreatePrizeTier(Guid id, [FromBody] CreatePrizeTierRequest request, CancellationToken ct)
        => Ok(await contestService.CreatePrizeTierAsync(id, request, ct));

    [HttpPut("{id:guid}/prize-tiers/{tierId:guid}")]
    public async Task<IActionResult> UpdatePrizeTier(Guid id, Guid tierId, [FromBody] UpdatePrizeTierRequest request, CancellationToken ct)
    {
        var updated = await contestService.UpdatePrizeTierAsync(id, tierId, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}/prize-tiers/{tierId:guid}")]
    public async Task<IActionResult> DeletePrizeTier(Guid id, Guid tierId, CancellationToken ct)
    {
        var ok = await contestService.DeletePrizeTierAsync(id, tierId, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Sponsors ─────────────────────────────────────────────
    [HttpPost("{id:guid}/sponsors")]
    public async Task<IActionResult> CreateSponsor(Guid id, [FromBody] CreateSponsorRequest request, CancellationToken ct)
        => Ok(await contestService.CreateSponsorAsync(id, request, ct));

    [HttpPut("{id:guid}/sponsors/{sponsorId:guid}")]
    public async Task<IActionResult> UpdateSponsor(Guid id, Guid sponsorId, [FromBody] UpdateSponsorRequest request, CancellationToken ct)
    {
        var updated = await contestService.UpdateSponsorAsync(id, sponsorId, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}/sponsors/{sponsorId:guid}")]
    public async Task<IActionResult> DeleteSponsor(Guid id, Guid sponsorId, CancellationToken ct)
    {
        var ok = await contestService.DeleteSponsorAsync(id, sponsorId, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Presigned PUT URL để upload logo sponsor lên R2.</summary>
    [HttpPost("{id:guid}/sponsors/{sponsorId:guid}/logo/presign")]
    public async Task<IActionResult> RequestSponsorLogoUpload(
        Guid id, Guid sponsorId, [FromBody] SponsorLogoPresignRequest request, CancellationToken ct)
    {
        var result = await contestService.RequestSponsorLogoUploadAsync(id, sponsorId, request.FileName, request.ContentType, ct);
        return result is null ? BadRequest(new { error = "Không thể tạo upload URL — kiểm tra content-type hoặc sponsor tồn tại." }) : Ok(result);
    }
}

public record SponsorLogoPresignRequest(string FileName, string ContentType);

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
}

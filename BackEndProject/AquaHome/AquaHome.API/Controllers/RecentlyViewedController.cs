using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using FishLover.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/recently-viewed")]
[Authorize]
public class RecentlyViewedController(
    IRecentlyViewedService service,
    ICurrentUserSession currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecentlyViewedDto>>> GetAll()
    {
        var items = await service.GetRecentlyViewedAsync(currentUser.UserId);
        return Ok(items);
    }

    [HttpPost("{specCode:int}")]
    public async Task<ActionResult> Record(int specCode)
    {
        if (specCode <= 0) return BadRequest();
        await service.RecordViewAsync(currentUser.UserId, specCode);
        return NoContent();
    }
}

using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/aquariums")]
[Authorize]
public class AquariumsController(IAquariumService aquariumService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await aquariumService.GetMyAquariumsAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await aquariumService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAquariumRequest request, CancellationToken ct)
    {
        var result = await aquariumService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAquariumRequest request, CancellationToken ct)
    {
        var result = await aquariumService.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await aquariumService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/fish")]
    public async Task<IActionResult> AddFish(Guid id, [FromBody] AddFishRequest request, CancellationToken ct)
    {
        if (request.SpecCode <= 0) return BadRequest("Invalid specCode.");
        var ok = await aquariumService.AddFishAsync(id, request.SpecCode, request.Quantity, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/fish/{specCode:int}")]
    public async Task<IActionResult> RemoveFish(Guid id, int specCode, CancellationToken ct)
    {
        var ok = await aquariumService.RemoveFishAsync(id, specCode, ct);
        return ok ? NoContent() : NotFound();
    }
}

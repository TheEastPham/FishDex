using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/aquariums/{aquariumId:guid}/reminders")]
[Authorize]
public class RemindersController(IReminderService reminderService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ReminderDto>> GetAll(Guid aquariumId, CancellationToken ct)
        => await reminderService.GetByAquariumAsync(aquariumId, ct);

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Create(
        Guid aquariumId,
        [FromBody] CreateReminderRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await reminderService.CreateAsync(aquariumId, request, ct);
            return CreatedAtAction(nameof(GetAll), new { aquariumId }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{reminderId:guid}/complete")]
    public async Task<ActionResult<CompleteReminderResponse>> Complete(
        Guid aquariumId,
        Guid reminderId,
        CancellationToken ct)
    {
        try
        {
            var result = await reminderService.CompleteAsync(aquariumId, reminderId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{reminderId:guid}")]
    public async Task<IActionResult> Delete(Guid aquariumId, Guid reminderId, CancellationToken ct)
    {
        var deleted = await reminderService.DeleteAsync(aquariumId, reminderId, ct);
        return deleted ? NoContent() : NotFound();
    }
}

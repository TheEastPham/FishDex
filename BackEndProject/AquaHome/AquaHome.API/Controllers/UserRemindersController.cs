using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
public class UserRemindersController(IReminderService reminderService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UserReminderDto>> GetAll(CancellationToken ct)
        => await reminderService.GetAllByUserAsync(ct);
}

using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/admin/quota")]
[Authorize(Policy = "RequireSystemAdmin")]
public class AdminQuotaController(IQuotaService quotaService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RoleQuotaDto>> GetAll(CancellationToken ct)
        => quotaService.GetAllAsync(ct);

    [HttpPut("{role}")]
    public async Task<ActionResult<RoleQuotaDto>> Update(string role, [FromBody] UpdateRoleQuotaRequest request, CancellationToken ct)
    {
        var updated = await quotaService.UpdateAsync(role, request, ct);
        return updated is null ? NotFound($"No quota config for role '{role}'.") : Ok(updated);
    }
}

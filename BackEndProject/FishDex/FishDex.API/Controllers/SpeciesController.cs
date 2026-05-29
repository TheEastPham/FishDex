using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpeciesController(
    ISpeciesService speciesService,
    IMediaService mediaService) : ControllerBase
{
    [HttpGet("families")]
    public async Task<IActionResult> GetFamilies(CancellationToken ct)
    {
        var result = await speciesService.GetFamiliesAsync(ct);
        return Ok(result);
    }

    [HttpGet("families/{famId:guid}/genera")]
    public async Task<IActionResult> GetGenera(Guid famId, CancellationToken ct)
    {
        var result = await speciesService.GetGenusByFamilyAsync(famId, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] GetSpeciesSearchRequest request, CancellationToken ct)
    {
        var result = await speciesService.SearchSpeciesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetTopLanguages(CancellationToken ct)
    {
        var result = await speciesService.GetTopLanguagesAsync(ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}/media")]
    public async Task<IActionResult> GetMedia(int specCode, CancellationToken ct)
    {
        var result = await mediaService.GetBySpecCodeAsync(specCode, ct);
        return Ok(result);
    }
}

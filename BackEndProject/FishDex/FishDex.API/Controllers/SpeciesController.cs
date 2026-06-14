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
    IMediaService mediaService,
    IOccurrenceService occurrenceService) : ControllerBase
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

    [HttpGet("{specCode:int}/detail")]
    public async Task<IActionResult> GetDetail(int specCode, [FromQuery] string? language, CancellationToken ct)
    {
        var result = await speciesService.GetDetailAsync(specCode, language, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{specCode:int}/media")]
    public async Task<IActionResult> GetMedia(int specCode, CancellationToken ct)
    {
        var result = await mediaService.GetBySpecCodeAsync(specCode, ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}/occurrences")]
    public async Task<IActionResult> GetOccurrences(int specCode, [FromQuery] int limit = 500, CancellationToken ct = default)
    {
        var result = await occurrenceService.GetBySpecCodeAsync(specCode, limit, ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}/related")]
    public async Task<IActionResult> GetRelated(int specCode, [FromQuery] int limit = 6, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        var result = await speciesService.GetRelatedAsync(specCode, limit, language, ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}/countries")]
    public async Task<IActionResult> GetCountries(int specCode, CancellationToken ct)
    {
        var result = await occurrenceService.GetCountriesAsync(specCode, ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}/distribution")]
    public async Task<IActionResult> GetDistribution(int specCode, CancellationToken ct)
    {
        var result = await occurrenceService.GetDistributionAsync(specCode, ct);
        return Ok(result);
    }

    [HttpGet("summaries")]
    public async Task<IActionResult> GetSummaries([FromQuery] string codes, [FromQuery] string? language, CancellationToken ct)
    {
        var specCodes = codes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => int.TryParse(c, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .Distinct()
            .ToList();

        if (specCodes.Count == 0) return BadRequest("No valid spec codes provided.");
        if (specCodes.Count > 100) return BadRequest("Maximum 100 spec codes per request.");

        var result = await speciesService.GetSummariesAsync(specCodes, language, ct);
        return Ok(result);
    }
}

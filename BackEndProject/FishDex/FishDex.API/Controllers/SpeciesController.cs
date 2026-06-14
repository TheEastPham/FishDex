using FishDex.Domain.DTOs.Media;
using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishLover.Shared.Common;
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
    public Task<IReadOnlyList<FamilyDto>> GetFamilies(CancellationToken ct)
        => speciesService.GetFamiliesAsync(ct);

    [HttpGet("families/{famId:guid}/genera")]
    public Task<IReadOnlyList<GenusDto>> GetGenera(Guid famId, CancellationToken ct)
        => speciesService.GetGenusByFamilyAsync(famId, ct);

    [HttpGet("search")]
    public Task<PagedResult<SpeciesSearchResultDto>> Search([FromQuery] GetSpeciesSearchRequest request, CancellationToken ct)
        => speciesService.SearchSpeciesAsync(request, ct);

    [HttpGet("languages")]
    public Task<IReadOnlyList<LanguageCountDto>> GetTopLanguages(CancellationToken ct)
        => speciesService.GetTopLanguagesAsync(ct);

    [HttpGet("{specCode:int}/detail")]
    public Task<SpeciesDetailDto?> GetDetail(int specCode, [FromQuery] string? language, CancellationToken ct)
        => speciesService.GetDetailAsync(specCode, language, ct);

    [HttpGet("{specCode:int}/media")]
    public Task<IReadOnlyList<SystemImageDto>> GetMedia(int specCode, CancellationToken ct)
        => mediaService.GetBySpecCodeAsync(specCode, ct);

    [HttpGet("{specCode:int}/occurrences")]
    public Task<IReadOnlyList<OccurrenceDto>> GetOccurrences(int specCode, [FromQuery] int limit = 500, CancellationToken ct = default)
        => occurrenceService.GetBySpecCodeAsync(specCode, limit, ct);

    [HttpGet("{specCode:int}/related")]
    public Task<IReadOnlyList<SpeciesSearchResultDto>> GetRelated(int specCode, [FromQuery] int limit = 6, [FromQuery] string? language = null, CancellationToken ct = default)
        => speciesService.GetRelatedAsync(specCode, limit, language, ct);

    [HttpGet("{specCode:int}/countries")]
    public Task<IReadOnlyList<CountryDto>> GetCountries(int specCode, CancellationToken ct)
        => occurrenceService.GetCountriesAsync(specCode, ct);

    [HttpGet("{specCode:int}/distribution")]
    public Task<SpeciesDistributionDto> GetDistribution(int specCode, CancellationToken ct)
        => occurrenceService.GetDistributionAsync(specCode, ct);

    [HttpGet("summaries")]
    public async Task<IReadOnlyList<SpeciesSummaryDto>> GetSummaries([FromQuery] string codes, [FromQuery] string? language, CancellationToken ct)
    {
        var specCodes = codes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => int.TryParse(c, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .Distinct()
            .Take(100)
            .ToList();

        return await speciesService.GetSummariesAsync(specCodes, language, ct);
    }
}

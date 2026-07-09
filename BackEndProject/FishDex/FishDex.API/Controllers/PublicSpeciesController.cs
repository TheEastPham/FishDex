using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishLover.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Endpoint public (no auth) cho các trang không cần đăng nhập — vd AquaHome ký lại ảnh loài
/// khi render snapshot public cho người xem chưa login. Chỉ expose dữ liệu loài vốn đã công khai.
/// </summary>
[ApiController]
[Route("api/public/species")]
[AllowAnonymous]
public class PublicSpeciesController(
    ISpeciesService speciesService,
    IOccurrenceService occurrenceService) : ControllerBase
{
    private static List<int> ParseCodes(string codes) => codes
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(c => int.TryParse(c, out var n) ? n : (int?)null)
        .Where(n => n.HasValue)
        .Select(n => n!.Value)
        .Distinct()
        .Take(100)
        .ToList();

    /// <summary>Batch species summaries (name + commonName + presigned imageUrl mới ký) cho danh sách specCode.</summary>
    [HttpGet("summaries")]
    public Task<IReadOnlyList<SpeciesSummaryDto>> GetSummaries(
        [FromQuery] string codes, [FromQuery] string? language, CancellationToken ct)
        => speciesService.GetSummariesAsync(ParseCodes(codes), language, ct);

    /// <summary>Batch distribution cho nhiều loài trong 1 request — thay cho gọi /{specCode}/distribution N lần.</summary>
    [HttpGet("distributions")]
    public Task<IReadOnlyDictionary<int, SpeciesDistributionDto>> GetDistributions(
        [FromQuery] string codes, CancellationToken ct)
        => occurrenceService.GetDistributionsBatchAsync(ParseCodes(codes), ct);

    /// <summary>Danh sách họ cá (chỉ họ có loài) — clone public của SpeciesController.GetFamilies cho người chưa đăng nhập.</summary>
    [HttpGet("families")]
    public Task<IReadOnlyList<FamilyDto>> GetFamilies(CancellationToken ct)
        => speciesService.GetFamiliesAsync(ct);

    /// <summary>Search loài — clone public của SpeciesController.Search. Tách riêng để sau này dễ hạn chế field trả về cho public.</summary>
    [HttpGet("search")]
    public Task<PagedResult<SpeciesSearchResultDto>> Search([FromQuery] GetSpeciesSearchRequest request, CancellationToken ct)
        => speciesService.SearchSpeciesAsync(request, ct);
}

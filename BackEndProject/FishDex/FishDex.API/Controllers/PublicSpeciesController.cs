using FishDex.API.Filters;
using FishDex.Domain.DTOs.Media;
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
    IOccurrenceService occurrenceService,
    IMediaService mediaService) : ControllerBase
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

    // ── Profile loài cho khách chưa đăng nhập ────────────────────────────────
    // Bốn endpoint dưới đây là những gì FishProfilePage cần để render. Chúng trừ hạn mức
    // (mỗi specCode khác nhau = 1 lượt/ngày) và trả payload mỏng hơn bản [Authorize]:
    // chỉ ảnh đại diện thay vì cả gallery, phân bố chỉ tới mức quốc gia thay vì toạ độ điểm.
    // Đây vừa là phần cắt egress R2 lớn nhất, vừa là lý do thật để người xem đăng ký.
    // Search và các endpoint batch ở trên KHÔNG trừ lượt — chúng vốn đã public và rẻ.

    /// <summary>Chi tiết loài — trả đủ field như bản có auth, dữ liệu này vốn đã công khai.</summary>
    [HttpGet("{specCode:int}/detail")]
    [ServiceFilter(typeof(AnonSpeciesQuotaFilter))]
    public Task<SpeciesDetailDto?> GetDetail(int specCode, [FromQuery] string? language, CancellationToken ct)
        => speciesService.GetDetailAsync(specCode, language, ct);

    /// <summary>Ảnh loài — chỉ ảnh đại diện. Gallery đầy đủ dành cho người đã đăng nhập.</summary>
    [HttpGet("{specCode:int}/media")]
    [ServiceFilter(typeof(AnonSpeciesQuotaFilter))]
    public async Task<IReadOnlyList<SystemImageDto>> GetMedia(int specCode, CancellationToken ct)
    {
        var preferred = await mediaService.GetPreferredImageAsync(specCode, ct);
        return preferred is null ? [] : [preferred];
    }

    /// <summary>Phân bố theo quốc gia — bỏ toạ độ điểm, bản đồ chi tiết dành cho người đã đăng nhập.</summary>
    [HttpGet("{specCode:int}/distribution")]
    [ServiceFilter(typeof(AnonSpeciesQuotaFilter))]
    public async Task<SpeciesDistributionDto> GetDistribution(int specCode, CancellationToken ct)
    {
        var full = await occurrenceService.GetDistributionAsync(specCode, ct);
        return full with
        {
            Countries = full.Countries.Select(c => c with { Occurrences = [] }).ToList(),
        };
    }

    /// <summary>Loài liên quan — giữ nguyên, đây chính là thứ kéo người xem đi tiếp trong ngày.</summary>
    [HttpGet("{specCode:int}/related")]
    [ServiceFilter(typeof(AnonSpeciesQuotaFilter))]
    public Task<IReadOnlyList<SpeciesSearchResultDto>> GetRelated(
        int specCode, [FromQuery] int limit = 6, [FromQuery] string? language = null, CancellationToken ct = default)
        => speciesService.GetRelatedAsync(specCode, limit, language, ct);
}

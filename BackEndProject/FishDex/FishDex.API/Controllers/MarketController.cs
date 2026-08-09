using FishDex.Domain.DTOs.Market;
using FishDex.Domain.Services.Interfaces;
using FishLover.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Market layer — danh sách cá đang được bán ở từng quốc gia.
///
/// <para>Public (no auth) theo đúng tiền lệ <see cref="PublicSpeciesController"/>: dữ liệu ở đây
/// vốn đã công khai, và trang market là cửa vào của khách chưa đăng nhập.</para>
///
/// <para><c>cc</c> trong route là mã alpha-2 (vd <c>vn</c>). Mã số C_Code chỉ tồn tại trong DB,
/// không lộ ra API.</para>
/// </summary>
[ApiController]
[Route("api/market")]
[AllowAnonymous]
public class MarketController(IMarketService service) : ControllerBase
{
    /// <summary>Các nước đã bật trang market — đổ vào dropdown chọn quốc gia.</summary>
    [HttpGet("countries")]
    public ActionResult<IReadOnlyList<MarketCountryDto>> GetCountries()
        => Ok(service.GetCountries());

    /// <summary>
    /// Danh sách loài đang bán, phân trang 24. Hai bộ lọc: <c>sizeBand</c> và <c>nameStatus</c>.
    /// Không có bộ lọc mức phổ biến ở v1 vì dòng sinh từ bể cá không mang TradeStatus.
    /// </summary>
    [HttpGet("{cc}/species")]
    public async Task<ActionResult<PagedResult<MarketSpeciesDto>>> GetSpecies(
        string cc, [FromQuery] MarketSpeciesQuery query, CancellationToken ct)
    {
        var result = await service.GetSpeciesAsync(cc, query, ct);
        return result is null
            ? NotFound($"Quốc gia '{cc}' không có trang market.")
            : Ok(result);
    }

    /// <summary>
    /// Ba con số của dải thống kê. Tách endpoint riêng vì chỉ COUNT, không presign ảnh,
    /// nên trả về tức thì và không dính vào page size của danh sách.
    /// </summary>
    [HttpGet("{cc}/stats")]
    public async Task<ActionResult<MarketStatsDto>> GetStats(string cc, CancellationToken ct)
    {
        var stats = await service.GetStatsAsync(cc, ct);
        return stats is null
            ? NotFound($"Quốc gia '{cc}' không có trang market.")
            : Ok(stats);
    }

    /// <summary>
    /// Tra tên khoa học trên index toàn bộ FishBase để biết loài nằm ở nhánh nào:
    /// đã có trong FishDex, hay có trong FishBase nhưng chưa nạp. Không tìm thấy nghĩa là
    /// loài lai — FE chuyển sang luồng submit community species.
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<SpeciesLookupDto>>> Lookup(
        [FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<SpeciesLookupDto>());

        return Ok(await service.LookupAsync(q, Math.Clamp(limit, 1, 50), ct));
    }
}

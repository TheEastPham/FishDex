using FishDex.Domain.DTOs.Market;
using FishDex.Domain.Services.Interfaces;
using FishLover.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Endpoint nội bộ cho AquaHome.Worker báo các loài có trong bể theo quốc gia.
///
/// <para>Xác thực bằng scheme dùng chung <see cref="InternalApiKey"/>: header <c>X-Internal-Api-Key</c>, cùng một
/// secret cho mọi service, nạp từ biến môi trường <c>INTERNAL_API_KEY</c> — giống hệt cách
/// AquaHome.Worker gọi <c>/api/push/send</c> của UserManagement.</para>
///
/// <para>Không dùng JWT vì worker chạy nền, không có user context. Và không để mở như
/// <see cref="PublicSpeciesController"/> vì đây là endpoint GHI. Không expose qua API Gateway.</para>
/// </summary>
[ApiController]
[Route("api/market/ingest")]
[Authorize(Policy = InternalApiKey.PolicyName)]
public class MarketIngestController(IMarketService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IngestResultDto>> Ingest(
        [FromBody] IngestTankSpeciesRequest request,
        CancellationToken ct)
    {
        var result = await service.IngestAsync(request, ct);
        return result is null
            ? BadRequest($"Quốc gia '{request.CountryAlpha2}' không nằm trong danh sách market.")
            : Ok(result);
    }
}

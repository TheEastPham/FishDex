using FishDex.Domain.DTOs.Market;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Endpoint nội bộ cho AquaHome.Worker báo các loài có trong bể theo quốc gia.
///
/// <para>Dùng đúng cơ chế đã có sẵn của dự án: header <c>X-Internal-Api-Key</c>, cùng một
/// secret cho mọi service, nạp từ biến môi trường <c>INTERNAL_API_KEY</c> — giống hệt cách
/// AquaHome.Worker gọi <c>/api/push/send</c> của UserManagement.</para>
///
/// <para>Không dùng JWT vì worker chạy nền, không có user context. Và không để mở như
/// <see cref="PublicSpeciesController"/> vì đây là endpoint GHI. Không expose qua API Gateway.</para>
/// </summary>
[ApiController]
[Route("api/market/ingest")]
[AllowAnonymous]
public class MarketIngestController(
    IMarketService service,
    IConfiguration config,
    ILogger<MarketIngestController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IngestResultDto>> Ingest(
        [FromBody] IngestTankSpeciesRequest request,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        CancellationToken ct)
    {
        var expected = config["InternalSettings:ApiKey"];

        // Fail-closed: chưa cấu hình secret thì từ chối, không mở toang.
        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogError("Market ingest bị gọi nhưng InternalSettings:ApiKey chưa được cấu hình — từ chối.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Ingest chưa được cấu hình.");
        }

        if (apiKey != expected)
        {
            logger.LogWarning("Market ingest bị từ chối: thiếu hoặc sai X-Internal-Api-Key.");
            return Unauthorized();
        }

        var result = await service.IngestAsync(request, ct);
        return result is null
            ? BadRequest($"Quốc gia '{request.CountryAlpha2}' không nằm trong danh sách market.")
            : Ok(result);
    }
}

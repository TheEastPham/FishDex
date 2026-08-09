using FishDex.Domain.DTOs.Market;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FishDex.API.Controllers;

/// <summary>
/// Sửa danh sách market — chỉ admin. Tách file riêng khỏi <see cref="MarketController"/> để
/// hai phần đọc và ghi không đụng nhau khi làm song song.
///
/// <para><c>RequireContentAdmin</c> đã bao gồm cả SystemAdmin qua <c>RequireAssertion</c>,
/// nên không cần thêm role mới.</para>
///
/// <para><b>Người dùng thường không có endpoint nào ở đây.</b> Danh sách tự đầy từ dữ liệu bể cá;
/// admin chỉ can thiệp để seed, curate mức phổ biến và trạng thái pháp lý, hoặc gỡ dòng rác.</para>
/// </summary>
[ApiController]
[Route("api/market/{cc}/species")]
[Authorize(Policy = "RequireContentAdmin")]
public class MarketAdminController(IMarketService service) : ControllerBase
{
    /// <summary>Admin thêm một loài vào danh sách của quốc gia, từ màn danh sách hoặc chi tiết loài.</summary>
    [HttpPost]
    public async Task<IActionResult> Add(
        string cc, [FromBody] AddTradedSpeciesRequest request, CancellationToken ct)
        => ToResponse(await service.AddAsync(cc, request, ct));

    /// <summary>Sửa mức phổ biến hoặc trạng thái pháp lý của một dòng.</summary>
    [HttpPatch("{specCode:int}")]
    public async Task<IActionResult> Update(
        string cc, int specCode, [FromBody] UpdateTradedSpeciesRequest request, CancellationToken ct)
        => ToResponse(await service.UpdateAsync(cc, specCode, request, ct));

    /// <summary>Gỡ một loài khỏi danh sách — đường hậu kiểm vì đã bỏ tiền kiểm.</summary>
    [HttpDelete("{specCode:int}")]
    public async Task<IActionResult> Remove(string cc, int specCode, CancellationToken ct)
        => ToResponse(await service.RemoveAsync(cc, specCode, ct));

    private IActionResult ToResponse(MarketMutationOutcome outcome) => outcome switch
    {
        MarketMutationOutcome.Ok                  => NoContent(),
        MarketMutationOutcome.CountryNotFound     => NotFound("Quốc gia không nằm trong danh sách market."),
        MarketMutationOutcome.SpeciesNotFound     => NotFound("SpecCode không có trong FishDex."),
        MarketMutationOutcome.NotFound            => NotFound("Loài không có trong danh sách của quốc gia này."),
        MarketMutationOutcome.AlreadyExists       => Conflict("Loài đã có trong danh sách của quốc gia này."),
        MarketMutationOutcome.LegalSourceRequired => BadRequest(
            "Đặt Restricted hoặc Banned thì bắt buộc kèm LegalSourceUrl — không khẳng định trạng thái pháp lý mà không có nguồn."),
        _                                         => BadRequest(),
    };
}

using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FishDex.API.Controllers;

/// <summary>
/// Link ảnh ổn định cho bucket system-image mà thẻ <c>img</c> dùng thẳng, không cần Authorization header.
/// </summary>
/// <remarks>
/// <para>Cả hai endpoint đều anonymous: system-image chỉ chứa ảnh loài vốn công khai, và trang snapshot /
/// profile loài phải xem được khi chưa đăng nhập. Thứ được bảo vệ là thời hạn (SAS ≤ MaxExpiry) và việc
/// không ai có URL R2 dài hạn để hotlink.</para>
/// <para>Gateway có route riêng cho cả hai endpoint và KHÔNG bật rate limit: Ocelot đếm theo header
/// <c>ClientId</c>, mà không ai gửi header đó → mọi người dùng chung 1 bộ đếm/path, tức giới hạn toàn site.
/// 1 trang gallery = N request redeem nên giới hạn kiểu đó làm vỡ ảnh. Redeem rẻ (HMAC + presign cục bộ,
/// chỉ ảnh community mới tra DB) nên bỏ giới hạn an toàn hơn.</para>
/// </remarks>
[ApiController]
[Route("api/public/media-access")]
[AllowAnonymous]
public class PublicMediaAccessController(IMediaAccessService mediaAccess) : ControllerBase
{
    private const string MediaAccessExpiredCode = "MEDIA_ACCESS_EXPIRED";

    /// <summary>Redeem link → 302 sang presigned R2 URL ngắn hạn.</summary>
    /// <response code="302">Location là presigned URL.</response>
    /// <response code="403">Link sai, bị sửa, hoặc hết hạn (code MEDIA_ACCESS_EXPIRED).</response>
    /// <response code="503">Chưa cấu hình khoá ký hoặc storage.</response>
    [HttpGet]
    public async Task<IActionResult> Redeem(
        [FromQuery] string? path,
        [FromQuery] string? shared,
        [FromQuery] string? exp,
        [FromQuery] string? sig,
        CancellationToken ct)
    {
        var result = await mediaAccess.RedeemAsync(path, shared, exp, sig, ct);

        switch (result.Denial)
        {
            case MediaAccessDenial.NotConfigured:
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Media access chưa được cấu hình.");
            case MediaAccessDenial.Expired:
                return StatusCode(StatusCodes.Status403Forbidden, new { code = MediaAccessExpiredCode });
            case MediaAccessDenial.Invalid:
                return StatusCode(StatusCodes.Status403Forbidden, new { code = "FORBIDDEN" });
        }

        var maxAge = result.CacheMaxAgeSeconds;
        Response.Headers.CacheControl = maxAge > 0 ? $"private, max-age={maxAge}" : "no-store";

        // 302 cache được khác nhau theo Origin (CORS middleware chỉ thêm ACAO khi request có Origin).
        // Thiếu Vary thì 302 cache từ <img> (không Origin) bị fetch() CORS dùng lại → lỗi CORS chập chờn.
        Response.Headers.Append("Vary", "Origin");

        return Redirect(result.Location!);
    }

    /// <summary>Phát SAS dùng chung cho mọi key. Body tuỳ chọn.</summary>
    /// <response code="200">SAS đã phát.</response>
    /// <response code="400">expiryInSeconds ngoài khoảng cho phép.</response>
    /// <response code="503">Chưa cấu hình khoá ký.</response>
    [HttpPost("sas")]
    public ActionResult<SharedMediaAccessSignatureDto> CreateSharedSignature(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateMediaAccessSignatureRequest? request)
    {
        try
        {
            var dto = mediaAccess.CreateSharedSignature(request?.ExpiryInSeconds);
            return dto is null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, "Media access chưa được cấu hình.")
                : Ok(dto);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

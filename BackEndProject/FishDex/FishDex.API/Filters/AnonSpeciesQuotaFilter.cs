using FishDex.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FishDex.API.Filters;

/// <summary>
/// Trừ hạn mức xem loài của khách chưa đăng nhập trên các endpoint public có <c>{specCode}</c>.
/// Hết hạn mức trả 429 kèm mã <c>anon_quota_exceeded</c> — FE phải phân biệt được với 429 của
/// rate limit ở gateway (chỉ trả text) để hiện đúng màn "hết lượt hôm nay" thay vì "thử lại sau".
/// </summary>
public class AnonSpeciesQuotaFilter(IAnonQuotaService quota) : IAsyncActionFilter
{
    public const string VisitorHeader = "X-Visitor-Id";

    public static readonly string[] ResponseHeaders =
    [
        "X-Anon-Views-Limit",
        "X-Anon-Views-Used",
        "X-Anon-Views-Remaining",
        "X-Anon-Views-Reset",
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        // Người đã đăng nhập gọi nhầm route public thì không tính lượt.
        if (http.User.Identity?.IsAuthenticated == true)
        {
            await next();
            return;
        }

        if (!context.RouteData.Values.TryGetValue("specCode", out var raw)
            || !int.TryParse(raw?.ToString(), out var specCode))
        {
            await next();
            return;
        }

        var ip = ResolveClientIp(http);
        var result = await quota.TryConsumeAsync(ResolveVisitorId(http, ip), ip, specCode, http.RequestAborted);

        http.Response.Headers["X-Anon-Views-Limit"]     = result.Limit.ToString();
        http.Response.Headers["X-Anon-Views-Used"]      = result.Used.ToString();
        http.Response.Headers["X-Anon-Views-Remaining"] = result.Remaining.ToString();
        http.Response.Headers["X-Anon-Views-Reset"]     = result.ResetsInSeconds.ToString();

        if (!result.Allowed)
        {
            context.Result = new ObjectResult(new
            {
                error = "anon_quota_exceeded",
                limit = result.Limit,
                used = result.Used,
                resetsInSeconds = result.ResetsInSeconds,
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }

        await next();
    }

    /// <summary>
    /// Định danh khách bằng UUID FE tự sinh và gửi qua header — chọn header thay vì cookie vì FE
    /// (Cloudflare) khác origin với API, cookie sẽ phải SameSite=None + CORS credentials mà chẳng
    /// chắc chắn hơn. Xoá localStorage là reset được: đây là cái phanh mềm, trần IP mới là phanh cứng.
    /// </summary>
    private static string ResolveVisitorId(HttpContext http, string ip)
    {
        var raw = http.Request.Headers[VisitorHeader].ToString();

        // Giá trị lạ đi thẳng vào Redis key nên chỉ nhận đúng dạng UUID/token ngắn, còn lại quy về IP.
        if (raw.Length is >= 8 and <= 64 && raw.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            return raw;

        return $"ip:{ip}";
    }

    private static string ResolveClientIp(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

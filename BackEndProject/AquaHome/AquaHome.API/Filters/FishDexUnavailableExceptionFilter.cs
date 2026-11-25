using AquaHome.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AquaHome.API.Filters;

/// <summary>
/// Map FishDexUnavailableException → HTTP 503. Message cho FE là chung chung (không lộ chi tiết nội bộ);
/// nguyên nhân cụ thể đã được FishDexClient log.
/// </summary>
public class FishDexUnavailableExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not FishDexUnavailableException) return;

        context.Result = new ObjectResult(new
        {
            error = "fishdex_unavailable",
            message = "Không lấy được dữ liệu loài lúc này, vui lòng thử lại sau ít phút.",
        })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };
        context.HttpContext.Response.Headers.RetryAfter = "30";
        context.ExceptionHandled = true;
    }
}

using AquaHome.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AquaHome.API.Filters;

/// <summary>Map QuotaExceededException → HTTP 429 với payload gọn cho FE hiển thị.</summary>
public class QuotaExceededExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not QuotaExceededException ex) return;

        context.Result = new ObjectResult(new
        {
            error = "quota_exceeded",
            quotaType = ex.QuotaType.ToString(),
            limit = ex.Limit,
            role = ex.Role,
        })
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
        };
        context.ExceptionHandled = true;
    }
}

using AquaHome.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AquaHome.API.Filters;

/// <summary>Map StorageOverloadedException → HTTP 503 ("Hệ thống đang quá tải, vui lòng thử lại sau").</summary>
public class StorageOverloadedExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not StorageOverloadedException ex) return;

        context.Result = new ObjectResult(new { error = "storage_overloaded", message = ex.Message })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };
        context.ExceptionHandled = true;
    }
}

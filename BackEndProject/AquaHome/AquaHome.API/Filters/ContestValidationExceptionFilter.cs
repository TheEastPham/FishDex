using AquaHome.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AquaHome.API.Filters;

/// <summary>Map ContestValidationException → HTTP 422 với message rõ ràng cho FE hiển thị.</summary>
public class ContestValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ContestValidationException ex) return;

        context.Result = new ObjectResult(new { error = "invalid_video", message = ex.Message })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
        };
        context.ExceptionHandled = true;
    }
}

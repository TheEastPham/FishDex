using AquaHome.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AquaHome.API.Filters;

/// <summary>Map ArticleValidationException → HTTP 422 kèm mảng lỗi để form admin chỉ đúng chỗ sai.</summary>
public class ArticleValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ArticleValidationException ex) return;

        context.Result = new ObjectResult(new { error = "invalid_article_content", errors = ex.Errors })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
        };
        context.ExceptionHandled = true;
    }
}

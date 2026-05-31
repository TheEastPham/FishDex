using System.Text.Json;

namespace UserManagement.API.Middleware;

/// <summary>
/// Chuyển đổi InvalidOperationException từ OpenIddict thành 400 invalid_grant
/// thay vì để nó thành 500 — FE cần nhận được 400 để redirect về login.
/// </summary>
public class OpenIddictExceptionMiddleware(RequestDelegate next, ILogger<OpenIddictExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex) when (
            context.Request.Path.StartsWithSegments("/connect") &&
            (ex.Message.Contains("grant type") || ex.Message.Contains("token") || ex.Message.Contains("supported")))
        {
            logger.LogWarning(ex, "OpenIddict token error on {Path} — returning invalid_grant", context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error             = "invalid_grant",
                error_description = "The token is invalid or has expired. Please sign in again."
            }));
        }
    }
}

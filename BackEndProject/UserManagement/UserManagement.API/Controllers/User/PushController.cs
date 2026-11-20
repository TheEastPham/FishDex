using FishLover.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserManagement.Domain.DTOs.Push;
using UserManagement.Domain.Services.Interfaces;
using UserManagement.Domain.Settings;

namespace UserManagement.API.Controllers.User;

[ApiController]
[Route("api/push")]
public class PushController(
    IWebPushService pushService,
    ICurrentUserSession currentUser,
    IOptions<InternalSettings> internalSettings,
    ILogger<PushController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the VAPID public key for the FE to subscribe with.
    /// </summary>
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
        => Ok(new { publicKey = pushService.GetVapidPublicKey() });

    /// <summary>
    /// Saves a push subscription for the current user.
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] SaveSubscriptionRequest request)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await pushService.SaveSubscriptionAsync(currentUser.UserId, request);
            return Ok(new { message = "Subscribed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving push subscription for user {UserId}", currentUser.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a push subscription (user unsubscribes or revokes permission).
    /// </summary>
    [HttpDelete("unsubscribe")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await pushService.DeleteSubscriptionAsync(request.Endpoint);
            return Ok(new { message = "Unsubscribed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing push subscription");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Internal endpoint — gọi từ service khác (AquaHome) để gửi push cho user.
    /// Bảo vệ bằng X-Internal-Api-Key header, không expose qua API Gateway.
    /// </summary>
    [HttpPost("send")]
    [AllowAnonymous]
    public async Task<IActionResult> SendToUser(
        [FromBody] SendPushRequest request,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey)
    {
        if (apiKey != internalSettings.Value.ApiKey)
            return Unauthorized();

        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await pushService.SendNotificationAsync(request.UserId, request.Title, request.Body, request.Url);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push to user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }
}

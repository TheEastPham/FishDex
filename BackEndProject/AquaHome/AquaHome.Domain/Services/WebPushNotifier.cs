using System.Net.Http.Json;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

public class WebPushNotifier(
    IHttpClientFactory httpClientFactory,
    ILogger<WebPushNotifier> logger) : IWebPushNotifier
{
    public async Task SendAsync(Guid userId, string title, string body, string? url = null, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("UserManagement");
            var response = await client.PostAsJsonAsync("/api/push/send", new
            {
                userId,
                title,
                body,
                url,
            }, ct);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Push send returned {Status} for user {UserId}", response.StatusCode, userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send push notification to user {UserId}", userId);
        }
    }
}

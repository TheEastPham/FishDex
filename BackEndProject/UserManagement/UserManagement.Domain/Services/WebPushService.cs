using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserManagement.Domain.DTOs.Push;
using UserManagement.Domain.Services.Interfaces;
using UserManagement.EFCore.Entities.Push;
using UserManagement.EFCore.Repositories.Interfaces;
using WebPush;

namespace UserManagement.Domain.Services;

public class WebPushService(
    IPushSubscriptionRepository repository,
    IConfiguration configuration,
    ILogger<WebPushService> logger) : IWebPushService
{
    private readonly string _vapidPublicKey = configuration["VapidSettings:PublicKey"]
        ?? throw new InvalidOperationException("VapidSettings:PublicKey is not configured");
    private readonly string _vapidPrivateKey = configuration["VapidSettings:PrivateKey"]
        ?? throw new InvalidOperationException("VapidSettings:PrivateKey is not configured");
    private readonly string _vapidSubject = configuration["VapidSettings:Subject"]
        ?? "mailto:noreply@fishlover.org";

    public string GetVapidPublicKey() => _vapidPublicKey;

    public async Task SaveSubscriptionAsync(Guid userId, SaveSubscriptionRequest request)
    {
        // Upsert — nếu endpoint đã tồn tại thì bỏ qua (cùng device login lại)
        var existing = await repository.GetByEndpointAsync(request.Endpoint);
        if (existing is not null) return;

        await repository.AddAsync(new PushSubscriptionEntity
        {
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
            UserAgent = request.UserAgent,
        });
    }

    public Task DeleteSubscriptionAsync(string endpoint)
        => repository.DeleteByEndpointAsync(endpoint);

    public async Task SendNotificationAsync(Guid userId, string title, string body, string? url = null)
    {
        var subscriptions = await repository.GetByUserIdAsync(userId);
        if (!subscriptions.Any()) return;

        var client = new WebPushClient();
        client.SetVapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);

        var payload = JsonSerializer.Serialize(new { title, body, url });

        var staleEndpoints = new List<string>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode is
                System.Net.HttpStatusCode.Gone or
                System.Net.HttpStatusCode.NotFound)
            {
                // Subscription expired — clean up
                staleEndpoints.Add(sub.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send push to endpoint {Endpoint}", sub.Endpoint);
            }
        }

        foreach (var endpoint in staleEndpoints)
            await repository.DeleteByEndpointAsync(endpoint);
    }
}

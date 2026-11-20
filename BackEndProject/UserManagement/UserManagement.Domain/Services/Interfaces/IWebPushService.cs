using UserManagement.Domain.DTOs.Push;

namespace UserManagement.Domain.Services.Interfaces;

public interface IWebPushService
{
    string GetVapidPublicKey();
    Task SaveSubscriptionAsync(Guid userId, SaveSubscriptionRequest request);
    Task DeleteSubscriptionAsync(string endpoint);
    Task SendNotificationAsync(Guid userId, string title, string body, string? url = null);
}

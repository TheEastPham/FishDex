namespace AquaHome.Domain.Services.Interfaces;

public interface IWebPushNotifier
{
    Task SendAsync(Guid userId, string title, string body, string? url = null, CancellationToken ct = default);
}

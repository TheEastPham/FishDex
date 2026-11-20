using UserManagement.EFCore.Entities.User;

namespace UserManagement.EFCore.Entities.Push;

public class PushSubscriptionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual UserEntity User { get; set; } = null!;
}

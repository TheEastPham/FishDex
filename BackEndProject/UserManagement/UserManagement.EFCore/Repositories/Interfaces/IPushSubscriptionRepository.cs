using UserManagement.EFCore.Entities.Push;

namespace UserManagement.EFCore.Repositories.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<IEnumerable<PushSubscriptionEntity>> GetByUserIdAsync(Guid userId);
    Task<PushSubscriptionEntity?> GetByEndpointAsync(string endpoint);
    Task AddAsync(PushSubscriptionEntity subscription);
    Task DeleteByEndpointAsync(string endpoint);
    Task DeleteByUserIdAsync(Guid userId);
}

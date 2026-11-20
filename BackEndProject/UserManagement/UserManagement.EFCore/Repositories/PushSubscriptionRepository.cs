using Microsoft.EntityFrameworkCore;
using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.Push;
using UserManagement.EFCore.Repositories.Interfaces;

namespace UserManagement.EFCore.Repositories;

public class PushSubscriptionRepository(UserManagementDbContext context) : IPushSubscriptionRepository
{
    public async Task<IEnumerable<PushSubscriptionEntity>> GetByUserIdAsync(Guid userId)
        => await context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();

    public async Task<PushSubscriptionEntity?> GetByEndpointAsync(string endpoint)
        => await context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);

    public async Task AddAsync(PushSubscriptionEntity subscription)
    {
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByEndpointAsync(string endpoint)
    {
        var sub = await context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (sub is null) return;
        context.PushSubscriptions.Remove(sub);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        var subs = await context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        context.PushSubscriptions.RemoveRange(subs);
        await context.SaveChangesAsync();
    }
}

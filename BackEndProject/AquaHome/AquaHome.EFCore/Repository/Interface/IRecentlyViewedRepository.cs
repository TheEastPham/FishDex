using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IRecentlyViewedRepository
{
    Task UpsertAsync(Guid userId, int specCode);
    Task<IReadOnlyList<RecentlyViewed>> GetByUserAsync(Guid userId, int limit = 20);
}

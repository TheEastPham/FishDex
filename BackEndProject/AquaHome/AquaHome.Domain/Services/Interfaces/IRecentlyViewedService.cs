using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IRecentlyViewedService
{
    Task RecordViewAsync(Guid userId, int specCode);
    Task<IReadOnlyList<RecentlyViewedDto>> GetRecentlyViewedAsync(Guid userId);
}

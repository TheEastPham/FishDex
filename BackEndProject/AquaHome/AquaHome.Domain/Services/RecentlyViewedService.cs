using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Domain.Services;

public class RecentlyViewedService(IRecentlyViewedRepository repo) : IRecentlyViewedService
{
    public Task RecordViewAsync(Guid userId, int specCode)
        => repo.UpsertAsync(userId, specCode);

    public async Task<IReadOnlyList<RecentlyViewedDto>> GetRecentlyViewedAsync(Guid userId)
    {
        var items = await repo.GetByUserAsync(userId);
        return items.Select(x => new RecentlyViewedDto(x.SpecCode, x.ViewedAt)).ToList();
    }
}

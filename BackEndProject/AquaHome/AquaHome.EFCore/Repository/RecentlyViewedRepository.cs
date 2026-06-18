using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class RecentlyViewedRepository(AquaHomeDbContext context) : IRecentlyViewedRepository
{
    public async Task UpsertAsync(Guid userId, int specCode)
    {
        var existing = await context.RecentlyViewed
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SpecCode == specCode);

        if (existing is not null)
        {
            existing.ViewedAt = DateTime.UtcNow;
        }
        else
        {
            context.RecentlyViewed.Add(new RecentlyViewed
            {
                UserId   = userId,
                SpecCode = specCode,
                ViewedAt = DateTime.UtcNow
            });

            // Keep only latest 20 per user
            var count = await context.RecentlyViewed.CountAsync(x => x.UserId == userId);
            if (count > 20)
            {
                var oldest = await context.RecentlyViewed
                    .Where(x => x.UserId == userId)
                    .OrderBy(x => x.ViewedAt)
                    .FirstAsync();
                context.RecentlyViewed.Remove(oldest);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RecentlyViewed>> GetByUserAsync(Guid userId, int limit = 20)
    {
        return await context.RecentlyViewed
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ViewedAt)
            .Take(limit)
            .ToListAsync();
    }
}

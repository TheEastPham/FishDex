using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class QuotaRepository(AquaHomeDbContext context) : IQuotaRepository
{
    public Task<RoleQuota?> GetByRoleAsync(string role, CancellationToken ct = default)
        => context.RoleQuotas.AsNoTracking().FirstOrDefaultAsync(q => q.Role == role, ct);

    public async Task<IReadOnlyList<RoleQuota>> GetAllAsync(CancellationToken ct = default)
        => await context.RoleQuotas.AsNoTracking().OrderBy(q => q.Role).ToListAsync(ct);

    public async Task UpdateAsync(RoleQuota quota, CancellationToken ct = default)
    {
        context.RoleQuotas.Update(quota);
        await context.SaveChangesAsync(ct);
    }

    public Task<int> CountAquariumsAsync(Guid userId, CancellationToken ct = default)
        => context.Aquariums.CountAsync(a => a.UserId == userId, ct);

    public Task<int> CountFavoritesAsync(Guid userId, CancellationToken ct = default)
        => context.UserFavorites.CountAsync(f => f.UserId == userId, ct);

    public async Task<int> GetDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default)
        => await context.QuotaUsages.AsNoTracking()
            .Where(u => u.UserId == userId && u.QuotaType == quotaType && u.Day == day)
            .Select(u => u.Count)
            .FirstOrDefaultAsync(ct);

    public async Task<int> IncrementDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default)
    {
        // Upsert atomic phía Postgres — an toàn khi nhiều request song song
        var result = await context.Database
            .SqlQuery<int>($"""
                INSERT INTO "QuotaUsages" ("UserId", "QuotaType", "Day", "Count")
                VALUES ({userId}, {quotaType}, {day}, 1)
                ON CONFLICT ("UserId", "QuotaType", "Day")
                DO UPDATE SET "Count" = "QuotaUsages"."Count" + 1
                RETURNING "Count" AS "Value"
                """)
            .ToListAsync(ct);
        return result[0];
    }
}

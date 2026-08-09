using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Market;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class FishBaseSpeciesIndexRepository(FishDexDbContext db) : IFishBaseSpeciesIndexRepository
{
    public async Task<IReadOnlyList<FishBaseSpeciesIndex>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var pattern = $"%{query.Trim()}%";

        var matches = db.FishBaseSpeciesIndex.AsNoTracking();
        matches = matches.Where(i => EF.Functions.ILike(i.SpeciesName, pattern));

        return await matches
            // Loài đã nạp lên trước: đó là thứ người dùng chọn được ngay, không phải chờ ETL.
            .OrderByDescending(i => i.IsLoaded)
            .ThenBy(i => i.SpeciesName)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<FishBaseSpeciesIndex?> GetAsync(int specCode, CancellationToken ct = default)
    {
        return await db.FishBaseSpeciesIndex.AsNoTracking()
            .Where(i => i.SpecCode == specCode)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<FishBaseSpeciesIndex>> GetNotLoadedAsync(IReadOnlyCollection<int> specCodes, CancellationToken ct = default)
    {
        if (specCodes.Count == 0)
        {
            return [];
        }

        var pending = db.FishBaseSpeciesIndex.AsNoTracking()
            .Where(i => specCodes.Contains(i.SpecCode))
            .Where(i => !i.IsLoaded);

        return await pending
            .OrderBy(i => i.SpeciesName)
            .ToListAsync(ct);
    }
}

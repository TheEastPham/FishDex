using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class CommonNameRepository(FishDexDbContext context)
    : GenericRepository<CommonName>(context), ICommonNameRepository
{
    private readonly FishDexDbContext _db = context;

    public async Task<IReadOnlyList<(string Language, int Count)>> GetTopLanguagesAsync(
        int topCount, CancellationToken ct = default)
    {
        var rows = await _db.CommonNames
            .Where(c => c.Language != null)
            .GroupBy(c => c.Language!)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topCount)
            .ToListAsync(ct);

        return rows.Select(x => (x.Language, x.Count)).ToList();
    }
}

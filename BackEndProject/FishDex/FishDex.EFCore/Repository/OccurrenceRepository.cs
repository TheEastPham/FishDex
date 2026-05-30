using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class OccurrenceRepository(FishDexDbContext context) : GenericRepository<Occurrence>(context), IOccurrenceRepository
{
    private readonly FishDexDbContext _db = context;

    public async Task<IReadOnlyList<string>> GetDistinctCountryCodesAsync(int specCode, CancellationToken ct = default)
        => await _db.Occurrences
            .Where(o => o.SpecCode == specCode && o.CountryCode != null)
            .Select(o => o.CountryCode!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
}


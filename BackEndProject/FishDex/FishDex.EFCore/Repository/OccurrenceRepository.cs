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

    public async Task<IReadOnlyList<Occurrence>> GetAllWithCoordsAsync(int specCode, CancellationToken ct = default)
        => await _db.Occurrences
            .Where(o => o.SpecCode == specCode && o.LatitudeDec != 0 && o.LongitudeDec != 0)
            .OrderBy(o => o.CountryCode)
            .ToListAsync(ct);

    // Batch: 1 query cho nhiều loài — tránh N+1 khi build snapshot / render aquarium detail
    public async Task<IReadOnlyList<Occurrence>> GetAllWithCoordsAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default)
        => await _db.Occurrences
            .Where(o => specCodes.Contains(o.SpecCode) && o.LatitudeDec != 0 && o.LongitudeDec != 0)
            .OrderBy(o => o.CountryCode)
            .ToListAsync(ct);
}


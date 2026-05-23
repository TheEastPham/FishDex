using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class SpeciesRepository(FishDexDbContext context)
    : GenericRepository<Species>(context), ISpeciesRepository
{
    private readonly FishDexDbContext _db = context;

    public async Task<(IReadOnlyList<Species> Items, int TotalCount)> SearchWithCountAsync(
        string q, Guid? famId, int? genusCode, string? language,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Species
            .Include(s => s.Genus)
            .Include(s => s.Family)
            .Include(s => s.CommonNames)
            .Where(s =>
                EF.Functions.ILike(s.SpeciesName, $"%{q}%") ||
                s.CommonNames.Any(c =>
                    EF.Functions.ILike(c.ComName, $"%{q}%") &&
                    (language == null || c.Language == language)));

        if (famId.HasValue)    query = query.Where(s => s.FamId == famId.Value);
        if (genusCode.HasValue) query = query.Where(s => s.GenusCode == genusCode.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.SpeciesName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
